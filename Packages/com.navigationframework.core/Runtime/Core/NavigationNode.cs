using System;
using System.Collections.Generic;
using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// A single focusable position in a <see cref="NavigationGraph"/>. A node is pure data: it
    /// identifies where focus can land and how to move away from it, but never caches runtime
    /// state (a "currently selected" flag, resolved lookups, etc.) — that lives in
    /// <c>NavigationManager</c> so the same graph asset can be inspected, duplicated, or driven by
    /// more than one manager without the data itself getting out of sync.
    /// </summary>
    [Serializable]
    public sealed class NavigationNode
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private NavigationSelectable selectable;
        [SerializeField] private string groupId;
        [SerializeField] private string pageId;
        [SerializeField] private bool isDefault;
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private List<NavigationConnection> connections = new List<NavigationConnection>();
        [SerializeField] private Vector2 editorPosition;

        /// <summary> Creates a node with the given GUID and display name. </summary>
        public NavigationNode(string id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }

        /// <summary> Stable GUID identifying this node within its owning <see cref="NavigationGraph"/>. Never reused, even if the node is renamed. </summary>
        public string Id => id;

        /// <summary> Human-readable name shown in the graph editor and used for debugging. </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// The scene RectTransform this node represents, when authored directly against a scene
        /// (e.g. via Generate From Scene). Optional — nodes for dynamically spawned UI (inventory
        /// slots, carousel items) may leave this null and resolve their transform at runtime
        /// instead, through <see cref="NavigationSelectable"/> registration.
        /// </summary>
        public RectTransform RectTransform => rectTransform;

        /// <summary>
        /// The <see cref="NavigationSelectable"/> that receives Select/Deselect/Submit/Cancel calls
        /// for this node. Optional — a node with no selectable acts as a pure layout anchor (for
        /// example a group header used only to steer Auto Connect geometry).
        /// </summary>
        public NavigationSelectable Selectable => selectable;

        /// <summary> GUID of the owning <see cref="NavigationGroup"/>, or null/empty if the node belongs to no group. </summary>
        public string GroupId => groupId;

        /// <summary> GUID of the owning <see cref="NavigationPage"/>, or null/empty if the node belongs to no page. </summary>
        public string PageId => pageId;

        /// <summary> Whether this is the graph-wide fallback node selected by <c>NavigationManager.SelectDefault()</c>. </summary>
        public bool IsDefault => isDefault;

        /// <summary> Whether this node currently participates in navigation. Disabled nodes are skipped by Move and cannot be selected. </summary>
        public bool IsEnabled => isEnabled;

        /// <summary> Outgoing connections to other nodes. Multiple connections may share a <see cref="Direction"/>; see <see cref="NavigationConnection.Priority"/>. </summary>
        public IReadOnlyList<NavigationConnection> Connections => connections;

        /// <summary> Editor-only canvas position within the Navigation Graph window. Has no effect on runtime behaviour. </summary>
        public Vector2 EditorPosition => editorPosition;

        /// <summary> Enables or disables this node at runtime without removing it from the graph. </summary>
        public void SetEnabled(bool value) => isEnabled = value;

        /// <summary> Marks or unmarks this node as the graph's default. Intended for use by the graph editor. </summary>
        public void SetDefault(bool value) => isDefault = value;

        /// <summary> Assigns the group this node belongs to. Intended for use by the graph editor. </summary>
        public void SetGroup(string newGroupId) => groupId = newGroupId;

        /// <summary> Assigns the page this node belongs to. Intended for use by the graph editor. </summary>
        public void SetPage(string newPageId) => pageId = newPageId;

        /// <summary> Assigns the scene bindings for this node. Intended for use by the graph editor and Generate From Scene. </summary>
        public void SetSceneReferences(RectTransform newRectTransform, NavigationSelectable newSelectable)
        {
            rectTransform = newRectTransform;
            selectable = newSelectable;
        }

        /// <summary> Adds an outgoing connection. Intended for use by the graph editor when an edge is drawn. </summary>
        public void AddConnection(NavigationConnection connection) => connections.Add(connection);

        /// <summary> Removes an outgoing connection. Intended for use by the graph editor when an edge is deleted. </summary>
        public bool RemoveConnection(NavigationConnection connection) => connections.Remove(connection);

        /// <summary> Updates the editor-only canvas position. Called by the graph editor while dragging a node. </summary>
        public void SetEditorPosition(Vector2 position) => editorPosition = position;
    }
}
