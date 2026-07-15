using NavigationFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NavigationFrameworkTest
{
    /// <summary>
    /// Throwaway smoke-test driver for NavigationFramework Phases 1-3 — not part of the package.
    /// Polls the keyboard directly (arrow keys to Move, Enter to Submit, Escape to Cancel) so
    /// Move/Submit/Cancel can be exercised before real samples (Phase 7) exist. Delete this once
    /// samples land.
    /// </summary>
    public class NavigationTestDriver : MonoBehaviour
    {
        [SerializeField] private NavigationGraph graph;

        private NavigationManager manager;

        private void Start()
        {
            if (graph == null)
            {
                Debug.LogError("[NavTest] No NavigationGraph assigned.", this);
                return;
            }

            manager = new NavigationManager();
            manager.SetGraph(graph);
            manager.SelectDefault();
            manager.NodeChanged += OnNodeChanged;

            Debug.Log($"[NavTest] Started. Current node: {manager.CurrentNode?.DisplayName ?? "(none)"}", this);
        }

        private void Update()
        {
            if (manager == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame) manager.Move(Direction.Up);
            if (keyboard.downArrowKey.wasPressedThisFrame) manager.Move(Direction.Down);
            if (keyboard.leftArrowKey.wasPressedThisFrame) manager.Move(Direction.Left);
            if (keyboard.rightArrowKey.wasPressedThisFrame) manager.Move(Direction.Right);

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                LogSubmitOrCancelAttempt("Submit");
                manager.Submit();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                LogSubmitOrCancelAttempt("Cancel");
                manager.Cancel();
            }
        }

        private void LogSubmitOrCancelAttempt(string verb)
        {
            NavigationNode node = manager.CurrentNode;

            if (node == null)
            {
                Debug.LogWarning($"[NavTest] {verb} pressed but CurrentNode is null.", this);
            }
            else if (node.Selectable == null)
            {
                Debug.LogWarning($"[NavTest] {verb} pressed on '{node.DisplayName}' but its Selectable field is not assigned in the graph node — {verb} is a no-op. Open the Graph Window, select this node, and drag the button's NavigationSelectable into the Selectable field.", this);
            }
            else
            {
                Debug.Log($"[NavTest] {verb} pressed on '{node.DisplayName}', forwarding to its NavigationSelectable.", this);
            }
        }

        private void OnNodeChanged(NavigationNode previous, NavigationNode current)
        {
            Debug.Log($"[NavTest] {previous?.DisplayName ?? "(none)"} -> {current?.DisplayName ?? "(none)"}", this);

            if (previous?.Selectable != null)
            {
                previous.Selectable.Submitted -= OnCurrentSubmitted;
                previous.Selectable.Cancelled -= OnCurrentCancelled;
            }

            if (current?.Selectable != null)
            {
                current.Selectable.Submitted += OnCurrentSubmitted;
                current.Selectable.Cancelled += OnCurrentCancelled;
            }
        }

        private void OnCurrentSubmitted() => Debug.Log("[NavTest] NavigationSelectable.Submitted fired.", this);
        private void OnCurrentCancelled() => Debug.Log("[NavTest] NavigationSelectable.Cancelled fired.", this);
    }
}
