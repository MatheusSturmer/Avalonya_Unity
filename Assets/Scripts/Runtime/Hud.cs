using UnityEngine;
using UnityEngine.UI;

namespace Avalonya
{
    /// <summary>Exibe vida, dano, XP, tempo e contadores da partida.</summary>
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] private Image preenchimentoVida;
        [SerializeField] private Image preenchimentoXp;
        [SerializeField] private Text textoVida;
        [SerializeField] private Text textoDano;
        [SerializeField] private Text textoXp;
        [SerializeField] private Text textoTempo;
        [SerializeField] private Text textoAbates;
        [SerializeField] private Text textoInimigos;

        private VidaJogador _vida;
        private AtaqueBasico _ataque;
        private EstadoJogo _estado;
        private SpawnerSlime _spawnerSlime;
        private SpawnerAranha _spawnerAranha;
        private float _tempoPartida;

        private void Start()
        {
            Jogador jogador = FindAnyObjectByType<Jogador>();
            _vida = jogador != null ? jogador.GetComponent<VidaJogador>() : null;
            _ataque = jogador != null ? jogador.GetComponent<AtaqueBasico>() : null;
            _estado = EstadoJogo.Instancia;
            _spawnerSlime = FindAnyObjectByType<SpawnerSlime>();
            _spawnerAranha = FindAnyObjectByType<SpawnerAranha>();

            if (_vida == null || _estado == null)
            {
                Debug.LogError("HUD precisa de VidaJogador e EstadoJogo.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            _tempoPartida += Time.deltaTime;
            AtualizarVida();
            AtualizarExperiencia();
            AtualizarPartida();
        }

        private void AtualizarVida()
        {
            float maximo = Mathf.Max(1f, _vida.VidaMaxima);
            float valor = Mathf.Clamp(_vida.VidaAtual, 0f, maximo);
            preenchimentoVida.fillAmount = valor / maximo;
            textoVida.text = $"{valor:0} / {maximo:0}";
            textoDano.text = $"DANO  {(_ataque != null ? _ataque.Dano : 0f):0}";
        }

        private void AtualizarExperiencia()
        {
            int necessario = Mathf.Max(1, _estado.XpNecessario);
            preenchimentoXp.fillAmount = Mathf.Clamp01((float)_estado.XpAtual / necessario);
            textoXp.text = $"{_estado.XpAtual} / {necessario}";
        }

        private void AtualizarPartida()
        {
            float tempoAtual = _spawnerSlime != null ? _spawnerSlime.TempoPartida : _tempoPartida;
            int segundos = Mathf.FloorToInt(tempoAtual);
            int etapa = _spawnerSlime != null ? _spawnerSlime.Etapa : 1;
            textoTempo.text = $"{segundos / 60:00}:{segundos % 60:00}  |  ETAPA {etapa}";

            int slimesVivos = _spawnerSlime != null ? _spawnerSlime.SlimesVivos : 0;
            int limiteSlimes = _spawnerSlime != null ? _spawnerSlime.LimiteAtual : 0;
            int aranhasVivas = _spawnerAranha != null ? _spawnerAranha.AranhasVivas : 0;
            int limiteAranhas = _spawnerAranha != null ? _spawnerAranha.LimiteAtual : 0;
            textoInimigos.text = $"SLIMES  {slimesVivos}/{limiteSlimes}  |  ARANHAS  {aranhasVivas}/{limiteAranhas}";
            textoAbates.text = $"ABATES  {_estado.AbatesTotais}  |  ESC: PAUSA";
        }
    }
}
