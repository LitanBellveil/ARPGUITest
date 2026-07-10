using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NavigationFramework
{
    /// <summary>
    /// Bridges a UI widget (Button, Toggle, Slider, TMP_InputField, or any custom widget) to the
    /// navigation framework. <c>NavigationManager</c> drives this component explicitly through
    /// <see cref="Select"/>, <see cref="Deselect"/>, <see cref="InvokeSubmit"/> and
    /// <see cref="InvokeCancel"/> — it never relies on Unity's <c>EventSystem</c> selection state,
    /// so gamepad, keyboard, and a future virtual cursor all funnel through the same explicit API
    /// instead of fighting over <c>EventSystem.SetSelectedGameObject</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class NavigationSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform rectTransform;

        /// <summary> Raised after this widget becomes the focused node. </summary>
        public event Action Selected;

        /// <summary> Raised after this widget stops being the focused node. </summary>
        public event Action Deselected;

        /// <summary> Raised when <c>NavigationManager.Submit()</c> is called while this widget is focused. </summary>
        public event Action Submitted;

        /// <summary> Raised when <c>NavigationManager.Cancel()</c> is called while this widget is focused. </summary>
        public event Action Cancelled;

        /// <summary> Raised when a pointer (mouse or a future virtual cursor) enters this widget's bounds. </summary>
        public event Action HoverEntered;

        /// <summary> Raised when a pointer (mouse or a future virtual cursor) exits this widget's bounds. </summary>
        public event Action HoverExited;

        /// <summary> Raised whenever the highlight state changes, carrying the new state. </summary>
        public event Action<bool> HighlightChanged;

        /// <summary> Whether this widget currently reports itself as focused. </summary>
        public bool IsSelected { get; private set; }

        /// <summary> Whether this widget currently reports a pointer hovering over it. </summary>
        public bool IsHovered { get; private set; }

        /// <summary>
        /// Whether this widget currently reports itself as highlighted. Distinct from
        /// <see cref="IsSelected"/> — e.g. a preview/armed state driven by something other than
        /// focus, such as a drag-hover target.
        /// </summary>
        public bool IsHighlighted { get; private set; }

        /// <summary> This widget's RectTransform, cached at edit time so runtime code never needs GetComponent. </summary>
        public RectTransform RectTransform => rectTransform;

        /// <summary> Auto-fills <see cref="RectTransform"/> from the object this component is added to. </summary>
        protected virtual void Reset()
        {
            rectTransform = transform as RectTransform;
        }

        /// <summary> Keeps <see cref="RectTransform"/> populated if it was left empty. </summary>
        protected virtual void OnValidate()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }
        }

        /// <summary> Marks this widget as focused and raises <see cref="Selected"/>. Called by NavigationManager. </summary>
        public void Select()
        {
            if (IsSelected)
            {
                return;
            }

            IsSelected = true;
            Selected?.Invoke();
        }

        /// <summary> Clears focus from this widget and raises <see cref="Deselected"/>. Called by NavigationManager. </summary>
        public void Deselect()
        {
            if (!IsSelected)
            {
                return;
            }

            IsSelected = false;
            Deselected?.Invoke();
        }

        /// <summary> Raises <see cref="Submitted"/>. Called by NavigationManager when this node is focused during Submit(). </summary>
        public void InvokeSubmit() => Submitted?.Invoke();

        /// <summary> Raises <see cref="Cancelled"/>. Called by NavigationManager when this node is focused during Cancel(). </summary>
        public void InvokeCancel() => Cancelled?.Invoke();

        /// <summary> Sets the highlight state and raises <see cref="HighlightChanged"/> if it changed. </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (IsHighlighted == highlighted)
            {
                return;
            }

            IsHighlighted = highlighted;
            HighlightChanged?.Invoke(highlighted);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
            HoverEntered?.Invoke();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            HoverExited?.Invoke();
        }
    }
}
