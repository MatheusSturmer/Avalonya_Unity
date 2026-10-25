using System.Collections.Generic;
using UnityEngine;

namespace Avalonia
{
    /// <summary>Volume invisível que desacelera corpos e estreita o campo de visão.</summary>
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class AreaAguaAvalonia : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float velocidadeAgua = 2f;
        [SerializeField, Range(1f, 179f)] private float campoVisaoAgua = 50f;

        private static readonly Dictionary<MonoBehaviour, int> ContagemAreasPorCorpo =
            new Dictionary<MonoBehaviour, int>();
        private readonly HashSet<MonoBehaviour> _corposNestaArea = new HashSet<MonoBehaviour>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimparEstadoEstatico()
        {
            ContagemAreasPorCorpo.Clear();
        }

        private void Awake()
        {
            BoxCollider colisor = GetComponent<BoxCollider>();
            colisor.isTrigger = true;

            Rigidbody corpo = GetComponent<Rigidbody>();
            corpo.isKinematic = true;
            corpo.useGravity = false;
        }

        private void Update()
        {
            if (_corposNestaArea.Count == 0)
            {
                return;
            }

            List<MonoBehaviour> removidos = null;
            foreach (MonoBehaviour corpo in _corposNestaArea)
            {
                if (corpo != null)
                {
                    continue;
                }

                removidos ??= new List<MonoBehaviour>();
                removidos.Add(corpo);
            }

            if (removidos == null)
            {
                return;
            }

            foreach (MonoBehaviour corpo in removidos)
            {
                _corposNestaArea.Remove(corpo);
                AtualizarContagem(corpo, false, corpoAindaExiste: false);
            }
        }

        private void OnTriggerEnter(Collider outro)
        {
            MonoBehaviour corpo = outro.GetComponentInParent<JogadorAvalonia>();
            if (corpo == null)
            {
                corpo = outro.GetComponentInParent<InimigoSlimeAvalonia>();
            }

            if (corpo == null)
            {
                corpo = outro.GetComponentInParent<InimigoAranhaAvalonia>();
            }

            if (corpo == null || !(corpo is ICorpoAguaAvalonia) || !_corposNestaArea.Add(corpo))
            {
                return;
            }

            AtualizarContagem(corpo, true, corpoAindaExiste: true);
        }

        private void OnTriggerExit(Collider outro)
        {
            MonoBehaviour corpo = outro.GetComponentInParent<JogadorAvalonia>();
            if (corpo == null)
            {
                corpo = outro.GetComponentInParent<InimigoSlimeAvalonia>();
            }

            if (corpo == null)
            {
                corpo = outro.GetComponentInParent<InimigoAranhaAvalonia>();
            }

            if (corpo != null && _corposNestaArea.Remove(corpo))
            {
                AtualizarContagem(corpo, false, corpoAindaExiste: true);
            }
        }

        private void OnDisable()
        {
            foreach (MonoBehaviour corpo in _corposNestaArea)
            {
                AtualizarContagem(corpo, false, corpo != null);
            }

            _corposNestaArea.Clear();
        }

        private void AtualizarContagem(MonoBehaviour corpo, bool entrou, bool corpoAindaExiste)
        {
            if (ReferenceEquals(corpo, null))
            {
                return;
            }

            ContagemAreasPorCorpo.TryGetValue(corpo, out int contagem);
            if (entrou)
            {
                if (contagem == 0 && corpo is ICorpoAguaAvalonia alvo)
                {
                    alvo.EntrarNaAgua(velocidadeAgua, campoVisaoAgua);
                }

                ContagemAreasPorCorpo[corpo] = contagem + 1;
                return;
            }

            if (contagem <= 1)
            {
                ContagemAreasPorCorpo.Remove(corpo);
                if (corpoAindaExiste && corpo is ICorpoAguaAvalonia alvo)
                {
                    alvo.SairDaAgua();
                }
            }
            else
            {
                ContagemAreasPorCorpo[corpo] = contagem - 1;
            }
        }
    }
}
