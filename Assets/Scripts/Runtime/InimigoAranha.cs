using System;
using UnityEngine;

namespace Avalonya
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class InimigoAranha : MonoBehaviour, IAlvoDano, IRecuoMachado, ICorpoAgua
    {
        [Header("Alvo e movimento")]
        [SerializeField] private Transform alvo;
        [SerializeField, Min(0f)] private float velocidade = 5f;
        [SerializeField, Min(0f)] private float gravidade = 9.8f;

        [Header("Cabeçada")]
        [SerializeField, Min(1f)] private float vidaMaxima = 1f;
        [SerializeField, Min(0)] private int xpPorAbate = 1;
        [SerializeField, Min(0f)] private float distanciaInicioAtaque = 1.45f;
        [SerializeField, Min(0f)] private float distanciaAcerto = 1.75f;
        [SerializeField, Min(0f)] private float danoAtaque = 1f;
        [SerializeField, Min(0.01f)] private float duracaoAtaque = 0.25f;
        [SerializeField, Min(0f)] private float intervaloAtaque = 0.25f;
        [SerializeField, Min(0f)] private float velocidadeInvestida = 6f;

        [Header("Vida e recuo")]
        [SerializeField] private Sprite spriteRecuo;
        [SerializeField, Min(0.1f)] private float forcaRecuo = 8f;
        [SerializeField, Min(0.05f)] private float duracaoRecuo = 0.22f;
        [SerializeField, Min(0f)] private float raioHorizontalDano = 0.4f;

        [Header("Animacao")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] quadrosAndar;
        [SerializeField, Min(0.1f)] private float quadrosPorSegundoAndar = 4.5f;
        [SerializeField] private Sprite[] quadrosAtaque;

        private CharacterController _colisor;
        private Camera _camera;
        private VidaJogador _vidaAlvo;
        private SpriteRenderer _visualRecuo;
        private Vector3 _velocidadeRecuo;
        private Vector3 _direcaoAtaque;
        private float _vidaAtual;
        private float _velocidadeVertical;
        private float _tempoAnimacao;
        private float _tempoAtaque;
        private float _tempoRecargaAtaque;
        private float _tempoRecuo;
        private float _tempoAteDestruir;
        private bool _atacando;
        private bool _acertoTentado;
        private bool _morto;
        private int _ataquesIniciados;
        private int _acertosConfirmados;
        private float _velocidadeAntesDaAgua;
        private bool _naAgua;

        public event Action<InimigoAranha, GameObject> Morreu;

        public Transform Alvo => alvo;
        public float Velocidade => velocidade;
        public float VidaMaxima => vidaMaxima;
        public float VidaAtual => _vidaAtual;
        public bool Atacando => _atacando;
        public bool EstaMorto => _morto;
        public Vector3 PontoDano => transform.position + Vector3.up * 0.6f;
        public float RaioHorizontal => raioHorizontalDano;
        public int AtaquesIniciados => _ataquesIniciados;
        public int AcertosConfirmados => _acertosConfirmados;

        public void EntrarNaAgua(float velocidadeAgua, float campoVisaoAgua)
        {
            if (_naAgua)
            {
                return;
            }

            _naAgua = true;
            _velocidadeAntesDaAgua = velocidade;
            velocidade = velocidadeAgua;
        }

        public void SairDaAgua()
        {
            if (!_naAgua)
            {
                return;
            }

            velocidade = _velocidadeAntesDaAgua;
            _naAgua = false;
        }

        private void Awake()
        {
            _colisor = GetComponent<CharacterController>();
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (alvo == null)
            {
                Jogador jogador = FindAnyObjectByType<Jogador>();
                alvo = jogador != null ? jogador.transform : null;
            }

            AtualizarVidaAlvo();
            _vidaAtual = vidaMaxima;
            _camera = Camera.main;

            if (visual == null || quadrosAndar == null || quadrosAndar.Length == 0)
            {
                Debug.LogError("Aranha precisa de SpriteRenderer e quadros de caminhada.", this);
                enabled = false;
                return;
            }

            visual.sprite = quadrosAndar[0];
            CriarVisualRecuo();
        }

        private void OnEnable()
        {
            RegistroAlvosDano.Registrar(this);
        }

        private void OnDisable()
        {
            RegistroAlvosDano.Remover(this);
        }

        private void OnDestroy()
        {
            RegistroAlvosDano.Remover(this);
        }

        public void ConfigurarAlvo(Transform novoAlvo)
        {
            alvo = novoAlvo;
            AtualizarVidaAlvo();
        }

        public void ConfigurarValoresEnxame(float novaVelocidade, float novoDano)
        {
            velocidade = novaVelocidade;
            danoAtaque = novoDano;
        }

        public void ReceberDano(float quantidade, Vector3 origem, GameObject autor)
        {
            if (_morto || quantidade <= 0f)
            {
                return;
            }

            _vidaAtual = Mathf.Max(0f, _vidaAtual - quantidade);
            IniciarRecuo(origem, autor);
            if (_vidaAtual > 0f)
            {
                return;
            }

            _morto = true;
            RegistroAlvosDano.Remover(this);
            _atacando = false;
            _tempoAtaque = 0f;
            _tempoAteDestruir = duracaoRecuo + 0.04f;
            EstadoJogo.Instancia?.RegistrarMorte(alvo, transform.position, xpPorAbate);
            Morreu?.Invoke(this, autor);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f)
            {
                return;
            }

            if (_vidaAlvo != null && _vidaAlvo.EstaMorto)
            {
                if (_morto)
                {
                    if (_colisor.isGrounded)
                    {
                        _velocidadeVertical = Mathf.Min(_velocidadeVertical, -2f);
                    }
                    else
                    {
                        _velocidadeVertical -= gravidade * delta;
                    }

                    AtualizarRecuo(delta);
                    _colisor.Move((_velocidadeRecuo + Vector3.up * _velocidadeVertical) * delta);
                    _tempoAteDestruir -= delta;
                    if (_tempoAteDestruir <= 0f)
                    {
                        Destroy(gameObject);
                    }
                }

                return;
            }

            if (_colisor.isGrounded)
            {
                _velocidadeVertical = Mathf.Min(_velocidadeVertical, -2f);
            }
            else
            {
                _velocidadeVertical -= gravidade * delta;
            }

            _tempoRecargaAtaque = Mathf.Max(0f, _tempoRecargaAtaque - delta);
            if (_morto)
            {
                _tempoAteDestruir -= delta;
                if (_tempoAteDestruir <= 0f)
                {
                    Destroy(gameObject);
                }
            }

            Vector3 movimento = Vector3.zero;
            if (_tempoRecuo > 0f)
            {
                AtualizarRecuo(delta);
                movimento = _velocidadeRecuo;
            }
            else if (_morto)
            {
                // A aranha morta permanece imovel ate terminar o recuo visual.
                movimento = Vector3.zero;
            }
            else if (alvo != null)
            {
                Vector3 paraAlvo = alvo.position - transform.position;
                paraAlvo.y = 0f;
                float distancia = paraAlvo.magnitude;
                if (_atacando)
                {
                    movimento = AtualizarCabecada(delta);
                }
                else if (distancia <= distanciaInicioAtaque)
                {
                    if (_tempoRecargaAtaque <= 0f)
                    {
                        IniciarCabecada(paraAlvo);
                        movimento = AtualizarCabecada(delta);
                    }
                }
                else if (distancia > 0.01f)
                {
                    movimento = paraAlvo.normalized * velocidade;
                }
            }

            if (movimento.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(movimento, Vector3.up);
            }

            _colisor.Move((movimento + Vector3.up * _velocidadeVertical) * delta);
            if (_morto)
            {
                return;
            }

            if (_atacando)
            {
                AtualizarAnimacaoAtaque();
            }
            else
            {
                AtualizarAnimacao(movimento.sqrMagnitude > 0.0001f, delta);
            }
        }

        private void IniciarCabecada(Vector3 paraAlvo)
        {
            _atacando = true;
            _tempoAtaque = 0f;
            _acertoTentado = false;
            _direcaoAtaque = paraAlvo.sqrMagnitude > 0.0001f ? paraAlvo.normalized : Vector3.zero;
            _ataquesIniciados++;
            _tempoAnimacao = 0f;
        }

        private Vector3 AtualizarCabecada(float delta)
        {
            _tempoAtaque += delta;
            Vector3 movimento = _tempoAtaque >= duracaoAtaque * 0.3f
                && _tempoAtaque < duracaoAtaque * 0.8f
                ? _direcaoAtaque * velocidadeInvestida
                : Vector3.zero;

            if (!_acertoTentado && _tempoAtaque >= duracaoAtaque * 0.5f)
            {
                _acertoTentado = true;
                if (TentarAcertarJogador())
                {
                    _acertosConfirmados++;
                }
            }

            if (_tempoAtaque >= duracaoAtaque)
            {
                _atacando = false;
                _tempoRecargaAtaque = intervaloAtaque;
                _tempoAnimacao = 0f;
            }

            return movimento;
        }

        private bool TentarAcertarJogador()
        {
            if (alvo == null || _vidaAlvo == null || _vidaAlvo.EstaMorto)
            {
                return false;
            }

            Vector3 separacao = alvo.position - transform.position;
            separacao.y = 0f;
            if (separacao.magnitude > distanciaAcerto)
            {
                return false;
            }

            Vector3 origem = transform.position + Vector3.up * 0.6f;
            Vector3 destino = alvo.position + Vector3.up * 0.6f;
            Vector3 raio = destino - origem;
            float comprimento = raio.magnitude;
            if (comprimento > 0.001f
                && Physics.Raycast(origem, raio / comprimento, comprimento,
                    LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            _vidaAlvo.ReceberDano(danoAtaque, transform.position);
            return true;
        }

        private void AtualizarVidaAlvo()
        {
            _vidaAlvo = alvo != null ? alvo.GetComponent<VidaJogador>() : null;
            if (_vidaAlvo == null && alvo != null)
            {
                _vidaAlvo = alvo.GetComponentInParent<VidaJogador>();
            }
        }

        private void AtualizarAnimacao(bool andando, float delta)
        {
            if (quadrosAndar.Length == 0)
            {
                return;
            }

            if (!andando)
            {
                _tempoAnimacao = 0f;
                visual.sprite = quadrosAndar[0];
                return;
            }

            _tempoAnimacao += delta * quadrosPorSegundoAndar;
            int indice = Mathf.FloorToInt(_tempoAnimacao) % quadrosAndar.Length;
            visual.sprite = quadrosAndar[indice];
        }

        private void AtualizarAnimacaoAtaque()
        {
            if (quadrosAtaque == null || quadrosAtaque.Length == 0)
            {
                return;
            }

            float progresso = Mathf.Clamp01(_tempoAtaque / Mathf.Max(duracaoAtaque, 0.01f));
            int indice = Mathf.Min(Mathf.FloorToInt(progresso * quadrosAtaque.Length), quadrosAtaque.Length - 1);
            visual.sprite = quadrosAtaque[indice];
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null || visual == null)
            {
                return;
            }

            SpriteRenderer visualAtual = _visualRecuo != null && _visualRecuo.enabled ? _visualRecuo : visual;
            Vector3 paraCamera = _camera.transform.position - visualAtual.transform.position;
            if (paraCamera.sqrMagnitude > 0.0001f)
            {
                visualAtual.transform.rotation = Quaternion.LookRotation(paraCamera, _camera.transform.up);
            }

            Vector3 direcao = _atacando ? _direcaoAtaque : (alvo != null ? alvo.position - transform.position : transform.forward);
            direcao.y = 0f;
            visualAtual.flipX = direcao.sqrMagnitude > 0.0001f
                && Vector3.Dot(_camera.transform.right, direcao) > 0f;
        }

        private void CriarVisualRecuo()
        {
            if (spriteRecuo == null)
            {
                return;
            }

            GameObject objeto = new GameObject("VisualRecuoAranha");
            objeto.transform.SetParent(visual.transform.parent, false);
            objeto.transform.localPosition = visual.transform.localPosition;
            objeto.transform.localRotation = visual.transform.localRotation;
            objeto.transform.localScale = visual.transform.localScale;
            _visualRecuo = objeto.AddComponent<SpriteRenderer>();
            _visualRecuo.sprite = spriteRecuo;
            _visualRecuo.color = visual.color;
            _visualRecuo.sortingLayerID = visual.sortingLayerID;
            _visualRecuo.sortingOrder = visual.sortingOrder + 1;
            _visualRecuo.enabled = false;
        }

        public void RedirecionarRecuoMachado(Vector3 impulso)
        {
            if (_morto)
            {
                return;
            }

            impulso.y = 0f;
            _velocidadeRecuo = impulso;
            _tempoRecuo = duracaoRecuo;
        }

        private void IniciarRecuo(Vector3 origem, GameObject autor)
        {
            _atacando = false;
            _tempoAtaque = 0f;
            _tempoRecargaAtaque = Mathf.Max(_tempoRecargaAtaque, 0.18f);
            Vector3 direcao = transform.position - origem;
            direcao.y = 0f;
            if (direcao.sqrMagnitude < 0.0001f && autor != null)
            {
                direcao = transform.position - autor.transform.position;
                direcao.y = 0f;
            }

            if (direcao.sqrMagnitude < 0.0001f && alvo != null)
            {
                direcao = transform.position - alvo.position;
                direcao.y = 0f;
            }

            if (direcao.sqrMagnitude < 0.0001f)
            {
                direcao = Vector3.back;
            }

            _velocidadeRecuo = direcao.normalized * forcaRecuo;
            _tempoRecuo = duracaoRecuo;
            if (_visualRecuo != null)
            {
                _visualRecuo.flipX = visual.flipX;
                _visualRecuo.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(1.12f, 0.9f, 1f));
                _visualRecuo.enabled = true;
                visual.enabled = false;
            }
        }

        private void AtualizarRecuo(float delta)
        {
            _tempoRecuo = Mathf.Max(0f, _tempoRecuo - delta);
            _velocidadeRecuo = Vector3.MoveTowards(
                _velocidadeRecuo,
                Vector3.zero,
                forcaRecuo / Mathf.Max(duracaoRecuo, 0.01f) * delta
            );

            if (_tempoRecuo <= 0f && !_morto)
            {
                if (_visualRecuo != null)
                {
                    _visualRecuo.enabled = false;
                }

                visual.enabled = true;
            }
        }
    }
}
