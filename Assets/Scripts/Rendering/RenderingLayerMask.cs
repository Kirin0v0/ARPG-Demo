using System;
using UnityEngine;

namespace Rendering
{
    [Serializable]
    public class RenderingLayerMask
    {
        [SerializeField] private uint layerMask;
        public uint LayerMask => layerMask;
    }
}