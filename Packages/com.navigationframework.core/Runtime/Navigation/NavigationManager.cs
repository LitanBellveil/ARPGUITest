using System;
using System.Collections.Generic;
using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// Drives focus over a <see cref="NavigationGraph"/> using the three input-agnostic verbs
    /// <see cref="Move"/>, <see cref="Submit"/>, <see cref="Cancel"/>. A plain C# class, not a
    /// MonoBehaviour — it has no Update loop of its own and does nothing until a caller (an input
    /// binding, AI, replay tooling) invokes one of its methods, keeping "when navigation happens"
    /// entirely outside the framework.
    /// </summary>
    public sealed class NavigationManager
    {
        private readonly Dictionary<string, NavigationNode> nodesById = new Dictionary<string, NavigationNode>();
        private readonly Dictionary<string, bool> groupEnabled = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> lastSelectedNodeIdByPage = new Dictionary<string, string>();

        /// <summary> The graph currently loaded via <see cref="SetGraph"/>, or null if none. </summary>
        public NavigationGraph Graph { get; private set; }

        /// <summary> The node that currently holds focus, or null if nothing is focused. </summary>
        public NavigationNode CurrentNode { get; private set; }

        /// <summary> The page last activated via <see cref="SwitchToPage"/>, or null if none. </summary>
        public NavigationPage CurrentPage { get; private set; }

        /// <summary> Raised after focus moves from one node to another. Either argument may be null. </summary>
        public event Action<NavigationNode, NavigationNode> NodeChanged;

        /// <summary>
        /// Loads <paramref name="graph"/> and rebuilds the node/group lookup caches from its
        /// already-resolved scene references — no scene search happens here or on any later
        /// <see cref="Move"/>. Clears current selection and per-page selection memory; does not
        /// select anything by itself, so the caller decides when focus actually starts (e.g. by
        /// following up with <see cref="SelectDefault"/> or <see cref="SwitchToPage"/>).
        /// </summary>
        public void SetGraph(NavigationGraph graph)
        {
            Graph = graph;
            nodesById.Clear();
            groupEnabled.Clear();
            lastSelectedNodeIdByPage.Clear();
            SetCurrentNode(null);
            CurrentPage = null;

            if (graph == null)
            {
                return;
            }

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                NavigationNode node = graph.Nodes[i];
                nodesById[node.Id] = node;
            }

            for (int i = 0; i < graph.Groups.Count; i++)
            {
                NavigationGroup group = graph.Groups[i];
                groupEnabled[group.Id] = group.EnabledByDefault;
            }
        }

        /// <summary>
        /// Registers a <see cref="NavigationNode"/> for content that does not exist until runtime
        /// (a spawned inventory slot, a carousel item, a procedurally generated skill tree node).
        /// The node is tracked only by this manager, never added to the <see cref="NavigationGraph"/>
        /// asset — the graph stays read-only in play mode. Connect it to the rest of the graph with
        /// <see cref="NavigationNode.AddConnection"/> on the returned node (and on whichever
        /// authored nodes should lead into it); Auto Connect (Phase 6) is expected to do this
        /// automatically for geometry-based edges once it lands.
        /// </summary>
        public NavigationNode RegisterDynamicNode(string pageId, string groupId, NavigationSelectable selectable, RectTransform rectTransform, string displayName = null)
        {
            var node = new NavigationNode(Guid.NewGuid().ToString(), displayName ?? string.Empty);
            node.SetPage(pageId);
            node.SetGroup(groupId);
            node.SetSceneReferences(rectTransform, selectable);
            nodesById[node.Id] = node;
            return node;
        }

        /// <summary>
        /// Removes a node previously added via <see cref="RegisterDynamicNode"/> (e.g. when a
        /// spawned inventory slot is destroyed). Any surviving connection that still targets this
        /// id simply fails to resolve on the next <see cref="Move"/> — callers are not required to
        /// clean up the far side of those edges.
        /// </summary>
        public void UnregisterDynamicNode(string nodeId)
        {
            if (nodeId == null || !nodesById.TryGetValue(nodeId, out NavigationNode node))
            {
                return;
            }

            nodesById.Remove(nodeId);

            if (CurrentNode == node)
            {
                SetCurrentNode(null);
            }
        }

        /// <summary>
        /// Enables or disables every node in <paramref name="groupId"/> as a batch, without
        /// touching each node's own <see cref="NavigationNode.SetEnabled"/> flag. If the group
        /// containing <see cref="CurrentNode"/> is disabled, focus is cleared; the caller is
        /// expected to reselect (e.g. via <see cref="SelectDefault"/>) afterwards.
        /// </summary>
        public void SetGroupEnabled(string groupId, bool enabled)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return;
            }

            groupEnabled[groupId] = enabled;

            if (!enabled && CurrentNode != null && CurrentNode.GroupId == groupId)
            {
                SetCurrentNode(null);
            }
        }

        /// <summary> Selects the graph's node flagged <see cref="NavigationNode.IsDefault"/>, if any. </summary>
        public void SelectDefault()
        {
            if (Graph == null)
            {
                return;
            }

            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                if (Graph.Nodes[i].IsDefault)
                {
                    SelectNode(Graph.Nodes[i].Id);
                    return;
                }
            }
        }

        /// <summary>
        /// Activates <paramref name="pageId"/> and applies its <see cref="PageEntryMode"/>:
        /// <see cref="PageEntryMode.SelectDefaultNode"/> always focuses
        /// <see cref="NavigationPage.DefaultNodeId"/>; <see cref="PageEntryMode.RestoreLastSelected"/>
        /// focuses whatever node was last selected while this page was active, falling back to the
        /// default node the first time the page is activated (or if that node is no longer valid).
        /// </summary>
        public void SwitchToPage(string pageId)
        {
            NavigationPage page = Graph != null ? Graph.FindPage(pageId) : null;

            if (page == null)
            {
                return;
            }

            CurrentPage = page;
            string targetNodeId = page.DefaultNodeId;

            if (page.EntryMode == PageEntryMode.RestoreLastSelected &&
                lastSelectedNodeIdByPage.TryGetValue(pageId, out string lastNodeId) &&
                IsSelectable(ResolveNode(lastNodeId)))
            {
                targetNodeId = lastNodeId;
            }

            SelectNode(targetNodeId);
        }

        /// <summary>
        /// Selects <paramref name="nodeId"/> directly, bypassing directional movement. No-op if the
        /// node cannot be found or is not currently selectable (disabled node, or disabled group).
        /// </summary>
        public bool SelectNode(string nodeId)
        {
            NavigationNode node = ResolveNode(nodeId);

            if (!IsSelectable(node))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(node.PageId))
            {
                lastSelectedNodeIdByPage[node.PageId] = node.Id;
            }

            SetCurrentNode(node);
            return true;
        }

        /// <summary>
        /// Moves focus from <see cref="CurrentNode"/> in <paramref name="direction"/>. Among the
        /// current node's connections that face this direction, are enabled, and resolve to a
        /// currently selectable node, the highest <see cref="NavigationConnection.Priority"/> wins;
        /// ties keep whichever was declared first. No-op if there is no current node or no valid
        /// candidate.
        /// </summary>
        public void Move(Direction direction)
        {
            if (CurrentNode == null)
            {
                return;
            }

            NavigationConnection best = null;
            NavigationNode bestTarget = null;
            IReadOnlyList<NavigationConnection> connections = CurrentNode.Connections;

            for (int i = 0; i < connections.Count; i++)
            {
                NavigationConnection connection = connections[i];

                if (connection.Direction != direction || !connection.IsEnabled)
                {
                    continue;
                }

                NavigationNode target = ResolveNode(connection.TargetNodeId);

                if (!IsSelectable(target))
                {
                    continue;
                }

                if (best == null || connection.Priority > best.Priority)
                {
                    best = connection;
                    bestTarget = target;
                }
            }

            if (bestTarget != null)
            {
                SelectNode(bestTarget.Id);
            }
        }

        /// <summary> Invokes Submit on <see cref="CurrentNode"/>'s <see cref="NavigationSelectable"/>, if any. </summary>
        public void Submit()
        {
            if (CurrentNode != null && CurrentNode.Selectable != null)
            {
                CurrentNode.Selectable.InvokeSubmit();
            }
        }

        /// <summary> Invokes Cancel on <see cref="CurrentNode"/>'s <see cref="NavigationSelectable"/>, if any. </summary>
        public void Cancel()
        {
            if (CurrentNode != null && CurrentNode.Selectable != null)
            {
                CurrentNode.Selectable.InvokeCancel();
            }
        }

        private NavigationNode ResolveNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            nodesById.TryGetValue(nodeId, out NavigationNode node);
            return node;
        }

        private bool IsSelectable(NavigationNode node)
        {
            if (node == null || !node.IsEnabled)
            {
                return false;
            }

            if (string.IsNullOrEmpty(node.GroupId))
            {
                return true;
            }

            return !groupEnabled.TryGetValue(node.GroupId, out bool enabled) || enabled;
        }

        private void SetCurrentNode(NavigationNode node)
        {
            if (CurrentNode == node)
            {
                return;
            }

            NavigationNode previous = CurrentNode;

            if (previous != null && previous.Selectable != null)
            {
                previous.Selectable.Deselect();
            }

            CurrentNode = node;

            if (CurrentNode != null && CurrentNode.Selectable != null)
            {
                CurrentNode.Selectable.Select();
            }

            NodeChanged?.Invoke(previous, CurrentNode);
        }
    }
}
