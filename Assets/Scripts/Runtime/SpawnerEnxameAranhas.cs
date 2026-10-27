using System.Collections.Generic;
using UnityEngine;

namespace Avalonya
{
    /// <summary>Cria ondas de aranhas pequenas em torno do jogador.</summary>
    public sealed class SpawnerEnxameAranhas : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Transform jogador;
        [SerializeField] private InimigoAranha prefabAranhaPequena;

        [Header("Ritmo das ondas")]
        [SerializeField, Min(0f)] private float atrasoInicial = 30f;
        [SerializeField, Min(0f)] private float aleatoriedadePrimeiraOnda = 30f;
        [SerializeField, Min(0.1f)] private float intervaloOndas = 10f;
        [SerializeField, Min(1)] private int tamanhoOndaBase = 12;
        [SerializeField, Min(0)] private int tamanhoOndaPorCarta = 4;
        [SerializeField, Min(1)] private int tamanhoOndaMaximo = 30;
        [SerializeField, Range(1, 200)] private int maximoAbsoluto = 200;

        [Header("Atributos do enxame")]
        [SerializeField, Min(0f)] private float velocidadeMobs = 8f;
        [SerializeField, Min(0f)] private float danoToque = 5f;
        [SerializeField, Min(0f)] private float raioMinimo = 15f;
        [SerializeField, Min(0f)] private float raioMaximo = 30f;
        [SerializeField, Min(0f)] private float dispersao = 4f;

        private readonly List<InimigoAranha> _aranhasCriadas = new List<InimigoAranha>();
        private VidaJogador _vidaJogador;
        private float _tempoAteOnda;

        public int AranhasPequenasVivas
        {
            get
            {
                RemoverAranhasDestruidas();
                int total = 0;
                foreach (InimigoAranha aranha in _aranhasCriadas)
                {
                    if (aranha != null && !aranha.EstaMorto)
                    {
                        total++;
                    }
                }

                return total;
            }
        }

        public int TamanhoOndaAtual
        {
            get
            {
                int cartas = EstadoJogo.Instancia != null
                    ? EstadoJogo.Instancia.CartasSelecionadas
                    : 0;
                return Mathf.Clamp(
                    tamanhoOndaBase + cartas * tamanhoOndaPorCarta,
                    tamanhoOndaBase,
                    tamanhoOndaMaximo
                );
            }
        }

        public void Configurar(Transform alvo, InimigoAranha prefab)
        {
            jogador = alvo;
            prefabAranhaPequena = prefab;
        }

        private void Awake()
        {
            if (jogador == null)
            {
                Jogador encontrado = FindAnyObjectByType<Jogador>();
                jogador = encontrado != null ? encontrado.transform : null;
            }

            _tempoAteOnda = Mathf.Max(0f, atrasoInicial)
                + Random.Range(0f, Mathf.Max(0f, aleatoriedadePrimeiraOnda));

            if (jogador == null || prefabAranhaPequena == null)
            {
                Debug.LogError("O enxame de aranhas precisa do jogador e do prefab pequeno.", this);
                enabled = false;
                return;
            }

            _vidaJogador = jogador.GetComponent<VidaJogador>();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || jogador == null || prefabAranhaPequena == null
                || (_vidaJogador != null && _vidaJogador.EstaMorto))
            {
                return;
            }

            _tempoAteOnda -= Time.deltaTime;
            if (_tempoAteOnda > 0f)
            {
                return;
            }

            DispararOndaAgora();
            _tempoAteOnda = Mathf.Max(0.1f, intervaloOndas);
        }

        public void DispararOndaAgora()
        {
            if (jogador == null || prefabAranhaPequena == null
                || (_vidaJogador != null && _vidaJogador.EstaMorto))
            {
                return;
            }

            float angulo = Random.Range(0f, Mathf.PI * 2f);
            float raio = Random.Range(raioMinimo, raioMaximo);
            Vector3 centro = jogador.position + new Vector3(Mathf.Cos(angulo) * raio, 0f, Mathf.Sin(angulo) * raio);
            int quantidade = TamanhoOndaAtual;

            for (int indice = 0; indice < quantidade; indice++)
            {
                if (AranhasPequenasVivas >= maximoAbsoluto)
                {
                    break;
                }

                Vector3 espalhamento = new Vector3(
                    Random.Range(-dispersao, dispersao),
                    0f,
                    Random.Range(-dispersao, dispersao)
                );
                InimigoAranha aranha = Instantiate(
                    prefabAranhaPequena,
                    centro + espalhamento,
                    Quaternion.identity,
                    transform.root
                );
                aranha.ConfigurarAlvo(jogador);
                aranha.ConfigurarValoresEnxame(velocidadeMobs, danoToque);
                _aranhasCriadas.Add(aranha);
            }
        }

        private void RemoverAranhasDestruidas()
        {
            for (int indice = _aranhasCriadas.Count - 1; indice >= 0; indice--)
            {
                if (_aranhasCriadas[indice] == null)
                {
                    _aranhasCriadas.RemoveAt(indice);
                }
            }
        }
    }
}
