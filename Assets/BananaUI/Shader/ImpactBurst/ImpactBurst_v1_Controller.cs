using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class ImpactBurst_v1_Controller : MonoBehaviour
{
    [Header("播放")]
    public float duration = 0.8f;
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("進度（可被 Animation 錄製）")]
    [SerializeField, Range(0f, 1f)] float _progress = 0f;

    [Header("顏色")]
    [SerializeField, ColorUsage(true, true)] Color _innerColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, ColorUsage(true, true)] Color _outerColor = new Color(0.4f, 0.2f, 1f, 0f);

    [Header("光線")]
    [SerializeField, Range(4,   32)]  int   _rayCount      = 12;
    [SerializeField, Range(1f,  20f)] float _raySharpness  = 6f;
    [SerializeField, Range(-5f, 5f)]  float _rotationSpeed = 0.8f;

    [Header("光環")]
    [SerializeField, Range(0.01f, 0.5f)] float _ringWidth = 0.08f;
    [SerializeField, Range(0.5f,  3f)]   float _ringSpeed = 1.2f;

    [Header("中心光暈")]
    [SerializeField, Range(0.1f, 1f)] float _glowRadius = 0.5f;

    [Header("強度")]
    [SerializeField, Range(0f, 5f)] float _intensity = 2.0f;

    // ── 私有 ────────────────────────────────────────────
    RawImage  _img;
    Material  _mat;
    Coroutine _co;

    float _lastProgress = -1f;
    Color _lastInner, _lastOuter;
    int   _lastRC;
    float _lastRS, _lastRot, _lastRW, _lastRP, _lastGR, _lastIN;

    static readonly int PP  = Shader.PropertyToID("_Progress");
    static readonly int PCI = Shader.PropertyToID("_ColorInner");
    static readonly int PCO = Shader.PropertyToID("_ColorOuter");
    static readonly int PRC = Shader.PropertyToID("_RayCount");
    static readonly int PRS = Shader.PropertyToID("_RaySharpness");
    static readonly int PRO = Shader.PropertyToID("_RotationSpeed");
    static readonly int PRW = Shader.PropertyToID("_RingWidth");
    static readonly int PRP = Shader.PropertyToID("_RingSpeed");
    static readonly int PGR = Shader.PropertyToID("_GlowRadius");
    static readonly int PIN = Shader.PropertyToID("_Intensity");

    void OnEnable()
    {
        _img = GetComponent<RawImage>();
        if (_mat == null)
        {
            _mat = _img.material != null
                 ? new Material(_img.material)
                 : new Material(Shader.Find("UI/ImpactBurst_v1_Circle"));
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
        if (_progress    != _lastProgress) { _mat.SetFloat(PP,  _progress);      _lastProgress = _progress; }
        if (_innerColor  != _lastInner)    { _mat.SetColor(PCI, _innerColor);     _lastInner    = _innerColor; }
        if (_outerColor  != _lastOuter)    { _mat.SetColor(PCO, _outerColor);     _lastOuter    = _outerColor; }
        if (_rayCount    != _lastRC)       { _mat.SetFloat(PRC, _rayCount);       _lastRC       = _rayCount; }
        if (_raySharpness!= _lastRS)       { _mat.SetFloat(PRS, _raySharpness);   _lastRS       = _raySharpness; }
        if (_rotationSpeed!=_lastRot)      { _mat.SetFloat(PRO, _rotationSpeed);  _lastRot      = _rotationSpeed; }
        if (_ringWidth   != _lastRW)       { _mat.SetFloat(PRW, _ringWidth);      _lastRW       = _ringWidth; }
        if (_ringSpeed   != _lastRP)       { _mat.SetFloat(PRP, _ringSpeed);      _lastRP       = _ringSpeed; }
        if (_glowRadius  != _lastGR)       { _mat.SetFloat(PGR, _glowRadius);     _lastGR       = _glowRadius; }
        if (_intensity   != _lastIN)       { _mat.SetFloat(PIN, _intensity);      _lastIN       = _intensity; }
    }

#if UNITY_EDITOR
    void OnValidate() { if (_mat != null) ApplyAll(); }
#endif
}
