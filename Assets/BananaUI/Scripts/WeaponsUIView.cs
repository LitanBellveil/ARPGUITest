using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
//  資料結構：單一 Tab ↔ Page ↔ WeaponGroup 的對應
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class WeaponTab
{
    [Tooltip("此分類對應的 Tab 按鈕")]
    public Button button;

    [Tooltip("此分類對應的 2D UI 物件，切換時直接 SetActive 開關，無動畫")]
    public GameObject page;

    [Tooltip("此分類對應的武器展示物件")]
    public GameObject weaponGroup;

    [Header("WeaponGroup Animation（選填，留空則直接切換 Active）")]
    [Tooltip("WeaponGroup 身上的 Animator，留空則不播放動畫，直接 SetActive")]
    public Animator weaponGroupAnimator;

    [Tooltip("進場動畫的 Trigger 參數名稱（需在 Animator Controller 的 Parameters 中定義）")]
    public string weaponGroupShowTrigger = "Show";

    [Tooltip("退場動畫的 Trigger 參數名稱（需在 Animator Controller 的 Parameters 中定義）")]
    public string weaponGroupHideTrigger = "Hide";
}

// ─────────────────────────────────────────────────────────────────────────────
//  WeaponsUIView
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Weapons 頁面：管理分類 Tab 按鈕、對應 Page、對應 WeaponGroup 的同步切換。
/// 取代 WeaponGroupController，Tab 數量完全由 Inspector 的 _weaponTabs 清單決定，不寫死。
///
/// 切換流程：
///   Page：       純 SetActive 開關，無動畫，同一個 frame 內完成。
///   WeaponGroup：只有 Awake() 的初始化會真正 SetActive(false)；
///                之後不論是切換 Tab 或離開整個 Weapons 頁面，
///                舊 Group 一律只觸發 Hide Trigger，不再呼叫 SetActive(false)，
///                避免動畫/VFX 粒子還沒播完就被切斷。
///                （視覺上的隱藏交給 Hide 動畫本身，或 VFX 自身的 Stop Action 處理。）
/// </summary>
public class WeaponsUIView : UIView
{
    // ─────────────────────────────────────────────
    //  Inspector 欄位
    // ─────────────────────────────────────────────

    [Header("Weapon Tabs")]
    [Tooltip("所有 Tab ↔ Page ↔ WeaponGroup 的對應清單，依序填入，可自由增減")]
    [SerializeField] private List<WeaponTab> _weaponTabs = new();

    [Tooltip("此 UIView 顯示時，預設開啟的 Tab 索引")]
    [SerializeField] private int _defaultTabIndex = 0;

    // ─────────────────────────────────────────────
    //  公開屬性
    // ─────────────────────────────────────────────

    /// <summary>目前選中的 Tab 索引，尚未初始化時為 -1</summary>
    public int CurrentTabIndex { get; private set; } = -1;

    // ─────────────────────────────────────────────
    //  Unity 生命週期
    // ─────────────────────────────────────────────

    private void Awake()
    {
        for (int i = 0; i < _weaponTabs.Count; i++)
        {
            var tab = _weaponTabs[i];
            if (tab.button == null) continue;

            int capturedIndex = i;
            tab.button.onClick.AddListener(() => SelectTab(capturedIndex));
        }

        // 唯一一次真正關閉：往後切換 Tab 或離開頁面都不再 SetActive(false) weaponGroup。
        foreach (var tab in _weaponTabs)
        {
            if (tab.page != null)
                tab.page.SetActive(false);
            if (tab.weaponGroup != null)
                tab.weaponGroup.SetActive(false);
        }

        CurrentTabIndex = -1;
    }

    // ─────────────────────────────────────────────
    //  UIView 生命週期擴充
    // ─────────────────────────────────────────────

    /// <summary>
    /// WeaponsUIView 顯示後，開啟目前（或預設）Tab。
    /// 使用 force:true 讓即便索引與上次相同，也會重新播放 Show 流程。
    /// </summary>
    protected override void OnShow()
    {
        int index = CurrentTabIndex >= 0 ? CurrentTabIndex : _defaultTabIndex;
        SelectTab(index, force: true);
    }

    /// <summary>
    /// 離開 WeaponsUIView 的當下（Hide() 一呼叫就執行，早於 Hide Timeline、早於 Root 關閉），
    /// 立即對目前顯示中的 WeaponGroup 觸發 Hide Trigger，讓退場動畫馬上開始播放，
    /// 不必等整個頁面的 Hide Timeline 播完、Root 已經 SetActive(false)（那時候 Animator 已停用，觸發也沒用）。
    /// </summary>
    protected override void OnHideBegin()
    {
        if (CurrentTabIndex >= 0 && CurrentTabIndex < _weaponTabs.Count)
            HideWeaponGroup(_weaponTabs[CurrentTabIndex]);
    }

    /// <summary>
    /// WeaponsUIView 完全隱藏後（Hide Timeline 播完、Root 已關閉），把所有 Page 收乾淨。
    /// 保留 CurrentTabIndex，下次 OnShow 會回到同一個 Tab。
    /// </summary>
    protected override void OnHide()
    {
        foreach (var tab in _weaponTabs)
        {
            if (tab.page != null)
                tab.page.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────
    //  公開 API
    // ─────────────────────────────────────────────

    /// <summary>切換至指定索引的 Tab</summary>
    /// <param name="index">Weapon Tabs 清單中的索引</param>
    /// <param name="force">為 true 時，即使索引與目前相同也會重新播放 Show 流程</param>
    public void SelectTab(int index, bool force = false)
    {
        if (_weaponTabs == null || index < 0 || index >= _weaponTabs.Count)
        {
            Debug.LogWarning($"[WeaponsUIView] SelectTab 索引 {index} 超出 Weapon Tabs 範圍。");
            return;
        }

        if (!force && index == CurrentTabIndex) return;

        // previous == next 時（例如 force 重新開啟同一個 Tab）視為沒有舊分頁，
        // 避免對同一個 Page/WeaponGroup 重複播放 Hide 再 Show。
        WeaponTab previous = CurrentTabIndex >= 0 && CurrentTabIndex < _weaponTabs.Count && CurrentTabIndex != index
            ? _weaponTabs[CurrentTabIndex]
            : null;
        WeaponTab next = _weaponTabs[index];

        CurrentTabIndex = index;

        // Page、WeaponGroup 同一時間觸發，彼此不等待。
        SwitchPageImmediate(previous, next);
        SwitchWeaponGroup(previous, next);
    }

    /// <summary>切換到下一個 Tab（超出範圍時循環回第一個）</summary>
    public void NextTab()
    {
        if (_weaponTabs == null || _weaponTabs.Count == 0) return;
        int next = (CurrentTabIndex + 1 + _weaponTabs.Count) % _weaponTabs.Count;
        SelectTab(next);
    }

    /// <summary>切換到上一個 Tab（超出範圍時循環回最後一個）</summary>
    public void PreviousTab()
    {
        if (_weaponTabs == null || _weaponTabs.Count == 0) return;
        int prev = (CurrentTabIndex - 1 + _weaponTabs.Count) % _weaponTabs.Count;
        SelectTab(prev);
    }

    // ─────────────────────────────────────────────
    //  切換邏輯
    // ─────────────────────────────────────────────

    /// <summary>Page 純 SetActive 開關，無動畫，直接同步切換</summary>
    private void SwitchPageImmediate(WeaponTab previous, WeaponTab next)
    {
        if (previous != null && previous.page != null)
            previous.page.SetActive(false);

        if (next != null && next.page != null)
            next.page.SetActive(true);
    }

    /// <summary>
    /// 新 WeaponGroup 立即顯示並觸發 Show Trigger；
    /// 舊 WeaponGroup 只觸發 Hide Trigger，不再 SetActive(false)，
    /// 讓動畫與 VFX 能自然播完（視覺隱藏交給動畫本身或 VFX 的 Stop Action）。
    /// </summary>
    private void SwitchWeaponGroup(WeaponTab previous, WeaponTab next)
    {
        if (next != null && next.weaponGroup != null)
            ShowWeaponGroup(next);

        if (previous != null && previous != next && previous.weaponGroup != null)
            HideWeaponGroup(previous);
    }

    private void ShowWeaponGroup(WeaponTab tab)
    {
        tab.weaponGroup.SetActive(true);

        if (tab.weaponGroupAnimator != null && !string.IsNullOrEmpty(tab.weaponGroupShowTrigger))
            tab.weaponGroupAnimator.SetTrigger(tab.weaponGroupShowTrigger);
    }

    private void HideWeaponGroup(WeaponTab tab)
    {
        if (tab.weaponGroupAnimator != null && !string.IsNullOrEmpty(tab.weaponGroupHideTrigger))
            tab.weaponGroupAnimator.SetTrigger(tab.weaponGroupHideTrigger);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < _weaponTabs.Count; i++)
        {
            var tab = _weaponTabs[i];
            if (tab.button == null)
                Debug.LogWarning($"[WeaponsUIView] Weapon Tabs[{i}] 的 Button 未設定。");
            if (tab.page == null)
                Debug.LogWarning($"[WeaponsUIView] Weapon Tabs[{i}] 的 Page 未設定。");
            if (tab.weaponGroup == null)
                Debug.LogWarning($"[WeaponsUIView] Weapon Tabs[{i}] 的 WeaponGroup 未設定。");
        }
    }
#endif
}
