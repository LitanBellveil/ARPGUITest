Shader "UI/WaterReflection"
{
    Properties
    {
        [PerRendererData]_MainTex ("Texture", 2D) = "white" {}

        _NoiseTex ("Noise Texture", 2D) = "gray" {}

        _Tint ("Tint", Color) = (1,1,1,1)

        _Alpha ("Alpha", Range(0,1)) = 0.7

        _WaveStrength ("Wave Strength", Range(0,0.1)) = 0.02
        _WaveSpeed ("Wave Speed", Range(0,10)) = 1
        _WaveFrequency ("Wave Frequency", Range(1,50)) = 20

        _NoiseStrength ("Noise Strength", Range(0,0.1)) = 0.03
        _NoiseSpeed ("Noise Speed", Range(0,5)) = 0.3

        _BlurStrength ("Blur Strength", Range(0,0.02)) = 0.003

        _FadePower ("Fade Power", Range(0,10)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            Name "WaterReflection"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float4 _MainTex_ST;

            float4 _Tint;

            float _Alpha;

            float _WaveStrength;
            float _WaveSpeed;
            float _WaveFrequency;

            float _NoiseStrength;
            float _NoiseSpeed;

            float _BlurStrength;

            float _FadePower;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                OUT.color = IN.color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                //----------------------------------
                // Sin Wave
                //----------------------------------

                float wave =
                    sin(
                        uv.y * _WaveFrequency +
                        _Time.y * _WaveSpeed
                    );

                //----------------------------------
                // Noise
                //----------------------------------

                float2 noiseUV = uv;

                noiseUV.y += _Time.y * _NoiseSpeed;

                float noise =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        noiseUV
                    ).r;

                noise = noise * 2 - 1;

                float depthMask =
                    saturate(uv.y);

                uv.x +=
                    wave * _WaveStrength +
                    noise * _NoiseStrength * depthMask;

                //----------------------------------
                // Fake Blur
                //----------------------------------

                half4 c0 =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    );

                half4 c1 =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(_BlurStrength,0)
                    );

                half4 c2 =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv - float2(_BlurStrength,0)
                    );

                half4 c3 =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv + float2(_BlurStrength * 2,0)
                    );

                half4 c4 =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv - float2(_BlurStrength * 2,0)
                    );

                half4 col =
                    (c0 + c1 + c2 + c3 + c4) / 5.0;

                //----------------------------------
                // Fade
                //----------------------------------

                float fade =
                    pow(
                        saturate(1.0 - uv.y),
                        _FadePower
                    );

                col *= _Tint;

                col.a *= fade;
                col.a *= _Alpha;

                return col * IN.color;
            }

            ENDHLSL
        }
    }
}