// 需要安裝 DOTween 才能使用此元件。
// 使用方式：刪除 LerpCarouselItemMover，改掛此元件，並指定給 CarouselManager 的 itemMoverBehaviour。

// 安裝 DOTween 後此檔案會自動啟用（DOTween 安裝時會自動加入 DOTWEEN 定義符號）
#if DOTWEEN

using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// DOTween 版移動實作。
/// 替換 LerpCarouselItemMover 時只需更換此元件即可，Manager 不需修改。
/// </summary>
public class DOTweenCarouselItemMover : MonoBehaviour, ICarouselItemMover
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    public IEnumerator MoveItem(RectTransform item, Vector2 targetPos)
    {
        yield return item.DOAnchorPos(targetPos, duration).SetEase(ease).WaitForCompletion();
    }
}

#endif
