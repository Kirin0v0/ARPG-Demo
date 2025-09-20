using Sirenix.Utilities;
using UnityEngine;

namespace Rendering
{
    public static class RenderingUtil
    {
        public static void AddRenderingLayerMask(GameObject root, LayerMask physicsLayerMask,
            RenderingLayerMask renderingLayerMask)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            renderers.ForEach(childRenderer =>
            {
                if ((1 << childRenderer.gameObject.layer & physicsLayerMask) == 0)
                {
                    return;
                }

                childRenderer.renderingLayerMask |= renderingLayerMask.LayerMask;
            });
        }

        public static void RemoveRenderingLayerMask(GameObject root, LayerMask physicsLayerMask,
            RenderingLayerMask renderingLayerMask)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            renderers.ForEach(childRenderer =>
            {
                if ((1 << childRenderer.gameObject.layer & physicsLayerMask) == 0)
                {
                    return;
                }

                childRenderer.renderingLayerMask &= ~renderingLayerMask.LayerMask;
            });
        }
    }
}