using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class OutlineRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private RenderingLayerMask renderingLayerMask;

        private OutlineRenderPass _outlineRenderPass;

        private bool IsMaterialValid => outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported;

        public override void Create()
        {
            if (!IsMaterialValid)
            {
                return;
            }

            _outlineRenderPass = new OutlineRenderPass(outlineMaterial, renderingLayerMask.LayerMask);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_outlineRenderPass == null)
            {
                return;
            }

            renderer.EnqueuePass(_outlineRenderPass);
        }

        protected override void Dispose(bool disposing)
        {
            _outlineRenderPass?.Destroy();
        }
    }
}