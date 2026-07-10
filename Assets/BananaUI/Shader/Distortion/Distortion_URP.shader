Shader "2DxFX/URP/Distortion"
{
    Properties
    {
        _MainTex        ("Sprite Texture", 2D)          = "white" {}
        _Color          ("Tint",           Color)       = (1,1,1,1)

        [Header(Distortion)]
        _OffsetX        ("Wave Frequency X",  Range(0, 128))    = 10.0
        _OffsetY        ("Wave Frequency Y",  Range(0, 128))    = 10.0
        _DistanceX      ("Wave Amplitude X",  Range(0, 0.2))    = 0.03
        _DistanceY      ("Wave Amplitude Y",  Range(0, 0.2))    = 0.03

        [Header(Speed)]
        _SpeedX         ("Speed X",           Range(-10, 10))   = 1.0
        _SpeedY         ("Speed Y",           Range(-10, 10))   = 0.8

        [Header(Alpha)]
        _Alpha          ("Alpha",             Range(0, 1))      = 1.0

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // ── SpriteRenderer (2D Renderer) ──
        Pass
        {
            Name "Distortion_2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Alpha;
                float  _OffsetX;
                float  _OffsetY;
                float  _DistanceX;
                float  _DistanceY;
                float  _SpeedX;
                float  _SpeedY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // 雙軸正弦疊加，產生流體感
                float dx = sin(uv.y * _OffsetX + t * _SpeedX) * _DistanceX;
                float dy = sin(uv.x * _OffsetY + t * _SpeedY) * _DistanceY;

                // 二次疊加讓流體更自然（避免太規則）
                dx += sin(uv.y * _OffsetX * 0.5 + t * _SpeedX * 1.3 + 1.2) * _DistanceX * 0.4;
                dy += sin(uv.x * _OffsetY * 0.5 + t * _SpeedY * 1.3 + 2.1) * _DistanceY * 0.4;

                half4 col  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(dx, dy));
                col       *= IN.color;
                col.a     *= _Alpha;
                return col;
            }
            ENDHLSL
        }

        // ── UI Image / UniversalForward ──
        Pass
        {
            Name "Distortion_Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Alpha;
                float  _OffsetX;
                float  _OffsetY;
                float  _DistanceX;
                float  _DistanceY;
                float  _SpeedX;
                float  _SpeedY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                float dx = sin(uv.y * _OffsetX + t * _SpeedX) * _DistanceX;
                float dy = sin(uv.x * _OffsetY + t * _SpeedY) * _DistanceY;

                dx += sin(uv.y * _OffsetX * 0.5 + t * _SpeedX * 1.3 + 1.2) * _DistanceX * 0.4;
                dy += sin(uv.x * _OffsetY * 0.5 + t * _SpeedY * 1.3 + 2.1) * _DistanceY * 0.4;

                half4 col  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(dx, dy));
                col       *= IN.color;
                col.a     *= _Alpha;
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
