Shader "Custom/URP/CosmicFluidURP"
{
    Properties
    {
        [Header(Flow Shape)]
        _FlowSpeed          ("Flow Speed",          Range(0.0, 5.0))    = 0.8
        _FlowTwist          ("Flow Twist / Winding", Range(0.0, 4.0))   = 1.6
        _RiverWidth         ("River Width",          Range(0.05, 1.0))   = 0.38
        _RiverSoftness      ("River Edge Softness",  Range(0.001, 0.5))  = 0.12

        [Header(3D Depth)]
        _DepthLayers        ("Depth Layer Count",    Range(1, 6))        = 4
        _DepthSpread        ("Layer Depth Spread",   Range(0.0, 1.0))    = 0.35
        _DepthParallax      ("Parallax Strength",    Range(0.0, 0.5))    = 0.12
        _LayerSpeedFalloff  ("Layer Speed Falloff",  Range(0.1, 1.0))    = 0.6

        [Header(Color and Glow)]
        _ColorA             ("Color Deep (River)",   Color)              = (0.18, 0.12, 0.55, 1.0)
        _ColorB             ("Color Mid (Glow)",     Color)              = (0.45, 0.60, 1.00, 1.0)
        _ColorC             ("Color Bright (Core)",  Color)              = (0.85, 0.92, 1.00, 1.0)
        _EmissionStrength   ("Emission Strength",    Range(0.0, 8.0))    = 3.5
        _RimGlow            ("Rim Glow Width",       Range(0.0, 1.0))    = 0.35

        [Header(Stars and Particles)]
        _StarDensity        ("Star Density",         Range(0.0, 1.0))    = 0.65
        _StarBrightness     ("Star Brightness",      Range(0.0, 5.0))    = 2.2
        _StarTwinkle        ("Star Twinkle Speed",   Range(0.0, 5.0))    = 1.5
        _DustIntensity      ("Nebula Dust Intensity",Range(0.0, 1.0))    = 0.5

        [Header(Alpha and Transparency)]
        _AlphaPower         ("Alpha Falloff Power",  Range(0.5, 6.0))    = 2.0
        _GlobalAlpha        ("Global Alpha",         Range(0.0, 1.0))    = 1.0

        [Header(Noise Texture)]
        _NoiseTex           ("Noise Texture (optional)", 2D)             = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CosmicFluid"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ─── Uniforms ────────────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _FlowSpeed;
                float  _FlowTwist;
                float  _RiverWidth;
                float  _RiverSoftness;

                int    _DepthLayers;
                float  _DepthSpread;
                float  _DepthParallax;
                float  _LayerSpeedFalloff;

                float4 _ColorA;
                float4 _ColorB;
                float4 _ColorC;
                float  _EmissionStrength;
                float  _RimGlow;

                float  _StarDensity;
                float  _StarBrightness;
                float  _StarTwinkle;
                float  _DustIntensity;

                float  _AlphaPower;
                float  _GlobalAlpha;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            // ─── Structs ─────────────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─── Noise Helpers ────────────────────────────────────────────────────────
            // Hash-based procedural 2D noise (no texture dependency)
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            float hash1(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Smooth value noise
            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash1(i + float2(0,0));
                float b = hash1(i + float2(1,0));
                float c = hash1(i + float2(0,1));
                float d = hash1(i + float2(1,1));

                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            // Fractal Brownian Motion
            float fbm(float2 p, int octaves)
            {
                float val = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    val  += amp * vnoise(p * freq);
                    amp  *= 0.5;
                    freq *= 2.0;
                }
                return val;
            }

            // Domain-warped FBM for organic winding
            float warpedFBM(float2 p, float t)
            {
                float2 warp = float2(
                    fbm(p + float2(t * 0.3, 0.0), 3),
                    fbm(p + float2(0.0, t * 0.3), 3)
                );
                return fbm(p + _FlowTwist * warp, 4);
            }

            // ─── Star Field ───────────────────────────────────────────────────────────
            float starField(float2 uv, float t)
            {
                float2 cell = floor(uv * 40.0);
                float2 local = frac(uv * 40.0) - 0.5;

                float2 jitter = hash2(cell) - 0.5;
                float  dist   = length(local - jitter * 0.4);

                float existence = step(1.0 - _StarDensity, hash1(cell));
                float twinkle   = 0.5 + 0.5 * sin(t * _StarTwinkle + hash1(cell + 99.9) * 6.28);
                float glow      = existence * twinkle * smoothstep(0.18, 0.0, dist);

                return glow;
            }

            // ─── Single River Layer ───────────────────────────────────────────────────
            float4 riverLayer(float2 uv, float t, float layerIdx, float totalLayers)
            {
                // Parallax offset per layer
                float normLayer = layerIdx / max(totalLayers - 1.0, 1.0);
                float2 offset   = float2(normLayer * _DepthParallax, normLayer * _DepthParallax * 0.4);
                float2 p        = uv + offset;

                // Each layer flows at slightly different speed
                float speedMult = pow(_LayerSpeedFalloff, layerIdx);
                float warpedVal = warpedFBM(p * 1.2 + float2(0.0, -t * _FlowSpeed * speedMult), t);

                // Map noise to river band
                // Center of the band oscillates gently
                float center    = 0.5 + 0.15 * sin(t * 0.3 + layerIdx * 1.1);
                float dist      = abs(warpedVal - center);
                float width     = _RiverWidth * (0.6 + 0.4 * (1.0 - normLayer)); // deeper layers are narrower
                float band      = smoothstep(width, width - _RiverSoftness, dist);

                // Brightness falloff toward edges for 3-D depth cue
                float depthBright = 1.0 - normLayer * _DepthSpread;

                // Rim glow outside core
                float rimBand   = smoothstep(width + _RimGlow, width, dist) * (1.0 - band);

                // Color gradient: deep blue → periwinkle → white-core
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, band * 0.5);
                col = lerp(col, _ColorC.rgb, band * band);
                col += _ColorB.rgb * rimBand * 0.4;
                col *= _EmissionStrength * depthBright;

                float alpha = (band + rimBand * 0.4) * depthBright;
                return float4(col, alpha);
            }

            // ─── Vertex ───────────────────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                return OUT;
            }

            // ─── Fragment ─────────────────────────────────────────────────────────────
            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // --- Background: deep space gradient (fully transparent at borders) ---
                float vignette = smoothstep(0.0, 0.35, uv.y) * smoothstep(1.0, 0.65, uv.y)
                               * smoothstep(0.0, 0.15, uv.x) * smoothstep(1.0, 0.85, uv.x);

                float3 bgCol   = _ColorA.rgb * 0.25;
                float  bgAlpha = vignette * 0.4;

                // --- Nebula dust ---
                float dust     = fbm(uv * 3.5 + float2(t * 0.05, 0.0), 3);
                dust           = saturate(dust - 0.3) * _DustIntensity;
                float3 dustCol = lerp(_ColorA.rgb, _ColorB.rgb * 0.5, dust);

                // --- Accumulate depth layers (back to front) ---
                float3 fluidCol   = bgCol + dustCol * dust;
                float  fluidAlpha = bgAlpha + dust * 0.3 * vignette;

                int layers = clamp(_DepthLayers, 1, 6);
                for (int i = layers - 1; i >= 0; i--)
                {
                    float4 layer = riverLayer(uv, t, (float)i, (float)layers);
                    // Alpha-composite this layer on top
                    float srcA     = layer.a;
                    fluidCol   = fluidCol * (1.0 - srcA) + layer.rgb * srcA;
                    fluidAlpha = fluidAlpha + srcA * (1.0 - fluidAlpha);
                }

                // --- Star field (only visible where fluid isn't fully opaque) ---
                float stars    = starField(uv, t);
                float starMask = (1.0 - saturate(fluidAlpha * 2.0));   // stars hidden under bright fluid
                fluidCol  += float3(0.9, 0.95, 1.0) * stars * _StarBrightness * starMask;
                fluidAlpha = saturate(fluidAlpha + stars * _StarBrightness * 0.08 * vignette);

                // --- Final alpha shaping ---
                // Sharp removal of anything below the flow region (transparent background)
                float finalAlpha = pow(saturate(fluidAlpha), _AlphaPower) * _GlobalAlpha;

                return float4(fluidCol, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
