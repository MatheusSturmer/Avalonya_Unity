using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Avalonia
{
    /// <summary>Controla pausa por Esc, derrota, reinício e retorno ao menu.</summary>
    public sealed class FluxoPartidaAvalonia : MonoBehaviour
    {
        [SerializeField] private GameObject painelFluxo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoDica;
        [SerializeField] private Button botaoContinuar;
        [SerializeField] private Button botaoReiniciar;
        [SerializeField] private Button botaoMenu;
        [SerializeField] private Button botaoSair;

        private EstadoJogoAvalonia _estado;
        private VidaJogadorAvalonia _vida;
        private bool _aberto;
        private bool _fimDeJogo;

        private void Awake()
        {
            _estado = EstadoJogoAvalonia.Instancia;
            if (painelFluxo != null)
            {
                painelFluxo.SetActive(false);
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Start()
        {
            ObterEstado();
            JogadorAvalonia jogador = FindAnyObjectByType<JogadorAvalonia>();
            _vida = jogador != null ? jogador.GetComponent<VidaJogadorAvalonia>() : null;
            if (_vida == null)
            {
                Debug.LogError("FluxoPartidaAvalonia precisa encontrar VidaJogadorAvalonia.", this);
                return;
            }

            _vida.Morreu += AoMorrer;
            botaoContinuar?.onClick.AddListener(Continuar);
            botaoReiniciar?.onClick.AddListener(Reiniciar);
            botaoMenu?.onClick.AddListener(VoltarAoMenu);
            botaoSair?.onClick.AddListener(Sair);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (_fimDeJogo)
            {
                return;
            }

            if (_aberto)
            {
                Continuar();
            }
            else
            {
                AbrirPausa();
            }
        }

        private void OnDestroy()
        {
            if (_vida != null)
            {
                _vida.Morreu -= AoMorrer;
            }

            AudioListener.pause = false;
            Time.timeScale = 1f;
        }

        public void AbrirPausa()
        {
            if (_aberto || _fimDeJogo || _vida == null || _vida.EstaMorto)
            {
                return;
            }

            Exibir("JOGO PAUSADO", true);
        }

        public void Continuar()
        {
            if (!_aberto || _fimDeJogo)
            {
                return;
            }

            _aberto = false;
            painelFluxo.SetActive(false);
            EstadoJogoAvalonia estado = ObterEstado();
            bool cartasPausando = estado != null && (estado.EscolhendoCarta || estado.CartasPendentes > 0);
            Time.timeScale = cartasPausando ? 0f : 1f;
            AudioListener.pause = cartasPausando;
        }

        public void Reiniciar()
        {
            EncerrarPausaDeAudio();
            ObterEstado()?.NovaPartida();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void VoltarAoMenu()
        {
            EncerrarPausaDeAudio();
            ObterEstado()?.NovaPartida();
            SceneManager.LoadScene("MenuAvalonya");
        }

        public void Sair()
        {
            EncerrarPausaDeAudio();
            Application.Quit();
        }

        private void AoMorrer()
        {
            if (!_fimDeJogo)
            {
                Exibir("FIM DA PARTIDA", false);
            }
        }

        private void Exibir(string titulo, bool permitirContinuar)
        {
            if (painelFluxo == null || textoTitulo == null)
            {
                Debug.LogError("O painel de pausa e fim de jogo não foi configurado.", this);
                return;
            }

            _aberto = true;
            _fimDeJogo = !permitirContinuar;
            textoTitulo.text = titulo;
            if (textoDica != null)
            {
                textoDica.text = permitirContinuar ? "ESC  •  PAUSAR / CONTINUAR" : "REINICIE OU VOLTE AO MENU";
            }

            botaoContinuar.gameObject.SetActive(permitirContinuar);
            painelFluxo.SetActive(true);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (permitirContinuar)
            {
                botaoContinuar.Select();
            }
            else
            {
                botaoReiniciar.Select();
            }
        }

        private static void EncerrarPausaDeAudio()
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
        }

        private EstadoJogoAvalonia ObterEstado()
        {
            if (_estado == null)
            {
                _estado = EstadoJogoAvalonia.Instancia;
            }

            return _estado;
        }
    }
}
