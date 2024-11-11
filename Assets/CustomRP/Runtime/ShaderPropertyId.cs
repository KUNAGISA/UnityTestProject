using UnityEngine;

namespace CustomRP
{
    internal static class ShaderPropertyId
    {
        public static int KernalSize = Shader.PropertyToID("_KernalSize");
        public static int Spread = Shader.PropertyToID("_Spread");
        public static int MainTex = Shader.PropertyToID("_MainTex");
    }
}