using System.Collections.Generic;
using UnityEngine;

namespace Avalonya
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ProjetilMachado : MonoBehaviour
    {
        private readonly Collider[] _colisores = new Collider[24];
        private readonly HashSet<MonoBehaviour> _alvosAtingidos = new HashSet<MonoBehaviour>();
        private SpriteRenderer _renderizador;
        private Sprite[] _quadros;
        private Transform _autor;
        private Vector3 _direcao;
        private float _dano;
        private float _velocidade;
        private float _distanciaMaxima;
        private float _raioAcerto;
        private float _distanciaPercorrida;
        private float _tempoAnimacao;
        private bool _iniciado;

        public int AlvosAtingidos => _alvosAtingidos.Count;
        public float DistanciaPercorrida => _distanciaPercorrida;

        private void Awake()
        {
            _renderizador = GetComponent<SpriteRenderer>();
        }

        public void Iniciar(Sprite[] quadros, Vector3 direcao, float dano, float velocidade, float distanciaMaxima, float raioAcerto, Transform autor)
        {
            if (quadros == null || quadros.Length == 0 || direcao.sqrMagnitude < 0.0001f)
            {
                Destroy(gameObject);
                return;
            }

            _quadros = quadros;
            _direcao = new Vector3(direcao.x, 0f, direcao.z).normalized;
            _dano = Mathf.Max(0f, dano);
            _velocidade = Mathf.Max(0.1f, velocidade);
            _distanciaMaxima = Mathf.Max(1f, distanciaMaxima);
            _raioAcerto = Mathf.Max(0.1f, raioAcerto);
            _autor = autor;
            _renderizador.sprite = _quadros[0];
            _renderizador.sortingOrder = 12;
            _iniciado = true;
            AtualizarOrientacaoCamera();
        }

        private void Update()
        {
            if (!_iniciado)
            {
                return;
            }

            if (_autor == null)
            {
                Destroy(gameObject);
                return;
            }

            VidaJogador vida = _autor.GetComponent<VidaJogador>();
            if (vida != null && vida.EstaMorto)
            {
                Destroy(gameObject);
                return;
            }

            float delta = Time.deltaTime;
            Vector3 passo = _direcao * (_velocidade * delta);
            transform.position += passo;
            _distanciaPercorrida += passo.magnitude;
            _tempoAnimacao += delta;

            if (_quadros.Length > 1)
            {
                int quadro = Mathf.FloorToInt(_tempoAnimacao * 10f) % _quadros.Length;
                _renderizador.sprite = _quadros[quadro];
            }

            AtualizarOrientacaoCamera();
            AtingirInimigos();
            if (_distanciaPercorrida >= _distanciaMaxima)
            {
                Destroy(gameObject);
            }
        }

        private void AtingirInimigos()
        {
            int quantidade = Physics.OverlapSphereNonAlloc(transform.position, _raioAcerto, _colisores, ~0, QueryTriggerInteraction.Ignore);
            for (int indice = 0; indice < quantidade; indice++)
            {
                Collider colisor = _colisores[indice];
                if (colisor == null)
                {
                    continue;
                }

                MonoBehaviour[] componentes = colisor.GetComponentsInParent<MonoBehaviour>();
                foreach (MonoBehaviour componente in componentes)
                {
                    if (!(componente is IAlvoDano alvo) || alvo.EstaMorto)
                    {
                        continue;
                    }

                    if (_alvosAtingidos.Add(componente))
                    {
                        alvo.ReceberDano(_dano, transform.position, _autor.gameObject);
                        if (!alvo.EstaMorto && componente is IRecuoMachado recuo)
                        {
                            Vector3 lateral = new Vector3(-_direcao.z, 0f, _direcao.x);
                            float lado = Vector3.Dot(lateral, componente.transform.position - transform.position) >= 0f ? 1f : -1f;
                            Vector3 impulso = (_direcao + lateral * (lado * 0.7f)).normalized * 8f;
                            recuo.RedirecionarRecuoMachado(impulso);
                        }
                    }

                    break;
                }

                _colisores[indice] = null;
            }
        }

        private void AtualizarOrientacaoCamera()
        {
            Camera cameraPrincipal = Camera.main;
            if (cameraPrincipal == null)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(-cameraPrincipal.transform.forward, cameraPrincipal.transform.up);
        }
    }
}
