using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Demonstrates a popup that is inactive-by-default (see Phase 5's "inactive children
    /// included" scan) and takes over navigation focus while open. Opening/closing toggles two
    /// Groups (base screen vs. popup content) via <see cref="NavigationManager.SetGroupEnabled"/>
    /// so arrow keys can never reach the base screen while the popup is up — <c>SwitchToPage</c>
    /// isn't used here since both the base screen and the popup want to stay on the same page,
    /// only one Group active at a time.
    /// <para/>
    /// Closing via Escape doesn't go through <see cref="NavigationSelectable.Cancelled"/> — Cancel
    /// only fires on whichever node happens to be focused inside the popup, which isn't a reliable
    /// "the popup itself should close" signal. Instead this listens directly to the same
    /// <see cref="INavigationInputSource.CancelPressed"/> event the <see cref="NavigationInputRouter"/>
    /// already forwards to <c>Manager.Cancel()</c> — both fire from the same key press, which is
    /// fine for this sample.
    /// </summary>
    public class PopupController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private NavigationSelectable openButton;
        [SerializeField] private NavigationSelectable closeButton;
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private NavigationSelectable popupDefaultSelectable;
        [SerializeField] private string baseGroupName;
        [SerializeField] private string popupGroupName;

        private INavigationInputSource source;
        private string baseGroupId;
        private string popupGroupId;
        private string returnNodeId;
        private bool isOpen;

        private void Start()
        {
            NavigationGraph graph = router.Manager.Graph;
            baseGroupId = FindGroupId(graph, baseGroupName);
            popupGroupId = FindGroupId(graph, popupGroupName);

            popupPanel.SetActive(false);
            router.Manager.SetGroupEnabled(popupGroupId, false);
            router.Manager.SetGroupEnabled(baseGroupId, true);

            openButton.Submitted += Open;
            closeButton.Submitted += Close;

            source = inputSource as INavigationInputSource;

            if (source != null)
            {
                source.CancelPressed += OnCancelPressed;
            }
        }

        private void OnDestroy()
        {
            openButton.Submitted -= Open;
            closeButton.Submitted -= Close;

            if (source != null)
            {
                source.CancelPressed -= OnCancelPressed;
            }
        }

        private void OnCancelPressed()
        {
            if (isOpen)
            {
                Close();
            }
        }

        private void Open()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            returnNodeId = router.Manager.CurrentNode?.Id;

            popupPanel.SetActive(true);
            router.Manager.SetGroupEnabled(baseGroupId, false);
            router.Manager.SetGroupEnabled(popupGroupId, true);

            string popupDefaultNodeId = FindNodeIdBySelectable(router.Manager.Graph, popupDefaultSelectable);
            router.Manager.SelectNode(popupDefaultNodeId);
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;

            popupPanel.SetActive(false);
            router.Manager.SetGroupEnabled(popupGroupId, false);
            router.Manager.SetGroupEnabled(baseGroupId, true);
            router.Manager.SelectNode(returnNodeId);
        }

        private static string FindGroupId(NavigationGraph graph, string displayName)
        {
            foreach (NavigationGroup group in graph.Groups)
            {
                if (group.DisplayName == displayName)
                {
                    return group.Id;
                }
            }

            Debug.LogError($"[NavigationFramework] No NavigationGroup named '{displayName}' found on '{graph.name}'.");
            return null;
        }

        private static string FindNodeIdBySelectable(NavigationGraph graph, NavigationSelectable selectable)
        {
            foreach (NavigationNode node in graph.Nodes)
            {
                if (node.Selectable == selectable)
                {
                    return node.Id;
                }
            }

            Debug.LogError($"[NavigationFramework] No NavigationNode references Selectable '{selectable?.name}' on '{graph.name}'.");
            return null;
        }
    }
}
