Shader "UI/EnergyRibbonCircle"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "gray" {}

        _ColorA("Color A", Color) = (0.2,1,0.8,1)
        _ColorB("Color B", Color) = (0.7,0.3,1,1)

        _Speed("Speed", Range(0,5)) = 1

        _Intensity("Intensity", Range(0,10)) = 5

        _BranchCount("Branch Count", Range(1,8)) = 5

        _BranchSpread("Branch Spread", Range(0,0.5)) = 0.08

        _Amplitude("Amplitude", Range(0,0.5)) = 0.12

        _Thickness("Thickness", Range(0.001,0.1)) = 0.02

        _RingRadius("Ring Radius", Range(0,0.5)) = 0.35

        _Alpha("Alpha", Range(0,2)) = 1

        [HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifndef PI
            #define PI 3.14159265359
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float4 _ColorA;
            float4 _ColorB;

            float _Speed;
            float _Intensity;

            float _BranchCount;
            float _BranchSpread;

            float _Amplitude;
            float _Thickness;

            float _RingRadius;

            float _Alpha;

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // 把方形 UV 轉成極座標：
                // angle 0~1 繞完整一圈（在角度上是週期量，繞回 0 跟原本的 1 是同一個點，天生無縫）
                // radius 是離中心的距離，取代原本沿 UV.y 的「跨越寬度」軸
                float2 centered = uv - 0.5;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x) / (2 * PI) + 0.5;

                float t = _Time.y * _Speed;

                float noise =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        float2(angle, radius) * 3 +
                        float2(t * 0.1, 0)
                    ).r;

                float ribbon = 0;

                [unroll]
                for (int k = 0; k < 8; k++)
                {
                    if (k >= (int)_BranchCount)
                        break;

                    float rnd1 = Hash(k * 13.7);
                    float rnd2 = Hash(k * 27.3);
                    float rnd3 = Hash(k * 51.9);
                    float rnd4 = Hash(k * 97.1);

                    float amplitude =
                        lerp(_Amplitude * 0.3, _Amplitude * 1.8, rnd1);

                    // 完全比照本體的頻率／振幅比例（frequency、frequency*0.5、amplitude*0.5），
                    // 差異只有兩點：
                    // 1) 頻率量化成整數圈數，繞一圈頭尾的相位才會完全對齊、不會有接縫。
                    // 2) 數值範圍縮小（本體是 4~25），因為繞成一整圈後每一圈都是一個凹凸波峰，
                    //    用本體的範圍會擠出太多凸點；縮小範圍讓波峰數變少、弧度變大，比較像飄動的絲帶。
                    float frequency = max(round(lerp(2, 7, rnd2)), 1);
                    float frequency2 = max(round(frequency * 0.5), 1);

                    float speed = lerp(0.5, 3, rnd3);
                    float thickness = lerp(0.5, 2.0, rnd4);

                    float offset =
                        (k - (_BranchCount - 1) * 0.5) * _BranchSpread;

                    offset += (rnd1 - 0.5) * _BranchSpread;

                    float wave =
                        sin(angle * frequency * 2 * PI + t * speed + rnd2 * 100) * amplitude;

                    wave +=
                        sin(angle * frequency2 * 2 * PI + t * speed * 1.8 + rnd3 * 50) *
                        amplitude * 0.5;

                    float ringCenter = _RingRadius + offset + wave;

                    float dist = abs(radius - ringCenter);

                    float branch =
                        smoothstep(_Thickness * thickness, 0, dist);

                    ribbon += branch * lerp(0.2, 0.6, rnd4);
                }

                ribbon = pow(saturate(ribbon), 0.8);

                float glow = pow(saturate(ribbon), 0.35);
                glow *= 0.35;

                // 用週期函數（sin）沿角度做顏色漸層，跟頻率量化同理，繞一圈頭尾顏色才會接得上
                float colorWave = 0.5 + 0.5 * sin(angle * 2 * PI);
                float gradient = saturate(colorWave + noise * 0.2);

                float4 col = lerp(_ColorA, _ColorB, gradient);

                float brightness = ribbon;

                col.rgb *= brightness * _Intensity;

                col.rgb += glow * (_ColorA.rgb * 0.5);

                col.a = max(ribbon, glow * 0.35) * _Alpha;

                col *= i.color;

                return col;
            }

            ENDHLSL
        }
    }
}
