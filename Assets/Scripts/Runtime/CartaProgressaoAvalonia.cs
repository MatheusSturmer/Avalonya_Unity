using System;
using UnityEngine;

namespace Avalonia
{
    [Serializable]
    public sealed class CartaProgressaoAvalonia
    {
        public string id;
        public string titulo;
        [TextArea(2, 4)] public string descricao;
        public Sprite imagem;
    }
}
