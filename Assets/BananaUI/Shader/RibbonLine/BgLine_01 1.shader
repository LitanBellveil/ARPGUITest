Shader "UI/EnergyRibbon"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "gray" {}

        _ColorA("Color A", Color) = (0.2,1,0.8,1)
        _ColorB("Color B", Color) = (0.7,0.3,1,1)

        _Speed("Speed", Range(0,5)) = 1

        _Intensity("Intensity", Range(0,10)) = 4

        _WaveHeight("Wave Height", Range(0.001,0.5)) = 0.03

        _Amplitude("Amplitude", Range(0,0.5)) = 0.08
        _Amplitude2("Amplitude 2", Range(0,0.5)) = 0.03

        _Frequency("Frequency", Range(1,50)) = 10
        _Frequency2("Frequency 2", Range(1,50)) = 24

        _Jitter("Jitter", Range(0,0.2)) = 0.015
        _Bulge("Bulge", Range(0,0.3)) = 0.05

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
            float _WaveHeight;

            float _Amplitude;
            float _Amplitude2;

            float _Frequency;
            float _Frequency2;

            float _Jitter;
            float _Bulge;

            float _Alpha;

            Varyings vert(Attributes v)
            {
                Varyings o;

                o.positionHCS =
                    TransformObjectToHClip(v.positionOS.xyz);

                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                float t =
                    _Time.y * _Speed;

                //--------------------------------
                // Noise
                //--------------------------------

                float noise1 =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv * 2 +
                        float2(
                            t * 0.15,
                            0
                        )
                    ).r;

                float noise2 =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv * 4 +
                        float2(
                            -t * 0.08,
                            t * 0.05
                        )
                    ).r;

                //--------------------------------
                // Waves
                //--------------------------------

                float wave1 =
                    sin(
                        uv.x * _Frequency +
                        noise1 * 4 +
                        t
                    ) * _Amplitude;

                float wave2 =
                    sin(
                        uv.x * _Frequency2 -
                        t * 1.7
                    ) * _Amplitude2;

                //--------------------------------
                // Jitter
                //--------------------------------

                float jitter =
                    (noise1 - 0.5)
                    * _Jitter;

                //--------------------------------
                // Bulge
                //--------------------------------

                float bulge =
                    (noise2 - 0.5)
                    * _Bulge;

                //--------------------------------
                // Center Line
                //--------------------------------

                float ribbonCenter =
                    0.5 +
                    wave1 +
                    wave2 +
                    jitter +
                    bulge;

                //--------------------------------
                // Distance
                //--------------------------------

                float dist =
                    abs(
                        uv.y -
                        ribbonCenter
                    );

                //--------------------------------
                // Ribbon Shape
                //--------------------------------

                float ribbon =
                    smoothstep(
                        _WaveHeight,
                        0,
                        dist
                    );

                //--------------------------------
                // Internal Energy
                //--------------------------------

                float energy =
                    noise1 * 0.7 +
                    noise2 * 0.3;

                //--------------------------------
                // Moving Highlights
                //--------------------------------

                float highlight =
                    sin(
                        uv.x * 30 +
                        energy * 8 +
                        t * 4
                    );

                highlight =
                    highlight * 0.5 + 0.5;

                highlight =
                    pow(
                        highlight,
                        6
                    );

                //--------------------------------
                // Gradient
                //--------------------------------

                float gradient =
                    saturate(
                        uv.y +
                        energy * 0.25
                    );

                float4 col =
                    lerp(
                        _ColorA,
                        _ColorB,
                        gradient
                    );

                //--------------------------------
                // Brightness
                //--------------------------------

                float brightness =
                    ribbon *
                    (
                        0.6 +
                        energy * 0.6 +
                        highlight * 1.2
                    );

                //--------------------------------
                // Final
                //--------------------------------

                col.rgb *=
                    brightness *
                    _Intensity;

                col.a =
                    ribbon *
                    brightness *
                    _Alpha;

                col *= i.color;

                return col;
            }

            ENDHLSL
        }
    }
}