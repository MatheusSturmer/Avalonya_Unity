using System;
using UnityEngine;
using UnityEngine.UI;

namespace Avalonia
{
    public sealed class VisualCartaAvalonia : MonoBehaviour
    {
        [SerializeField] private Image imagemCarta;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoDescricao;
        [SerializeField] private Button botao;

        public void Configurar(CartaProgressaoAvalonia carta, Action<string> aoSelecionar)
        {
            imagemCarta.sprite = carta.imagem;
            imagemCarta.preserveAspect = true;
            textoTitulo.text = carta.titulo;
            textoDescricao.text = carta.descricao;
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(() => aoSelecionar(carta.id));
        }
    }
}
