Shader "UI/ImpactBurst_v5_Border"
{
    Properties
    {
        _MainTex      ("Texture", 2D)                      = "white" {}
        _Progress     ("Progress",          Range(0,1))    = 0
        _ColorInner   ("Inner Color",       Color)         = (1, 1, 1, 1)
        _ColorOuter   ("Outer Color",       Color)         = (0.3, 0.1, 1, 1)
        _BorderWidth  ("Border Width",      Range(0.02,0.5))= 0.18
        _CenterHole   ("Center Hole",       Range(0, 0.7)) = 0.35
        _HoleSharp    ("Hole Sharpness",    Range(1, 40))  = 20.0
        _WaveAmp      ("Wave Amplitude",    Range(0, 0.15))= 0.04
        _WaveSpeed    ("Wave Speed",        Range(-5, 5))  = 1.2
        _WaveFreqX    ("Wave Freq X",       Range(1, 12))  = 3.0
        _WaveFreqY    ("Wave Freq Y",       Range(1, 12))  = 3.0
        _Intensity    ("Intensity",         Range(0, 8))   = 3.0
        _PulseSpeed   ("Pulse Speed",       Range(0, 5))   = 1.5
        _PulseAmp     ("Pulse Amplitude",   Range(0, 0.5)) = 0.08
        _RotationSpeed("Rotation Speed",    Range(-3, 3))  = 0.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };
            struct Varyings   { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ColorInner, _ColorOuter;
                float  _Progress, _BorderWidth, _CenterHole, _HoleSharp;
                float  _WaveAmp, _WaveSpeed, _WaveFreqX, _WaveFreqY;
                float  _Intensity, _PulseSpeed, _PulseAmp, _RotationSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings O;
                O.pos = TransformObjectToHClip(IN.pos.xyz);
                O.uv  = TRANSFORM_TEX(IN.uv, _MainTex);
                O.col = IN.col;
                return O;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv   = IN.uv - 0.5;   // -0.5 ~ 0.5
                float  t    = _Progress;
                float  time = _Time.y;

                // ── 旋轉 UV（不用 atan2，直接旋轉 XY） ──────────
                float  s = sin(time * _RotationSpeed);
                float  c = cos(time * _RotationSpeed);
                float2 ruv = float2(uv.x * c - uv.y * s,
                                    uv.x * s + uv.y * c);

                // ── Wave 偏移（用 XY 直接算，完全無接縫） ────────
                // X 方向的 wave 用 ruv.y 控制（垂直位置決定左右波浪）
                float wx = sin(ruv.y * _WaveFreqX * 6.28318 + time * _WaveSpeed)
                         * _WaveAmp;
                // Y 方向的 wave 用 ruv.x 控制（水平位置決定上下波浪）
                float wy = sin(ruv.x * _WaveFreqY * 6.28318 + time * _WaveSpeed * 1.1)
                         * _WaveAmp;
                // 疊一層諧波增加不規則感
                wx += sin(ruv.y * (_WaveFreqX + 1.0) * 6.28318 + time * _WaveSpeed * 0.7 + 1.3)
                    * _WaveAmp * 0.4;
                wy += sin(ruv.x * (_WaveFreqY + 1.0) * 6.28318 + time * _WaveSpeed * 1.3 + 2.1)
                    * _WaveAmp * 0.4;

                // 把 wave 套到 uv 上再算距離
                float2 warpedUV = ruv + float2(wx, wy);
                float  dist     = length(warpedUV);

                // ── 呼吸脈動 ─────────────────────────────────────
                float pulse  = sin(time * _PulseSpeed) * _PulseAmp;
                float outerR = 0.48 + pulse;
                float innerR = _CenterHole;

                // ── 外框光暈 ─────────────────────────────────────
                float holeMask   = saturate((dist - innerR) * _HoleSharp);
                float borderEdge = outerR - _BorderWidth;
                float borderGlow = smoothstep(borderEdge - 0.04, outerR,     dist)
                                 * smoothstep(outerR + 0.04,     borderEdge, dist);
                float innerGlow  = smoothstep(innerR, innerR + _BorderWidth, dist)
                                 * smoothstep(borderEdge, innerR + _BorderWidth * 0.5, dist);

                float border = saturate(borderGlow + innerGlow * 0.5) * holeMask;

                // ── Progress 動畫 ─────────────────────────────────
                float appear  = smoothstep(0.0, 0.3, t);
                float fadeOut = smoothstep(1.0, 0.65, t);
                float bright  = border * appear * fadeOut * _Intensity;

                // ── 顏色 ─────────────────────────────────────────
                float  colorT = saturate((dist - innerR) / max(_BorderWidth, 0.001));
                float4 col    = lerp(_ColorInner, _ColorOuter, colorT);

                float alpha = saturate(bright * 0.9) * IN.col.a * fadeOut;
                return float4(col.rgb * bright, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
