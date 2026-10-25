using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Avalonia
{
    [RequireComponent(typeof(JogadorAvalonia))]
    public sealed class AtaqueBasicoAvalonia : MonoBehaviour
    {
        [Header("Ataque corpo a corpo")]
        [SerializeField, Min(0f)] private float dano = 2f;
        [SerializeField, Min(0.1f)] private float alcance = 7f;
        [SerializeField, Range(1f, 90f)] private float aberturaGraus = 45f;
        [SerializeField, Min(0.05f)] private float intervalo = 1.6f;
        [SerializeField] private float alturaAtaque = 0.65f;
        [SerializeField] private float alturaGuia = 0.35f;
        [SerializeField] private float duracaoEfeito = 0.18f;
        [SerializeField] private Sprite spriteGolpe;
        [SerializeField] private Material materialGuia;
        [SerializeField] private LayerMask mascaraObstaculos = 1;

        private readonly LineRenderer[] _linhasGuia = new LineRenderer[3];
        private Camera _camera;
        private JogadorAvalonia _jogador;
        private VidaJogadorAvalonia _vidaJogador;
        private Vector3 _direcao = Vector3.forward;
        private float _recarga;
        private float _multiplicadorVelocidadeAtaque = 1f;
        private int _ataquesExecutados;
        private int _alvosAtingidosNoUltimoAtaque;

        public float Dano => dano;
        public float Alcance => alcance;
        public float Intervalo => intervalo / Mathf.Max(0.1f, _multiplicadorVelocidadeAtaque);
        public int AtaquesExecutados => _ataquesExecutados;
        public int AlvosAtingidosNoUltimoAtaque => _alvosAtingidosNoUltimoAtaque;
        public Vector3 Direcao => _direcao;

        private void Awake()
        {
            _jogador = GetComponent<JogadorAvalonia>();
            _vidaJogador = GetComponent<VidaJogadorAvalonia>();
            _camera = Camera.main;
            CriarGuia();
            AtualizarMira();
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            AtualizarMira();
            AtualizarGuia();
            _recarga = Mathf.Max(0f, _recarga - Time.deltaTime);

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && _recarga <= 0f
                && (_vidaJogador == null || !_vidaJogador.EstaMorto))
            {
                ExecutarAtaque();
            }
        }

        /// <summary>Ponto de entrada público também usado pela validação no Play Mode.</summary>
        public void ExecutarAtaque()
        {
            if (!isActiveAndEnabled || _recarga > 0f || (_vidaJogador != null && _vidaJogador.EstaMorto))
            {
                return;
            }

            _recarga = Mathf.Max(Intervalo, 0.05f);
            _ataquesExecutados++;
            _alvosAtingidosNoUltimoAtaque = 0;

            Vector3 origem = transform.position + Vector3.up * alturaAtaque;
            MonoBehaviour[] componentes = FindObjectsByType<MonoBehaviour>();
            foreach (MonoBehaviour componente in componentes)
            {
                if (componente == null || componente.gameObject == gameObject
                    || !(componente is IAlvoDanoAvalonia alvo) || alvo.EstaMorto)
                {
                    continue;
                }

                Vector3 deslocamento = alvo.PontoDano - origem;
                if (Mathf.Abs(deslocamento.y) > 1f)
                {
                    continue;
                }

                deslocamento.y = 0f;
                float distancia = deslocamento.magnitude;
                float raio = Mathf.Max(0f, alvo.RaioHorizontal);
                if (distancia > alcance + raio)
                {
                    continue;
                }

                float meiaAbertura = aberturaGraus * 0.5f;
                if (distancia > raio && distancia > 0.0001f)
                {
                    meiaAbertura += Mathf.Asin(Mathf.Clamp01(raio / distancia)) * Mathf.Rad2Deg;
                    if (Vector3.Dot(_direcao, deslocamento / distancia) < Mathf.Cos(meiaAbertura * Mathf.Deg2Rad))
                    {
                        continue;
                    }
                }

                alvo.ReceberDano(dano, origem, gameObject);
                _alvosAtingidosNoUltimoAtaque++;
            }

            ExibirEfeito(origem);
        }

        public void DefinirDano(float valor)
        {
            dano = Mathf.Max(0f, valor);
        }

        public void DefinirMultiplicadorVelocidade(float multiplicador)
        {
            _multiplicadorVelocidadeAtaque = Mathf.Max(0.1f, multiplicador);
        }

        private void AtualizarMira()
        {
            if (_camera == null || Mouse.current == null)
            {
                return;
            }

            Vector3 origem = transform.position + Vector3.up * alturaAtaque;
            Vector3 centroTela = _camera.WorldToScreenPoint(origem);
            Vector2 diferenca = Mouse.current.position.ReadValue() - new Vector2(centroTela.x, centroTela.y);
            if (diferenca.magnitude < 8f)
            {
                return;
            }

            float angulo = Mathf.Round(Mathf.Atan2(diferenca.y, diferenca.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
            Vector2 pixel = new Vector2(centroTela.x, centroTela.y) + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 100f;
            Ray raio = _camera.ScreenPointToRay(pixel);
            Plane plano = new Plane(Vector3.up, origem);
            if (!plano.Raycast(raio, out float distancia))
            {
                return;
            }

            Vector3 ponto = raio.GetPoint(distancia);
            Vector3 vetor = ponto - origem;
            vetor.y = 0f;
            if (vetor.sqrMagnitude > 0.0001f)
            {
                _direcao = vetor.normalized;
            }
        }

        private void CriarGuia()
        {
            for (int indice = 0; indice < _linhasGuia.Length; indice++)
            {
                GameObject objeto = new GameObject(indice == 2 ? "ArcoAtaque" : $"LadoAtaque{indice + 1}");
                objeto.transform.SetParent(transform, false);
                LineRenderer linha = objeto.AddComponent<LineRenderer>();
                linha.useWorldSpace = true;
                linha.loop = indice == 2;
                linha.widthMultiplier = 0.025f;
                linha.positionCount = indice == 2 ? 17 : 2;
                linha.startColor = new Color(1f, 0.62f, 0.12f, 0.85f);
                linha.endColor = new Color(1f, 0.62f, 0.12f, 0.85f);
                linha.numCapVertices = 2;
                linha.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                linha.receiveShadows = false;
                if (materialGuia != null)
                {
                    linha.sharedMaterial = materialGuia;
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    if (shader != null)
                    {
                        linha.material = new Material(shader) { color = new Color(1f, 0.62f, 0.12f, 0.85f) };
                    }
                }

                _linhasGuia[indice] = linha;
            }
        }

        private void AtualizarGuia()
        {
            Vector3 centro = transform.position + Vector3.up * alturaGuia;
            float meiaAbertura = aberturaGraus * 0.5f;
            Vector3 primeiro = Quaternion.AngleAxis(-meiaAbertura, Vector3.up) * _direcao;
            Vector3 segundo = Quaternion.AngleAxis(meiaAbertura, Vector3.up) * _direcao;
            _linhasGuia[0].SetPosition(0, centro);
            _linhasGuia[0].SetPosition(1, centro + primeiro * alcance);
            _linhasGuia[1].SetPosition(0, centro);
            _linhasGuia[1].SetPosition(1, centro + segundo * alcance);

            for (int indice = 0; indice <= 16; indice++)
            {
                float angulo = Mathf.Lerp(-meiaAbertura, meiaAbertura, indice / 16f);
                Vector3 direcao = Quaternion.AngleAxis(angulo, Vector3.up) * _direcao;
                _linhasGuia[2].SetPosition(indice, centro + direcao * alcance);
            }
        }

        private void ExibirEfeito(Vector3 origem)
        {
            if (spriteGolpe == null)
            {
                return;
            }

            Vector3 fim = origem + _direcao * alcance;
            if (Physics.Raycast(origem, _direcao, out RaycastHit bloqueio, alcance, mascaraObstaculos, QueryTriggerInteraction.Ignore))
            {
                fim = bloqueio.point;
            }

            GameObject objeto = new GameObject("EfeitoAtaqueBasico");
            SpriteRenderer renderizador = objeto.AddComponent<SpriteRenderer>();
            renderizador.sprite = spriteGolpe;
            renderizador.sortingOrder = 10;
            StartCoroutine(AnimarEfeito(objeto.transform, renderizador, origem, fim));
        }

        private IEnumerator AnimarEfeito(Transform efeito, SpriteRenderer renderizador, Vector3 inicio, Vector3 fim)
        {
            float duracao = Mathf.Max(0.01f, duracaoEfeito);
            float tempo = 0f;
            while (tempo < duracao && efeito != null)
            {
                tempo += Time.deltaTime;
                float progresso = Mathf.Clamp01(tempo / duracao);
                efeito.position = Vector3.Lerp(inicio, fim, progresso);
                if (_camera != null)
                {
                    Vector3 paraCamera = _camera.transform.position - efeito.position;
                    if (paraCamera.sqrMagnitude > 0.0001f)
                    {
                        efeito.rotation = Quaternion.LookRotation(paraCamera, _camera.transform.up);
                    }

                    Vector3 inicioTela = _camera.WorldToScreenPoint(inicio);
                    Vector3 fimTela = _camera.WorldToScreenPoint(fim);
                    Vector2 direcaoTela = new Vector2(fimTela.x - inicioTela.x, fimTela.y - inicioTela.y);
                    if (direcaoTela.sqrMagnitude > 0.0001f)
                    {
                        efeito.Rotate(Vector3.forward, -Vector2.SignedAngle(Vector2.right, direcaoTela), Space.Self);
                    }
                }

                Color cor = renderizador.color;
                cor.a = 1f - progresso;
                renderizador.color = cor;
                yield return null;
            }

            if (efeito != null)
            {
                Destroy(efeito.gameObject);
            }
        }
    }
}
