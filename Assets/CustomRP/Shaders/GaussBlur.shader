Shader "Custom/GaussBlur"
{
    Properties
    {

    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off

        HLSLINCLUDE

        #define USE_FULL_PRECISION_BLIT_TEXTURE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

        static const float E = 2.71828;

        CBUFFER_START(UnityPerMaterial)
        int _KernalSize;
        float _Spread;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        uniform float4 _MainTex_TexelSize;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

            output.positionCS = pos;
            output.texcoord   = uv;

            return output;
        }

        float Gaussian(int x)
        {
            float sigmaSqu = _Spread * _Spread;
            return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
        }

        float4 GaussBlur(Varyings input, float2 dir)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = input.texcoord.xy;
            float4 color = float4(0.0, 0.0, 0.0, 1.0);
            float2 texlSize = _MainTex_TexelSize.xy * dir;
            float kernelSum = 0.0;

            for(int x = -_KernalSize; x <= _KernalSize; ++x)
            {
                float gauss = Gaussian(x);
                kernelSum += gauss;
                color += gauss * SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + x * texlSize);
            }

            return color / kernelSum;
        }

        ENDHLSL

        Pass
        {
            Name "GaussBlurHorizontal"

            HLSLPROGRAM
           
            #pragma vertex Vert
            #pragma fragment FragGaussBlurHorizontal

            float4 FragGaussBlurHorizontal(Varyings input) : SV_Target0
            {
                return GaussBlur(input, float2(1, 0));
            }

            ENDHLSL
        }

        Pass
        {
            Name "GaussBlurVertical"

            HLSLPROGRAM
           
            #pragma vertex Vert
            #pragma fragment FragGaussBlurVertical

            float4 FragGaussBlurVertical(Varyings input) : SV_Target0
            {
                return GaussBlur(input, float2(0, 1));
            }

            ENDHLSL
        }
    }
}
