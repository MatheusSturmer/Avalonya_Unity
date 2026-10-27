using System.Collections.Generic;
using UnityEngine;

namespace Avalonya
{
    [RequireComponent(typeof(Jogador))]
    public sealed class MachadoAutomatico : MonoBehaviour
    {
        [SerializeField] private Sprite[] quadrosMachado;
        [SerializeField, Min(0f)] private float alturaLancamento = 0.65f;
        [SerializeField, Min(0f)] private float deslocamentoFrente = 0.5f;
        [SerializeField, Min(0f)] private float alcanceDepoisDoAlvo = 12f;
        [SerializeField, Min(0.1f)] private float raioAcerto = 0.65f;

        private readonly List<AlvoComDistancia> _alvosMaisProximos = new List<AlvoComDistancia>(3);
        private EstadoJogo _estado;
        private VidaJogador _vida;
        private float _tempoParaLancamento;
        private float _intervaloConhecido;
        private bool _desbloqueadoAnteriormente;
        private int _machadosLancados;

        public int MachadosLancados => _machadosLancados;

        private void Start()
        {
            _estado = EstadoJogo.Instancia;
            _vida = GetComponent<VidaJogador>();
            _intervaloConhecido = _estado != null ? _estado.IntervaloMachado : 8f;
            _desbloqueadoAnteriormente = _estado != null && _estado.MachadoDesbloqueado;
            if (_desbloqueadoAnteriormente)
            {
                _tempoParaLancamento = _intervaloConhecido;
            }
        }

        private void Update()
        {
            if (_estado == null || _vida == null || _vida.EstaMorto)
            {
                return;
            }

            if (!_estado.MachadoDesbloqueado)
            {
                _desbloqueadoAnteriormente = false;
                return;
            }

            if (!_desbloqueadoAnteriormente)
            {
                _desbloqueadoAnteriormente = true;
                _intervaloConhecido = _estado.IntervaloMachado;
                _tempoParaLancamento = _intervaloConhecido;
                return;
            }

            if (!Mathf.Approximately(_intervaloConhecido, _estado.IntervaloMachado))
            {
                float fracaoRestante = _intervaloConhecido > 0f
                    ? Mathf.Clamp01(_tempoParaLancamento / _intervaloConhecido)
                    : 1f;
                _intervaloConhecido = _estado.IntervaloMachado;
                _tempoParaLancamento = _intervaloConhecido * fracaoRestante;
            }

            _tempoParaLancamento -= Time.deltaTime;
            if (_tempoParaLancamento <= 0f)
            {
                DispararAgora();
            }
        }

        /// <summary>Arremessa contra os inimigos vivos mais próximos após o tempo de recarga.</summary>
        public void DispararAgora()
        {
            if (_estado == null || !_estado.MachadoDesbloqueado || _vida == null || _vida.EstaMorto
                || quadrosMachado == null || quadrosMachado.Length == 0)
            {
                return;
            }

            EncontrarAlvosProximos(_estado.QuantidadeMachados);
            for (int indice = 0; indice < _alvosMaisProximos.Count; indice++)
            {
                IAlvoDano alvo = _alvosMaisProximos[indice].Alvo;
                MonoBehaviour componente = alvo as MonoBehaviour;
                if (componente == null || alvo.EstaMorto)
                {
                    continue;
                }

                Vector3 vetor = alvo.PontoDano - (transform.position + Vector3.up * alturaLancamento);
                vetor.y = 0f;
                float distancia = vetor.magnitude;
                Vector3 direcao = distancia > 0.0001f ? vetor / distancia : Vector3.forward;
                Vector3 origem = transform.position + Vector3.up * alturaLancamento + direcao * deslocamentoFrente;
                GameObject objeto = new GameObject("ProjetilMachado");
                objeto.transform.position = origem;
                SpriteRenderer renderizador = objeto.AddComponent<SpriteRenderer>();
                renderizador.sprite = quadrosMachado[0];
                ProjetilMachado projetil = objeto.AddComponent<ProjetilMachado>();
                projetil.Iniciar(
                    quadrosMachado,
                    direcao,
                    _estado.DanoMachado + _estado.DanoExtraMachado,
                    _estado.VelocidadeMachado,
                    Mathf.Max(1f, distancia - deslocamentoFrente) + alcanceDepoisDoAlvo,
                    raioAcerto,
                    transform
                );
                _machadosLancados++;
            }

            _intervaloConhecido = _estado.IntervaloMachado;
            _tempoParaLancamento = _intervaloConhecido;
        }

        private void EncontrarAlvosProximos(int quantidade)
        {
            _alvosMaisProximos.Clear();
            if (quantidade <= 0)
            {
                return;
            }

            RegistroAlvosDano.RemoverInvalidos();
            for (int indice = 0; indice < RegistroAlvosDano.Quantidade; indice++)
            {
                IAlvoDano alvo = RegistroAlvosDano.Obter(indice);
                MonoBehaviour componente = alvo as MonoBehaviour;
                if (componente == null || alvo.EstaMorto)
                {
                    continue;
                }

                InserirAlvoOrdenado(alvo, DistanciaQuadrada(alvo), quantidade);
            }
        }

        private void InserirAlvoOrdenado(IAlvoDano alvo, float distanciaQuadrada, int quantidade)
        {
            int posicao = _alvosMaisProximos.Count;
            while (posicao > 0 && _alvosMaisProximos[posicao - 1].DistanciaQuadrada > distanciaQuadrada)
            {
                posicao--;
            }

            if (posicao >= quantidade && _alvosMaisProximos.Count >= quantidade)
            {
                return;
            }

            _alvosMaisProximos.Insert(posicao, new AlvoComDistancia(alvo, distanciaQuadrada));
            if (_alvosMaisProximos.Count > quantidade)
            {
                _alvosMaisProximos.RemoveAt(_alvosMaisProximos.Count - 1);
            }
        }

        private float DistanciaQuadrada(IAlvoDano alvo)
        {
            Vector3 deslocamento = alvo.PontoDano - transform.position;
            deslocamento.y = 0f;
            return deslocamento.sqrMagnitude;
        }

        private readonly struct AlvoComDistancia
        {
            public readonly IAlvoDano Alvo;
            public readonly float DistanciaQuadrada;

            public AlvoComDistancia(IAlvoDano alvo, float distanciaQuadrada)
            {
                Alvo = alvo;
                DistanciaQuadrada = distanciaQuadrada;
            }
        }
    }
}
