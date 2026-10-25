using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Avalonia
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class JogadorAvalonia : MonoBehaviour, ICorpoAguaAvalonia
    {
        public event Action<float> Pousou;

        [Header("Movimento")]
        [SerializeField] private float velocidadeAndar = 5f;
        [SerializeField] private float aceleracao = 28f;
        [SerializeField] private float atritoChao = 22f;
        [SerializeField, Range(0f, 1f)] private float controleNoAr = 0.45f;
        [SerializeField] private float arrastoNoAr = 1.5f;
        [SerializeField] private float gravidade = 9.8f;

        [Header("Derrapagem")]
        [SerializeField] private float freioDerrapagem = 55f;
        [SerializeField] private float velocidadeMinimaDerrapagem = 3.5f;
        [SerializeField, Range(-1f, 0f)] private float limiteOposicao = -0.5f;
        [SerializeField] private bool manterLadoNaDerrapagem = true;
        [SerializeField, Range(0f, 1f)] private float zonaMortaLado = 0.1f;

        [Header("Recuo")]
        [SerializeField] private float velocidadeRecuo = 2.5f;
        [SerializeField] private float duracaoRecuo = 0.12f;

        private CharacterController _colisor;
        private AnimacaoJogadorAvalonia _visual;
        private Vector3 _velocidadeHorizontal;
        private Vector2 _entrada;
        private float _velocidadeVertical;
        private float _tempoRecuo;
        private float _multiplicadorVelocidade = 1f;
        private float _multiplicadorAgua = 1f;
        private float _multiplicadorAguaAnterior = 1f;
        private bool _derrapando;
        private bool _olhandoDireita = true;
        private bool _morto;
        private bool _naAgua;
        private CameraFollowAvalonia _cameraFollow;

        public Vector3 VelocidadeHorizontal => _velocidadeHorizontal;
        public Vector2 Entrada => _entrada;
        public bool Derrapando => _derrapando;
        public bool OlhandoDireita => _olhandoDireita;
        public bool CameraAfastada { get; set; }
        public bool Morto => _morto;
        public float VelocidadeAndar => velocidadeAndar * _multiplicadorVelocidade * _multiplicadorAgua;

        private void Awake()
        {
            _colisor = GetComponent<CharacterController>();
            _visual = GetComponentInChildren<AnimacaoJogadorAvalonia>();
            _cameraFollow = FindAnyObjectByType<CameraFollowAvalonia>();
            if (_visual == null)
            {
                Debug.LogError("O jogador precisa de um componente AnimacaoJogadorAvalonia em um filho.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_morto)
            {
                _entrada = Vector2.zero;
                _velocidadeHorizontal = Vector3.zero;
                _visual.Atualizar(Vector2.zero, Vector3.zero, false, CameraAfastada, _colisor.isGrounded, VelocidadeAndar, Time.deltaTime);
                return;
            }

            _entrada = LerEntrada();
            Mover(Time.deltaTime);
            _visual.Atualizar(
                _entrada,
                _velocidadeHorizontal,
                _derrapando && manterLadoNaDerrapagem,
                CameraAfastada,
                _colisor.isGrounded,
                VelocidadeAndar,
                Time.deltaTime
            );
        }

        public void ReceberDanoDe(Vector3 origem)
        {
            if (_morto)
            {
                return;
            }

            Vector3 afastamento = transform.position - origem;
            afastamento.y = 0f;
            _tempoRecuo = 0f;
            _visual.ReceberDanoDe(origem);

            if (afastamento.sqrMagnitude <= 0.0001f || velocidadeRecuo <= 0f || duracaoRecuo <= 0f)
            {
                return;
            }

            _velocidadeHorizontal = afastamento.normalized * velocidadeRecuo;
            _tempoRecuo = duracaoRecuo;
        }

        public void DefinirMorto()
        {
            _morto = true;
            _entrada = Vector2.zero;
            _velocidadeHorizontal = Vector3.zero;
            _tempoRecuo = 0f;
            _derrapando = false;
        }

        public void DefinirMultiplicadorVelocidade(float multiplicador)
        {
            _multiplicadorVelocidade = Mathf.Max(0.1f, multiplicador);
        }

        public void EntrarNaAgua(float velocidadeAgua, float campoVisaoAgua)
        {
            if (_naAgua)
            {
                return;
            }

            _naAgua = true;
            _multiplicadorAguaAnterior = _multiplicadorAgua;
            _multiplicadorAgua = Mathf.Clamp(
                velocidadeAgua / Mathf.Max(velocidadeAndar, 0.001f),
                0f,
                1f
            );
            _velocidadeHorizontal = Vector3.zero;

            if (_cameraFollow == null)
            {
                _cameraFollow = FindAnyObjectByType<CameraFollowAvalonia>();
            }

            _cameraFollow?.DefinirAgua(true, campoVisaoAgua);
        }

        public void SairDaAgua()
        {
            if (!_naAgua)
            {
                return;
            }

            _naAgua = false;
            _multiplicadorAgua = _multiplicadorAguaAnterior;
            _cameraFollow?.DefinirAgua(false, 50f);
        }

        private static Vector2 LerEntrada()
        {
            Vector2 direcao = Vector2.zero;
            Keyboard teclado = Keyboard.current;
            if (teclado != null)
            {
                direcao.x = (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed ? 1f : 0f)
                    - (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed ? 1f : 0f);
                direcao.y = (teclado.sKey.isPressed || teclado.downArrowKey.isPressed ? 1f : 0f)
                    - (teclado.wKey.isPressed || teclado.upArrowKey.isPressed ? 1f : 0f);
            }

            Gamepad controle = Gamepad.current;
            if (controle != null && controle.leftStick.ReadValue().sqrMagnitude > direcao.sqrMagnitude)
            {
                Vector2 analogo = controle.leftStick.ReadValue();
                direcao = new Vector2(analogo.x, -analogo.y);
            }

            return Vector2.ClampMagnitude(direcao, 1f);
        }

        private void Mover(float delta)
        {
            if (delta <= 0f)
            {
                return;
            }

            bool estavaNoChao = _colisor.isGrounded;
            if (!estavaNoChao)
            {
                _velocidadeVertical -= gravidade * delta;
            }
            else if (_velocidadeVertical < 0f)
            {
                _velocidadeVertical = -2f;
            }

            Vector3 direcao = new Vector3(_entrada.x, 0f, _entrada.y);
            bool temEntrada = direcao.sqrMagnitude > 0.0001f;
            bool noChao = _colisor.isGrounded;

            if (_tempoRecuo > 0f)
            {
                float freioRecuo = velocidadeRecuo / Mathf.Max(duracaoRecuo, 0.001f);
                _velocidadeHorizontal = Vector3.MoveTowards(
                    _velocidadeHorizontal,
                    Vector3.zero,
                    freioRecuo * delta
                );
                _tempoRecuo = Mathf.Max(_tempoRecuo - delta, 0f);
                _derrapando = false;
            }
            else if (temEntrada)
            {
                bool derrapandoAgora = false;
                if (noChao && _velocidadeHorizontal.magnitude > velocidadeMinimaDerrapagem)
                {
                    float alinhamento = Vector3.Dot(_velocidadeHorizontal.normalized, direcao.normalized);
                    derrapandoAgora = alinhamento < limiteOposicao;
                }

                if (derrapandoAgora)
                {
                    _velocidadeHorizontal = Vector3.MoveTowards(
                        _velocidadeHorizontal,
                        Vector3.zero,
                        freioDerrapagem * delta
                    );
                    if (!_derrapando)
                    {
                        _visual.ReagirDerrapagem();
                    }
                }
                else
                {
                    float controle = noChao ? 1f : controleNoAr;
                    Vector3 velocidadeDesejada = direcao.normalized * VelocidadeAndar;
                    _velocidadeHorizontal = Vector3.MoveTowards(
                        _velocidadeHorizontal,
                        velocidadeDesejada,
                        aceleracao * controle * delta
                    );
                }

                _derrapando = derrapandoAgora;
                if (!(derrapandoAgora && manterLadoNaDerrapagem) && Mathf.Abs(_entrada.x) > zonaMortaLado)
                {
                    _olhandoDireita = _entrada.x > 0f;
                }
            }
            else
            {
                _derrapando = false;
                float atrito = noChao ? atritoChao : arrastoNoAr;
                _velocidadeHorizontal = Vector3.MoveTowards(
                    _velocidadeHorizontal,
                    Vector3.zero,
                    atrito * delta
                );
            }

            float velocidadeQueda = Mathf.Max(-_velocidadeVertical, 0f);
            Vector3 movimento = _velocidadeHorizontal + Vector3.up * _velocidadeVertical;
            _colisor.Move(movimento * delta);

            if (_colisor.isGrounded && !estavaNoChao)
            {
                Pousou?.Invoke(velocidadeQueda);
                _visual.ReagirPouso(velocidadeQueda);
            }
        }
    }
}
