using UnityEngine;

namespace Avalonya
{
    /// <summary>Joia que salta, espera, é atraída e entrega um ponto de XP.</summary>
    public sealed class JoiaXp : MonoBehaviour
    {
        private const float TempoSalto = 0.3f;
        private const float AlturaSalto = 0.45f;
        private const float RaioAtracao = 6f;
        private const float VelocidadeInicial = 4f;
        private const float Aceleracao = 24f;
        private const float VelocidadeMaxima = 18f;
        private const float TempoBrilho = 0.28f;

        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Light brilho;

        private Transform _jogador;
        private Vector3 _posicaoInicial;
        private Vector3 _posicaoNoChao;
        private float _tempo;
        private float _tempoColeta;
        private float _velocidade;
        private bool _preparada;
        private bool _voando;
        private bool _coletada;

        public bool Preparada => _preparada;
        public bool Voando => _voando;
        public bool Coletada => _coletada;

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (brilho == null)
            {
                brilho = GetComponentInChildren<Light>();
            }

            if (visual == null)
            {
                Debug.LogError("Joia de XP precisa de SpriteRenderer.", this);
                enabled = false;
                return;
            }

            if (brilho != null)
            {
                brilho.enabled = false;
            }
        }

        public void Configurar(Transform jogador, Vector3 deslocamento)
        {
            _jogador = jogador;
            _posicaoInicial = transform.position;
            _posicaoNoChao = _posicaoInicial + deslocamento;
            _velocidade = VelocidadeInicial;
            _preparada = true;
            transform.localScale = Vector3.one * 0.6f;
        }

        private void Update()
        {
            if (!_preparada || _jogador == null)
            {
                return;
            }

            VidaJogador vida = _jogador.GetComponent<VidaJogador>();
            if (vida != null && vida.EstaMorto)
            {
                return;
            }

            float delta = Time.deltaTime;
            if (delta <= 0f)
            {
                return;
            }

            if (_coletada)
            {
                AnimarColeta(delta);
                return;
            }

            _tempo += delta;
            if (_tempo < TempoSalto)
            {
                AnimarSalto();
                return;
            }

            if (!_voando)
            {
                EsperarJogador();
            }
            else
            {
                VoarAteJogador(delta);
            }
        }

        private void LateUpdate()
        {
            Camera cameraPrincipal = Camera.main;
            if (cameraPrincipal == null)
            {
                return;
            }

            Vector3 paraCamera = cameraPrincipal.transform.position - transform.position;
            if (paraCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(paraCamera, cameraPrincipal.transform.up);
            }
        }

        private void AnimarSalto()
        {
            float progresso = Mathf.Clamp01(_tempo / TempoSalto);
            transform.position = Vector3.Lerp(_posicaoInicial, _posicaoNoChao, progresso);
            transform.position += Vector3.up * (Mathf.Sin(progresso * Mathf.PI) * AlturaSalto);
            float tamanho = Mathf.Lerp(0.6f, 1f, progresso) + Mathf.Sin(progresso * Mathf.PI) * 0.25f;
            transform.localScale = Vector3.one * tamanho;
        }

        private void EsperarJogador()
        {
            float tempoNoChao = _tempo - TempoSalto;
            transform.position = _posicaoNoChao + Vector3.up * (Mathf.Sin(tempoNoChao * 4f) * 0.05f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(tempoNoChao * 4f) * 0.06f);

            Vector3 direcao = _jogador.position - transform.position;
            direcao.y = 0f;
            if (direcao.magnitude <= RaioAtracao)
            {
                _voando = true;
            }
        }

        private void VoarAteJogador(float delta)
        {
            Vector3 destino = _jogador.position + Vector3.up * 0.6f;
            Vector3 direcao = destino - transform.position;
            float distancia = direcao.magnitude;
            _velocidade = Mathf.Min(_velocidade + Aceleracao * delta, VelocidadeMaxima);
            float passo = _velocidade * delta;
            if (distancia <= passo)
            {
                transform.position = destino;
                Coletar();
                return;
            }

            transform.position += direcao.normalized * passo;
            float tamanho = Mathf.Clamp(distancia / 0.8f, 0.35f, 1f);
            transform.localScale = new Vector3(0.85f, 1.15f, 1f) * tamanho;
        }

        private void Coletar()
        {
            if (_coletada)
            {
                return;
            }

            _coletada = true;
            EstadoJogo.Instancia?.AdicionarXp(1, _jogador);
            transform.localScale = Vector3.one * 1.2f;
            visual.color = new Color(1.55f, 1.3f, 1.8f, 1f);
            if (brilho != null)
            {
                brilho.enabled = true;
                brilho.color = new Color(0.93f, 0.65f, 1f, 1f);
                brilho.intensity = 2.4f;
                brilho.range = 2f;
            }
        }

        private void AnimarColeta(float delta)
        {
            _tempoColeta += delta;
            float progresso = Mathf.Clamp01(_tempoColeta / TempoBrilho);
            float restante = 1f - progresso;
            if (_jogador != null)
            {
                transform.position = _jogador.position + Vector3.up * 0.6f;
            }

            float tamanho = Mathf.Lerp(1.2f, 1.7f, progresso) + Mathf.Sin(progresso * Mathf.PI) * 0.3f;
            transform.localScale = Vector3.one * tamanho;
            Color cor = visual.color;
            cor.a = restante;
            visual.color = cor;
            if (brilho != null)
            {
                brilho.intensity = 2.4f * restante;
                brilho.range = Mathf.Lerp(2f, 3.2f, progresso);
            }

            if (progresso >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
