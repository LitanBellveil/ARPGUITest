Shader "UI/ImpactBurst_v1_Circle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _ColorInner ("Inner Color", Color) = (1, 1, 1, 1)
        _ColorOuter ("Outer Color", Color) = (0.4, 0.2, 1, 0)
        _RayCount ("Ray Count", Range(4, 32)) = 12
        _RaySharpness ("Ray Sharpness", Range(1, 20)) = 6
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.08
        _RingSpeed ("Ring Expand Speed", Range(0.5, 3)) = 1.2
        _GlowRadius ("Glow Radius", Range(0.1, 1)) = 0.5
        _Intensity ("Intensity", Range(0, 5)) = 2.0
        _RotationSpeed ("Rotation Speed", Range(-5, 5)) = 0.8
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
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _ColorInner, _ColorOuter;
                float  _Progress, _RayCount, _RaySharpness, _RingWidth, _RingSpeed, _GlowRadius, _Intensity, _RotationSpeed;
            CBUFFER_END
            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }
            float4 frag(Varyings IN) : SV_Target {
                float2 uv = IN.uv - 0.5;
                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);
                float t = _Progress;
                float rotatedAngle = angle + _Time.y * _RotationSpeed;
                float rays = pow(abs(sin(rotatedAngle * _RayCount * 0.5)), _RaySharpness);
                float rayMask = rays * smoothstep(0.0, 0.3, dist) * smoothstep(0.7, 0.2, dist);
                rayMask *= smoothstep(0.0, 0.3, t);
                float ringCenter = t * _RingSpeed;
                float ring = smoothstep(_RingWidth, 0.0, abs(dist - ringCenter));
                ring *= smoothstep(1.2, 0.0, ringCenter);
                float glow = smoothstep(_GlowRadius, 0.0, dist);
                glow *= smoothstep(0.0, 0.15, t) * smoothstep(1.0, 0.6, t);
                float totalBright = (ring + rayMask * 0.6 + glow * 0.8) * _Intensity;
                float4 col = lerp(_ColorInner, _ColorOuter, saturate(dist * 2.0));
                float alpha = totalBright * IN.color.a * smoothstep(1.0, 0.7, t);
                return float4(col.rgb * totalBright, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
