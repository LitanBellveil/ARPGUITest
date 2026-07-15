using System.Collections.Generic;
using NavigationFramework;
using UnityEngine;
using UnityEngine.UI;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Demonstrates <see cref="NavigationManager.RegisterDynamicNode"/> /
    /// <see cref="NavigationManager.UnregisterDynamicNode"/> for a runtime-spawned inventory list
    /// that can also grow later (e.g. new items added as the player collects them via
    /// <see cref="AddItem"/>), instead of authoring nodes ahead of time in the Graph Window.
    /// Adjacency is computed by <see cref="NavigationGeometryConnector"/> from each slot's actual
    /// <c>RectTransform</c> position after <see cref="slotParent"/>'s own <c>LayoutGroup</c> has
    /// positioned it — this works for a vertical list, a wrapping grid, or whatever layout
    /// <see cref="slotParent"/> uses, without hardcoding row/column counts. Submitting a slot
    /// discards it, demonstrating that a connection whose target has since been unregistered simply
    /// fails to resolve on the next Move — no cleanup of the far side of that edge is required.
    /// <para/>
    /// If this list sits on a page with other regular authored buttons around it (not just above —
    /// see the README), assign <see cref="scrollViewAnchor"/> to a
    /// <see cref="NavigationScrollViewAnchor"/> placeholder positioned where the list goes, wired
    /// into the rest of the page like any other button via Generate From Scene + Auto Connect. This
    /// controller transplants that anchor's connections onto the real list once, via
    /// <see cref="NavigationDynamicListConnector.AttachDynamicList"/>, and disables the anchor.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private RectTransform slotParent;
        [SerializeField] private NavigationSelectable slotPrefab;
        [SerializeField] private int initialItemCount = 12;
        [SerializeField] private NavigationScrollViewAnchor scrollViewAnchor;

        private readonly List<NavigationNode> spawnedNodes = new List<NavigationNode>();

        private void Start()
        {
            for (int i = 0; i < initialItemCount; i++)
            {
                SpawnSlot($"Item {i}");
            }

            RefreshConnections();

            if (spawnedNodes.Count > 0)
            {
                if (scrollViewAnchor != null)
                {
                    NavigationDynamicListConnector.AttachDynamicList(
                        router.Manager.Graph, scrollViewAnchor, spawnedNodes[0], spawnedNodes[spawnedNodes.Count - 1]);
                }

                router.Manager.SelectNode(spawnedNodes[0].Id);
            }
        }

        /// <summary> Spawns one more slot (e.g. the player just picked up an item) and reconnects the list around it. </summary>
        public void AddItem(string displayName)
        {
            SpawnSlot(displayName);
            RefreshConnections();
        }

        private void SpawnSlot(string displayName)
        {
            NavigationSelectable instance = Instantiate(slotPrefab, slotParent);
            instance.name = displayName;

            NavigationNode node = router.Manager.RegisterDynamicNode(
                null, null, instance, instance.RectTransform, displayName);

            spawnedNodes.Add(node);

            GameObject slotObject = instance.gameObject;
            instance.Submitted += () => Discard(node, slotObject);
        }

        private void Discard(NavigationNode node, GameObject slotObject)
        {
            router.Manager.UnregisterDynamicNode(node.Id);
            spawnedNodes.Remove(node);
            Destroy(slotObject);
        }

        private void RefreshConnections()
        {
            // slotParent's LayoutGroup positions children lazily (end of frame) - force it to
            // finish now so NavigationGeometryConnector reads real, up-to-date RectTransform rects
            // rather than stale/zeroed ones from before this frame's spawns were laid out.
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);

            NavigationGeometryConnector.DisconnectAll(spawnedNodes);
            NavigationGeometryConnector.Connect(spawnedNodes);
        }
    }
}
