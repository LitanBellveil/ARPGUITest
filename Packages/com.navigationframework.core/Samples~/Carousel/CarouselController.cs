using System.Collections.Generic;
using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// A single row of runtime-spawned items (<c>RegisterDynamicNode</c>, same reasoning as the
    /// Inventory sample) that wraps around: moving Right off the last item lands on the first, and
    /// Left off the first lands on the last. <see cref="NavigationManager.Move"/> has no built-in
    /// wraparound (deliberately, per Phase 1/6 — it's not implied by the graph model), so this is
    /// implemented entirely by <see cref="ConnectRow"/> adding one extra <see cref="NavigationConnection"/>
    /// pair between the two end items — from the graph's point of view a wrapped carousel is
    /// indistinguishable from a normal row that happens to loop, no framework changes needed.
    /// </summary>
    public class CarouselController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private RectTransform itemParent;
        [SerializeField] private NavigationSelectable itemPrefab;
        [SerializeField] private int itemCount = 6;

        private readonly List<NavigationNode> items = new List<NavigationNode>();

        private void Start()
        {
            for (int i = 0; i < itemCount; i++)
            {
                NavigationSelectable instance = Instantiate(itemPrefab, itemParent);
                instance.name = $"CarouselItem_{i}";

                NavigationNode node = router.Manager.RegisterDynamicNode(
                    null, null, instance, instance.RectTransform, instance.name);

                items.Add(node);
            }

            ConnectRow();

            if (items.Count > 0)
            {
                router.Manager.SelectNode(items[0].Id);
            }
        }

        private void ConnectRow()
        {
            for (int i = 0; i < items.Count - 1; i++)
            {
                Connect(items[i], items[i + 1]);
            }

            if (items.Count > 1)
            {
                Connect(items[items.Count - 1], items[0]);
            }
        }

        private static void Connect(NavigationNode left, NavigationNode right)
        {
            left.AddConnection(new NavigationConnection(right.Id, Direction.Right));
            right.AddConnection(new NavigationConnection(left.Id, Direction.Left));
        }
    }
}
