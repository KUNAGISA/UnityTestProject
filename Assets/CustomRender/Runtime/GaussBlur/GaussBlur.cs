using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRender.Runtime
{
    [Serializable, VolumeComponentMenu("Custom/GaussBlur")]
    [VolumeRequiresRendererFeatures(typeof(GaussBlurPassFeature))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class GaussBlur : VolumeComponent, IPostProcessComponent
    {
        public ClampedIntParameter KernalSize = new ClampedIntParameter(0, 0, 10);

        public FloatParameter Spread = new FloatParameter(0f);

        public bool IsActive() => KernalSize.value > 0 && Spread.value > float.Epsilon;
    }
}
