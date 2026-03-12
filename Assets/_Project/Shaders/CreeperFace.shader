// ------------------------------------------------------------
//  CreeperFace.shader
//  Unlit transparent shader for the Creeper face overlay.
//  Maps a face texture onto the AR face mesh UVs.
// ------------------------------------------------------------

Shader "ARmonia/AR/CreeperFace"
{
    Properties
    {
        [MainTexture]
        _MainTex ("Creeper Face Texture", 2D) = "white" {}

        [MainColor]
        _Color   ("Tint", Color) = (1, 1, 1, 1)

        _Alpha   ("Overall Alpha", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "CreeperFaceOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _Alpha;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 posOS : POSITION;
                float2 uv    : TEXCOORD0;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float  fog   : TEXCOORD1;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.posCS = TransformObjectToHClip(i.posOS.xyz);
                o.uv    = TRANSFORM_TEX(i.uv, _MainTex);
                o.fog   = ComputeFogFactor(o.posCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 col = tex * _Color;
                col.a *= _Alpha;
                col.rgb = MixFog(col.rgb, i.fog);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
