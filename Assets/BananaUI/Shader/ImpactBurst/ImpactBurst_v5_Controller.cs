using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]  // 讓 Editor 不播放也能即時預覽
[RequireComponent(typeof(RawImage))]
public class ImpactBurst_v5_Controller : MonoBehaviour
{
    [Header("播放")]
    public float duration = 1.0f;
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("進度（可被 Animation 錄製）")]
    [SerializeField, Range(0f, 1f)] float _progress = 0f;

    [Header("顏色")]
    [SerializeField, ColorUsage(true, true)] Color _innerColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, ColorUsage(true, true)] Color _outerColor = new Color(0f, 0f, 1f, 1f);

    [Header("外框形狀")]
    [SerializeField, Range(0.02f, 0.5f)] float _borderWidth = 0.155f;
    [SerializeField, Range(0f,    0.7f)] float _centerHole  = 0.562f;
    [SerializeField, Range(1f,    40f)]  float _holeSharp   = 10.2f;

    [Header("Wave 扭曲")]
    [SerializeField, Range(0f,    0.15f)] float _waveAmp   = 0.0893f;
    [SerializeField, Range(-5f,   5f)]   float _waveSpeed  = 0.04f;
    [SerializeField, Range(1f,    12f)]  float _waveFreqX  = 6.07f;
    [SerializeField, Range(1f,    12f)]  float _waveFreqY  = 8.75f;

    [Header("旋轉 & 脈動")]
    [SerializeField, Range(-3f,   3f)]   float _rotationSpeed = 0.21f;
    [SerializeField, Range(0f,    5f)]   float _pulseSpeed    = 2.15f;
    [SerializeField, Range(0f,    0.5f)] float _pulseAmp      = 0f;

    [Header("強度")]
    [SerializeField, Range(0f,    8f)]   float _intensity = 2.01f;

    // ── 私有 ────────────────────────────────────────────
    RawImage  _img;
    Material  _mat;
    Coroutine _co;

    // 快取上一幀的值，只有變化時才推給 Material（效能優化）
    float  _lastProgress = -1f;
    Color  _lastInner, _lastOuter;
    float  _lastBW, _lastCH, _lastHS, _lastWA, _lastWS, _lastWX, _lastWY;
    float  _lastRS, _lastPS, _lastPA, _lastIN;

    static readonly int PP  = Shader.PropertyToID("_Progress");
    static readonly int PCI = Shader.PropertyToID("_ColorInner");
    static readonly int PCO = Shader.PropertyToID("_ColorOuter");
    static readonly int PBW = Shader.PropertyToID("_BorderWidth");
    static readonly int PCH = Shader.PropertyToID("_CenterHole");
    static readonly int PHS = Shader.PropertyToID("_HoleSharp");
    static readonly int PWA = Shader.PropertyToID("_WaveAmp");
    static readonly int PWS = Shader.PropertyToID("_WaveSpeed");
    static readonly int PWX = Shader.PropertyToID("_WaveFreqX");
    static readonly int PWY = Shader.PropertyToID("_WaveFreqY");
    static readonly int PRS = Shader.PropertyToID("_RotationSpeed");
    static readonly int PPS = Shader.PropertyToID("_PulseSpeed");
    static readonly int PPA = Shader.PropertyToID("_PulseAmp");
    static readonly int PIN = Shader.PropertyToID("_Intensity");

    void OnEnable()
    {
        _img = GetComponent<RawImage>();
        if (_mat == null)
        {
            _mat = _img.material != null
                 ? new Material(_img.material)
                 : new Material(Shader.Find("UI/ImpactBurst_v5_Border"));
            _img.material = _mat;
        }
        ApplyAll();
    }

    void OnDisable()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
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

    // ── 公開 API ─────────────────────────────────────────

    public void Play()
    {
        if (!Application.isPlaying) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run());
    }

    public void Stop()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        _progress = 0f;
        if (_mat) _mat.SetFloat(PP, 0f);
        if (_img) _img.enabled = false;
    }

    // ── 內部 ─────────────────────────────────────────────

    IEnumerator Run()
    {
        if (_img) _img.enabled = true;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            _progress = progressCurve.Evaluate(Mathf.Clamp01(e / duration));
            yield return null;
        }
        _progress = 1f;
        yield return new WaitForSeconds(0.05f);
        _progress = 0f;
        if (_img) _img.enabled = false;
        _co = null;
    }

    void ApplyAll()
    {
        // 只推有變化的值
        if (_progress   != _lastProgress) { _mat.SetFloat(PP,  _progress);    _lastProgress = _progress; }
        if (_innerColor != _lastInner)     { _mat.SetColor(PCI, _innerColor);  _lastInner    = _innerColor; }
        if (_outerColor != _lastOuter)     { _mat.SetColor(PCO, _outerColor);  _lastOuter    = _outerColor; }
        if (_borderWidth!= _lastBW)        { _mat.SetFloat(PBW, _borderWidth); _lastBW       = _borderWidth; }
        if (_centerHole != _lastCH)        { _mat.SetFloat(PCH, _centerHole);  _lastCH       = _centerHole; }
        if (_holeSharp  != _lastHS)        { _mat.SetFloat(PHS, _holeSharp);   _lastHS       = _holeSharp; }
        if (_waveAmp    != _lastWA)        { _mat.SetFloat(PWA, _waveAmp);     _lastWA       = _waveAmp; }
        if (_waveSpeed  != _lastWS)        { _mat.SetFloat(PWS, _waveSpeed);   _lastWS       = _waveSpeed; }
        if (_waveFreqX  != _lastWX)        { _mat.SetFloat(PWX, _waveFreqX);   _lastWX       = _waveFreqX; }
        if (_waveFreqY  != _lastWY)        { _mat.SetFloat(PWY, _waveFreqY);   _lastWY       = _waveFreqY; }
        if (_rotationSpeed!=_lastRS)       { _mat.SetFloat(PRS, _rotationSpeed);_lastRS      = _rotationSpeed; }
        if (_pulseSpeed != _lastPS)        { _mat.SetFloat(PPS, _pulseSpeed);  _lastPS       = _pulseSpeed; }
        if (_pulseAmp   != _lastPA)        { _mat.SetFloat(PPA, _pulseAmp);    _lastPA       = _pulseAmp; }
        if (_intensity  != _lastIN)        { _mat.SetFloat(PIN, _intensity);   _lastIN       = _intensity; }
    }

#if UNITY_EDITOR
    void OnValidate() { if (_mat != null) ApplyAll(); }
#endif
}
