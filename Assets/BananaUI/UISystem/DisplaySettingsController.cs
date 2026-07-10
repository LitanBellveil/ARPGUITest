using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
//  資料結構：單一解析度選項
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public struct ResolutionOption
{
    public int width;
    public int height;
}

// ─────────────────────────────────────────────────────────────────────────────
//  DisplaySettingsController
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 讓玩家在 UI 上選擇解析度（皆與 1920x1080 同為 16:9 比例）與視窗／全螢幕模式。
/// 與 UITabSystem / UIView / EscUIToggle 完全獨立，不依賴任何頁面系統。
///
/// 使用方式：
///   1. 在場景中放一個 Resolution TMP_Dropdown 與 WindowMode TMP_Dropdown，拖進對應欄位。
///   2. 選項清單可在 Inspector 增減，但需保持 16:9 比例（OnValidate 會警告不符合的項目）。
///   3. 設定會即時套用並存到 PlayerPrefs，下次啟動自動讀回。
/// </summary>
public class DisplaySettingsController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 欄位
    // ─────────────────────────────────────────────

    [Header("解析度清單（皆為 16:9，與 1920x1080 同比例）")]
    [SerializeField]
    private List<ResolutionOption> _resolutions = new()
    {
        new ResolutionOption { width = 1280, height = 720 },
        new ResolutionOption { width = 1600, height = 900 },
        new ResolutionOption { width = 1920, height = 1080 },
        new ResolutionOption { width = 2560, height = 1440 },
        new ResolutionOption { width = 3840, height = 2160 },
    };

    [Header("UI 元件")]
    [Tooltip("顯示解析度選項的 Dropdown")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;

    [Tooltip("顯示視窗／全螢幕選項的 Dropdown（0 = 視窗，1 = 全螢幕）")]
    [SerializeField] private TMP_Dropdown _windowModeDropdown;

    // ─────────────────────────────────────────────
    //  PlayerPrefs Key
    // ─────────────────────────────────────────────

    private const string PrefKeyResIndex = "DisplaySettings_ResolutionIndex";
    private const string PrefKeyFullscreen = "DisplaySettings_Fullscreen";

    // ─────────────────────────────────────────────
    //  Unity 生命週期
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (_resolutionDropdown == null || _windowModeDropdown == null)
        {
            Debug.LogWarning($"[DisplaySettingsController] {name}：Resolution Dropdown 或 WindowMode Dropdown 未設定。");
            return;
        }

        BuildResolutionDropdown();
        BuildWindowModeDropdown();
        LoadAndApplySavedSettings();

        _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        _windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
    }

    private void OnDestroy()
    {
        if (_resolutionDropdown != null)
            _resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        if (_windowModeDropdown != null)
            _windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);
    }

    // ─────────────────────────────────────────────
    //  初始化
    // ─────────────────────────────────────────────

    private void BuildResolutionDropdown()
    {
        var options = new List<string>();
        foreach (var res in _resolutions)
            options.Add($"{res.width} x {res.height}");

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(options);
    }

    private void BuildWindowModeDropdown()
    {
        _windowModeDropdown.ClearOptions();
        _windowModeDropdown.AddOptions(new List<string> { "視窗", "全螢幕" });
    }

    private void LoadAndApplySavedSettings()
    {
        int resIndex = PlayerPrefs.GetInt(PrefKeyResIndex, FindClosestIndexToCurrent());
        resIndex = Mathf.Clamp(resIndex, 0, _resolutions.Count - 1);

        bool fullscreen = PlayerPrefs.GetInt(PrefKeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;

        _resolutionDropdown.SetValueWithoutNotify(resIndex);
        _windowModeDropdown.SetValueWithoutNotify(fullscreen ? 1 : 0);

        ApplyResolution(resIndex, fullscreen);
    }

    private int FindClosestIndexToCurrent()
    {
        for (int i = 0; i < _resolutions.Count; i++)
        {
            if (_resolutions[i].width == Screen.width && _resolutions[i].height == Screen.height)
                return i;
        }

        return _resolutions.Count - 1;
    }

    // ─────────────────────────────────────────────
    //  UI 事件
    // ─────────────────────────────────────────────

    private void OnResolutionChanged(int index)
    {
        ApplyResolution(index, _windowModeDropdown.value == 1);
    }

    private void OnWindowModeChanged(int index)
    {
        ApplyResolution(_resolutionDropdown.value, index == 1);
    }

    // ─────────────────────────────────────────────
    //  套用設定
    // ─────────────────────────────────────────────

    private void ApplyResolution(int resIndex, bool fullscreen)
    {
        if (resIndex < 0 || resIndex >= _resolutions.Count) return;

        var res = _resolutions[resIndex];
        var mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(res.width, res.height, mode);

        PlayerPrefs.SetInt(PrefKeyResIndex, resIndex);
        PlayerPrefs.SetInt(PrefKeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var res in _resolutions)
        {
            if (res.height == 0) continue;

            float aspect = (float)res.width / res.height;
            if (Mathf.Abs(aspect - 16f / 9f) > 0.01f)
                Debug.LogWarning($"[DisplaySettingsController] 解析度 {res.width}x{res.height} 不是 16:9 比例（與 1920x1080 不同比例）。");
        }
    }
#endif
}
