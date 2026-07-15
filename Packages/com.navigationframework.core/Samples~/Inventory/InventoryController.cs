using System.Collections.Generic;
using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Demonstrates <see cref="NavigationManager.RegisterDynamicNode"/> /
    /// <see cref="NavigationManager.UnregisterDynamicNode"/>: spawns a grid of inventory slots at
    /// runtime and wires them into the same <see cref="NavigationManager"/> a
    /// <see cref="NavigationInputRouter"/> already set up, instead of authoring them ahead of time
    /// in the Graph Window. Grid adjacency (who is Left/Right/Up/Down of whom) is computed here
    /// from row/column, since Auto Connect only operates on nodes already present in a
    /// <see cref="NavigationGraph"/> asset at edit time — it has no way to see nodes that won't
    /// exist until Play. Submitting a slot discards it, demonstrating that a connection whose
    /// target has been unregistered simply fails to resolve on the next Move — no cleanup of the
    /// far side of the edge is required.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private RectTransform slotParent;
        [SerializeField] private NavigationSelectable slotPrefab;
        [SerializeField] private int itemCount = 12;
        [SerializeField] private int columns = 4;

        private readonly List<NavigationNode> spawnedNodes = new List<NavigationNode>();

        private void Start()
        {
            for (int i = 0; i < itemCount; i++)
            {
                NavigationSelectable instance = Instantiate(slotPrefab, slotParent);
                instance.name = $"Slot_{i}";

                NavigationNode node = router.Manager.RegisterDynamicNode(
                    null, null, instance, instance.RectTransform, instance.name);

                spawnedNodes.Add(node);

                GameObject slotObject = instance.gameObject;
                instance.Submitted += () => Discard(node, slotObject);
            }

            ConnectGrid();

            if (spawnedNodes.Count > 0)
            {
                router.Manager.SelectNode(spawnedNodes[0].Id);
            }
        }

        private void Discard(NavigationNode node, GameObject slotObject)
        {
            router.Manager.UnregisterDynamicNode(node.Id);
            spawnedNodes.Remove(node);
            Destroy(slotObject);
        }

        private void ConnectGrid()
        {
            for (int i = 0; i < spawnedNodes.Count; i++)
            {
                int col = i % columns;

                if (col < columns - 1 && i + 1 < spawnedNodes.Count)
                {
                    Connect(spawnedNodes[i], spawnedNodes[i + 1], Direction.Right);
                }

                if (i + columns < spawnedNodes.Count)
                {
                    Connect(spawnedNodes[i], spawnedNodes[i + columns], Direction.Down);
                }
            }
        }

        private static void Connect(NavigationNode a, NavigationNode b, Direction aToB)
        {
            a.AddConnection(new NavigationConnection(b.Id, aToB));
            b.AddConnection(new NavigationConnection(a.Id, Opposite(aToB)));
        }

        private static Direction Opposite(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return direction;
            }
        }
    }
}
