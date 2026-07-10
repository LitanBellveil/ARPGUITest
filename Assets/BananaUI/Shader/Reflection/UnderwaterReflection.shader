Shader "Custom/URP/UnderwaterReflection"
{
    Properties
    {
        [Header(Base)]
        _MainTex        ("RawImage",                   2D)    = "white" {}

        [Header(Reflection)]
        _ReflTex        ("Reflection Tex",             2D)    = "black" {}
        _ReflStrength   ("Reflection Strength",        Range(0,1))    = 0.75
        _ReflTint       ("Reflection Tint",            Color)         = (0.55, 0.75, 0.85, 1.0)
        _ReflStretch    ("Vertical Stretch",           Range(1,4))    = 1.8

        [Header(Wave UV)]
        _FreqY1         ("Main Wave FreqY",            Range(1,40))   = 12.0
        _AmpX1          ("Main Wave AmpX",             Range(0,0.12)) = 0.030
        _Speed1         ("Main Wave Speed",            Range(0,3))    = 0.6
        _PhaseShift     ("Phase Shift",                Range(0,6.28)) = 1.3
        _FreqY2         ("Detail Wave FreqY",          Range(1,80))   = 30.0
        _AmpX2          ("Detail Wave AmpX",           Range(0,0.04)) = 0.008
        _Speed2         ("Detail Wave Speed",          Range(0,3))    = 1.4
        _AmpY           ("Vertical Jitter AmpY",       Range(0,0.01)) = 0.002

        [Header(Highlight Streaks)]
        _StreakColor    ("Streak Color",               Color)         = (1.0, 1.0, 0.9, 1.0)
        _StreakFreq     ("Streak FreqY",               Range(1,60))   = 20.0
        _StreakSpeed    ("Streak Speed",               Range(0,3))    = 0.8
        _StreakPow      ("Streak Sharpness",           Range(2,32))   = 12.0
        _StreakStr      ("Streak Intensity",           Range(0,2))    = 0.5

        [Header(Caustics)]
        _CausticsTex    ("Caustics Texture",           2D)    = "white" {}
        _CausticsScale  ("Caustics Scale",             Range(1,20))   = 7.0
        _CausticsSpeed  ("Caustics Speed",             Range(0,2))    = 0.35
        _CausticsStr    ("Caustics Strength",          Range(0,1.5))  = 0.28
        _CausticsColor  ("Caustics Color",             Color)         = (0.5, 0.85, 0.9, 1.0)

        [Header(Alpha)]
        _Alpha          ("Overall Alpha",              Range(0,1))    = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+1"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull   Off
        ZWrite Off
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "WaterReflection"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            TEXTURE2D(_ReflTex);     SAMPLER(sampler_ReflTex);
            TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ReflTex_ST;
                float4 _CausticsTex_ST;

                half4  _ReflTint;
                half4  _StreakColor;
                half4  _CausticsColor;

                half   _ReflStrength;
                half   _ReflStretch;

                float  _FreqY1;
                float  _AmpX1;
                float  _Speed1;
                float  _PhaseShift;
                float  _FreqY2;
                float  _AmpX2;
                float  _Speed2;
                float  _AmpY;

                half   _StreakFreq;
                half   _StreakSpeed;
                half   _StreakPow;
                half   _StreakStr;

                half   _CausticsScale;
                half   _CausticsSpeed;
                half   _CausticsStr;

                half   _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float  fogFactor   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // 計算波紋 X/Y 偏移量（純 offset，不含翻轉）
            float2 WaveOffset(float2 uv, float t)
            {
                float dx = sin(uv.y * _FreqY1 - t * _Speed1) * _AmpX1;
                dx      += sin(uv.y * _FreqY1 * 0.6 + t * _Speed1 * 0.7 + _PhaseShift) * _AmpX1 * 0.35;
                dx      += sin(uv.y * _FreqY2 - t * _Speed2) * _AmpX2;

                float dy = sin(uv.x * 5.0 + t * _Speed1 * 0.5) * _AmpY;

                return float2(dx, dy);
            }

            half Caustics(float2 uv, float t)
            {
                float2 u1 = uv * _CausticsScale + float2(t, t * 0.7) * _CausticsSpeed;
                float2 u2 = uv * _CausticsScale + float2(-t * 0.8, t * 1.1) * _CausticsSpeed;
                u2 = float2(u2.x * 0.707 - u2.y * 0.707, u2.x * 0.707 + u2.y * 0.707);
                half a = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, u1).r;
                half b = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, u2).r;
                return saturate(min(a, b) * 2.0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor   = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float  t   = _Time.y;
                float2 uv  = IN.uv;

                // ── 波紋偏移量（共用）────────────────────────────────
                float2 offset = WaveOffset(uv, t);

                // ── 原圖 UV：直接加上偏移，圖本身跟著波紋搖曳 ────────
                float2 baseUV = uv + offset;
                half4  base   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);
                clip(base.a - 0.01);

                // ── 倒影 UV：翻轉 + 拉伸 + 同樣偏移 ─────────────────
                float2 reflUV;
                reflUV.x = uv.x + offset.x;
                reflUV.y = (1.0 - uv.y) / _ReflStretch + offset.y;
                half4 refl = SAMPLE_TEXTURE2D_BIAS(_ReflTex, sampler_ReflTex, reflUV, 0.8);
                refl.rgb  *= _ReflTint.rgb;

                // ── 高光條紋（相位跟著 offset 走）────────────────────
                float sPhase  = uv.y * _StreakFreq - t * _StreakSpeed + offset.x * 8.0;
                float sPhase2 = uv.y * _StreakFreq * 0.65 + t * _StreakSpeed * 0.6 + uv.x * 1.8;
                half  streak  = pow(saturate(sin(sPhase)  * 0.5 + 0.5), _StreakPow);
                streak       += pow(saturate(sin(sPhase2) * 0.5 + 0.5), _StreakPow + 4.0) * 0.45;
                streak        = saturate(streak * _StreakStr);

                // ── Caustics ──────────────────────────────────────────
                half caus = Caustics(uv + offset * 0.4, t) * _CausticsStr;

                // ── 合成（alpha 跟著 baseUV 取樣結果走）──────────────
                half4 col;
                col.rgb  = lerp(base.rgb, refl.rgb, _ReflStrength * base.a);
                col.rgb += _StreakColor.rgb   * streak * base.a;
                col.rgb += _CausticsColor.rgb * caus   * base.a;
                col.a    = base.a * _Alpha;

                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
