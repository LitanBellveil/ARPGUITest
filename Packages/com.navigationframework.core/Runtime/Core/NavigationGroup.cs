using System;
using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// A named, independently enable/disable-able bundle of <see cref="NavigationNode"/> instances.
    /// Groups let gameplay code batch-toggle related UI without touching individual nodes — e.g.
    /// disabling every node under a "Locked Skill" group, or hiding a widget's nodes when its
    /// parent panel closes. Groups are also a hard boundary for Auto Connect: an edge is never
    /// generated between nodes that belong to different groups.
    /// </summary>
    [Serializable]
    public sealed class NavigationGroup
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private bool enabledByDefault = true;

        /// <summary> Creates a group with the given GUID and display name. </summary>
        public NavigationGroup(string id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }

        /// <summary> Stable GUID identifying this group within its owning <see cref="NavigationGraph"/>. </summary>
        public string Id => id;

        /// <summary> Human-readable name shown in the graph editor and used for debugging. </summary>
        public string DisplayName => displayName;

        /// <summary> Whether nodes in this group are navigable when the graph is first loaded. </summary>
        public bool EnabledByDefault => enabledByDefault;
    }
}
