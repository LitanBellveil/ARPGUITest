using System.Collections.Generic;
using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// Rewires a <see cref="NavigationScrollViewAnchor"/>'s authored connections onto a
    /// runtime-spawned dynamic list's boundary nodes, then disables the anchor. Lets a page mixing
    /// regular authored buttons with one dynamic list (e.g. an inventory ScrollView) be wired in a
    /// single Generate From Scene + Auto Connect pass at edit time, with the list's real items
    /// substituted in once they exist at Play.
    /// </summary>
    public static class NavigationDynamicListConnector
    {
        /// <summary>
        /// Assumes <paramref name="firstNode"/>/<paramref name="lastNode"/> are the top/bottom (or
        /// left/right) ends of a single row or column — a grid's side edges won't get outside
        /// connections from this beyond the very first/last cell. For each of the anchor's own
        /// connections, an equivalent connection is added to <paramref name="firstNode"/> if its
        /// direction is Up or Left, or <paramref name="lastNode"/> if Down or Right — since that's
        /// the boundary item adjacent to whatever lies beyond the list in that direction. For every
        /// connection elsewhere in <paramref name="graph"/> that targets the anchor, it's redirected
        /// to whichever of <paramref name="firstNode"/>/<paramref name="lastNode"/> is now adjacent
        /// to it (the opposite boundary rule, since the connection is stored from the other node's
        /// own point of view). Finally the anchor node is disabled so it's never itself reachable.
        /// Intended to run exactly once, right after the list's first population — it only adds
        /// connections, so calling it again duplicates whatever it added the first time (most
        /// visibly on <paramref name="firstNode"/>, which usually doesn't change across growth). If
        /// the list can grow past <paramref name="lastNode"/> and something beyond it needs to keep
        /// reaching the current last item, redirect that one connection by hand instead of calling
        /// this again.
        /// </summary>
        public static void AttachDynamicList(NavigationGraph graph, NavigationSelectable anchorSelectable, NavigationNode firstNode, NavigationNode lastNode)
        {
            NavigationNode anchorNode = FindNodeBySelectable(graph, anchorSelectable);

            if (anchorNode == null)
            {
                Debug.LogError($"[NavigationFramework] No NavigationNode references anchor '{anchorSelectable?.name}' on '{graph.name}'.");
                return;
            }

            foreach (NavigationConnection connection in anchorNode.Connections)
            {
                NavigationNode boundaryNode = BoundaryNodeFor(connection.Direction, firstNode, lastNode);

                if (boundaryNode == null)
                {
                    continue;
                }

                boundaryNode.AddConnection(new NavigationConnection(connection.TargetNodeId, connection.Direction, connection.Priority, connection.IsEnabled));
            }

            foreach (NavigationNode candidate in graph.Nodes)
            {
                if (candidate == anchorNode)
                {
                    continue;
                }

                List<NavigationConnection> toRedirect = null;

                foreach (NavigationConnection connection in candidate.Connections)
                {
                    if (connection.TargetNodeId == anchorNode.Id)
                    {
                        (toRedirect ??= new List<NavigationConnection>()).Add(connection);
                    }
                }

                if (toRedirect == null)
                {
                    continue;
                }

                foreach (NavigationConnection connection in toRedirect)
                {
                    NavigationNode boundaryNode = BoundaryNodeFor(Opposite(connection.Direction), firstNode, lastNode);

                    if (boundaryNode == null)
                    {
                        continue;
                    }

                    candidate.RemoveConnection(connection);
                    candidate.AddConnection(new NavigationConnection(boundaryNode.Id, connection.Direction, connection.Priority, connection.IsEnabled));
                }
            }

            anchorNode.SetEnabled(false);
        }

        private static NavigationNode BoundaryNodeFor(Direction direction, NavigationNode firstNode, NavigationNode lastNode)
        {
            switch (direction)
            {
                case Direction.Up:
                case Direction.Left:
                    return firstNode;
                case Direction.Down:
                case Direction.Right:
                    return lastNode;
                default:
                    return null;
            }
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

        private static NavigationNode FindNodeBySelectable(NavigationGraph graph, NavigationSelectable selectable)
        {
            foreach (NavigationNode node in graph.Nodes)
            {
                if (node.Selectable == selectable)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
