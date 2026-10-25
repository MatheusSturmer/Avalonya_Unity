using UnityEngine;
using UnityEngine.Rendering;

namespace Avalonia
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollowAvalonia : MonoBehaviour
    {
        [Header("Alvo")]
        public JogadorAvalonia jogador;

        [Header("Mola")]
        [SerializeField] private float frequenciaAcompanhamento = 5f;
        [SerializeField] private float frequenciaVertical = 8f;
        [SerializeField, Range(0.1f, 2f)] private float amortecimento = 0.75f;

        [Header("Espelhamento")]
        [SerializeField] private bool espelharDeslocamento = true;
        [SerializeField] private float suavizacaoLado = 6f;

        [Header("Antecipacao")]
        [SerializeField] private float antecipacao = 0.15f;
        [SerializeField] private float antecipacaoMaxima = 1f;

        [Header("Afastamento")]
        [SerializeField] private float atrasoAfastamento = 1f;
        [SerializeField] private float distanciaAfastamento = 3f;
        [SerializeField] private float velocidadeMinima = 0.5f;
        [SerializeField] private float suavizacaoAfastar = 2f;
        [SerializeField] private float suavizacaoAproximar = 4f;

        [Header("Campo de visao")]
        [SerializeField] private float aumentoCampoVisao = 40f;
        [SerializeField] private float velocidadeReferencia = 20f;
        [SerializeField] private float suavizacaoCampoVisao = 4f;

        [Header("Impacto")]
        [SerializeField] private float impulsoPouso = 0.6f;
        [SerializeField] private float impulsoPousoMaximo = 6f;

        private Camera _camera;
        private Vector3 _deslocamento;
        private Vector3 _velocidadeMola;
        private Vector3 _direcaoParaTras;
        private float _campoVisaoBase;
        private float _tempoMovendo;
        private float _quantidadeAfastamento;
        private float _ladoAlvo = 1f;
        private float _ladoAtual = 1f;
        private bool _inverterCullingAnterior;
        private bool _cullingInvertido;
        private bool _naAgua;
        private float _campoVisaoAgua = 50f;
        private float _campoVisaoAntesDaAgua;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += AoIniciarRenderizacao;
            RenderPipelineManager.endCameraRendering += AoTerminarRenderizacao;
            if (jogador != null)
            {
                jogador.Pousou += ReagirPouso;
            }
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= AoIniciarRenderizacao;
            RenderPipelineManager.endCameraRendering -= AoTerminarRenderizacao;
            if (jogador != null)
            {
                jogador.Pousou -= ReagirPouso;
            }

            RestaurarCulling();
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (jogador == null)
            {
                jogador = FindAnyObjectByType<JogadorAvalonia>();
            }

            if (jogador == null)
            {
                Debug.LogWarning("CameraFollowAvalonia nao encontrou o jogador.", this);
                enabled = false;
                return;
            }

            _deslocamento = transform.position - jogador.transform.position;
            _direcaoParaTras = -transform.forward.normalized;
            _campoVisaoBase = _camera.fieldOfView;
        }

        private void AoIniciarRenderizacao(ScriptableRenderContext contexto, Camera cameraRenderizada)
        {
            if (cameraRenderizada != _camera)
            {
                return;
            }

            _inverterCullingAnterior = GL.invertCulling;
            GL.invertCulling = !_inverterCullingAnterior;
            _cullingInvertido = true;
        }

        private void AoTerminarRenderizacao(ScriptableRenderContext contexto, Camera cameraRenderizada)
        {
            if (cameraRenderizada == _camera)
            {
                RestaurarCulling();
            }
        }

        private void RestaurarCulling()
        {
            if (!_cullingInvertido)
            {
                return;
            }

            GL.invertCulling = _inverterCullingAnterior;
            _cullingInvertido = false;
        }

        private void ReagirPouso(float velocidadeImpacto)
        {
            _velocidadeMola.y -= Mathf.Clamp(
                velocidadeImpacto * impulsoPouso,
                0f,
                impulsoPousoMaximo
            );
        }

        public void DefinirAgua(bool dentro, float campoVisaoAgua)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (dentro && !_naAgua)
            {
                _campoVisaoAntesDaAgua = _camera.fieldOfView;
            }
            else if (!dentro && _naAgua)
            {
                _camera.fieldOfView = _campoVisaoAntesDaAgua;
            }

            _campoVisaoAgua = campoVisaoAgua;
            _naAgua = dentro;
        }

        private void LateUpdate()
        {
            float delta = Mathf.Min(Time.deltaTime, 0.05f);
            if (delta <= 0f)
            {
                return;
            }

            Vector3 velocidadeHorizontal = jogador.VelocidadeHorizontal;
            velocidadeHorizontal.y = 0f;

            if (velocidadeHorizontal.magnitude > velocidadeMinima)
            {
                _tempoMovendo += delta;
            }
            else
            {
                _tempoMovendo = 0f;
            }

            bool afastarAgora = _tempoMovendo >= atrasoAfastamento;
            jogador.CameraAfastada = afastarAgora;
            float alvoAfastamento = afastarAgora ? 1f : 0f;
            float suavizacao = alvoAfastamento > _quantidadeAfastamento
                ? suavizacaoAfastar
                : suavizacaoAproximar;
            _quantidadeAfastamento = Mathf.Lerp(
                _quantidadeAfastamento,
                alvoAfastamento,
                1f - Mathf.Exp(-suavizacao * delta)
            );

            _ladoAlvo = jogador.OlhandoDireita ? 1f : -1f;
            _ladoAtual = Mathf.Lerp(
                _ladoAtual,
                _ladoAlvo,
                1f - Mathf.Exp(-suavizacaoLado * delta)
            );

            Vector3 deslocamentoAtual = _deslocamento;
            if (espelharDeslocamento)
            {
                deslocamentoAtual.x *= _ladoAtual;
            }

            Vector3 lead = Vector3.ClampMagnitude(velocidadeHorizontal * antecipacao, antecipacaoMaxima);
            Vector3 afastamento = _direcaoParaTras * distanciaAfastamento * _quantidadeAfastamento;
            Vector3 desejada = jogador.transform.position + deslocamentoAtual + lead + afastamento;

            float frequenciaHorizontalQuadrada = frequenciaAcompanhamento * frequenciaAcompanhamento;
            float amortecimentoHorizontal = 2f * amortecimento * frequenciaAcompanhamento;
            float frequenciaVerticalQuadrada = frequenciaVertical * frequenciaVertical;
            float amortecimentoVertical = 2f * amortecimento * frequenciaVertical;
            Vector3 deslocamento = transform.position - desejada;
            Vector3 aceleracao = new Vector3(
                -frequenciaHorizontalQuadrada * deslocamento.x - amortecimentoHorizontal * _velocidadeMola.x,
                -frequenciaVerticalQuadrada * deslocamento.y - amortecimentoVertical * _velocidadeMola.y,
                -frequenciaHorizontalQuadrada * deslocamento.z - amortecimentoHorizontal * _velocidadeMola.z
            );

            _velocidadeMola += aceleracao * delta;
            transform.position += _velocidadeMola * delta;

            float razaoVelocidade = Mathf.Clamp(velocidadeHorizontal.magnitude / velocidadeReferencia, 0f, 5f);
            float campoVisaoAlvo = _naAgua
                ? _campoVisaoAgua
                : _campoVisaoBase + aumentoCampoVisao * razaoVelocidade * _quantidadeAfastamento;
            _camera.fieldOfView = Mathf.Lerp(
                _camera.fieldOfView,
                campoVisaoAlvo,
                1f - Mathf.Exp(-(_naAgua ? 15f : suavizacaoCampoVisao) * delta)
            );

            _camera.ResetProjectionMatrix();
            Matrix4x4 projecao = _camera.projectionMatrix;
            projecao.m00 *= -1f;
            _camera.projectionMatrix = projecao;
        }
    }
}
