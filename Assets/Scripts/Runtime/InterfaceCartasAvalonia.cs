using UnityEngine;
using UnityEngine.UI;

namespace Avalonia
{
    public sealed class InterfaceCartasAvalonia : MonoBehaviour
    {
        [SerializeField] private GameObject painelCartas;
        [SerializeField] private Text tituloPainel;
        [SerializeField] private Text instrucaoPainel;
        [SerializeField] private VisualCartaAvalonia[] visuaisCartas;

        private EstadoJogoAvalonia _estado;
        private VidaJogadorAvalonia _vida;
        private bool _aberto;
        private float _escalaAnterior = 1f;

        private void Awake()
        {
            _estado = EstadoJogoAvalonia.Instancia;
            JogadorAvalonia jogador = FindAnyObjectByType<JogadorAvalonia>();
            _vida = jogador != null ? jogador.GetComponent<VidaJogadorAvalonia>() : null;
            if (painelCartas != null)
            {
                painelCartas.SetActive(false);
            }
        }

        private void Update()
        {
            if (_estado == null)
            {
                _estado = EstadoJogoAvalonia.Instancia;
            }

            if (_vida == null)
            {
                JogadorAvalonia jogador = FindAnyObjectByType<JogadorAvalonia>();
                _vida = jogador != null ? jogador.GetComponent<VidaJogadorAvalonia>() : null;
            }

            if (!_aberto && _estado != null && _estado.CartasPendentes > 0 && (_vida == null || !_vida.EstaMorto))
            {
                AbrirCartas();
            }
        }

        private void OnDestroy()
        {
            if (_aberto)
            {
                Time.timeScale = _escalaAnterior;
            }
        }

        private void AbrirCartas()
        {
            _estado.SortearCartas();
            if (painelCartas == null || tituloPainel == null || instrucaoPainel == null
                || visuaisCartas == null || visuaisCartas.Length == 0)
            {
                Debug.LogError("A interface de cartas não está ligada aos elementos da cena.", this);
                return;
            }

            if (_estado.OpcoesCartas.Count == 0)
            {
                Debug.LogError("Não há cartas elegíveis para mostrar.", this);
                return;
            }

            _escalaAnterior = Time.timeScale;
            _aberto = true;
            _estado.DefinirEscolhendoCarta(true);
            Time.timeScale = 0f;
            painelCartas.SetActive(true);
            AtualizarVisuais();
            if (tituloPainel != null)
            {
                tituloPainel.text = "ESCOLHA UMA CARTA";
            }

            if (instrucaoPainel != null)
            {
                instrucaoPainel.text = "Escolha uma melhoria para continuar";
            }
        }

        private void AtualizarVisuais()
        {
            for (int indice = 0; indice < visuaisCartas.Length; indice++)
            {
                bool temCarta = indice < _estado.OpcoesCartas.Count;
                visuaisCartas[indice].gameObject.SetActive(temCarta);
                if (temCarta)
                {
                    visuaisCartas[indice].Configurar(_estado.OpcoesCartas[indice], SelecionarCarta);
                }
            }
        }

        private void SelecionarCarta(string id)
        {
            if (!_aberto || !_estado.SelecionarCarta(id))
            {
                return;
            }

            if (_estado.CartasPendentes > 0)
            {
                AtualizarVisuais();
                return;
            }

            _estado.DefinirEscolhendoCarta(false);
            painelCartas.SetActive(false);
            _aberto = false;
            Time.timeScale = _escalaAnterior;
        }
    }
}
