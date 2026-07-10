// CosmicFluidController.cs
// Attach to any GameObject that has a MeshRenderer + CosmicFluidURP material.
// Works with Unity 6.0 URP.
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class CosmicFluidController : MonoBehaviour
{
    // ── Shader property IDs ───────────────────────────────────────────────────
    static readonly int ID_FlowSpeed        = Shader.PropertyToID("_FlowSpeed");
    static readonly int ID_FlowTwist        = Shader.PropertyToID("_FlowTwist");
    static readonly int ID_RiverWidth       = Shader.PropertyToID("_RiverWidth");
    static readonly int ID_RiverSoftness    = Shader.PropertyToID("_RiverSoftness");

    static readonly int ID_DepthLayers      = Shader.PropertyToID("_DepthLayers");
    static readonly int ID_DepthSpread      = Shader.PropertyToID("_DepthSpread");
    static readonly int ID_DepthParallax    = Shader.PropertyToID("_DepthParallax");
    static readonly int ID_LayerSpeedFalloff= Shader.PropertyToID("_LayerSpeedFalloff");

    static readonly int ID_ColorA           = Shader.PropertyToID("_ColorA");
    static readonly int ID_ColorB           = Shader.PropertyToID("_ColorB");
    static readonly int ID_ColorC           = Shader.PropertyToID("_ColorC");
    static readonly int ID_EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    static readonly int ID_RimGlow          = Shader.PropertyToID("_RimGlow");

    static readonly int ID_StarDensity      = Shader.PropertyToID("_StarDensity");
    static readonly int ID_StarBrightness   = Shader.PropertyToID("_StarBrightness");
    static readonly int ID_StarTwinkle      = Shader.PropertyToID("_StarTwinkle");
    static readonly int ID_DustIntensity    = Shader.PropertyToID("_DustIntensity");

    static readonly int ID_AlphaPower       = Shader.PropertyToID("_AlphaPower");
    static readonly int ID_GlobalAlpha      = Shader.PropertyToID("_GlobalAlpha");

    // ── Exposed parameters (Inspector + scripting) ────────────────────────────

    [Header("Flow Shape")]
    [Range(0f, 5f)]   public float flowSpeed       = 0.8f;
    [Range(0f, 4f)]   public float flowTwist        = 1.6f;
    [Range(0.05f,1f)] public float riverWidth       = 0.38f;
    [Range(0.001f,0.5f)] public float riverSoftness = 0.12f;

    [Header("3D Depth")]
    [Range(1, 6)]     public int   depthLayers      = 4;
    [Range(0f, 1f)]   public float depthSpread      = 0.35f;
    [Range(0f, 0.5f)] public float depthParallax    = 0.12f;
    [Range(0.1f,1f)]  public float layerSpeedFalloff= 0.6f;

    [Header("Color & Glow")]
    public Color colorA           = new Color(0.18f, 0.12f, 0.55f, 1f);
    public Color colorB           = new Color(0.45f, 0.60f, 1.00f, 1f);
    public Color colorC           = new Color(0.85f, 0.92f, 1.00f, 1f);
    [Range(0f, 8f)] public float emissionStrength = 3.5f;
    [Range(0f, 1f)] public float rimGlow          = 0.35f;

    [Header("Stars & Dust")]
    [Range(0f, 1f)] public float starDensity      = 0.65f;
    [Range(0f, 5f)] public float starBrightness   = 2.2f;
    [Range(0f, 5f)] public float starTwinkle      = 1.5f;
    [Range(0f, 1f)] public float dustIntensity     = 0.5f;

    [Header("Transparency")]
    [Range(0.5f,6f)] public float alphaPower      = 2.0f;
    [Range(0f, 1f)]  public float globalAlpha     = 1.0f;

    // ── Optional: animated presets ────────────────────────────────────────────
    [Header("Animated Pulse (optional)")]
    public bool   animatePulse     = false;
    [Range(0f,2f)] public float pulseSpeed       = 0.5f;
    [Range(0f,2f)] public float pulseAmplitude   = 0.3f;

    // ── Private state ─────────────────────────────────────────────────────────
    MeshRenderer _renderer;
    MaterialPropertyBlock _mpb;

    // ─────────────────────────────────────────────────────────────────────────
    void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb      = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_renderer == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        float emission = emissionStrength;
        if (animatePulse)
            emission += Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmplitude;

        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetFloat(ID_FlowSpeed,         flowSpeed);
        _mpb.SetFloat(ID_FlowTwist,         flowTwist);
        _mpb.SetFloat(ID_RiverWidth,        riverWidth);
        _mpb.SetFloat(ID_RiverSoftness,     riverSoftness);

        _mpb.SetInt  (ID_DepthLayers,       depthLayers);
        _mpb.SetFloat(ID_DepthSpread,       depthSpread);
        _mpb.SetFloat(ID_DepthParallax,     depthParallax);
        _mpb.SetFloat(ID_LayerSpeedFalloff, layerSpeedFalloff);

        _mpb.SetColor(ID_ColorA,            colorA);
        _mpb.SetColor(ID_ColorB,            colorB);
        _mpb.SetColor(ID_ColorC,            colorC);
        _mpb.SetFloat(ID_EmissionStrength,  emission);
        _mpb.SetFloat(ID_RimGlow,           rimGlow);

        _mpb.SetFloat(ID_StarDensity,       starDensity);
        _mpb.SetFloat(ID_StarBrightness,    starBrightness);
        _mpb.SetFloat(ID_StarTwinkle,       starTwinkle);
        _mpb.SetFloat(ID_DustIntensity,     dustIntensity);

        _mpb.SetFloat(ID_AlphaPower,        alphaPower);
        _mpb.SetFloat(ID_GlobalAlpha,       globalAlpha);

        _renderer.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    void OnValidate() => Update();
#endif
}
