using System;
using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// A "Delete Item?" Yes/No confirm dialog, demonstrating <see cref="NavigationSelectable.Submitted"/>
    /// and <see cref="NavigationSelectable.Cancelled"/> used directly rather than through the
    /// Popup sample's global-input-listener workaround. That workaround exists because a popup with
    /// many controls can't practically subscribe to every one's <c>Cancelled</c> individually; a
    /// two-button dialog can, and doing so here means Escape always resolves to "No" regardless of
    /// which of the two buttons happened to be focused.
    /// </summary>
    public class DialogController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private NavigationSelectable openButton;
        [SerializeField] private NavigationSelectable yesButton;
        [SerializeField] private NavigationSelectable noButton;
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private string baseGroupName;
        [SerializeField] private string dialogGroupName;

        /// <summary> Raised with true if confirmed (Yes / Submit on Yes), false if cancelled (No, or Escape). </summary>
        public event Action<bool> Resolved;

        private string baseGroupId;
        private string dialogGroupId;
        private string returnNodeId;
        private bool isOpen;

        private void Start()
        {
            NavigationGraph graph = router.Manager.Graph;
            baseGroupId = FindGroupId(graph, baseGroupName);
            dialogGroupId = FindGroupId(graph, dialogGroupName);

            dialogPanel.SetActive(false);
            router.Manager.SetGroupEnabled(dialogGroupId, false);
            router.Manager.SetGroupEnabled(baseGroupId, true);

            openButton.Submitted += Open;
            yesButton.Submitted += () => Close(true);
            noButton.Submitted += () => Close(false);
            yesButton.Cancelled += () => Close(false);
            noButton.Cancelled += () => Close(false);
        }

        private void OnDestroy()
        {
            openButton.Submitted -= Open;
        }

        private void Open()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            returnNodeId = router.Manager.CurrentNode?.Id;

            dialogPanel.SetActive(true);
            router.Manager.SetGroupEnabled(baseGroupId, false);
            router.Manager.SetGroupEnabled(dialogGroupId, true);
            router.Manager.SelectNode(FindNodeIdBySelectable(router.Manager.Graph, noButton));
        }

        private void Close(bool confirmed)
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;

            dialogPanel.SetActive(false);
            router.Manager.SetGroupEnabled(dialogGroupId, false);
            router.Manager.SetGroupEnabled(baseGroupId, true);
            router.Manager.SelectNode(returnNodeId);

            Resolved?.Invoke(confirmed);
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
