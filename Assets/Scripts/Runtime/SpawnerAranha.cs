using System.Collections.Generic;
using UnityEngine;

namespace Avalonya
{
    public sealed class SpawnerAranha : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InimigoAranha prefabAranha;
        [SerializeField] private Transform jogador;
        private VidaJogador _vidaJogador;

        [Header("Raio de surgimento")]
        [SerializeField, Min(0f)] private float raioMinimo = 15f;
        [SerializeField, Min(0f)] private float raioMaximo = 30f;

        [Header("Ritmo da partida")]
        [SerializeField, Min(0f)] private float atrasoInicial = 3f;
        [SerializeField] private float[] inicioEtapas = { 0f, 30f, 60f, 120f, 180f };
        [SerializeField] private float[] intervalos = { 3f, 2f, 1.5f, 1f, 0.75f };
        [SerializeField] private int[] limites = { 5, 10, 15, 20, 30 };
        [SerializeField, Range(1, 200)] private int maximoAbsoluto = 30;

        private readonly List<InimigoAranha> _aranhas = new List<InimigoAranha>();
        private float _tempoPartida;
        private float _tempoAteSpawn;
        private int _etapa;
        private int _limiteAtual = 5;

        public int AranhasVivas
        {
            get
            {
                RemoverAranhasDestruidas();
                int vivas = 0;
                foreach (InimigoAranha aranha in _aranhas)
                {
                    if (aranha != null && !aranha.EstaMorto)
                    {
                        vivas++;
                    }
                }

                return vivas;
            }
        }

        public int LimiteAtual => _limiteAtual;

        public void Configurar(Transform alvo, InimigoAranha prefab)
        {
            jogador = alvo;
            prefabAranha = prefab;
        }

        private void Awake()
        {
            if (jogador == null)
            {
                Jogador encontrado = FindAnyObjectByType<Jogador>();
                jogador = encontrado != null ? encontrado.transform : null;
            }

            _tempoAteSpawn = Mathf.Max(0f, atrasoInicial);
            AtualizarEtapa();
            if (jogador == null || prefabAranha == null)
            {
                Debug.LogError("Spawner de aranha precisa de jogador e prefab configurados.", this);
                enabled = false;
                return;
            }

            _vidaJogador = jogador.GetComponent<VidaJogador>();
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f || jogador == null || prefabAranha == null
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
                TentarCriarAranha();
            }
        }

        private void AtualizarEtapa()
        {
            int quantidadeEtapas = Mathf.Min(inicioEtapas.Length, Mathf.Min(intervalos.Length, limites.Length));
            if (quantidadeEtapas == 0)
            {
                _etapa = 0;
                _limiteAtual = Mathf.Min(5, maximoAbsoluto);
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

        private void TentarCriarAranha()
        {
            if (AranhasVivas >= _limiteAtual)
            {
                return;
            }

            float angulo = Random.Range(0f, Mathf.PI * 2f);
            float raio = Random.Range(raioMinimo, raioMaximo);
            Vector3 deslocamento = new Vector3(Mathf.Cos(angulo) * raio, 0f, Mathf.Sin(angulo) * raio);
            Vector3 posicao = jogador.position + deslocamento;
            InimigoAranha aranha = Instantiate(
                prefabAranha,
                posicao,
                Quaternion.identity,
                transform.root
            );
            aranha.ConfigurarAlvo(jogador);
            _aranhas.Add(aranha);
        }

        private void RemoverAranhasDestruidas()
        {
            for (int indice = _aranhas.Count - 1; indice >= 0; indice--)
            {
                if (_aranhas[indice] == null)
                {
                    _aranhas.RemoveAt(indice);
                }
            }
        }
    }
}
