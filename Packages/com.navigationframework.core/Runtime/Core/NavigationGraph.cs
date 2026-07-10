using System.Collections.Generic;
using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// The serialized, designer-authored source of truth for a UI's navigable layout: its nodes,
    /// the connections between them, and the groups/pages they belong to. A graph is read-only at
    /// runtime — <c>NavigationManager</c> loads it once (see <c>SetGraph</c>) and builds its own
    /// lookup caches from it; it never mutates the asset in play mode. All mutation methods on this
    /// class exist for the graph editor and scene-generation tooling, not for gameplay code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNavigationGraph", menuName = "Navigation Framework/Navigation Graph")]
    public sealed class NavigationGraph : ScriptableObject
    {
        [SerializeField] private List<NavigationNode> nodes = new List<NavigationNode>();
        [SerializeField] private List<NavigationGroup> groups = new List<NavigationGroup>();
        [SerializeField] private List<NavigationPage> pages = new List<NavigationPage>();

        /// <summary> All nodes authored in this graph. </summary>
        public IReadOnlyList<NavigationNode> Nodes => nodes;

        /// <summary> All groups authored in this graph. </summary>
        public IReadOnlyList<NavigationGroup> Groups => groups;

        /// <summary> All pages authored in this graph. </summary>
        public IReadOnlyList<NavigationPage> Pages => pages;

        /// <summary> Finds a node by its GUID, or null if no node in this graph has that id. </summary>
        public NavigationNode FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == nodeId)
                {
                    return nodes[i];
                }
            }

            return null;
        }

        /// <summary> Finds a group by its GUID, or null if no group in this graph has that id. </summary>
        public NavigationGroup FindGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Id == groupId)
                {
                    return groups[i];
                }
            }

            return null;
        }

        /// <summary> Finds a page by its GUID, or null if no page in this graph has that id. </summary>
        public NavigationPage FindPage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return null;
            }

            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Id == pageId)
                {
                    return pages[i];
                }
            }

            return null;
        }

        /// <summary> Adds a node to the graph. Intended for use by the graph editor and Generate From Scene, not gameplay code. </summary>
        public void AddNode(NavigationNode node) => nodes.Add(node);

        /// <summary> Removes a node from the graph. Intended for use by the graph editor, not gameplay code. </summary>
        public bool RemoveNode(NavigationNode node) => nodes.Remove(node);

        /// <summary> Adds a group to the graph. Intended for use by the graph editor, not gameplay code. </summary>
        public void AddGroup(NavigationGroup group) => groups.Add(group);

        /// <summary> Removes a group from the graph. Intended for use by the graph editor, not gameplay code. </summary>
        public bool RemoveGroup(NavigationGroup group) => groups.Remove(group);

        /// <summary> Adds a page to the graph. Intended for use by the graph editor, not gameplay code. </summary>
        public void AddPage(NavigationPage page) => pages.Add(page);

        /// <summary> Removes a page from the graph. Intended for use by the graph editor, not gameplay code. </summary>
        public bool RemovePage(NavigationPage page) => pages.Remove(page);
    }
}
