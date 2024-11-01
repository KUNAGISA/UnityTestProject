using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRender.Runtime
{
    internal class GaussBlurPassFeature : ScriptableRendererFeature
    {
        class CustomRenderPass : ScriptableRenderPass
        {
            // This class stores the data needed by the RenderGraph pass.
            // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
            private class PassData
            {
                public Material Material;
                public TextureHandle Source;
                public TextureHandle Destination;
            }

            static readonly MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

            private static void ExecutePass(PassData data, UnsafeGraphContext context)
            {
                var gaussBlur = VolumeManager.instance.stack.GetComponent<GaussBlur>();
                s_PropertyBlock.SetInt("_KernalSize", gaussBlur.KernalSize.value);
                s_PropertyBlock.SetFloat("_Spread", gaussBlur.Spread.value);

                context.cmd.SetRenderTarget(data.Destination);
                s_PropertyBlock.SetTexture("_MainTex", data.Source);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1, s_PropertyBlock);

                context.cmd.SetRenderTarget(data.Source);
                s_PropertyBlock.SetTexture("_MainTex", data.Destination);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 1, MeshTopology.Triangles, 3, 1, s_PropertyBlock);

                s_PropertyBlock.Clear();
            }

            private Material m_material = null;

            public void Setup(Material material)
            {
                m_material = material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                var targetDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                targetDesc.name = "GaussBlurPassFeature";
                targetDesc.clearBuffer = false;

                var source = resourceData.activeColorTexture;
                var destination = renderGraph.CreateTexture(targetDesc);

                using var builder = renderGraph.AddUnsafePass<PassData>("Gauss Blur Pass", out var passData);
                {
                    passData.Material = m_material;
                    passData.Source = source;
                    passData.Destination = destination;

                    builder.UseTexture(source, AccessFlags.ReadWrite);
                    builder.UseTexture(destination, AccessFlags.ReadWrite);

                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc<PassData>(ExecutePass);
                }
            }
        }

        [SerializeField]
        private Material m_material = null;

        private CustomRenderPass m_scriptablePass;

        /// <inheritdoc/>
        public override void Create()
        {
            m_scriptablePass = new CustomRenderPass();
            m_scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var gaussBlur = VolumeManager.instance.stack.GetComponent<GaussBlur>();
            if (m_material && gaussBlur.IsActive())
            {
                m_scriptablePass.Setup(m_material);
                renderer.EnqueuePass(m_scriptablePass);
            }
        }
    }
}