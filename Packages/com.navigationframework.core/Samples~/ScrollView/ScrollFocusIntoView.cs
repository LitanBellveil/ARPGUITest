using NavigationFramework;
using UnityEngine;
using UnityEngine.UI;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Keeps the currently focused node visible inside a vertical <see cref="ScrollRect"/> by
    /// subscribing to <see cref="NavigationManager.NodeChanged"/> and nudging
    /// <c>scrollRect.content</c> whenever focus lands on a node above or below the viewport.
    /// <see cref="NavigationManager"/> itself has no concept of scrolling — moving focus onto an
    /// off-screen node is perfectly valid from the graph's point of view, "make it visible" is a
    /// UI-layer concern this sample adds on top, not something the framework does for you.
    /// Vertical-only; a horizontal ScrollRect (e.g. for a shelf of items) would need the mirrored
    /// X-axis version of <see cref="ScrollToShow"/>.
    /// </summary>
    public class ScrollFocusIntoView : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private ScrollRect scrollRect;

        private void OnEnable()
        {
            router.Manager.NodeChanged += OnNodeChanged;
        }

        private void OnDisable()
        {
            router.Manager.NodeChanged -= OnNodeChanged;
        }

        private void OnNodeChanged(NavigationNode previous, NavigationNode current)
        {
            if (current?.RectTransform != null)
            {
                ScrollToShow(current.RectTransform);
            }
        }

        private void ScrollToShow(RectTransform target)
        {
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            RectTransform content = scrollRect.content;

            if (viewport == null || content == null)
            {
                return;
            }

            var viewportCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            float viewportTop = viewportCorners[1].y;
            float viewportBottom = viewportCorners[0].y;

            var targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);
            float targetTop = targetCorners[1].y;
            float targetBottom = targetCorners[0].y;

            scrollRect.velocity = Vector2.zero;

            if (targetTop > viewportTop)
            {
                content.position += new Vector3(0f, viewportTop - targetTop, 0f);
            }
            else if (targetBottom < viewportBottom)
            {
                content.position += new Vector3(0f, viewportBottom - targetBottom, 0f);
            }
        }
    }
}
