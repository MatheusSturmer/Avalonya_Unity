using System.Collections.Generic;
using UnityEngine;

namespace Avalonya
{
    /// <summary>Mantém os inimigos ativos que podem receber dano durante a partida.</summary>
    internal static class RegistroAlvosDano
    {
        private static readonly List<IAlvoDano> Alvos = new List<IAlvoDano>(64);

        internal static int Quantidade => Alvos.Count;

        internal static IAlvoDano Obter(int indice)
        {
            return Alvos[indice];
        }

        internal static void Registrar(IAlvoDano alvo)
        {
            if (!EstaAtivo(alvo) || alvo.EstaMorto)
            {
                return;
            }

            RemoverInvalidos();
            for (int indice = 0; indice < Alvos.Count; indice++)
            {
                if (ReferenceEquals(Alvos[indice], alvo))
                {
                    return;
                }
            }

            Alvos.Add(alvo);
        }

        internal static void Remover(IAlvoDano alvo)
        {
            if (ReferenceEquals(alvo, null))
            {
                return;
            }

            for (int indice = Alvos.Count - 1; indice >= 0; indice--)
            {
                if (ReferenceEquals(Alvos[indice], alvo))
                {
                    Alvos.RemoveAt(indice);
                }
            }
        }

        internal static void RemoverInvalidos()
        {
            for (int indice = Alvos.Count - 1; indice >= 0; indice--)
            {
                IAlvoDano alvo = Alvos[indice];
                if (!EstaAtivo(alvo) || alvo.EstaMorto)
                {
                    Alvos.RemoveAt(indice);
                }
            }
        }

        private static bool EstaAtivo(IAlvoDano alvo)
        {
            if (ReferenceEquals(alvo, null))
            {
                return false;
            }

            MonoBehaviour componente = alvo as MonoBehaviour;
            return componente != null && componente.isActiveAndEnabled;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimparAoIniciarRuntime()
        {
            Alvos.Clear();
        }
    }
}
