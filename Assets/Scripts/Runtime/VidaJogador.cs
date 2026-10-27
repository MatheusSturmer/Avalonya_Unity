using System;
using UnityEngine;

namespace Avalonya
{
    public sealed class VidaJogador : MonoBehaviour
    {
        [SerializeField] private Jogador jogador;
        [SerializeField, Min(1f)] private float vidaMaxima = 12f;

        public event Action<float, float> VidaAlterada;
        public event Action<Vector3> DanoRecebido;
        public event Action Morreu;

        public float VidaAtual { get; private set; }
        public float VidaMaxima => vidaMaxima;
        public bool EstaMorto { get; private set; }

        private void Awake()
        {
            if (jogador == null)
            {
                jogador = GetComponent<Jogador>();
            }

            VidaAtual = vidaMaxima;
            VidaAlterada?.Invoke(VidaAtual, vidaMaxima);
        }

        public void ReceberDano(float quantidade, Vector3 origem)
        {
            if (EstaMorto || quantidade <= 0f)
            {
                return;
            }

            float vidaAnterior = VidaAtual;
            VidaAtual = Mathf.Max(0f, VidaAtual - quantidade);
            if (VidaAtual >= vidaAnterior)
            {
                return;
            }

            jogador?.ReceberDanoDe(origem);
            DanoRecebido?.Invoke(origem);
            VidaAlterada?.Invoke(VidaAtual, vidaMaxima);

            if (VidaAtual <= 0f)
            {
                EstaMorto = true;
                jogador?.DefinirMorto();
                Morreu?.Invoke();
            }
        }

        public void AumentarVidaMaxima(float quantidadeMaxima, float quantidadeRestaurada)
        {
            if (quantidadeMaxima <= 0f)
            {
                return;
            }

            vidaMaxima += quantidadeMaxima;
            VidaAtual = Mathf.Min(vidaMaxima, VidaAtual + Mathf.Max(0f, quantidadeRestaurada));
            VidaAlterada?.Invoke(VidaAtual, vidaMaxima);
        }

        public void CurarCompletamente()
        {
            if (EstaMorto)
            {
                return;
            }

            VidaAtual = vidaMaxima;
            VidaAlterada?.Invoke(VidaAtual, vidaMaxima);
        }

        public void DefinirVidaMaxima(float valor, bool curar)
        {
            vidaMaxima = Mathf.Max(1f, valor);
            VidaAtual = curar ? vidaMaxima : Mathf.Clamp(VidaAtual, 0f, vidaMaxima);
            EstaMorto = VidaAtual <= 0f;
            VidaAlterada?.Invoke(VidaAtual, vidaMaxima);
        }
    }
}
