using System.Collections;
using UnityEngine;

public class LerpCarouselItemMover : MonoBehaviour, ICarouselItemMover
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public IEnumerator MoveItem(RectTransform item, Vector2 targetPos)
    {
        Vector2 startPos = item.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            item.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, t);
            yield return null;
        }

        item.anchoredPosition = targetPos;
    }

    /// <summary>
    /// Loop wrap 動畫，總時間與 MoveItem 相同。
    /// Phase 1（前半）：從現在位置移到 exitPos，同時 alpha 1 → 0
    /// Phase 2（後半）：瞬移到 enterPos，再移到 targetPos，同時 alpha 0 → 1
    /// </summary>
    public IEnumerator WrapItem(RectTransform item, CanvasGroup cg,
                                Vector2 exitPos, Vector2 enterPos, Vector2 targetPos)
    {
        float half = duration * 0.5f;

        // Phase 1: exit
        Vector2 startPos = item.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            item.anchoredPosition = Vector2.Lerp(startPos, exitPos, curve.Evaluate(t));
            if (cg != null) cg.alpha = 1f - t;
            yield return null;
        }
        if (cg != null) cg.alpha = 0f;

        // 瞬移到入場位置
        item.anchoredPosition = enterPos;

        // Phase 2: enter
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            item.anchoredPosition = Vector2.Lerp(enterPos, targetPos, curve.Evaluate(t));
            if (cg != null) cg.alpha = t;
            yield return null;
        }
        item.anchoredPosition = targetPos;
        if (cg != null) cg.alpha = 1f;
    }
}
