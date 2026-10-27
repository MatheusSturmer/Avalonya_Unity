using System;
using UnityEngine;

namespace Avalonya
{
    [Serializable]
    public sealed class CartaProgressao
    {
        public string id;
        public string titulo;
        [TextArea(2, 4)] public string descricao;
        public Sprite imagem;
    }
}
