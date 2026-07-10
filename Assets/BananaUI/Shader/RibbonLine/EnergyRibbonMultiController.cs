using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]  // 讓 Editor 不播放也能即時預覽
public class EnergyRibbonMultiController : MonoBehaviour
{
    [Header("噪聲")]
    [SerializeField] Texture _noiseTex;

    [Header("顏色（可被 Animation 錄製）")]
    [SerializeField] Color _colorA = new Color(0.2f, 1f, 0.8f, 1f);
    [SerializeField] Color _colorB = new Color(0.7f, 0.3f, 1f, 1f);

    [Header("動態")]
    [SerializeField, Range(0f, 5f)]  float _speed = 1f;
    [SerializeField, Range(0f, 10f)] float _intensity = 5f;

    [Header("絲帶分支")]
    [SerializeField, Range(1, 8)]      int   _branchCount = 5;
    [SerializeField, Range(0f, 0.5f)]  float _branchSpread = 0.08f;
    [SerializeField, Range(0f, 0.5f)]  float _amplitude = 0.12f;
    [SerializeField, Range(0.001f, 0.1f)] float _thickness = 0.02f;

    [Header("邊緣")]
    [SerializeField, Range(0.01f, 0.5f)] float _edgeFade = 0.2f;

    [Header("透明度")]
    [SerializeField, Range(0f, 2f)] float _alpha = 1f;

    // ── 私有 ────────────────────────────────────────────
    Graphic  _graphic;
    Material _mat;

    // 快取上一幀的值，只有變化時才推給 Material（效能優化）
    Texture _lastNoiseTex;
    Color   _lastColorA, _lastColorB;
    float   _lastSpeed, _lastIntensity;
    int     _lastBranchCount;
    float   _lastBranchSpread, _lastAmplitude, _lastThickness, _lastEdgeFade, _lastAlpha;
    bool    _initialized;

    static readonly int PNoise        = Shader.PropertyToID("_NoiseTex");
    static readonly int PColorA       = Shader.PropertyToID("_ColorA");
    static readonly int PColorB       = Shader.PropertyToID("_ColorB");
    static readonly int PSpeed        = Shader.PropertyToID("_Speed");
    static readonly int PIntensity    = Shader.PropertyToID("_Intensity");
    static readonly int PBranchCount  = Shader.PropertyToID("_BranchCount");
    static readonly int PBranchSpread = Shader.PropertyToID("_BranchSpread");
    static readonly int PAmplitude    = Shader.PropertyToID("_Amplitude");
    static readonly int PThickness    = Shader.PropertyToID("_Thickness");
    static readonly int PEdgeFade     = Shader.PropertyToID("_EdgeFade");
    static readonly int PAlpha        = Shader.PropertyToID("_Alpha");

    void OnEnable()
    {
        _graphic = GetComponent<Graphic>();
        if (_graphic == null)
        {
            Debug.LogWarning($"{nameof(EnergyRibbonMultiController)} 需要掛在有 Image / RawImage 的物件上。", this);
            return;
        }

        // 每個物件各自持有一份材質實例，避免共用 sharedMaterial 互相干擾
        if (_mat == null)
        {
            _mat = _graphic.material != null
                 ? new Material(_graphic.material)
                 : new Material(Shader.Find("UI/EnergyRibbonD"));
            _graphic.material = _mat;
        }

        _initialized = false; // 強制下一次 ApplyAll 全量推一次
        ApplyAll();
    }

    void OnDestroy()
    {
        if (_mat != null) DestroyImmediate(_mat);
    }

    // Update 在 ExecuteAlways 下 Editor 也會跑，讓 Animation 預覽生效
    void Update()
    {
        if (_mat == null) return;
        ApplyAll();
    }

    void ApplyAll()
    {
        if (!_initialized || _noiseTex != _lastNoiseTex)
        {
            if (_noiseTex != null) _mat.SetTexture(PNoise, _noiseTex);
            _lastNoiseTex = _noiseTex;
        }

        if (!_initialized || _colorA != _lastColorA) { _mat.SetColor(PColorA, _colorA); _lastColorA = _colorA; }
        if (!_initialized || _colorB != _lastColorB) { _mat.SetColor(PColorB, _colorB); _lastColorB = _colorB; }

        if (!_initialized || _speed     != _lastSpeed)     { _mat.SetFloat(PSpeed,     _speed);     _lastSpeed     = _speed; }
        if (!_initialized || _intensity != _lastIntensity) { _mat.SetFloat(PIntensity, _intensity); _lastIntensity = _intensity; }

        if (!_initialized || _branchCount != _lastBranchCount) { _mat.SetFloat(PBranchCount, _branchCount); _lastBranchCount = _branchCount; }
        if (!_initialized || _branchSpread != _lastBranchSpread) { _mat.SetFloat(PBranchSpread, _branchSpread); _lastBranchSpread = _branchSpread; }

        if (!_initialized || _amplitude != _lastAmplitude) { _mat.SetFloat(PAmplitude, _amplitude); _lastAmplitude = _amplitude; }
        if (!_initialized || _thickness != _lastThickness) { _mat.SetFloat(PThickness, _thickness); _lastThickness = _thickness; }
        if (!_initialized || _edgeFade != _lastEdgeFade) { _mat.SetFloat(PEdgeFade, _edgeFade); _lastEdgeFade = _edgeFade; }

        if (!_initialized || _alpha != _lastAlpha) { _mat.SetFloat(PAlpha, _alpha); _lastAlpha = _alpha; }

        _initialized = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_mat != null) ApplyAll();
    }
#endif
}
