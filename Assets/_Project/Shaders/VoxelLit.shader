Shader "ARmonia/Blocks/VoxelLit"
{
    // ??????????????????????????????????????????????????????????????????????????
    //  VoxelLit.shader
    //  URP-compatible lit shader for voxel blocks.
    //  Features:
    //   • Point-sampled (pixelated) albedo texture — preserves pixel-art crispness.
    //   • Toon-stepped diffuse lighting (3 bands) — flat Minecraft-like shading.
    //   • Configurable vertex-colour ambient occlusion (darkens edges/corners).
    //   • Emission support for torch/glowing blocks.
    //   • Receives URP shadows and main directional light.
    // ??????????????????????????????????????????????????????????????????????????

    Properties
    {
        [MainTexture]
        _BaseMap        ("Albedo (pixel-art texture)", 2D)      = "white" {}

        [MainColor]
        _BaseColor      ("Tint", Color)                          = (1,1,1,1)

        _AmbientOcclusion ("AO Strength (vertex colour R)", Range(0,1)) = 0.6
        _ShadowStrength   ("Shadow Strength",               Range(0,1)) = 0.55
        _EmissionColor    ("Emission",  Color)                   = (0,0,0,1)
        _EmissionIntensity("Emission Intensity", Range(0,8))     = 0

        // Toon bands  ?????????????????????????????????????????????????????????
        _BandLight  ("Band – full light threshold",  Range(0,1)) = 0.6
        _BandMid    ("Band – mid light threshold",   Range(0,1)) = 0.25
        _MidScale   ("Mid band brightness",          Range(0,1)) = 0.55
        _ShadowScale("Shadow band brightness",       Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ?? Textures ??????????????????????????????????????????????????????
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);   // declared as point in material settings

            // ?? Constant buffer ???????????????????????????????????????????????
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _AmbientOcclusion;
                half   _ShadowStrength;
                half4  _EmissionColor;
                half   _EmissionIntensity;
                half   _BandLight;
                half   _BandMid;
                half   _MidScale;
                half   _ShadowScale;
            CBUFFER_END

            // ?? Vertex data ???????????????????????????????????????????????????
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                half4  vertexColor  : COLOR;        // R = baked AO (0=occluded)
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                half4  vertexColor  : COLOR;
                float  fogFactor    : TEXCOORD3;
            };

            // ?? Vertex shader ?????????????????????????????????????????????????
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.vertexColor = IN.vertexColor;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            // ?? Toon step helper ??????????????????????????????????????????????
            half ToonDiffuse(half NdotL, half bandLight, half bandMid,
                             half midScale, half shadowScale)
            {
                if (NdotL >= bandLight) return 1.0;
                if (NdotL >= bandMid)  return midScale;
                return shadowScale;
            }

            // ?? Fragment shader ???????????????????????????????????????????????
            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Albedo — point-sampled for pixel-art crispness.
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 2. Main directional light + shadows.
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalize(IN.normalWS), mainLight.direction));

                // Blend URP shadow attenuation with our strength control.
                half shadowAtten = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                half diffuse     = ToonDiffuse(NdotL * shadowAtten,
                                              _BandLight, _BandMid,
                                              _MidScale,  _ShadowScale);

                // 3. Ambient (URP SH).
                half3 ambient = SampleSH(normalize(IN.normalWS));

                // 4. Vertex-colour AO (R channel baked into mesh — 1=lit, 0=occluded).
                half ao = lerp(1.0, IN.vertexColor.r, _AmbientOcclusion);

                // 5. Compose.
                half3 color = albedo.rgb * (ambient + mainLight.color * diffuse) * ao;

                // 6. Emission.
                color += _EmissionColor.rgb * _EmissionIntensity;

                // 7. Fog.
                color = MixFog(color, IN.fogFactor);

                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // Shadow caster pass — required for blocks to cast shadows.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        // Depth-only pass — required for depth-based effects (SSAO, etc.).
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
