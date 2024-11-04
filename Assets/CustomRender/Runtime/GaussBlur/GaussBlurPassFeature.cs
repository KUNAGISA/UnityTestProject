using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRender.Runtime
{
    internal class GaussBlurPassFeature : ScriptableRendererFeature
    {
        class GaussBlurPass : ScriptableRenderPass
        {
            private class PassData
            {
                public Material Material;
                public TextureHandle Source;
                public TextureHandle Destination;
            }

            private static void ExecutePass(PassData data, UnsafeGraphContext context)
            {
                var gaussBlur = VolumeManager.instance.stack.GetComponent<GaussBlur>();
                var propertyBlock = context.renderGraphPool.GetTempMaterialPropertyBlock();

                propertyBlock.SetInt(ShaderPropertyId.KernalSize, gaussBlur.KernalSize.value);
                propertyBlock.SetFloat(ShaderPropertyId.Spread, gaussBlur.Spread.value);

                context.cmd.SetRenderTarget(data.Destination);
                propertyBlock.SetTexture(ShaderPropertyId.MainTex, data.Source);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1, propertyBlock);

                context.cmd.SetRenderTarget(data.Source);
                propertyBlock.SetTexture(ShaderPropertyId.MainTex, data.Destination);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 1, MeshTopology.Triangles, 3, 1, propertyBlock);
            }

            private readonly Material m_material = null;

            public GaussBlurPass(Material material)
            {
                m_material = material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var gaussBlur = VolumeManager.instance.stack.GetComponent<GaussBlur>();

                var targetDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                targetDesc.name = "GaussBlurPassFeature";
                targetDesc.clearBuffer = false;

                var source = resourceData.activeColorTexture;
                var destination = renderGraph.CreateTexture(targetDesc);

                using var builder = renderGraph.AddUnsafePass<PassData>("Gauss Blur Pass", out var passData, profilingSampler);

                passData.Material = m_material;
                passData.Source = source;
                passData.Destination = destination;

                builder.UseTexture(source, AccessFlags.ReadWrite);
                builder.UseTexture(destination, AccessFlags.ReadWrite);

                builder.AllowPassCulling(false);
                builder.SetRenderFunc<PassData>(ExecutePass);
            }
        }

        [SerializeField]
        private Material m_material = null;

        private GaussBlurPass m_scriptablePass;

        public override void Create()
        {
            m_scriptablePass = new GaussBlurPass(m_material);
            m_scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var gaussBlur = VolumeManager.instance.stack.GetComponent<GaussBlur>();
            if (m_material && gaussBlur.IsActive())
            {
                renderer.EnqueuePass(m_scriptablePass);
            }
        }
    }
}