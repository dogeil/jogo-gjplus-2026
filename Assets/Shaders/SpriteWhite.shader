// Silhueta de sprite: ignora as cores da textura e usa só o alpha dela + a cor do SpriteRenderer (branco por padrão).
// Usado pelo SpriteOutline. URP 2D Renderer.
Shader "Custom/SpriteWhite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes a)
            {
                Varyings v;
                v.pos = TransformObjectToHClip(a.pos.xyz);
                v.col = a.col;
                v.uv = a.uv;
                return v;
            }

            half4 frag(Varyings v) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, v.uv).a * v.col.a;
                return half4(v.col.rgb, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes a)
            {
                Varyings v;
                v.pos = TransformObjectToHClip(a.pos.xyz);
                v.col = a.col;
                v.uv = a.uv;
                return v;
            }

            half4 frag(Varyings v) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, v.uv).a * v.col.a;
                return half4(v.col.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
