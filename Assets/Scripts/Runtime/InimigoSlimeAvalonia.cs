using System;
using UnityEngine;

namespace Avalonia
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class InimigoSlimeAvalonia : MonoBehaviour, IAlvoDanoAvalonia, IRecuoMachadoAvalonia, ICorpoAguaAvalonia
    {
        [Header("Alvo e movimento")]
        [SerializeField] private Transform alvo;
        [SerializeField] private float velocidade = 2f;
        [SerializeField] private float distanciaParar = 1.45f;
        [SerializeField] private float raioSeparacao = 1.2f;
        [SerializeField] private float forcaSeparacao = 1.5f;
        [SerializeField] private float gravidade = 9.8f;

        [Header("Cabeçada")]
        [SerializeField, Min(1f)] private float vidaMaxima = 4f;
        [SerializeField, Min(0)] private int xpPorAbate = 3;
        [SerializeField] private Sprite[] quadrosAtaque;
        [SerializeField] private float quadrosPorSegundoAtaque = 2f / 1.2f;
        [SerializeField] private float distanciaAcerto = 1.75f;
        [SerializeField] private float danoAtaque = 2f;
        [SerializeField] private float duracaoAtaque = 1.2f;
        [SerializeField] private float intervaloAtaque = 1.3f;
        [SerializeField] private float velocidadeInvestida = 1.5f;

        [Header("Vida e recuo")]
        [SerializeField] private Sprite spriteRecuo;
        [SerializeField, Min(0.1f)] private float forcaRecuo = 8f;
        [SerializeField, Min(0.05f)] private float duracaoRecuo = 0.22f;
        [SerializeField, Min(0f), Tooltip("Raio equivalente ao canto da caixa 1.05 x 0.8 m usada no Godot.")]
        private float raioHorizontalDano = 0.66f;

        [Header("Animacao")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] quadrosAndar;
        [SerializeField] private float quadrosPorSegundo = 1.8f;
        [SerializeField] private float frequenciaPulso = 0.9f;

        private CharacterController _colisor;
        private Camera _camera;
        private VidaJogadorAvalonia _vidaAlvo;
        private readonly Collider[] _vizinhos = new Collider[24];
        private float _velocidadeVertical;
        private float _tempoAnimacao;
        private float _tempoPulso;
        private bool _andando;
        private bool _atacando;
        private bool _acertoTentado;
        private Vector3 _direcaoAtaque;
        private float _tempoAtaque;
        private float _tempoRecargaAtaque;
        private int _acertosConfirmados;
        private int _ataquesIniciados;
        private float _ladoSeparacaoReserva;
        private Vector3 _escalaVisual;
        private SpriteRenderer _visualRecuo;
        private Vector3 _velocidadeRecuo;
        private float _tempoRecuo;
        private float _tempoAteDestruir;
        private float _vidaAtual;
        private bool _morto;
        private float _velocidadeAntesDaAgua;
        private bool _naAgua;

        public event Action<InimigoSlimeAvalonia, GameObject> Morreu;

        public Transform Alvo => alvo;
        public float Velocidade => velocidade;
        public bool Atacando => _atacando;
        public float VidaMaxima => vidaMaxima;
        public float VidaAtual => _vidaAtual;
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
                JogadorAvalonia jogador = FindAnyObjectByType<JogadorAvalonia>();
                if (jogador != null)
                {
                    alvo = jogador.transform;
                }
            }

            AtualizarVidaAlvo();
            _vidaAtual = vidaMaxima;

            if (visual == null || quadrosAndar == null || quadrosAndar.Length == 0)
            {
                Debug.LogError("Slime precisa de SpriteRenderer e quadros de caminhada.", this);
                enabled = false;
                return;
            }

            _camera = Camera.main;
            _ladoSeparacaoReserva = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _escalaVisual = visual.transform.localScale;
            visual.sprite = quadrosAndar[0];
            CriarVisualRecuo();
        }

        public void ConfigurarAlvo(Transform novoAlvo)
        {
            alvo = novoAlvo;
            AtualizarVidaAlvo();
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
            _atacando = false;
            _tempoAtaque = 0f;
            _tempoAteDestruir = duracaoRecuo + 0.04f;
            EstadoJogoAvalonia.Instancia?.RegistrarMorte(alvo, transform.position, xpPorAbate);
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
                _atacando = false;
                _andando = false;
                visual.transform.localScale = _escalaVisual;
                if (_morto)
                {
                    AtualizarRecuo(delta);
                    _tempoAteDestruir -= delta;
                    if (_tempoAteDestruir <= 0f)
                    {
                        Destroy(gameObject);
                    }
                }
                AtualizarAnimacao(false, delta);
                return;
            }

            if (_colisor.isGrounded)
            {
                if (_velocidadeVertical < 0f)
                {
                    _velocidadeVertical = -2f;
                }
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
                // O Godot deixa o recuo terminar e entao mantem o corpo imovel ate ser removido.
                // Nao deixe o inimigo iniciar outra perseguicao/cabecada nesse intervalo curto.
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
                else if (distancia <= distanciaParar)
                {
                    movimento = Vector3.zero;
                    if (_tempoRecargaAtaque <= 0f)
                    {
                        IniciarCabecada(paraAlvo);
                        movimento = AtualizarCabecada(delta);
                    }
                }
                else if (distancia > 0.01f)
                {
                    Vector3 perseguicao = paraAlvo.normalized;
                    Vector3 separacao = CalcularSeparacao();
                    Vector3 direcao = perseguicao + separacao * forcaSeparacao;
                    if (direcao.sqrMagnitude > 0.0001f)
                    {
                        movimento = direcao.normalized * velocidade;
                    }
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
                AtualizarAnimacaoCabecada();
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
            _andando = false;
            visual.transform.localScale = _escalaVisual;
        }

        private void AtualizarVidaAlvo()
        {
            _vidaAlvo = alvo != null ? alvo.GetComponent<VidaJogadorAvalonia>() : null;
            if (_vidaAlvo == null && alvo != null)
            {
                _vidaAlvo = alvo.GetComponentInParent<VidaJogadorAvalonia>();
            }
        }

        private Vector3 AtualizarCabecada(float delta)
        {
            _tempoAtaque += delta;
            float inicioInvestida = duracaoAtaque * 0.5f;
            float fimInvestida = duracaoAtaque * 0.8f;
            Vector3 movimento = _tempoAtaque >= inicioInvestida && _tempoAtaque < fimInvestida
                ? _direcaoAtaque * velocidadeInvestida
                : Vector3.zero;

            if (!_acertoTentado && _tempoAtaque >= duracaoAtaque * 0.62f)
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
                _andando = false;
                visual.transform.localScale = _escalaVisual;
            }

            return movimento;
        }

        private bool TentarAcertarJogador()
        {
            if (alvo == null)
            {
                return false;
            }

            Vector3 separacao = alvo.position - transform.position;
            separacao.y = 0f;
            if (separacao.magnitude > distanciaAcerto)
            {
                return false;
            }

            VidaJogadorAvalonia vida = alvo.GetComponent<VidaJogadorAvalonia>();
            if (vida == null)
            {
                vida = alvo.GetComponentInParent<VidaJogadorAvalonia>();
            }

            if (vida == null || vida.EstaMorto)
            {
                return false;
            }

            Vector3 origem = transform.position + Vector3.up * 0.6f;
            Vector3 destino = alvo.position + Vector3.up * 0.6f;
            Vector3 raio = destino - origem;
            float comprimento = raio.magnitude;
            if (comprimento > 0.001f
                && Physics.Raycast(origem, raio / comprimento, comprimento, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            vida.ReceberDano(danoAtaque, transform.position);
            return true;
        }

        private void AtualizarAnimacaoCabecada()
        {
            if (quadrosAtaque == null || quadrosAtaque.Length == 0)
            {
                return;
            }

            float duracaoQuadro = 1f / Mathf.Max(quadrosPorSegundoAtaque, 0.01f);
            int indice = Mathf.FloorToInt(_tempoAtaque / duracaoQuadro);
            indice = Mathf.Clamp(indice, 0, quadrosAtaque.Length - 1);
            visual.sprite = quadrosAtaque[indice];
            visual.transform.localScale = _escalaVisual;
        }

        private Vector3 CalcularSeparacao()
        {
            if (raioSeparacao <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 centro = transform.position + Vector3.up * _colisor.center.y;
            int quantidadeVizinhos = Physics.OverlapSphereNonAlloc(
                centro,
                raioSeparacao,
                _vizinhos,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );
            Vector3 separacao = Vector3.zero;
            for (int indice = 0; indice < quantidadeVizinhos; indice++)
            {
                Collider vizinho = _vizinhos[indice];
                _vizinhos[indice] = null;
                if (vizinho == _colisor || vizinho.transform.IsChildOf(transform))
                {
                    continue;
                }

                InimigoSlimeAvalonia outro = vizinho.GetComponentInParent<InimigoSlimeAvalonia>();
                if (outro == null || outro == this)
                {
                    continue;
                }

                Vector3 afastamento = transform.position - outro.transform.position;
                afastamento.y = 0f;
                float distancia = afastamento.magnitude;
                if (distancia >= raioSeparacao)
                {
                    continue;
                }

                if (distancia <= 0.001f)
                {
                    afastamento = Vector3.Cross(Vector3.up, transform.forward);
                    afastamento *= _ladoSeparacaoReserva;
                }

                float intensidade = 1f - distancia / raioSeparacao;
                separacao += afastamento.normalized * intensidade;
            }

            return Vector3.ClampMagnitude(separacao, 1f);
        }

        private void LateUpdate()
        {
            if (visual == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            SpriteRenderer visualAtual = _visualRecuo != null && _visualRecuo.enabled ? _visualRecuo : visual;
            if (_camera != null)
            {
                Vector3 paraCamera = _camera.transform.position - visualAtual.transform.position;
                if (paraCamera.sqrMagnitude > 0.0001f)
                {
                    visualAtual.transform.rotation = Quaternion.LookRotation(paraCamera, _camera.transform.up);
                }

                if (alvo != null)
                {
                    Vector3 direcaoAlvo = alvo.position - transform.position;
                    direcaoAlvo.y = 0f;
                    visualAtual.flipX = direcaoAlvo.sqrMagnitude > 0.0001f
                        && Vector3.Dot(_camera.transform.right, direcaoAlvo) > 0f;
                }
            }
        }

        private void CriarVisualRecuo()
        {
            if (spriteRecuo == null)
            {
                return;
            }

            GameObject objeto = new GameObject("SpriteRecuo");
            objeto.transform.SetParent(visual.transform.parent, false);
            objeto.transform.localPosition = visual.transform.localPosition;
            objeto.transform.localRotation = visual.transform.localRotation;
            objeto.transform.localScale = visual.transform.localScale;
            _visualRecuo = objeto.AddComponent<SpriteRenderer>();
            _visualRecuo.sprite = spriteRecuo;
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
                _visualRecuo.transform.localScale = Vector3.Scale(_escalaVisual, new Vector3(1.12f, 0.9f, 1f));
                _visualRecuo.enabled = true;
                visual.enabled = false;
            }
        }

        private void AtualizarRecuo(float delta)
        {
            _tempoRecuo = Mathf.Max(0f, _tempoRecuo - delta);
            float desaceleracao = forcaRecuo / Mathf.Max(duracaoRecuo, 0.001f);
            _velocidadeRecuo = Vector3.MoveTowards(_velocidadeRecuo, Vector3.zero, desaceleracao * delta);
            if (_tempoRecuo <= 0f && _visualRecuo != null)
            {
                _visualRecuo.enabled = false;
                visual.enabled = true;
                visual.transform.localScale = _escalaVisual;
            }
        }

        private void AtualizarAnimacao(bool mover, float delta)
        {
            if (mover != _andando)
            {
                _andando = mover;
                _tempoAnimacao = 0f;
                _tempoPulso = 0f;
                visual.transform.localScale = _escalaVisual;
            }

            if (!_andando)
            {
                visual.sprite = quadrosAndar[0];
                return;
            }

            _tempoAnimacao += delta;
            float duracaoQuadro = 1f / Mathf.Max(quadrosPorSegundo, 0.01f);
            int indiceQuadro = Mathf.FloorToInt(_tempoAnimacao / duracaoQuadro) % quadrosAndar.Length;
            visual.sprite = quadrosAndar[indiceQuadro];

            _tempoPulso += delta * frequenciaPulso * Mathf.PI * 2f;
            float pulso = Mathf.Sin(_tempoPulso);
            visual.transform.localScale = new Vector3(
                _escalaVisual.x * (1f - pulso * 0.04f),
                _escalaVisual.y * (1f + pulso * 0.07f),
                _escalaVisual.z
            );
        }
    }
}
