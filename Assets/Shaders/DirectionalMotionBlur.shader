Shader "Hidden/DirectionalMotionBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "DirectionalMotionBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Globals set from C# — no Properties block on purpose
            float  _BlurAmount;
            float2 _BlurDir;

            #define SAMPLES 16

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                if (_BlurAmount <= 0.0001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                half4 col = 0;
                [unroll]
                for (int s = 0; s < SAMPLES; s++)
                {
                    float t = (s / (float)(SAMPLES - 1)) - 0.5;
                    col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + _BlurDir * t * _BlurAmount);
                }
                return col / SAMPLES;
            }
            ENDHLSL
        }
    }
}