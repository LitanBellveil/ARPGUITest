using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarouselManager : MonoBehaviour
{
    [Header("Carousel — 順序對應 Slot（Top → Center → Bottom...）")]
    [SerializeField] private List<CarouselItem> items;
    [SerializeField] private List<RectTransform> slots;
    [SerializeField] private int centerSlotIndex = 1;

    [Header("Off-Screen Anchors（loop wrap 用，放在可見區域外）")]
    [SerializeField] private RectTransform offScreenTop;
    [SerializeField] private RectTransform offScreenBottom;

    [Header("Weapon Data — 順序對應 items")]
    [SerializeField] private List<WeaponData> weaponDataList;

    [Header("Components")]
    [SerializeField] private WeaponSelectPanel weaponSelect;

    [Header("Intro")]
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private AnimationCurve introCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private ICarouselItemMover itemMover;
    private List<CarouselItem> currentOrder;
    private bool isBusy;
    private bool isInitialized;

    public CarouselItem CenterItem => currentOrder[centerSlotIndex];

    private void Awake()
    {
        itemMover = GetComponent<ICarouselItemMover>();
        if (itemMover == null)
            Debug.LogError("[CarouselManager] 同一 GameObject 上找不到 ICarouselItemMover，請掛上 LerpCarouselItemMover。");
    }

    private void Start()
    {
        if (items.Count != slots.Count)
        {
            Debug.LogError($"[CarouselManager] items({items.Count}) 與 slots({slots.Count}) 數量不一致。");
            return;
        }

        currentOrder = new List<CarouselItem>(items);

        for (int i = 0; i < items.Count; i++)
        {
            if (i < weaponDataList.Count)
                items[i].SetData(weaponDataList[i]);

            items[i].OnClicked += HandleItemClicked;
        }

        SnapAllToSlots();
        weaponSelect.Refresh(CenterItem.Data);

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (i == centerSlotIndex)
                currentOrder[i].PlaySelected();
            else
                currentOrder[i].PlayUnselected();
        }

        isInitialized = true;
        StartCoroutine(PlayIntro());
    }

    private void OnEnable()
    {
        if (!isInitialized) return;
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        isBusy = true;

        yield return new WaitForEndOfFrame();

        Vector2 centerPos = slots[centerSlotIndex].anchoredPosition;

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (i != centerSlotIndex)
                currentOrder[i].RectTransform.anchoredPosition = centerPos;
        }

        yield return null;

        Vector2[] targets = new Vector2[currentOrder.Count];
        Vector2[] starts  = new Vector2[currentOrder.Count];
        for (int i = 0; i < currentOrder.Count; i++)
        {
            starts[i]  = currentOrder[i].RectTransform.anchoredPosition;
            targets[i] = slots[i].anchoredPosition;
        }

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = introCurve.Evaluate(Mathf.Clamp01(elapsed / introDuration));
            for (int i = 0; i < currentOrder.Count; i++)
            {
                if (i == centerSlotIndex) continue;
                currentOrder[i].RectTransform.anchoredPosition = Vector2.Lerp(starts[i], targets[i], t);
            }
            yield return null;
        }

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (i == centerSlotIndex) continue;
            currentOrder[i].RectTransform.anchoredPosition = targets[i];
        }

        isBusy = false;
    }

    private void HandleItemClicked(CarouselItem clicked)
    {
        if (isBusy) return;

        int clickedIndex = currentOrder.IndexOf(clicked);
        if (clickedIndex == centerSlotIndex) return;

        StartCoroutine(SwitchFlow(clickedIndex));
    }

    private IEnumerator SwitchFlow(int clickedOrderIndex)
    {
        isBusy = true;

        int steps = clickedOrderIndex - centerSlotIndex;

        CenterItem.PlayUnselected();

        var oldOrder = new List<CarouselItem>(currentOrder);
        RotateOrder(steps);

        int[] pending = { 2 };
        StartCoroutine(RunThen(weaponSelect.Hide(),             () => pending[0]--));
        StartCoroutine(RunThen(MoveAllToSlots(oldOrder, steps), () => pending[0]--));
        yield return new WaitUntil(() => pending[0] <= 0);

        weaponSelect.Refresh(CenterItem.Data);

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (i == centerSlotIndex)
                currentOrder[i].PlaySelected();
            else
                currentOrder[i].PlayUnselected();
        }

        yield return StartCoroutine(weaponSelect.Show());

        isBusy = false;
    }

    private IEnumerator RunThen(IEnumerator coroutine, Action onDone)
    {
        yield return StartCoroutine(coroutine);
        onDone();
    }

    private void RotateOrder(int steps)
    {
        int count = currentOrder.Count;
        int normalized = ((steps % count) + count) % count;
        if (normalized == 0) return;

        var head = currentOrder.GetRange(0, normalized);
        currentOrder.RemoveRange(0, normalized);
        currentOrder.AddRange(head);
    }

    private IEnumerator MoveAllToSlots(List<CarouselItem> oldOrder, int steps)
    {
        int count = currentOrder.Count;
        int[] pending = { count };

        for (int newIndex = 0; newIndex < count; newIndex++)
        {
            CarouselItem item = currentOrder[newIndex];
            int oldIndex = oldOrder.IndexOf(item);
            Vector2 targetPos = slots[newIndex].anchoredPosition;

            bool wrapTopToBottom = steps > 0 && oldIndex < steps;
            bool wrapBottomToTop = steps < 0 && oldIndex >= count + steps;

            if (wrapTopToBottom)
                StartCoroutine(WrapOne(item,
                    offScreenTop.anchoredPosition,
                    offScreenBottom.anchoredPosition,
                    targetPos, () => pending[0]--));
            else if (wrapBottomToTop)
                StartCoroutine(WrapOne(item,
                    offScreenBottom.anchoredPosition,
                    offScreenTop.anchoredPosition,
                    targetPos, () => pending[0]--));
            else
                StartCoroutine(MoveOne(item, targetPos, () => pending[0]--));
        }

        yield return new WaitUntil(() => pending[0] <= 0);
    }

    private IEnumerator MoveOne(CarouselItem item, Vector2 targetPos, Action onDone)
    {
        yield return StartCoroutine(itemMover.MoveItem(item.RectTransform, targetPos));
        onDone?.Invoke();
    }

    private IEnumerator WrapOne(CarouselItem item, Vector2 exitPos, Vector2 enterPos,
                                Vector2 targetPos, Action onDone)
    {
        yield return StartCoroutine(
            itemMover.WrapItem(item.RectTransform, item.CanvasGroup, exitPos, enterPos, targetPos));
        onDone?.Invoke();
    }

    private void SnapAllToSlots()
    {
        for (int i = 0; i < currentOrder.Count; i++)
            currentOrder[i].RectTransform.anchoredPosition = slots[i].anchoredPosition;
    }
}
