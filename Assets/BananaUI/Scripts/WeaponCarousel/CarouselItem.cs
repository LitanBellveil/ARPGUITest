using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CarouselItem : MonoBehaviour
{
    [Header("Animations")]
    public UnityEvent onSelected;
    public UnityEvent onDeselected;

    [Header("Alpha")]
    [Range(0f, 1f)] [SerializeField] private float selectedAlpha = 1f;
    [Range(0f, 1f)] [SerializeField] private float deselectedAlpha = 0.4f;
    [SerializeField] private float alphaDuration = 0.25f;

    public WeaponData Data { get; private set; }
    public RectTransform RectTransform { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }

    public event Action<CarouselItem> OnClicked;

    private Coroutine alphaCoroutine;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void SetData(WeaponData data) => Data = data;

    public void PlaySelected()
    {
        onSelected?.Invoke();
        SetAlpha(selectedAlpha);
    }

    public void PlayUnselected()
    {
        onDeselected?.Invoke();
        SetAlpha(deselectedAlpha);
    }

    private void SetAlpha(float target)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        alphaCoroutine = StartCoroutine(FadeAlpha(target));
    }

    private IEnumerator FadeAlpha(float target)
    {
        float start = CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < alphaDuration)
        {
            elapsed += Time.deltaTime;
            CanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / alphaDuration);
            yield return null;
        }

        CanvasGroup.alpha = target;
    }

    // 掛到 Button 元件的 OnClick
    public void HandleClick() => OnClicked?.Invoke(this);
}
