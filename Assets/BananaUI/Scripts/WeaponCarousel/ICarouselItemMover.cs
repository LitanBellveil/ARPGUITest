using System.Collections;
using UnityEngine;

public interface ICarouselItemMover
{
    // 一般位移（非 wrap item）
    IEnumerator MoveItem(RectTransform item, Vector2 targetPos);

    // Loop wrap：fade out 移到 exitPos → 瞬移 enterPos → fade in 移到 targetPos
    IEnumerator WrapItem(RectTransform item, CanvasGroup canvasGroup,
                         Vector2 exitPos, Vector2 enterPos, Vector2 targetPos);
}
