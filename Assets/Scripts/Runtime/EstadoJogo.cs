using System;
using System.Collections.Generic;
using UnityEngine;

namespace Avalonya
{
    /// <summary>Dados de partida e regras de progressão compartilhados pelo jogo.</summary>
    public sealed class EstadoJogo : MonoBehaviour
    {
        private const float VidaInicial = 12f;
        private const float DanoInicial = 2f;
        private const float DanoMachadoInicial = 1f;
        private const float IntervaloMachadoInicial = 8f;
        private const float VelocidadeMachadoInicial = 12f;
        private const int QuantidadeMachadosInicial = 1;
        private const int QuantidadeMachadosMaxima = 3;

        private static EstadoJogo _instancia;

        [SerializeField] private JoiaXp prefabJoiaXp;
        [SerializeField, Min(1)] private int xpBase = 10;
        [SerializeField, Min(0)] private int xpPorNivel = 10;
        [SerializeField] private CartaProgressao[] cartas = Array.Empty<CartaProgressao>();

        private readonly List<CartaProgressao> _opcoesCartas = new List<CartaProgressao>(3);
        private readonly List<string> _cartasSelecionadas = new List<string>();

        public static EstadoJogo Instancia => _instancia;

        public event Action StatusAlterado;
        public event Action<int> AbatesAlterados;

        public int AbatesTotais { get; private set; }
        public int XpTotal { get; private set; }
        public int XpAtual { get; private set; }
        public int CartasPendentes { get; private set; }
        public int CartasSelecionadas => _cartasSelecionadas.Count;
        public int XpNecessario => xpBase + CartasSelecionadas * xpPorNivel;
        public bool EscolhendoCarta { get; private set; }
        public IReadOnlyList<CartaProgressao> OpcoesCartas => _opcoesCartas;

        public float DanoJogador { get; private set; } = DanoInicial;
        public float VidaMaxima { get; private set; } = VidaInicial;
        public float MultiplicadorMovimento { get; private set; } = 1f;
        public float MultiplicadorAtaque { get; private set; } = 1f;
        public float DanoMachado { get; private set; } = DanoMachadoInicial;
        public float DanoExtraMachado { get; private set; }
        public float VelocidadeMachado { get; private set; } = VelocidadeMachadoInicial;
        public float IntervaloMachado { get; private set; } = IntervaloMachadoInicial;
        public int QuantidadeMachados { get; private set; } = QuantidadeMachadosInicial;
        public bool MachadoDesbloqueado { get; private set; }

        private void Awake()
        {
            if (_instancia != null && _instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            _instancia = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instancia == this)
            {
                _instancia = null;
            }
        }

        public void Configurar(JoiaXp prefab)
        {
            prefabJoiaXp = prefab;
        }

        public void RegistrarMorte(Transform jogador, Vector3 posicao, int xpDoAbate)
        {
            AbatesTotais++;
            AbatesAlterados?.Invoke(AbatesTotais);

            if (jogador == null || xpDoAbate <= 0)
            {
                StatusAlterado?.Invoke();
                return;
            }

            VidaJogador vida = jogador.GetComponent<VidaJogador>();
            if (vida != null && vida.EstaMorto)
            {
                StatusAlterado?.Invoke();
                return;
            }

            if (prefabJoiaXp == null)
            {
                Debug.LogError("EstadoJogo precisa do prefab de joia de XP.", this);
                StatusAlterado?.Invoke();
                return;
            }

            float anguloInicial = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            for (int indice = 0; indice < xpDoAbate; indice++)
            {
                float angulo = anguloInicial + Mathf.PI * 2f * indice / xpDoAbate;
                Vector3 deslocamento = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * 0.35f;
                JoiaXp joia = Instantiate(prefabJoiaXp, posicao + Vector3.up * 0.28f, Quaternion.identity);
                joia.Configurar(jogador, deslocamento);
            }

            StatusAlterado?.Invoke();
        }

        public void AdicionarXp(int quantidade, Transform jogador)
        {
            if (quantidade <= 0 || jogador == null)
            {
                return;
            }

            VidaJogador vida = jogador.GetComponent<VidaJogador>();
            if (vida != null && vida.EstaMorto)
            {
                return;
            }

            XpTotal += quantidade;
            XpAtual += quantidade;
            while (XpAtual >= XpNecessario)
            {
                XpAtual -= XpNecessario;
                CartasPendentes++;
            }

            StatusAlterado?.Invoke();
        }

        public void SortearCartas()
        {
            _opcoesCartas.Clear();
            if (cartas == null || cartas.Length == 0)
            {
                Debug.LogError("EstadoJogo não tem cartas configuradas.", this);
                return;
            }

            List<CartaProgressao> elegiveis = new List<CartaProgressao>();
            foreach (CartaProgressao carta in cartas)
            {
                if (carta == null || string.IsNullOrWhiteSpace(carta.id) || carta.imagem == null || !CartaDisponivel(carta.id))
                {
                    continue;
                }

                elegiveis.Add(carta);
            }

            for (int indice = elegiveis.Count - 1; indice > 0; indice--)
            {
                int outro = UnityEngine.Random.Range(0, indice + 1);
                CartaProgressao temporaria = elegiveis[indice];
                elegiveis[indice] = elegiveis[outro];
                elegiveis[outro] = temporaria;
            }

            int quantidade = Mathf.Min(3, elegiveis.Count);
            for (int indice = 0; indice < quantidade; indice++)
            {
                _opcoesCartas.Add(elegiveis[indice]);
            }
        }

        public bool SelecionarCarta(string id)
        {
            if (!EscolhendoCarta || CartasPendentes <= 0 || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            bool opcaoAtual = false;
            foreach (CartaProgressao carta in _opcoesCartas)
            {
                if (carta != null && carta.id == id)
                {
                    opcaoAtual = true;
                    break;
                }
            }

            if (!opcaoAtual || !AplicarCarta(id))
            {
                return false;
            }

            _cartasSelecionadas.Add(id);
            CartasPendentes--;
            EscolhendoCarta = CartasPendentes > 0;
            if (EscolhendoCarta)
            {
                SortearCartas();
            }

            StatusAlterado?.Invoke();
            return true;
        }

        public void DefinirEscolhendoCarta(bool escolhendo)
        {
            EscolhendoCarta = escolhendo && CartasPendentes > 0;
        }

        public void NovaPartida()
        {
            AbatesTotais = 0;
            XpTotal = 0;
            XpAtual = 0;
            CartasPendentes = 0;
            EscolhendoCarta = false;
            _opcoesCartas.Clear();
            _cartasSelecionadas.Clear();

            DanoJogador = DanoInicial;
            VidaMaxima = VidaInicial;
            MultiplicadorMovimento = 1f;
            MultiplicadorAtaque = 1f;
            DanoMachado = DanoMachadoInicial;
            DanoExtraMachado = 0f;
            VelocidadeMachado = VelocidadeMachadoInicial;
            IntervaloMachado = IntervaloMachadoInicial;
            QuantidadeMachados = QuantidadeMachadosInicial;
            MachadoDesbloqueado = false;

            AplicarMelhoriasAoJogador();
            AbatesAlterados?.Invoke(AbatesTotais);
            StatusAlterado?.Invoke();
        }

        private bool AplicarCarta(string id)
        {
            Jogador jogador = FindAnyObjectByType<Jogador>();
            VidaJogador vida = jogador != null ? jogador.GetComponent<VidaJogador>() : null;
            AtaqueBasico ataque = jogador != null ? jogador.GetComponent<AtaqueBasico>() : null;

            switch (id)
            {
                case "forca":
                    DanoJogador += 1f;
                    ataque?.DefinirDano(DanoJogador);
                    break;
                case "vitalidade":
                    VidaMaxima += 2f;
                    vida?.AumentarVidaMaxima(2f, 2f);
                    break;
                case "cura":
                    vida?.CurarCompletamente();
                    break;
                case "acelerar":
                    MultiplicadorMovimento += 0.1f;
                    jogador?.DefinirMultiplicadorVelocidade(MultiplicadorMovimento);
                    break;
                case "destreza":
                    MultiplicadorAtaque += 0.2f;
                    ataque?.DefinirMultiplicadorVelocidade(MultiplicadorAtaque);
                    break;
                case "machado":
                    MachadoDesbloqueado = true;
                    break;
                case "machado_dano":
                    DanoExtraMachado += 1f;
                    break;
                case "machado_velocidade":
                    VelocidadeMachado *= 1.2f;
                    break;
                case "machado_intervalo":
                    IntervaloMachado = Mathf.Max(2f, IntervaloMachado - 2f);
                    break;
                case "machado_quantidade":
                    QuantidadeMachados = Mathf.Min(QuantidadeMachados + 1, QuantidadeMachadosMaxima);
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool CartaDisponivel(string id)
        {
            if (id == "machado" && MachadoDesbloqueado)
            {
                return false;
            }

            if (id == "machado_quantidade" && QuantidadeMachados >= QuantidadeMachadosMaxima)
            {
                return false;
            }

            bool cartaMachado = id == "machado_dano" || id == "machado_velocidade"
                || id == "machado_intervalo" || id == "machado_quantidade";
            return !cartaMachado || MachadoDesbloqueado;
        }

        private void AplicarMelhoriasAoJogador()
        {
            Jogador jogador = FindAnyObjectByType<Jogador>();
            if (jogador == null)
            {
                return;
            }

            jogador.DefinirMultiplicadorVelocidade(MultiplicadorMovimento);
            AtaqueBasico ataque = jogador.GetComponent<AtaqueBasico>();
            ataque?.DefinirDano(DanoJogador);
            ataque?.DefinirMultiplicadorVelocidade(MultiplicadorAtaque);

            VidaJogador vida = jogador.GetComponent<VidaJogador>();
            vida?.DefinirVidaMaxima(VidaMaxima, curar: true);
        }
    }
}
