using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

// ─────────────────────────────────────────────────────────────────────────────
//  資料結構：Inspector 中每一組 Tab ↔ Page 的對應
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class TabPage
{
    [Tooltip("此分頁對應的 Toggle UI 元件")]
    public Toggle toggle;

    [Tooltip("此分頁對應的 UIView 頁面")]
    public UIView page;

    [Tooltip("切換到此分頁時的預設 Virtual Camera（未在 Camera Transitions 設定對應路徑時使用）")]
    public CinemachineCamera defaultCamera;
}

// ─────────────────────────────────────────────────────────────────────────────
//  資料結構：指定「fromPages → toPages」路徑所使用的 Virtual Camera
//  fromPages、toPages 皆可設定一個以上，來源與目標頁面各自命中清單中任一項即套用此 Camera。
//
//  用途範例：
//    fromPages = [VC_Character],           toPages = [VC_Setting]              → 使用 VC_Setting_FromCharacter
//    fromPages = [VC_Weapon],               toPages = [VC_Setting]              → 使用 VC_Setting_FromWeapon
//    fromPages = [VC_Setting],              toPages = [VC_Character, VC_Weapon] → 使用同一顆 Camera
//    fromPages = [VC_Character, VC_Weapon], toPages = [VC_Setting]              → 兩個來源共用同一顆 Camera
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class CameraTransition
{
    [Tooltip("來源頁面（從哪個頁面切換過來，可設定一個以上，任一個符合即套用此路徑）")]
    public List<UIView> fromPages = new();

    [Tooltip("目標頁面（要切換到的頁面，可設定一個以上，任一個符合即套用此路徑）")]
    public List<UIView> toPages = new();

    [Tooltip("此路徑專用的 Virtual Camera（不會移動 Transform，只調整 Priority）")]
    public CinemachineCamera virtualCamera;
}

// ─────────────────────────────────────────────────────────────────────────────
//  UITabSystem：管理所有 Tab 切換邏輯
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 管理 Tab Toggle 與 UIView 頁面切換的核心系統。
///
/// Camera 切換優先順序（只調整 Priority，不移動 Transform）：
///   1. 查詢 _cameraTransitions，找到 fromPage 落於 fromPages、toPage 落於 toPages 的項目 → 使用該 Camera
///   2. 退回使用 toPage 在 _pages 中設定的 defaultCamera
///   3. 兩者皆為 null → 不改變相機狀態
///
/// 切換流程（新頁先開、舊頁後關，無空窗）：
///   1. 根據 (CurrentPage → targetPage) 找到正確 Camera 並切換 Priority
///   2. 新頁 SetActive(true) → 播放 Show Timeline
///   3. 等待 overlapDelay（讓兩頁短暫重疊，避免空窗）
///   4. 舊頁播放 Hide Timeline
///   5. 等待 Hide Timeline 播放完畢 → 舊頁 SetActive(false)
///   6. CurrentPage 更新為新頁
///
/// 防呆設計：
///   - target == currentPage → 直接 return
///   - 切換中再次切換 → 中止前一次切換，立即以新目標重新開始
/// </summary>
public class UITabSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 欄位
    // ─────────────────────────────────────────────

    [Header("分頁設定")]
    [Tooltip("所有 Tab ↔ Page 的對應清單，依序填入")]
    [SerializeField] private List<TabPage> _pages = new();

    [Header("相機切換路徑設定")]
    [Tooltip("指定特定 fromPage → toPage 路徑所使用的 Virtual Camera（優先於 defaultCamera）")]
    [SerializeField] private List<CameraTransition> _cameraTransitions = new();

    [Header("切換時序設定")]
    [Tooltip("新頁開始顯示後，等待多久才開始播放舊頁退場動畫（秒）")]
    [SerializeField, Range(0f, 1f)] private float _overlapDelay = 0.1f;

    // ─────────────────────────────────────────────
    //  公開屬性
    // ─────────────────────────────────────────────

    /// <summary>目前顯示中的頁面</summary>
    public UIView CurrentPage { get; private set; }

    /// <summary>目前是否正在切換頁面中</summary>
    public bool IsSwitching => _switchCoroutine != null;

    // ─────────────────────────────────────────────
    //  私有狀態
    // ─────────────────────────────────────────────

    /// <summary>正在執行的切換協程（用於快速連點時中斷舊流程）</summary>
    private Coroutine _switchCoroutine;

    /// <summary>切換中需要被隱藏的舊頁（協程共用狀態）</summary>
    private UIView _pendingHidePage;

    // ─────────────────────────────────────────────
    //  Unity 生命週期
    // ─────────────────────────────────────────────

    private void Start()
    {
        InitializeTabs();
    }

    // ─────────────────────────────────────────────
    //  初始化
    // ─────────────────────────────────────────────

    /// <summary>
    /// 初始化所有 Tab 與 Page：
    ///   - 讀取預設 On 的 Toggle → 顯示對應頁面
    ///   - 其餘頁面 HideImmediate()（不播動畫，直接關閉）
    ///   - 為每個 Toggle 註冊 onValueChanged 事件
    /// </summary>
    private void InitializeTabs()
    {
        if (_pages == null || _pages.Count == 0)
        {
            Debug.LogWarning("[UITabSystem] Pages 清單為空，請在 Inspector 中設定。");
            return;
        }

        UIView defaultPage = null;

        foreach (var tabPage in _pages)
        {
            if (tabPage.toggle == null || tabPage.page == null)
            {
                Debug.LogWarning("[UITabSystem] 有 TabPage 的 Toggle 或 Page 未設定，已略過。");
                continue;
            }

            if (tabPage.toggle.isOn && defaultPage == null)
            {
                defaultPage = tabPage.page;
            }
            else
            {
                tabPage.page.HideImmediate();
            }

            // 使用區域變數捕獲，避免 Lambda 閉包問題
            UIView capturedPage = tabPage.page;

            tabPage.toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SwitchPage(capturedPage);
            });
        }

        // 顯示預設頁面（初始化時 fromPage = null，直接使用 defaultCamera）
        if (defaultPage != null)
        {
            defaultPage.Show();
            CurrentPage = defaultPage;
            ActivateCamera(null, defaultPage);
        }
        else
        {
            // 若沒有任何 Toggle 預設 On，顯示第一個頁面
            var first = _pages[0];
            if (first.toggle != null && first.page != null)
            {
                first.toggle.SetIsOnWithoutNotify(true);
                first.page.Show();
                CurrentPage = first.page;
                ActivateCamera(null, first.page);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  公開切換介面
    // ─────────────────────────────────────────────

    /// <summary>
    /// 切換至目標頁面。
    /// 若目標已是目前頁面則直接 return。
    /// 若切換中再次呼叫，會中斷舊流程，以新目標重新開始。
    /// </summary>
    /// <param name="target">要切換到的 UIView</param>
    public void SwitchPage(UIView target)
    {
        // ── 防呆：目標 == 目前頁面，不做任何事
        if (target == CurrentPage)
            return;

        // ── 若正在切換，中止舊流程
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;

            // 若舊的「待關閉頁」仍存在（尚未被關閉），立即強制關閉
            if (_pendingHidePage != null && _pendingHidePage != target)
            {
                _pendingHidePage.HideImmediate();
                _pendingHidePage = null;
            }
        }

        _switchCoroutine = StartCoroutine(SwitchRoutine(target));
    }

    // ─────────────────────────────────────────────
    //  切換協程
    // ─────────────────────────────────────────────

    /// <summary>
    /// 核心切換流程（詳見類別說明）
    /// </summary>
    private IEnumerator SwitchRoutine(UIView nextPage)
    {
        UIView previousPage = CurrentPage;

        // 記錄待關閉頁（供快速連點中斷使用）
        _pendingHidePage = previousPage;

        // ── Step 1：根據 (previousPage → nextPage) 找到正確 Camera 並切換
        //           Camera 切換在 Show 之前，確保畫面出現時已是正確視角
        ActivateCamera(previousPage, nextPage);
        nextPage.Show();

        // 暫時更新 CurrentPage，讓快速連點的防呆判斷生效
        CurrentPage = nextPage;

        // ── Step 2：等待 overlapDelay（讓兩頁短暫重疊，避免空窗）
        if (_overlapDelay > 0f)
            yield return new WaitForSeconds(_overlapDelay);

        // ── Step 3：舊頁播放 Hide Timeline
        if (previousPage != null)
        {
            previousPage.Hide(() =>
            {
                // Hide 完成 callback：清除待關閉頁記錄
                if (_pendingHidePage == previousPage)
                    _pendingHidePage = null;
            });

            // ── Step 4：等待 Hide Timeline 播放完畢
            float hideDuration = previousPage.HideDuration;
            if (hideDuration > 0f)
                yield return new WaitForSeconds(hideDuration);
        }

        // ── Step 5：切換完成，清除協程記錄
        _pendingHidePage = null;
        _switchCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  公用工具
    // ─────────────────────────────────────────────

    /// <summary>
    /// 透過索引切換頁面（方便程式碼呼叫）
    /// </summary>
    /// <param name="index">Pages 清單中的索引</param>
    public void SwitchPageByIndex(int index)
    {
        if (index < 0 || index >= _pages.Count)
        {
            Debug.LogWarning($"[UITabSystem] 索引 {index} 超出 Pages 範圍。");
            return;
        }

        var tabPage = _pages[index];
        if (tabPage.page == null) return;

        // 同步更新 Toggle 狀態（不觸發事件，避免循環呼叫）
        tabPage.toggle?.SetIsOnWithoutNotify(true);

        for (int i = 0; i < _pages.Count; i++)
        {
            if (i != index)
                _pages[i].toggle?.SetIsOnWithoutNotify(false);
        }

        SwitchPage(tabPage.page);
    }

    /// <summary>
    /// 立即重置回指定分頁（預設索引 0），不播放切換動畫。
    /// 用於選單每次重新開啟前，強制回到首頁（例如角色介面），
    /// 不論玩家上次關閉選單時停在哪一頁。
    /// </summary>
    /// <param name="index">要重置到的 Pages 索引，預設 0</param>
    public void ResetToDefaultPage(int index = 0)
    {
        if (_pages == null || _pages.Count == 0) return;
        if (index < 0 || index >= _pages.Count) index = 0;

        // 中斷任何進行中的切換協程，不留殘餘的待關閉頁
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
        }
        _pendingHidePage = null;

        var defaultTab = _pages[index];
        if (defaultTab.page == null) return;

        for (int i = 0; i < _pages.Count; i++)
        {
            var tabPage = _pages[i];
            if (tabPage.page == null) continue;

            if (i == index)
            {
                tabPage.page.ShowImmediate();
                tabPage.toggle?.SetIsOnWithoutNotify(true);
            }
            else
            {
                tabPage.page.HideImmediate();
                tabPage.toggle?.SetIsOnWithoutNotify(false);
            }
        }

        CurrentPage = defaultTab.page;

        // 相機也一併重置回預設路徑（fromPage 傳 null，直接採用 defaultCamera 或符合的 Transition）
        ActivateCamera(null, defaultTab.page);
    }

    // ─────────────────────────────────────────────
    //  Cinemachine 相機切換
    // ─────────────────────────────────────────────

    /// <summary>
    /// 根據切換路徑 (fromPage → toPage) 選擇並啟用正確的 Virtual Camera。
    ///
    /// 選擇邏輯（只修改 Priority，Camera Transform 完全不動）：
    ///   1. 在 _cameraTransitions 中尋找 fromPage 落於 fromPages、且 toPage 落於 toPages 的項目
    ///   2. 若找不到，使用 toPage 對應 TabPage 的 defaultCamera
    ///   3. 若兩者皆為 null，不改變相機狀態
    ///
    /// 所有已知的 Virtual Camera 會先全部降為 Priority 0，
    /// 再將目標 Camera 升至 Priority 10。
    /// </summary>
    /// <param name="fromPage">來源頁面，初始化時傳入 null</param>
    /// <param name="toPage">目標頁面</param>
    private void ActivateCamera(UIView fromPage, UIView toPage)
    {
        // ── 1. 嘗試從 Transition 表尋找精確路徑的 Camera
        CinemachineCamera targetVC = null;

        if (fromPage != null)
        {
            foreach (var transition in _cameraTransitions)
            {
                if (transition.fromPages != null &&
                    transition.fromPages.Contains(fromPage) &&
                    transition.toPages != null &&
                    transition.toPages.Contains(toPage))
                {
                    targetVC = transition.virtualCamera;
                    break;
                }
            }
        }

        // ── 2. Transition 未命中，退回 toPage 的 defaultCamera
        if (targetVC == null)
        {
            foreach (var tabPage in _pages)
            {
                if (tabPage.page == toPage)
                {
                    targetVC = tabPage.defaultCamera;
                    break;
                }
            }
        }

        // ── 3. 兩者皆為 null，不改變相機狀態
        if (targetVC == null) return;

        // ── 4. 將所有已知 Camera 降為 Priority 0
        ResetAllCameraPriorities();

        // ── 5. 啟用目標 Camera（Priority 10 > 其餘 0，Cinemachine Brain 自動 Blend）
        targetVC.Priority.Value = 10;
    }

    /// <summary>
    /// 將 _pages 的所有 defaultCamera 與 _cameraTransitions 的所有 virtualCamera
    /// 全部設為 Priority 0，為下次切換做好準備。
    /// </summary>
    private void ResetAllCameraPriorities()
    {
        foreach (var tabPage in _pages)
        {
            if (tabPage.defaultCamera != null)
                tabPage.defaultCamera.Priority.Value = 0;
        }

        foreach (var transition in _cameraTransitions)
        {
            if (transition.virtualCamera != null)
                transition.virtualCamera.Priority.Value = 0;
        }
    }

#if UNITY_EDITOR
    // ─────────────────────────────────────────────
    //  Editor 輔助（僅在 Editor 執行）
    // ─────────────────────────────────────────────

    private void OnValidate()
    {
        if (_pages != null)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                var p = _pages[i];
                if (p.toggle == null)
                    Debug.LogWarning($"[UITabSystem] Pages[{i}] 的 Toggle 未設定。");
                if (p.page == null)
                    Debug.LogWarning($"[UITabSystem] Pages[{i}] 的 Page 未設定。");
            }
        }

        if (_cameraTransitions != null)
        {
            for (int i = 0; i < _cameraTransitions.Count; i++)
            {
                var t = _cameraTransitions[i];
                if (t.fromPages == null || t.fromPages.Count == 0)
                    Debug.LogWarning($"[UITabSystem] CameraTransitions[{i}] 的 FromPages 未設定任何頁面。");
                else if (t.fromPages.Exists(p => p == null))
                    Debug.LogWarning($"[UITabSystem] CameraTransitions[{i}] 的 FromPages 中有未設定的項目。");
                if (t.toPages == null || t.toPages.Count == 0)
                    Debug.LogWarning($"[UITabSystem] CameraTransitions[{i}] 的 ToPages 未設定任何頁面。");
                else if (t.toPages.Exists(p => p == null))
                    Debug.LogWarning($"[UITabSystem] CameraTransitions[{i}] 的 ToPages 中有未設定的項目。");
                if (t.virtualCamera == null)
                    Debug.LogWarning($"[UITabSystem] CameraTransitions[{i}] 的 VirtualCamera 未設定。");
            }
        }
    }
#endif
}
