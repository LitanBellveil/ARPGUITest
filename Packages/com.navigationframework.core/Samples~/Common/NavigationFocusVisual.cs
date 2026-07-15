using NavigationFramework;
using UnityEngine;
using UnityEngine.UI;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Tints a <see cref="Graphic"/> (usually the same widget's background Image) to show whether
    /// this <see cref="NavigationSelectable"/> currently holds focus. Every sample needs some kind
    /// of "this is the focused widget" visual, so it lives here once instead of being rewritten
    /// per sample.
    /// </summary>
    [RequireComponent(typeof(NavigationSelectable))]
    public class NavigationFocusVisual : MonoBehaviour
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color focusedColor = new Color(1f, 0.85f, 0.3f);

        private NavigationSelectable selectable;

        private void Reset()
        {
            targetGraphic = GetComponent<Graphic>();
        }

        private void Awake()
        {
            selectable = GetComponent<NavigationSelectable>();
        }

        private void OnEnable()
        {
            selectable.Selected += ApplyFocused;
            selectable.Deselected += ApplyNormal;
            ApplyNormal();
        }

        private void OnDisable()
        {
            selectable.Selected -= ApplyFocused;
            selectable.Deselected -= ApplyNormal;
        }

        private void ApplyFocused()
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = focusedColor;
            }
        }

        private void ApplyNormal()
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = normalColor;
            }
        }
    }
}
