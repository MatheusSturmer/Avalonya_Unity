using UnityEngine;

namespace Avalonya
{
    /// <summary>Reproduz as duas faixas da partida e faz a transição após 80 abates.</summary>
    public sealed class AudioJogo : MonoBehaviour
    {
        private const int AbatesParaFaixaFrenetica = 80;
        private const float DuracaoTransicao = 2f;
        private const float VolumeInicialEmDecibeis = 0f;
        private const float VolumeFreneticoEmDecibeis = -10f;
        private const float VolumeMudoEmDecibeis = -80f;
        private const float VelocidadeTransicaoEmDecibeis = (VolumeInicialEmDecibeis - VolumeMudoEmDecibeis) / DuracaoTransicao;

        [SerializeField] private AudioSource musicaInicial;
        [SerializeField] private AudioSource musicaFrenetica;

        private EstadoJogo _estado;
        private float _volumeInicialAlvoDb = VolumeInicialEmDecibeis;
        private float _volumeFreneticoAlvoDb = VolumeMudoEmDecibeis;

        private void Start()
        {
            _estado = EstadoJogo.Instancia;
            PrepararFaixa(musicaInicial, VolumeInicialEmDecibeis);
            PrepararFaixa(musicaFrenetica, VolumeMudoEmDecibeis);
            if (musicaInicial != null && !musicaInicial.isPlaying)
            {
                musicaInicial.Play();
            }

            if (musicaFrenetica != null && !musicaFrenetica.isPlaying)
            {
                musicaFrenetica.Play();
            }

            if (_estado != null)
            {
                _estado.AbatesAlterados += AtualizarFaixa;
                AtualizarFaixa(_estado.AbatesTotais);
            }
        }

        private void Update()
        {
            AudioListener.pause = Mathf.Approximately(Time.timeScale, 0f);
            float passoDb = VelocidadeTransicaoEmDecibeis * Time.unscaledDeltaTime;
            if (musicaInicial != null)
            {
                musicaInicial.volume = AplicarTransicaoDb(musicaInicial.volume, _volumeInicialAlvoDb, passoDb);
            }

            if (musicaFrenetica != null)
            {
                musicaFrenetica.volume = AplicarTransicaoDb(musicaFrenetica.volume, _volumeFreneticoAlvoDb, passoDb);
            }
        }

        private void OnDestroy()
        {
            if (_estado != null)
            {
                _estado.AbatesAlterados -= AtualizarFaixa;
            }

            AudioListener.pause = false;
        }

        private void AtualizarFaixa(int abates)
        {
            bool usarFaixaFrenetica = abates >= AbatesParaFaixaFrenetica;
            _volumeInicialAlvoDb = usarFaixaFrenetica ? VolumeMudoEmDecibeis : VolumeInicialEmDecibeis;
            _volumeFreneticoAlvoDb = usarFaixaFrenetica ? VolumeFreneticoEmDecibeis : VolumeMudoEmDecibeis;
        }

        private static void PrepararFaixa(AudioSource faixa, float volumeDb)
        {
            if (faixa == null)
            {
                return;
            }

            faixa.loop = true;
            faixa.spatialBlend = 0f;
            faixa.playOnAwake = false;
            faixa.volume = DecibeisParaVolume(volumeDb);
        }

        private static float AplicarTransicaoDb(float volumeAtual, float volumeAlvoDb, float passoDb)
        {
            float volumeAtualDb = 20f * Mathf.Log10(Mathf.Max(volumeAtual, DecibeisParaVolume(VolumeMudoEmDecibeis)));
            float volumeNovoDb = Mathf.MoveTowards(volumeAtualDb, volumeAlvoDb, passoDb);
            return DecibeisParaVolume(volumeNovoDb);
        }

        private static float DecibeisParaVolume(float decibeis)
        {
            return Mathf.Pow(10f, decibeis / 20f);
        }
    }
}
