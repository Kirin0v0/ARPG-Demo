using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class OutlineRenderPass : ScriptableRenderPass
    {
        private static readonly List<ShaderTagId> ShaderTagIds = new List<ShaderTagId>
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        private static readonly int ShaderPropertyOutlineMask = Shader.PropertyToID("_OutlineMask");

        private readonly Material _material;
        private readonly FilteringSettings _filteringSettings;
        private readonly MaterialPropertyBlock _materialPropertyBlock;

        private RTHandle _maskRT;

        public OutlineRenderPass(Material material = null, uint renderingLayerMask = 0)
        {
            _material = material;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, renderingLayerMask: renderingLayerMask);
            _materialPropertyBlock = new MaterialPropertyBlock();
            // 在后处理渲染阶段前执行
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public void Destroy()
        {
            _maskRT?.Release();
            _maskRT = null;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ResetTarget();
            // 分配渲染句柄
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            RenderingUtils.ReAllocateIfNeeded(ref _maskRT, descriptor, name: "_maskRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 从命令缓冲区中获取命令
            var commandBuffer = CommandBufferPool.Get("Outline Render Command");
            // 设置命令的渲染目标
            commandBuffer.SetRenderTarget(_maskRT);
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            // 设置命令的渲染列表
            var drawingSettings = CreateDrawingSettings(ShaderTagIds, ref renderingData, SortingCriteria.None);
            var rendererListParams =
                new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
            var rendererList = context.CreateRendererList(ref rendererListParams);
            commandBuffer.DrawRendererList(rendererList);
            // 绘制轮廓
            commandBuffer.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
            _materialPropertyBlock.SetTexture(ShaderPropertyOutlineMask, _maskRT);
            commandBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3, 1,
                _materialPropertyBlock);
            // 执行命令
            context.ExecuteCommandBuffer(commandBuffer);
            // 清空并回收命令
            commandBuffer.Clear();
            CommandBufferPool.Release(commandBuffer);
        }
    }
}