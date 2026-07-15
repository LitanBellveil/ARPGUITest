using System;
using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// Determines which node receives focus when a <see cref="NavigationPage"/> becomes active.
    /// </summary>
    public enum PageEntryMode
    {
        /// <summary> Always focus the page's <see cref="NavigationPage.DefaultNodeId"/> when the page becomes active. </summary>
        SelectDefaultNode,

        /// <summary>
        /// Focus whichever node was last selected while this page was active, falling back to
        /// <see cref="NavigationPage.DefaultNodeId"/> the first time the page is ever activated.
        /// </summary>
        RestoreLastSelected
    }

    /// <summary>
    /// A logical screen or tab (e.g. "Character", "Weapon", "Inventory") that groups a set of
    /// <see cref="NavigationNode"/> instances and defines what happens to focus when the page is
    /// switched to. Pages are a first-class part of the data model — see the "Tab Character /
    /// Skill1" vs. "Tab Weapon / Weapon1" example — so tabbed UIs never need bespoke
    /// focus-restoration logic bolted on after the fact.
    /// </summary>
    [Serializable]
    public sealed class NavigationPage
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string defaultNodeId;
        [SerializeField] private PageEntryMode entryMode = PageEntryMode.SelectDefaultNode;

        /// <summary> Creates a page with the given GUID and display name. </summary>
        public NavigationPage(string id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }

        /// <summary> Stable GUID identifying this page within its owning <see cref="NavigationGraph"/>. </summary>
        public string Id => id;

        /// <summary> Human-readable name shown in the graph editor and used for debugging. </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// GUID of the <see cref="NavigationNode"/> selected when this page opens under
        /// <see cref="PageEntryMode.SelectDefaultNode"/>, and used as the fallback for
        /// <see cref="PageEntryMode.RestoreLastSelected"/> before any node has been selected.
        /// </summary>
        public string DefaultNodeId => defaultNodeId;

        /// <summary> Policy applied when the manager switches focus onto this page. </summary>
        public PageEntryMode EntryMode => entryMode;

        /// <summary> Renames this page. Intended for use by the graph editor. </summary>
        public void SetDisplayName(string newDisplayName) => displayName = newDisplayName;

        /// <summary> Changes the default/fallback node for this page. Intended for use by the graph editor. </summary>
        public void SetDefaultNode(string newDefaultNodeId) => defaultNodeId = newDefaultNodeId;

        /// <summary> Changes the entry policy applied when this page becomes active. Intended for use by the graph editor. </summary>
        public void SetEntryMode(PageEntryMode value) => entryMode = value;
    }
}
