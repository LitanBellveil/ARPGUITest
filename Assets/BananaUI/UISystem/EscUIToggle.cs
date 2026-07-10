using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

// ─────────────────────────────────────────────────────────────────────────────
//  EscUIToggle
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 按 Esc 開關指定的 UI 物件（例如整個 UI 介面的 root）。
///
/// 行為：
///   - 第一次按 Esc：Target SetActive(true)，播放 Show Timeline（可留空，無進場動畫）。
///   - 再按一次 Esc：播放 Hide Timeline，播完後 Target 才 SetActive(false)。
///   - 播放 Hide Timeline 期間再按 Esc 不會有反應，避免動畫途中被打斷造成畫面異常。
///
/// 選填：若 Target 底下有 UITabSystem（分頁選單），可指定 Tab System 欄位，
/// 每次 Open() 都會先重置回預設分頁（例如角色介面），不論玩家上次停在哪一頁。
/// </summary>
public class EscUIToggle : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 欄位
    // ─────────────────────────────────────────────

    [Header("目標 UI")]
    [Tooltip("按 Esc 要開關的物件（例如整個 UI 介面的 root）")]
    [SerializeField] private GameObject _target;

    [Tooltip("一開始是否為開啟狀態")]
    [SerializeField] private bool _startOpen;

    [Header("分頁重置（選填）")]
    [Tooltip("Target 底下的分頁選單系統，留空則不做重置")]
    [SerializeField] private UITabSystem _tabSystem;

    [Tooltip("每次開啟選單時要重置到的分頁索引（對應 UITabSystem 的 Pages 清單）")]
    [SerializeField] private int _defaultPageIndex = 0;

    [Header("Timeline 設定")]
    [Tooltip("開啟時的進場動畫，留空則直接 SetActive(true) 不播放動畫")]
    [SerializeField] private PlayableDirector _showDirector;

    [Tooltip("關閉時的退場動畫，留空則直接 SetActive(false) 不播放動畫")]
    [SerializeField] private PlayableDirector _hideDirector;

    // ─────────────────────────────────────────────
    //  私有狀態
    // ─────────────────────────────────────────────

    private bool _isOpen;
    private bool _isClosing;
    private Coroutine _closeCoroutine;

    // ─────────────────────────────────────────────
    //  Unity 生命週期
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (_target == null)
        {
            Debug.LogWarning($"[EscUIToggle] {name}：Target 未設定。");
            return;
        }

        _isOpen = _startOpen;
        _target.SetActive(_startOpen);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    // ─────────────────────────────────────────────
    //  公開介面
    // ─────────────────────────────────────────────

    /// <summary>切換開/關（等同按一次 Esc）</summary>
    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    /// <summary>開啟 Target，播放 Show Timeline（若有設定）</summary>
    public void Open()
    {
        if (_target == null || _isOpen) return;

        if (_closeCoroutine != null)
        {
            StopCoroutine(_closeCoroutine);
            _closeCoroutine = null;
        }

        _isClosing = false;
        _isOpen = true;

        // 先重置回預設分頁，Target 打開的當下就已經是首頁，不會閃到上次停留的分頁
        _tabSystem?.ResetToDefaultPage(_defaultPageIndex);

        _hideDirector?.Stop();
        _target.SetActive(true);

        if (_showDirector != null)
        {
            _showDirector.Stop();
            _showDirector.Play();
        }
    }

    /// <summary>關閉 Target：播放 Hide Timeline，播完後才 SetActive(false)</summary>
    public void Close()
    {
        // 關閉動畫進行中，忽略重複觸發
        if (_target == null || !_isOpen || _isClosing) return;

        _isOpen = false;
        _isClosing = true;

        _showDirector?.Stop();

        _closeCoroutine = StartCoroutine(CloseRoutine());
    }

    // ─────────────────────────────────────────────
    //  私有協程
    // ─────────────────────────────────────────────

    private IEnumerator CloseRoutine()
    {
        if (_hideDirector != null && _hideDirector.playableAsset != null)
        {
            _hideDirector.Stop();
            _hideDirector.Play();

            yield return new WaitForSeconds((float)_hideDirector.playableAsset.duration);
        }

        _target.SetActive(false);

        _isClosing = false;
        _closeCoroutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_target == null)
            Debug.LogWarning($"[EscUIToggle] {name}：Target 未設定。");
    }
#endif
}
