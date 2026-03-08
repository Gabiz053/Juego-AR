Shader "ARmonia/AR/ARPlane"
{
    Properties
    {
        _Sand0 ("Sand Tone 0 darkest",  Color) = (0.620, 0.592, 0.490, 0.90)
        _Sand1 ("Sand Tone 1 dark",     Color) = (0.745, 0.718, 0.608, 0.90)
        _Sand2 ("Sand Tone 2 mid",      Color) = (0.820, 0.796, 0.690, 0.90)
        _Sand3 ("Sand Tone 3 light",    Color) = (0.920, 0.898, 0.808, 0.90)
        _Sand4 ("Sand Tone 4 lightest", Color) = (0.965, 0.950, 0.890, 0.90)

        _GridMinor ("Minor Line Colour", Color) = (0.208, 0.192, 0.145, 0.30)
        _GridMajor ("Major Line Colour", Color) = (0.180, 0.161, 0.114, 0.55)

        _CellSize    ("Cell Size metres",         Float)        = 0.1
        _LineWidth   ("Line Width metres",        Float)        = 0.003
        _MajorEvery  ("Major line every N cells", Float)        = 10
        _GrainSize   ("Sand Grain Size metres",   Float)        = 0.02

        _PulseSpeed   ("Pulse Speed",             Float)        = 0.6
        _PulseAmt     ("Pulse Amount",            Range(0,0.3)) = 0.10
        _ShimmerSpeed ("Shimmer Speed major",     Float)        = 0.25
        _ShimmerAmt   ("Shimmer Amount major",    Range(0,0.4)) = 0.18

        _Opacity      ("Opacity",                 Range(0,1))   = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ARPlaneOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Sand0, _Sand1, _Sand2, _Sand3, _Sand4;
                half4 _GridMinor;
                half4 _GridMajor;
                float _CellSize;
                float _LineWidth;
                float _MajorEvery;
                float _GrainSize;
                float _PulseSpeed;
                float _PulseAmt;
                float _ShimmerSpeed;
                float _ShimmerAmt;
                float _Opacity;
            CBUFFER_END

            float4x4 _GridMatrix;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                return OUT;
            }

            float Hash(float2 c)
            {
                return frac(sin(dot(floor(c), float2(127.1, 311.7))) * 43758.5453);
            }

            float Hash2(float2 c)
            {
                return frac(sin(dot(floor(c), float2(269.5, 183.3))) * 17231.7891);
            }

            half4 SandColour(float2 xz, float gs,
                             half4 s0, half4 s1, half4 s2, half4 s3, half4 s4)
            {
                float2 cell = xz / gs;
                float  h    = frac(Hash(cell) * 0.6 + Hash2(cell) * 0.4);
                if      (h < 0.10) return s0;
                else if (h < 0.30) return s1;
                else if (h < 0.65) return s2;
                else if (h < 0.90) return s3;
                else               return s4;
            }

            float GridLine(float coord, float cellSize, float lineWidth)
            {
                float f  = frac(coord / cellSize + 0.5) - 0.5;
                float df = fwidth(coord / cellSize);
                float lw = max(lineWidth / cellSize, df);
                return 1.0 - smoothstep(lw * 0.5, lw * 0.5 + df, abs(f));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 lp = mul(_GridMatrix, float4(IN.positionWS, 1.0)).xyz;

                half4 col = SandColour(lp.xz, _GrainSize,
                                       _Sand0, _Sand1, _Sand2, _Sand3, _Sand4);

                float rawPulse     = sin(_Time.y * _PulseSpeed   * 6.2832) * 0.5 + 0.5;
                float pulse        = smoothstep(0.0, 1.0, rawPulse);
                float pulseScale   = 1.0 - _PulseAmt + _PulseAmt * pulse;

                float rawShimmer   = sin(_Time.y * _ShimmerSpeed * 6.2832 + 1.3) * 0.5 + 0.5;
                float shimmer      = smoothstep(0.0, 1.0, rawShimmer);
                float shimmerScale = 1.0 - _ShimmerAmt + _ShimmerAmt * shimmer;

                float gx    = GridLine(lp.x, _CellSize, _LineWidth);
                float gz    = GridLine(lp.z, _CellSize, _LineWidth);
                float minor = saturate(gx + gz);

                float mx    = GridLine(lp.x, _CellSize * _MajorEvery, _LineWidth * 1.6);
                float mz    = GridLine(lp.z, _CellSize * _MajorEvery, _LineWidth * 1.6);
                float major = saturate(mx + mz);

                col.rgb = lerp(col.rgb, _GridMinor.rgb, minor * _GridMinor.a * pulseScale);
                col.rgb = lerp(col.rgb, _GridMajor.rgb, major * _GridMajor.a * shimmerScale);
                col.a   = saturate(col.a * (0.97 + 0.03 * pulse));

                return col * _Opacity;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
