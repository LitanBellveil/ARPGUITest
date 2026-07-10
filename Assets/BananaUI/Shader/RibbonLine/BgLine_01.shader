Shader "UI/ProceduralAurora"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "gray" {}

        _ColorA("Color A", Color) = (0.15,1,0.75,1)
        _ColorB("Color B", Color) = (0.5,0.4,1,1)

        _Speed("Speed", Range(0,5)) = 1
        _Intensity("Intensity", Range(0,10)) = 3

        _WaveHeight("Wave Height", Range(0,1)) = 0.35
        _Distortion("Distortion", Range(0,0.2)) = 0.05

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

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
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
            float _Distortion;
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

                float t = _Time.y * _Speed;

                //--------------------------------
                // Flow Noise
                //--------------------------------

                float noise1 =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv * 1.5 +
                        float2(t * 0.15, 0)
                    ).r;

                float noise2 =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv * 3.0 +
                        float2(-t * 0.08, t * 0.05)
                    ).r;

                float noise =
                    noise1 * 0.7 +
                    noise2 * 0.3;

                //--------------------------------
                // Aurora Ribbon Shape
                //--------------------------------

                float wave1 =
                    sin(
                        uv.x * 8 +
                        noise * 3 +
                        t
                    ) * 0.12;

                float wave2 =
                    sin(
                        uv.x * 16 -
                        t * 1.5
                    ) * 0.05;

                float ribbonCenter =
                    0.5 +
                    wave1 +
                    wave2;

                float dist =
                    abs(
                        uv.y -
                        ribbonCenter
                    );

                float ribbon =
                    smoothstep(
                        _WaveHeight,
                        0,
                        dist
                    );

                //--------------------------------
                // Aurora Detail
                //--------------------------------

                float detail =
                    smoothstep(
                        0.35,
                        1.0,
                        noise
                    );

                ribbon *=
                    detail;

                //--------------------------------
                // Moving Light Bands
                //--------------------------------

                float bands =
                    sin(
                        uv.x * 25 +
                        noise * 10 +
                        t * 3
                    );

                bands =
                    bands * 0.5 + 0.5;

                bands =
                    pow(
                        bands,
                        4
                    );

                //--------------------------------
                // Color
                //--------------------------------

                float gradient =
                    saturate(
                        uv.y +
                        noise * 0.2
                    );

                float4 col =
                    lerp(
                        _ColorA,
                        _ColorB,
                        gradient
                    );

                //--------------------------------
                // Final Lighting
                //--------------------------------

                float brightness =
                    ribbon *
                    (0.6 + bands * 1.2);

                col.rgb *=
                    brightness *
                    _Intensity;

                col.a =
                    brightness *
                    _Alpha;

                col *= i.color;

                return col;
            }

            ENDHLSL
        }
    }
}