using System.Collections.Generic;
using UnityEngine;

namespace Avalonia
{
    [RequireComponent(typeof(JogadorAvalonia))]
    public sealed class MachadoAutomaticoAvalonia : MonoBehaviour
    {
        [SerializeField] private Sprite[] quadrosMachado;
        [SerializeField, Min(0f)] private float alturaLancamento = 0.65f;
        [SerializeField, Min(0f)] private float deslocamentoFrente = 0.5f;
        [SerializeField, Min(0f)] private float alcanceDepoisDoAlvo = 12f;
        [SerializeField, Min(0.1f)] private float raioAcerto = 0.65f;

        private EstadoJogoAvalonia _estado;
        private VidaJogadorAvalonia _vida;
        private float _tempoParaLancamento;
        private float _intervaloConhecido;
        private bool _desbloqueadoAnteriormente;
        private int _machadosLancados;

        public int MachadosLancados => _machadosLancados;

        private void Start()
        {
            _estado = EstadoJogoAvalonia.Instancia;
            _vida = GetComponent<VidaJogadorAvalonia>();
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

            List<IAlvoDanoAvalonia> alvos = EncontrarAlvosProximos(_estado.QuantidadeMachados);
            foreach (IAlvoDanoAvalonia alvo in alvos)
            {
                MonoBehaviour componente = alvo as MonoBehaviour;
                if (componente == null)
                {
                    continue;
                }

                Vector3 vetor = alvo.PontoDano - (transform.position + Vector3.up * alturaLancamento);
                vetor.y = 0f;
                float distancia = vetor.magnitude;
                Vector3 direcao = distancia > 0.0001f ? vetor / distancia : Vector3.forward;
                Vector3 origem = transform.position + Vector3.up * alturaLancamento + direcao * deslocamentoFrente;
                GameObject objeto = new GameObject("ProjetilMachadoAvalonia");
                objeto.transform.position = origem;
                SpriteRenderer renderizador = objeto.AddComponent<SpriteRenderer>();
                renderizador.sprite = quadrosMachado[0];
                ProjetilMachadoAvalonia projetil = objeto.AddComponent<ProjetilMachadoAvalonia>();
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

        private List<IAlvoDanoAvalonia> EncontrarAlvosProximos(int quantidade)
        {
            List<IAlvoDanoAvalonia> candidatos = new List<IAlvoDanoAvalonia>();
            MonoBehaviour[] componentes = FindObjectsByType<MonoBehaviour>();
            foreach (MonoBehaviour componente in componentes)
            {
                if (componente == null || !(componente is IAlvoDanoAvalonia alvo) || alvo.EstaMorto)
                {
                    continue;
                }

                if (candidatos.Contains(alvo))
                {
                    continue;
                }

                candidatos.Add(alvo);
            }

            candidatos.Sort((a, b) => DistanciaQuadrada(a).CompareTo(DistanciaQuadrada(b)));
            if (candidatos.Count > quantidade)
            {
                candidatos.RemoveRange(quantidade, candidatos.Count - quantidade);
            }

            return candidatos;
        }

        private float DistanciaQuadrada(IAlvoDanoAvalonia alvo)
        {
            Vector3 deslocamento = alvo.PontoDano - transform.position;
            deslocamento.y = 0f;
            return deslocamento.sqrMagnitude;
        }
    }
}
