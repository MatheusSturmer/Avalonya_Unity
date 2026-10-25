using System.Collections.Generic;
using UnityEngine;

namespace Avalonia
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AnimacaoJogadorAvalonia : MonoBehaviour
    {
        [Header("Folhas de sprites")]
        public Texture2D spriteFrente;
        public Texture2D spriteCostas;
        public Texture2D spriteLadoEsquerdo;
        public Texture2D spriteLadoDireito;
        public Texture2D correrFrente1;
        public Texture2D correrFrente2;
        public Texture2D correrCostas1;
        public Texture2D correrCostas2;
        public Texture2D correrLado1;
        public Texture2D correrLado2;
        public Texture2D danoFrente;
        public Texture2D danoCostas;
        public Texture2D danoLado;

        [Header("Aparencia")]
        [SerializeField] private float duracaoDano = 0.25f;
        [SerializeField] private float afundamentoDanoCima = 0.05f;
        [SerializeField] private float achatamentoPouso = 0.03f;
        [SerializeField] private float achatamentoMaximo = 0.4f;
        [SerializeField] private float achatamentoDerrapagem = 0.18f;
        [SerializeField] private float alturaQuique = 0.06f;
        [SerializeField] private float frequenciaQuique = 1.6f;
        [SerializeField] private float pixelsPorUnidade = 333.3333f;

        private const int LarguraQuadro = 512;
        private const int AlturaQuadro = 768;
        private const float PassoFormaMaximo = 1f / 120f;
        private const float DeltaFormaMaximo = 0.25f;

        private enum Direcao
        {
            Frente,
            Direita,
            Costas,
            Esquerda
        }

        private enum TipoAnimacao
        {
            Parado,
            Andar,
            Correr,
            Dano
        }

        private SpriteRenderer _renderizador;
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private Sprite[][] _parado;
        private Sprite[][] _andar;
        private Sprite[][] _correr;
        private Sprite[][] _dano;
        private Direcao _direcaoAtual = Direcao.Frente;
        private TipoAnimacao _animacaoAtual = TipoAnimacao.Parado;
        private float _tempoNoQuadro;
        private float _tempoDano;
        private float _achatamento;
        private float _velocidadeMola;
        private float _tempoQuique;
        private Vector3 _posicaoInicial;
        private Vector3 _escalaInicial;

        private void Awake()
        {
            _renderizador = GetComponent<SpriteRenderer>();
            _posicaoInicial = transform.localPosition;
            _escalaInicial = transform.localScale;
            CriarAnimacoes();
            AplicarQuadroAtual(0f);
        }

        public void Atualizar(
            Vector2 entrada,
            Vector3 velocidade,
            bool usarVelocidade,
            bool cameraAfastada,
            bool noChao,
            float velocidadeAndar,
            float delta
        )
        {
            AtualizarForma(delta, velocidade, noChao, velocidadeAndar);
            _tempoDano = Mathf.Max(_tempoDano - delta, 0f);

            bool andando = entrada.sqrMagnitude > 0.0001f || velocidade.sqrMagnitude > 0.04f;
            if (andando)
            {
                Vector2 direcao = usarVelocidade || entrada.sqrMagnitude <= 0.0001f
                    ? new Vector2(velocidade.x, velocidade.z)
                    : entrada;
                if (direcao.sqrMagnitude > 0.0001f)
                {
                    _direcaoAtual = EscolherDirecao(direcao);
                }
            }

            TipoAnimacao tipo = _tempoDano > 0f
                ? TipoAnimacao.Dano
                : andando
                    ? (cameraAfastada ? TipoAnimacao.Correr : TipoAnimacao.Andar)
                    : TipoAnimacao.Parado;

            if (tipo != _animacaoAtual)
            {
                _animacaoAtual = tipo;
                _tempoNoQuadro = 0f;
            }

            float quadrosPorSegundo = tipo == TipoAnimacao.Andar ? 8f : tipo == TipoAnimacao.Correr ? 6f : 1f;
            AplicarQuadroAtual(quadrosPorSegundo);
        }

        public void ReceberDanoDe(Vector3 origem)
        {
            Vector3 ladoGolpe = origem - transform.parent.position;
            ladoGolpe.y = 0f;
            if (new Vector2(ladoGolpe.x, ladoGolpe.z).sqrMagnitude > 0.0001f)
            {
                _direcaoAtual = EscolherDirecao(new Vector2(ladoGolpe.x, ladoGolpe.z));
            }

            _tempoDano = Mathf.Max(duracaoDano, 0f);
        }

        public void ReagirPouso(float velocidadeImpacto)
        {
            float forca = Mathf.Clamp(velocidadeImpacto * achatamentoPouso, 0f, achatamentoMaximo);
            _achatamento = Mathf.Max(_achatamento, forca);
            _velocidadeMola = 0f;
        }

        public void ReagirDerrapagem()
        {
            _velocidadeMola += achatamentoDerrapagem * 12f;
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
                transform.rotation = Quaternion.LookRotation(
                    paraCamera.normalized,
                    cameraPrincipal.transform.up
                );
            }
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in _sprites.Values)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }
        }

        private void CriarAnimacoes()
        {
            Sprite[] frente = CriarCaminhada(spriteFrente);
            Sprite[] esquerda = CriarCaminhada(spriteLadoEsquerdo);
            Sprite[] direita = CriarCaminhada(spriteLadoDireito);
            Sprite[] costas = CriarCaminhada(spriteCostas);

            _parado = new[] { new[] { frente[0] }, new[] { direita[0] }, new[] { costas[0] }, new[] { esquerda[0] } };
            _andar = new[] { frente, direita, costas, esquerda };
            _correr = new[]
            {
                new[] { CriarSprite(correrFrente1, 733, 496), CriarSprite(correrFrente2, 733, 496) },
                new[] { CriarSprite(correrLado1, 684, 452), CriarSprite(correrLado2, 682, 486) },
                new[] { CriarSprite(correrCostas1, 736, 490), CriarSprite(correrCostas2, 736, 490) },
                new[] { CriarSprite(correrLado1, 684, 452), CriarSprite(correrLado2, 682, 486) }
            };
            _dano = new[]
            {
                new[] { CriarSprite(danoFrente, 732, 467) },
                new[] { CriarSprite(danoLado, 731, 494) },
                new[] { CriarSprite(danoCostas, 737, 498) },
                new[] { CriarSprite(danoLado, 731, 494) }
            };
        }

        private Sprite[] CriarCaminhada(Texture2D textura)
        {
            return new[]
            {
                CriarSprite(textura, 0, 0),
                CriarSprite(textura, LarguraQuadro, 0),
                CriarSprite(textura, LarguraQuadro * 2, 0),
                CriarSprite(textura, LarguraQuadro * 3, 0)
            };
        }

        private Sprite CriarSprite(Texture2D textura, int x, int y)
        {
            if (textura == null)
            {
                Debug.LogError("Falta uma textura de animacao no componente AnimacaoJogadorAvalonia.", this);
                return null;
            }

            string chave = $"{textura.name}:{x}:{y}";
            if (_sprites.TryGetValue(chave, out Sprite existente))
            {
                return existente;
            }

            int yUnity = textura.height - y - AlturaQuadro;
            Rect regiao = new Rect(x, yUnity, LarguraQuadro, AlturaQuadro);
            Sprite criado = Sprite.Create(
                textura,
                regiao,
                new Vector2(0.5f, 0.5f),
                pixelsPorUnidade,
                0,
                SpriteMeshType.FullRect
            );
            criado.name = $"{textura.name}_{x}_{y}";
            _sprites.Add(chave, criado);
            return criado;
        }

        private void AplicarQuadroAtual(float quadrosPorSegundo)
        {
            Sprite[] quadros = _animacaoAtual switch
            {
                TipoAnimacao.Dano => _dano[(int)_direcaoAtual],
                TipoAnimacao.Correr => _correr[(int)_direcaoAtual],
                TipoAnimacao.Andar => _andar[(int)_direcaoAtual],
                _ => _parado[(int)_direcaoAtual]
            };

            if (_animacaoAtual == TipoAnimacao.Dano && _direcaoAtual == Direcao.Frente)
            {
                quadrosPorSegundo = 1f;
            }

            _tempoNoQuadro += Time.deltaTime;
            int indice = quadros.Length <= 1 || quadrosPorSegundo <= 0f
                ? 0
                : (int)(_tempoNoQuadro * quadrosPorSegundo) % quadros.Length;
            _renderizador.sprite = quadros[indice];
            bool espelharCorrida = _animacaoAtual == TipoAnimacao.Correr
                && _direcaoAtual == Direcao.Esquerda;
            bool espelharDano = _animacaoAtual == TipoAnimacao.Dano
                && _direcaoAtual == Direcao.Direita;
            _renderizador.flipX = espelharCorrida || espelharDano;
        }

        private void AtualizarForma(float delta, Vector3 velocidade, bool noChao, float velocidadeAndar)
        {
            if (!ValorFinito(_achatamento) || !ValorFinito(_velocidadeMola))
            {
                _achatamento = 0f;
                _velocidadeMola = 0f;
            }

            if (!ValorFinito(delta) || delta < 0f)
            {
                delta = 0f;
            }

            float limiteCompressao = Mathf.Clamp(achatamentoMaximo, 0f, 0.75f);
            float tempoRestante = Mathf.Min(delta, DeltaFormaMaximo);
            while (tempoRestante > 0f)
            {
                float passo = Mathf.Min(tempoRestante, PassoFormaMaximo);
                float forca = -_achatamento * 220f - _velocidadeMola * 14f;
                _velocidadeMola += forca * passo;
                _achatamento += _velocidadeMola * passo;
                _velocidadeMola = Mathf.Clamp(_velocidadeMola, -20f, 20f);
                _achatamento = Mathf.Clamp(_achatamento, -0.2f, limiteCompressao);
                tempoRestante -= passo;
            }

            transform.localScale = Vector3.Scale(
                _escalaInicial,
                new Vector3(1f + _achatamento * 0.5f, 1f - _achatamento, 1f + _achatamento * 0.5f)
            );

            float velocidadeAtual = new Vector2(velocidade.x, velocidade.z).magnitude;
            float proporcao = Mathf.Clamp01(velocidadeAtual / Mathf.Max(velocidadeAndar, 0.001f));
            float quique = 0f;
            if (noChao && proporcao > 0.05f)
            {
                _tempoQuique += delta * velocidadeAtual * frequenciaQuique;
                quique = Mathf.Abs(Mathf.Sin(_tempoQuique)) * alturaQuique * proporcao;
            }

            float recuoVisual = 0f;
            if (_tempoDano > 0f && _direcaoAtual == Direcao.Costas)
            {
                float progresso = 1f - _tempoDano / Mathf.Max(duracaoDano, 0.001f);
                recuoVisual = -afundamentoDanoCima * Mathf.Sin(progresso * Mathf.PI);
            }

            transform.localPosition = _posicaoInicial + new Vector3(0f, quique + recuoVisual, 0f);
        }

        private static bool ValorFinito(float valor)
        {
            return !float.IsNaN(valor) && !float.IsInfinity(valor);
        }

        private static Direcao EscolherDirecao(Vector2 direcao)
        {
            if (Mathf.Abs(direcao.x) >= Mathf.Abs(direcao.y))
            {
                return direcao.x >= 0f ? Direcao.Direita : Direcao.Esquerda;
            }

            return direcao.y >= 0f ? Direcao.Frente : Direcao.Costas;
        }
    }
}
