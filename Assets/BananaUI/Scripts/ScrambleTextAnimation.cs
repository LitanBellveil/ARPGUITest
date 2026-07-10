using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
//  ScrambleTextAnimation
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// TMP 文字的亂碼還原效果：播放時先以亂碼字元覆蓋整段文字，
/// 隨時間逐字定案回 TMP 元件原本打好的文字，尚未定案的字元可套用另一個字型（例如亂碼專用符號字型）。
///
/// 用法：
///   1. 掛在有 TMP_Text（TextMeshProUGUI / TextMeshPro）的物件上，Inspector 打好最終要顯示的文字。
///   2. 在 Animation Clip 上加一個 Animation Event，呼叫 PlayScramble()（無參數，符合 Animation Event 規則）。
///   3. 也可以直接程式呼叫 Play("自訂文字") 播放任意文字。
/// </summary>
public class ScrambleTextAnimation : MonoBehaviour
{
    private enum RevealOrder
    {
        LeftToRight,
        RightToLeft,
        Random,
    }

    [Header("目標文字")]
    [Tooltip("留空則自動抓取同物件上的 TMP_Text")]
    [SerializeField] private TMP_Text _targetText;

    [Header("亂碼字元設定")]
    [Tooltip("亂碼字元池，播放時會從這裡隨機抽字元顯示")]
    [SerializeField] private string _scrambleCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";

    [Tooltip("亂碼字元使用的字型，留空則跟原本文字用同一個字型")]
    [SerializeField] private TMP_FontAsset _scrambleFontAsset;

    [Tooltip("空白字元（含全形空白）不會被亂碼取代，維持原本的排版間距")]
    [SerializeField] private bool _skipWhitespace = true;

    [Header("播放設定")]
    [Tooltip("勾選後，物件啟用（OnEnable）時會自動播放一次亂碼效果；取消勾選則需自行呼叫 PlayScramble() 或 Play()")]
    [SerializeField] private bool _playOnEnable = true;

    [Header("速度 / 節奏設定")]
    [Tooltip("整段動畫的總時長（秒），決定文字完全定案所需時間")]
    [SerializeField, Min(0.01f)] private float _duration = 1.2f;

    [Tooltip("亂碼字元的更新頻率（每秒抽換幾次），數字越大跳動越快")]
    [SerializeField, Min(1f)] private float _scrambleRefreshRate = 24f;

    [Tooltip("字元定案的分佈曲線：橫軸為字元順位（0~1），縱軸為該字元定案所需的時間比例（0~1）")]
    [SerializeField] private AnimationCurve _revealCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("字元定案的順序")]
    [SerializeField] private RevealOrder _revealOrder = RevealOrder.LeftToRight;

    [Header("事件")]
    [Tooltip("開始播放時觸發")]
    public UnityEvent onScrambleStart;

    [Tooltip("文字完全定案時觸發")]
    public UnityEvent onScrambleComplete;

    private string _finalText;
    private Coroutine _routine;
    private readonly System.Random _random = new();

    private void Awake()
    {
        if (_targetText == null)
            _targetText = GetComponent<TMP_Text>();

        if (_targetText == null)
        {
            Debug.LogWarning($"[ScrambleTextAnimation] {name} 找不到 TMP_Text 元件，效果不會執行。");
            return;
        }

        if (_scrambleFontAsset != null)
            MaterialReferenceManager.AddFontAsset(_scrambleFontAsset);

        _finalText = _targetText.text;
    }

    /// <summary>物件啟用時，若勾選了 _playOnEnable 就自動播放一次亂碼效果</summary>
    private void OnEnable()
    {
        if (_playOnEnable && _targetText != null)
            PlayScramble();
    }

    /// <summary>
    /// 物件被 SetActive(false) 時，Unity 會自動砍斷所有協程（例如切換 Tab/Page 時），
    /// 但畫面可能還停在「一半亂碼、一半原文，且 rich text font 標籤沒收尾」的中間狀態，
    /// 下次顯示時就會看到破碎或缺字的文字。這裡強制把文字收尾回最終文字，確保重新顯示時內容正確。
    /// </summary>
    private void OnDisable()
    {
        _routine = null;

        if (_targetText != null)
            _targetText.text = _finalText;
    }

    // ─────────────────────────────────────────────
    //  公開 API
    // ─────────────────────────────────────────────

    /// <summary>
    /// 供 Animation Event 呼叫：從 TMP 元件目前設定的文字（Inspector 打好的原文）開始播放亂碼→還原效果。
    /// 無參數，符合 Animation Event 只能呼叫零參數或單一基本型別參數方法的限制。
    /// </summary>
    public void PlayScramble()
    {
        Play(_finalText);
    }

    /// <summary>以指定文字播放亂碼→還原效果，供程式碼呼叫</summary>
    public void Play(string finalText)
    {
        _finalText = finalText ?? string.Empty;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(ScrambleRoutine());
    }

    /// <summary>立即中止動畫，直接顯示最終文字</summary>
    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _targetText.text = _finalText;
    }

    // ─────────────────────────────────────────────
    //  核心邏輯
    // ─────────────────────────────────────────────

    private IEnumerator ScrambleRoutine()
    {
        onScrambleStart?.Invoke();

        int length = _finalText.Length;
        float[] revealAt = BuildRevealSchedule(length);
        char[] currentScramble = new char[length];

        float elapsed = 0f;
        float scrambleInterval = 1f / _scrambleRefreshRate;
        float nextScrambleTime = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / _duration;

            if (elapsed >= nextScrambleTime)
            {
                RefreshScrambleChars(length, progress, revealAt, currentScramble);
                nextScrambleTime = elapsed + scrambleInterval;
            }

            ApplyText(progress, revealAt, currentScramble);
            yield return null;
        }

        _targetText.text = _finalText;
        _routine = null;

        onScrambleComplete?.Invoke();
    }

    /// <summary>依 _revealOrder 決定「第幾個定案」對應到哪個字元索引，再用 _revealCurve 換算成時間比例</summary>
    private float[] BuildRevealSchedule(int length)
    {
        int[] order = new int[length];
        for (int i = 0; i < length; i++)
            order[i] = i;

        switch (_revealOrder)
        {
            case RevealOrder.RightToLeft:
                Array.Reverse(order);
                break;
            case RevealOrder.Random:
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                break;
        }

        float[] revealAt = new float[length];
        for (int rank = 0; rank < length; rank++)
        {
            float t = length <= 1 ? 0f : (float)rank / (length - 1);
            revealAt[order[rank]] = _revealCurve.Evaluate(t);
        }

        return revealAt;
    }

    private void RefreshScrambleChars(int length, float progress, float[] revealAt, char[] currentScramble)
    {
        for (int i = 0; i < length; i++)
        {
            if (progress >= revealAt[i]) continue;

            currentScramble[i] = _skipWhitespace && char.IsWhiteSpace(_finalText[i])
                ? _finalText[i]
                : _scrambleCharacters[_random.Next(_scrambleCharacters.Length)];
        }
    }

    /// <summary>組出目前這一幀要顯示的字串，未定案的字元用亂碼字型的 Rich Text 標籤包起來</summary>
    private void ApplyText(float progress, float[] revealAt, char[] currentScramble)
    {
        var sb = new StringBuilder(_finalText.Length + 16);
        bool inScrambleFontBlock = false;

        for (int i = 0; i < _finalText.Length; i++)
        {
            bool revealed = progress >= revealAt[i];
            char c = revealed ? _finalText[i] : currentScramble[i];
            bool useScrambleFont = !revealed && _scrambleFontAsset != null;

            if (useScrambleFont && !inScrambleFontBlock)
            {
                sb.Append("<font=\"").Append(_scrambleFontAsset.name).Append("\">");
                inScrambleFontBlock = true;
            }
            else if (!useScrambleFont && inScrambleFontBlock)
            {
                sb.Append("</font>");
                inScrambleFontBlock = false;
            }

            sb.Append(c);
        }

        if (inScrambleFontBlock)
            sb.Append("</font>");

        _targetText.text = sb.ToString();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_targetText == null)
            _targetText = GetComponent<TMP_Text>();
    }
#endif
}
