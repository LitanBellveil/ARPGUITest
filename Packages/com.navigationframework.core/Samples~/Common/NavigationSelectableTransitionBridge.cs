using NavigationFramework;
using UnityEngine;
using UnityEngine.UI;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Alternative to <see cref="NavigationFocusVisual"/> for projects that already have a Button/
    /// Toggle's built-in <see cref="Selectable"/> Transition configured (ColorTint, SpriteSwap, or
    /// Animation) and want that to represent focus, instead of writing bespoke visual code.
    /// <see cref="Selectable.OnSelect"/>/<see cref="Selectable.OnDeselect"/> are public
    /// (they're the <c>ISelectHandler</c>/<c>IDeselectHandler</c> implementation) and can be
    /// called directly to drive the widget's own configured Transition without going through
    /// Unity's <c>EventSystem</c> selection at all — so this coexists safely with
    /// <see cref="NavigationSelectable"/> having turned the same widget's
    /// <see cref="Selectable.navigation"/> off.
    /// </summary>
    [RequireComponent(typeof(NavigationSelectable))]
    [RequireComponent(typeof(Selectable))]
    public class NavigationSelectableTransitionBridge : MonoBehaviour
    {
        private NavigationSelectable navigationSelectable;
        private Selectable unitySelectable;

        private void Awake()
        {
            navigationSelectable = GetComponent<NavigationSelectable>();
            unitySelectable = GetComponent<Selectable>();
        }

        private void OnEnable()
        {
            navigationSelectable.Selected += OnSelected;
            navigationSelectable.Deselected += OnDeselected;
        }

        private void OnDisable()
        {
            navigationSelectable.Selected -= OnSelected;
            navigationSelectable.Deselected -= OnDeselected;
        }

        private void OnSelected() => unitySelectable.OnSelect(null);
        private void OnDeselected() => unitySelectable.OnDeselect(null);
    }
}
