using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using NavDirection = NavigationFramework.Direction;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// The GraphView canvas for one open <see cref="NavigationGraph"/>. Builds a
    /// <see cref="NavigationNodeView"/> per <see cref="NavigationNode"/> and an <see cref="Edge"/>
    /// per <see cref="NavigationConnection"/>, and keeps the graph asset in sync as the user drags
    /// nodes, draws/removes edges, or adds/removes nodes through the context menu. Every mutation
    /// goes through <see cref="NavigationGraphAutoSaver.Touch"/>, which marks the asset dirty and
    /// debounce-saves it (Phase 4).
    /// </summary>
    public sealed class NavigationGraphView : GraphView
    {
        /// <summary> The graph asset this view is authoring. </summary>
        public NavigationGraph Graph { get; }

        /// <summary> Raised whenever the GraphView selection changes, for the side inspector panel to follow. </summary>
        public event Action<IReadOnlyList<ISelectable>> SelectionUpdated;

        private readonly Dictionary<string, NavigationNodeView> nodeViewsById = new Dictionary<string, NavigationNodeView>();
        private NavigationNodeView liveFocusedView;

        public NavigationGraphView(NavigationGraph graph)
        {
            Graph = graph;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var background = new GridBackground();
            Insert(0, background);
            background.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;

            BuildFromGraph();
        }

        private void BuildFromGraph()
        {
            foreach (NavigationNode node in Graph.Nodes)
            {
                AddNodeView(node);
            }

            foreach (NavigationNode node in Graph.Nodes)
            {
                foreach (NavigationConnection connection in node.Connections)
                {
                    NavigationNode target = Graph.FindNode(connection.TargetNodeId);

                    if (target != null)
                    {
                        ConnectView(node, connection, target);
                    }
                }
            }
        }

        private NavigationNodeView AddNodeView(NavigationNode node)
        {
            var view = new NavigationNodeView(node);
            nodeViewsById[node.Id] = view;
            AddElement(view);
            return view;
        }

        private void ConnectView(NavigationNode sourceNode, NavigationConnection connection, NavigationNode targetNode)
        {
            if (!nodeViewsById.TryGetValue(sourceNode.Id, out NavigationNodeView sourceView) ||
                !nodeViewsById.TryGetValue(targetNode.Id, out NavigationNodeView targetView) ||
                !sourceView.OutputPorts.TryGetValue(connection.Direction, out Port outputPort))
            {
                return;
            }

            Edge edge = outputPort.ConnectTo(targetView.InputPort);
            edge.userData = connection;
            AddElement(edge);
        }

        /// <inheritdoc />
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList()
                .Where(port => port.direction != startPort.direction && port.node != startPort.node)
                .ToList();
        }

        /// <inheritdoc />
        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SelectionUpdated?.Invoke(selection);
        }

        /// <inheritdoc />
        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SelectionUpdated?.Invoke(selection);
        }

        /// <inheritdoc />
        public override void ClearSelection()
        {
            base.ClearSelection();
            SelectionUpdated?.Invoke(selection);
        }

        /// <inheritdoc />
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this)
            {
                Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
                evt.menu.AppendAction("Create Node", _ => CreateNode(position));
            }

            base.BuildContextualMenu(evt);
        }

        /// <summary>
        /// Highlights the node view matching <paramref name="nodeId"/> as the live
        /// <c>NavigationManager.CurrentNode</c> during Play Mode, clearing any previous highlight
        /// first. Pass null to just clear (e.g. when Play Mode stops or no matching manager is
        /// found). Driven by <see cref="NavigationGraphEditorWindow"/>'s polling — this view has no
        /// Play Mode awareness of its own.
        /// </summary>
        public void SetLiveFocusedNode(string nodeId)
        {
            if (liveFocusedView != null)
            {
                liveFocusedView.SetLiveFocused(false);
                liveFocusedView = null;
            }

            if (nodeId != null && nodeViewsById.TryGetValue(nodeId, out NavigationNodeView view))
            {
                view.SetLiveFocused(true);
                liveFocusedView = view;
            }
        }

        /// <summary> Adds a new node to the graph and its view, positioned at <paramref name="position"/>. </summary>
        public void CreateNode(Vector2 position)
        {
            Undo.RecordObject(Graph, "Create Navigation Node");
            var node = new NavigationNode(Guid.NewGuid().ToString(), "New Node");
            node.SetEditorPosition(position);
            Graph.AddNode(node);
            AddNodeView(node);
            NavigationGraphAutoSaver.Touch(Graph);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            bool changed = false;

            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    CreateConnectionForEdge(edge);
                    changed = true;
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is Edge edge && edge.userData is NavigationConnection connection)
                    {
                        RemoveConnectionForEdge(edge, connection);
                        changed = true;
                    }
                    else if (element is NavigationNodeView nodeView)
                    {
                        RemoveNodeView(nodeView);
                        changed = true;
                    }
                }
            }

            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                changed = true;
            }

            if (changed)
            {
                NavigationGraphAutoSaver.Touch(Graph);
            }

            return change;
        }

        private void CreateConnectionForEdge(Edge edge)
        {
            if (!(edge.output.node is NavigationNodeView sourceView) || !(edge.input.node is NavigationNodeView targetView))
            {
                return;
            }

            NavDirection direction = sourceView.OutputPorts.First(kvp => kvp.Value == edge.output).Key;
            Undo.RecordObject(Graph, "Create Navigation Connection");
            var connection = new NavigationConnection(targetView.Node.Id, direction);
            sourceView.Node.AddConnection(connection);
            edge.userData = connection;
        }

        private void RemoveConnectionForEdge(Edge edge, NavigationConnection connection)
        {
            if (edge.output.node is NavigationNodeView sourceView)
            {
                Undo.RecordObject(Graph, "Remove Navigation Connection");
                sourceView.Node.RemoveConnection(connection);
            }
        }

        private void RemoveNodeView(NavigationNodeView nodeView)
        {
            Undo.RecordObject(Graph, "Remove Navigation Node");
            nodeViewsById.Remove(nodeView.Node.Id);
            Graph.RemoveNode(nodeView.Node);
        }
    }
}
