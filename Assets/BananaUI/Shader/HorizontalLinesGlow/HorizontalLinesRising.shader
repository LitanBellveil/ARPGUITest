Shader "Custom/HorizontalLinesRising"
{
    Properties
    {
        // Required by UI.Canvas — not sampled in the shader
        _MainTex            ("Base (unused)", 2D) = "white" {}

        [Header(Lines)]
        _LineCount          ("Line Count",              Range(1, 16))     = 8
        _LineSpacingUV      ("Line Spacing (UV)",       Range(0.02, 0.3)) = 0.10

        [Header(Base Line)]
        _BaseSigmaPx1       ("Base Sigma px L1",        Range(0.5,  12.0)) = 2.0
        _BaseSigmaPx2       ("Base Sigma px L2",        Range(2.0,  40.0)) = 10.0
        _BaseSigmaPx3       ("Base Sigma px L3",        Range(6.0,  80.0)) = 32.0
        _BaseGlowInt1       ("Base Glow Int 1",         Range(0.0,  3.0))  = 2.5
        _BaseGlowInt2       ("Base Glow Int 2",         Range(0.0,  2.0))  = 0.9
        _BaseGlowInt3       ("Base Glow Int 3",         Range(0.0,  1.0))  = 0.25
        _BaseAlpha          ("Base Alpha",              Range(0.0,  1.0))  = 1.0

        [Header(Thin Rising Line)]
        _ThinSigmaPx1       ("Thin Sigma px L1",        Range(0.5,  8.0))  = 1.5
        _ThinSigmaPx2       ("Thin Sigma px L2",        Range(1.5, 20.0))  = 5.0
        _ThinSigmaPx3       ("Thin Sigma px L3",        Range(4.0, 50.0))  = 16.0
        _ThinGlowInt1       ("Thin Glow Int 1",         Range(0.0,  3.0))  = 2.0
        _ThinGlowInt2       ("Thin Glow Int 2",         Range(0.0,  2.0))  = 0.65
        _ThinGlowInt3       ("Thin Glow Int 3",         Range(0.0,  1.0))  = 0.18
        // TravelLength is now auto = LineSpacingUV, this controls how much of that gap to use
        _TravelRatio        ("Travel Ratio (0-1)",      Range(0.1,  1.0))  = 0.92
        _ScrollSpeed        ("Scroll Speed",            Range(0.0,  5.0))  = 0.70
        _FadeCurve          ("Fade Curve Power",        Range(0.3,  6.0))  = 1.6
        _StaggerSeed        ("Stagger Seed",            Range(0.0, 10.0))  = 3.7
        _PhasesPerLine      ("Phases Per Line",         Range(1,    5))    = 3

        [Header(Render Target)]
        _TexelSizeY         ("Texel Size Y (1/height)", Float) = 0.000926

        [Header(Colors)]
        _CoreColor          ("Core Color",              Color) = (0.97, 0.92, 1.00, 1.0)
        _GlowColorInner     ("Glow Inner Color",        Color) = (0.72, 0.50, 1.00, 1.0)
        _GlowColorOuter     ("Glow Outer Color",        Color) = (0.28, 0.08, 0.72, 1.0)

        [Header(Output)]
        _BrightnessMult     ("Brightness Mult",         Range(0.1, 4.0))   = 1.2
        _AlphaGamma         ("Alpha Gamma",             Range(0.3, 2.5))   = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent+1"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "HorizontalLinesRising"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   4.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _LineCount;
                float  _LineSpacingUV;
                float  _BaseSigmaPx1;
                float  _BaseSigmaPx2;
                float  _BaseSigmaPx3;
                float  _BaseGlowInt1;
                float  _BaseGlowInt2;
                float  _BaseGlowInt3;
                float  _BaseAlpha;
                float  _ThinSigmaPx1;
                float  _ThinSigmaPx2;
                float  _ThinSigmaPx3;
                float  _ThinGlowInt1;
                float  _ThinGlowInt2;
                float  _ThinGlowInt3;
                float  _TravelRatio;
                float  _ScrollSpeed;
                float  _FadeCurve;
                float  _StaggerSeed;
                float  _PhasesPerLine;
                float  _TexelSizeY;
                float4 _CoreColor;
                float4 _GlowColorInner;
                float4 _GlowColorOuter;
                float  _BrightnessMult;
                float  _AlphaGamma;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float Hash(float n) { return frac(sin(n * 91.317 + 0.1) * 47453.5453); }

            float Gpx(float dPx, float sigmaPx)
            {
                float s = max(sigmaPx, 1.5);
                return exp(-(dPx * dPx) / (s * s));
            }

            float3 GlowLinePx(float dPx,
                              float s1, float s2, float s3,
                              float i1, float i2, float i3,
                              float alpha)
            {
                return float3(
                    Gpx(dPx, s1) * i1 * alpha,
                    Gpx(dPx, s2) * i2 * alpha,
                    Gpx(dPx, s3) * i3 * alpha
                );
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pixY   = IN.uv.y;
                float uvToPx = 1.0 / max(_TexelSizeY, 0.000001);

                // Travel is capped to spacing so thin lines NEVER cross into next base line
                float travelMax = _LineSpacingUV * clamp(_TravelRatio, 0.05, 1.0);

                float coreAcc = 0.0;
                float nearAcc = 0.0;
                float farAcc  = 0.0;

                int n      = (int)clamp(_LineCount,     1.0, 16.0);
                int phases = (int)clamp(_PhasesPerLine, 1.0,  5.0);

                for (int i = 0; i < 16; i++)
                {
                    if (i >= n) break;
                    float fi = (float)i;

                    // Evenly spaced base lines — UV 0 to (n-1)*spacing
                    // Use exact arithmetic: no accumulated float error
                    float baseY = fi * _LineSpacingUV;

                    // ── BASE LINE ──────────────────────────────────────
                    float dBasePx = abs(pixY - baseY) * uvToPx;
                    float3 base   = GlowLinePx(dBasePx,
                                    _BaseSigmaPx1, _BaseSigmaPx2, _BaseSigmaPx3,
                                    _BaseGlowInt1, _BaseGlowInt2, _BaseGlowInt3,
                                    _BaseAlpha);
                    coreAcc += base.x;
                    nearAcc += base.y;
                    farAcc  += base.z;

                    // ── THIN RISING LINES ──────────────────────────────
                    // Each thin line rises from baseY to baseY + travelMax
                    // travelMax <= _LineSpacingUV  →  never reaches next base line
                    for (int p = 0; p < 5; p++)
                    {
                        if (p >= phases) break;

                        float stagger = Hash(fi * 3.7 + (float)p * 1.3 + _StaggerSeed);
                        float phase   = frac(_Time.y * _ScrollSpeed + stagger);

                        // position strictly within [baseY, baseY + travelMax]
                        float thinY   = baseY + phase * travelMax;

                        // alpha: 1 at phase=0, 0 at phase=1 — only opacity changes
                        float a = 1.0 - pow(phase, _FadeCurve);
                        a      *= smoothstep(0.0, 0.04, phase); // no pop at birth
                        a       = saturate(a);

                        float dThinPx = abs(pixY - thinY) * uvToPx;
                        float3 thin   = GlowLinePx(dThinPx,
                                        _ThinSigmaPx1, _ThinSigmaPx2, _ThinSigmaPx3,
                                        _ThinGlowInt1, _ThinGlowInt2, _ThinGlowInt3,
                                        a);
                        coreAcc += thin.x;
                        nearAcc += thin.y;
                        farAcc  += thin.z;
                    }
                }

                coreAcc = saturate(coreAcc);
                nearAcc = saturate(nearAcc);
                farAcc  = saturate(farAcc);

                float3 col = float3(0.0, 0.0, 0.0);
                col += _GlowColorOuter.rgb * farAcc;
                col += _GlowColorInner.rgb * nearAcc;
                col += _CoreColor.rgb      * coreAcc * _BrightnessMult;
                col += float3(1.0, 0.98, 1.0) * pow(coreAcc, 2.5) * _BrightnessMult * 0.45;
                col  = col * _BrightnessMult;

                float lum = dot(col, float3(0.299, 0.587, 0.114));
                float a   = pow(saturate(lum), _AlphaGamma);

                return half4(saturate(col), a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
