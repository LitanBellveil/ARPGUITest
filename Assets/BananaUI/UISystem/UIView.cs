using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

// ─────────────────────────────────────────────────────────────────────────────
//  資料結構：單一 Animator + Trigger 的對應
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class AnimatorTriggerEntry
{
    [Tooltip("要觸發的目標 Animator")]
    public Animator animator;

    [Tooltip("要呼叫的 Trigger 名稱")]
    public string trigger;
}

// ─────────────────────────────────────────────────────────────────────────────
//  UIView
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 單一頁面的邏輯控制器。
///
/// 因為 HUD Canvas 與 Page Canvas 分離，
/// UIView 本身不隸屬於任何 Canvas，
/// 而是透過 _hudRoot / _pageRoot 分別控制兩邊的 GameObject。
///
/// Hierarchy 建議：
///   Scene
///   ├── HUD Canvas
///   │   └── CharacterHUD        ← _hudRoot
///   ├── Page Canvas
///   │   └── CharacterPage       ← _pageRoot
///   └── UIViews（空物件）
///       └── CharacterView       ← 掛此腳本（純邏輯，不在任何 Canvas 下）
/// </summary>
public class UIView : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 欄位
    // ─────────────────────────────────────────────

    [Header("根物件（分屬不同 Canvas）")]
    [Tooltip("此頁面在 HUD Canvas 下的根物件，留空則略過")]
    [SerializeField] private GameObject _hudRoot;

    [Tooltip("此頁面在 Page Canvas 下的根物件，留空則略過")]
    [SerializeField] private GameObject _pageRoot;

    [Header("Timeline 設定")]
    [Tooltip("進場動畫 PlayableDirector（Show），留空則無進場動畫")]
    [SerializeField] private PlayableDirector _showDirector;

    [Tooltip("退場動畫 PlayableDirector（Hide），留空則無退場動畫")]
    [SerializeField] private PlayableDirector _hideDirector;

    [Header("Animator Triggers（選填，可新增多筆）")]
    [Tooltip("Show 時依序觸發的 Animator Trigger 清單")]
    [SerializeField] private List<AnimatorTriggerEntry> _showTriggers = new();

    [Tooltip("Hide 時依序觸發的 Animator Trigger 清單")]
    [SerializeField] private List<AnimatorTriggerEntry> _hideTriggers = new();

    // ─────────────────────────────────────────────
    //  公開屬性
    // ─────────────────────────────────────────────

    /// <summary>此頁面目前是否可見（已呼叫 Show 且尚未完成 Hide）</summary>
    public bool IsVisible { get; private set; }

    /// <summary>Hide Timeline 的持續時間（秒），由 PlayableAsset 自動讀取</summary>
    public float HideDuration
    {
        get
        {
            if (_hideDirector != null && _hideDirector.playableAsset != null)
                return (float)_hideDirector.playableAsset.duration;
            return 0f;
        }
    }

    // ─────────────────────────────────────────────
    //  私有狀態
    // ─────────────────────────────────────────────

    private Coroutine _hideCoroutine;

    // ─────────────────────────────────────────────
    //  公開介面
    // ─────────────────────────────────────────────

    /// <summary>
    /// 顯示此頁面。
    /// 1. 中斷任何進行中的 Hide 協程。
    /// 2. HudRoot + PageRoot 同時 SetActive(true)。
    /// 3. 播放 Show Timeline。
    /// 4. 觸發所有 Show Animator Triggers。
    /// </summary>
    public void Show()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        SetRootsActive(true);
        IsVisible = true;

        if (_showDirector != null)
        {
            _showDirector.Stop();
            _showDirector.Play();
        }

        FireTriggers(_showTriggers);

        OnShow();
    }

    /// <summary>
    /// 隱藏此頁面。
    /// 播放 Hide Timeline，播放完畢後兩個 root 同時 SetActive(false)。
    /// </summary>
    /// <param name="onComplete">完成後的 callback（可為 null）</param>
    public void Hide(Action onComplete = null)
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }

        _showDirector?.Stop();

        FireTriggers(_hideTriggers);

        OnHideBegin();

        _hideCoroutine = StartCoroutine(HideRoutine(onComplete));
    }

    /// <summary>
    /// 立即隱藏，不播放動畫。
    /// 用於初始化時關閉非預設頁面。
    /// </summary>
    public void HideImmediate()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        _showDirector?.Stop();
        _hideDirector?.Stop();

        IsVisible = false;
        SetRootsActive(false);

        OnHide();
    }

    /// <summary>
    /// 立即顯示，不播放動畫、不觸發 Trigger。
    /// 用於需要瞬間重置到某頁面初始狀態的情境（例如選單重新開啟前，重設回預設分頁）。
    /// </summary>
    public void ShowImmediate()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        _showDirector?.Stop();
        _hideDirector?.Stop();

        IsVisible = true;
        SetRootsActive(true);

        OnShow();
    }

    // ─────────────────────────────────────────────
    //  私有協程
    // ─────────────────────────────────────────────

    private IEnumerator HideRoutine(Action onComplete)
    {
        IsVisible = false;

        if (_hideDirector != null && _hideDirector.playableAsset != null)
        {
            float hideDuration = (float)_hideDirector.playableAsset.duration;

            _hideDirector.Stop();
            _hideDirector.Play();

            yield return new WaitForSeconds(hideDuration);
        }

        SetRootsActive(false);
        _hideCoroutine = null;

        OnHide();

        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────
    //  子類別擴充點
    // ─────────────────────────────────────────────

    /// <summary>
    /// Show() 執行完畢後呼叫（SetActive + 播放 Timeline + 觸發 Trigger 之後）。
    /// 子類別可覆寫以在頁面顯示後執行額外邏輯，預設不做任何事。
    /// </summary>
    protected virtual void OnShow() { }

    /// <summary>
    /// Hide() 被呼叫的當下立即執行，早於 Hide Timeline 播放、早於 SetRootsActive(false)。
    /// 子類別可覆寫，在頁面「開始」隱藏的瞬間就觸發自己的退場動畫（例如子物件的 Animator Trigger），
    /// 不必等到 Hide Timeline 播完、Root 已經 SetActive(false) 之後才觸發（此時 GameObject 已停用，
    /// Animator 不會再播放，動畫等於白觸發）。
    /// </summary>
    protected virtual void OnHideBegin() { }

    /// <summary>
    /// 頁面完全隱藏後呼叫（不論是 Hide() 播放完 Timeline，或 HideImmediate() 立即隱藏）。
    /// 子類別可覆寫以在頁面隱藏後執行額外邏輯，預設不做任何事。
    /// </summary>
    protected virtual void OnHide() { }

    // ─────────────────────────────────────────────
    //  私有工具
    // ─────────────────────────────────────────────

    /// <summary>
    /// 同時控制 HudRoot 與 PageRoot 的 Active 狀態。
    /// 任一為 null 則略過，不報錯。
    /// </summary>
    private void SetRootsActive(bool active)
    {
        _hudRoot?.SetActive(active);
        _pageRoot?.SetActive(active);
    }

    /// <summary>
    /// 依序觸發清單中所有的 Animator Trigger。
    /// Animator 或 Trigger 名稱任一為空的項目會略過，不報錯。
    /// </summary>
    private void FireTriggers(List<AnimatorTriggerEntry> entries)
    {
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry.animator == null || string.IsNullOrEmpty(entry.trigger)) continue;
            entry.animator.SetTrigger(entry.trigger);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_hudRoot == null && _pageRoot == null)
            Debug.LogWarning($"[UIView] {name}：HUD Root 與 Page Root 皆未設定。");
    }
#endif
}
