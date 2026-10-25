using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Avalonia
{
    /// <summary>Menu inicial, opções de áudio/tela e créditos da versão Unity.</summary>
    public sealed class MenuPrincipalAvalonia : MonoBehaviour
    {
        private const string ChaveVolume = "Avalonia.Volume";
        private const string ChaveTelaCheia = "Avalonia.TelaCheia";
        private const float VolumeMenuEmDecibeis = -50.595f;

        [SerializeField] private GameObject painelPrincipal;
        [SerializeField] private GameObject painelOpcoes;
        [SerializeField] private GameObject painelCreditos;
        [SerializeField] private Button botaoJogar;
        [SerializeField] private Button botaoOpcoes;
        [SerializeField] private Button botaoCreditos;
        [SerializeField] private Button botaoSair;
        [SerializeField] private Button botaoVoltarOpcoes;
        [SerializeField] private Button botaoVoltarCreditos;
        [SerializeField] private Slider controleVolume;
        [SerializeField] private Toggle controleTelaCheia;
        [SerializeField] private AudioSource musicaMenu;

        private void OnEnable()
        {
            ControleEfeitoCrtAvalonia.DefinirAtivo(true);
        }

        private void OnDestroy()
        {
            ControleEfeitoCrtAvalonia.DefinirAtivo(false);
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            float volume = PlayerPrefs.GetFloat(ChaveVolume, 0.8f);
            bool telaCheia = PlayerPrefs.GetInt(ChaveTelaCheia, 0) == 1;
            AplicarVolume(volume);
            AplicarTelaCheia(telaCheia);
            if (controleVolume != null)
            {
                controleVolume.SetValueWithoutNotify(volume);
            }

            if (controleTelaCheia != null)
            {
                controleTelaCheia.SetIsOnWithoutNotify(telaCheia);
            }

            ExibirPainel(painelPrincipal);
            if (musicaMenu != null)
            {
                musicaMenu.loop = true;
                musicaMenu.spatialBlend = 0f;
                musicaMenu.volume = Mathf.Pow(10f, VolumeMenuEmDecibeis / 20f);
                if (!musicaMenu.isPlaying)
                {
                    musicaMenu.Play();
                }
            }
        }

        private void Start()
        {
            botaoJogar?.onClick.AddListener(IniciarJogo);
            botaoOpcoes?.onClick.AddListener(AbrirOpcoes);
            botaoCreditos?.onClick.AddListener(AbrirCreditos);
            botaoSair?.onClick.AddListener(Sair);
            botaoVoltarOpcoes?.onClick.AddListener(VoltarAoMenu);
            botaoVoltarCreditos?.onClick.AddListener(VoltarAoMenu);
            if (controleVolume != null)
            {
                controleVolume.onValueChanged.AddListener(DefinirVolume);
            }

            if (controleTelaCheia != null)
            {
                controleTelaCheia.onValueChanged.AddListener(DefinirTelaCheia);
            }

            botaoJogar?.Select();
        }

        public void IniciarJogo()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene("AvalonyaWorld");
        }

        public void AbrirOpcoes()
        {
            ExibirPainel(painelOpcoes);
        }

        public void AbrirCreditos()
        {
            ExibirPainel(painelCreditos);
        }

        public void VoltarAoMenu()
        {
            ExibirPainel(painelPrincipal);
        }

        public void DefinirVolume(float volume)
        {
            AplicarVolume(volume);
            PlayerPrefs.SetFloat(ChaveVolume, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        public void DefinirTelaCheia(bool telaCheia)
        {
            AplicarTelaCheia(telaCheia);
            PlayerPrefs.SetInt(ChaveTelaCheia, telaCheia ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Sair()
        {
            Application.Quit();
        }

        private static void AplicarVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        private static void AplicarTelaCheia(bool telaCheia)
        {
            Screen.fullScreenMode = telaCheia ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        private void ExibirPainel(GameObject painelAtivo)
        {
            if (painelPrincipal != null)
            {
                painelPrincipal.SetActive(painelPrincipal == painelAtivo);
            }

            if (painelOpcoes != null)
            {
                painelOpcoes.SetActive(painelOpcoes == painelAtivo);
            }

            if (painelCreditos != null)
            {
                painelCreditos.SetActive(painelCreditos == painelAtivo);
            }
        }
    }
}
