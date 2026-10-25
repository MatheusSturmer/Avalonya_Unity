using System.Collections.Generic;
using UnityEngine;

namespace Avalonia
{
    public sealed class SpawnerSlimeAvalonia : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InimigoSlimeAvalonia prefabSlime;
        [SerializeField] private Transform jogador;
        private VidaJogadorAvalonia _vidaJogador;

        [Header("Raio de surgimento")]
        [SerializeField] private float raioMinimo = 15f;
        [SerializeField] private float raioMaximo = 30f;

        [Header("Ritmo da partida")]
        [SerializeField] private float atrasoInicial = 1f;
        [SerializeField] private float[] inicioEtapas = { 0f, 30f, 60f, 120f, 180f };
        [SerializeField] private float[] intervalos = { 1f, 0.6f, 0.4f, 0.25f, 0.15f };
        [SerializeField] private int[] limites = { 15, 30, 50, 80, 120 };
        [SerializeField, Range(1, 200)] private int maximoAbsoluto = 120;

        private readonly List<InimigoSlimeAvalonia> _slimes = new List<InimigoSlimeAvalonia>();
        private float _tempoPartida;
        private float _tempoAteSpawn;
        private int _etapa;
        private int _limiteAtual = 15;

        public int SlimesVivos
        {
            get
            {
                RemoverSlimesDestruidos();
                return _slimes.Count;
            }
        }

        public float TempoPartida => _tempoPartida;
        public int Etapa => _etapa + 1;
        public int LimiteAtual => _limiteAtual;

        public void Configurar(Transform alvo, InimigoSlimeAvalonia prefab)
        {
            jogador = alvo;
            prefabSlime = prefab;
        }

        private void Awake()
        {
            if (jogador == null)
            {
                JogadorAvalonia encontrado = FindAnyObjectByType<JogadorAvalonia>();
                if (encontrado != null)
                {
                    jogador = encontrado.transform;
                }
            }

            _tempoAteSpawn = Mathf.Max(0f, atrasoInicial);
            AtualizarEtapa();
            if (jogador == null || prefabSlime == null)
            {
                Debug.LogError("Spawner de slime precisa de jogador e prefab configurados.", this);
                enabled = false;
                return;
            }

            _vidaJogador = jogador.GetComponent<VidaJogadorAvalonia>();
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f || jogador == null || prefabSlime == null
                || (_vidaJogador != null && _vidaJogador.EstaMorto))
            {
                return;
            }

            _tempoPartida += delta;
            AtualizarEtapa();
            _tempoAteSpawn -= delta;
            if (_tempoAteSpawn <= 0f)
            {
                _tempoAteSpawn = intervalos[_etapa];
                TentarCriarSlime();
            }
        }

        private void AtualizarEtapa()
        {
            int quantidadeEtapas = Mathf.Min(inicioEtapas.Length, Mathf.Min(intervalos.Length, limites.Length));
            if (quantidadeEtapas == 0)
            {
                _etapa = 0;
                _limiteAtual = Mathf.Min(15, maximoAbsoluto);
                return;
            }

            int anterior = _etapa;
            _etapa = 0;
            for (int indice = 0; indice < quantidadeEtapas; indice++)
            {
                if (_tempoPartida >= inicioEtapas[indice])
                {
                    _etapa = indice;
                }
            }

            _limiteAtual = Mathf.Clamp(limites[_etapa], 1, maximoAbsoluto);
            if (_etapa != anterior)
            {
                _tempoAteSpawn = Mathf.Min(_tempoAteSpawn, intervalos[_etapa]);
            }
        }

        private void TentarCriarSlime()
        {
            RemoverSlimesDestruidos();
            if (_slimes.Count >= _limiteAtual)
            {
                return;
            }

            float angulo = Random.Range(0f, Mathf.PI * 2f);
            float raio = Random.Range(raioMinimo, raioMaximo);
            Vector3 deslocamento = new Vector3(Mathf.Cos(angulo) * raio, 0f, Mathf.Sin(angulo) * raio);
            Vector3 posicao = jogador.position + deslocamento;
            InimigoSlimeAvalonia slime = Instantiate(
                prefabSlime,
                posicao,
                Quaternion.identity,
                transform.root
            );
            slime.ConfigurarAlvo(jogador);
            _slimes.Add(slime);
        }

        private void RemoverSlimesDestruidos()
        {
            for (int indice = _slimes.Count - 1; indice >= 0; indice--)
            {
                if (_slimes[indice] == null)
                {
                    _slimes.RemoveAt(indice);
                }
            }
        }
    }
}
