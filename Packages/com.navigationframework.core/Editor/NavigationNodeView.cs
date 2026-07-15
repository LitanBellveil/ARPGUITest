using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using PortDirection = UnityEditor.Experimental.GraphView.Direction;
using NavDirection = NavigationFramework.Direction;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// The GraphView visual representation of one <see cref="NavigationNode"/>. Exposes one input
    /// port (any number of incoming connections, regardless of which direction they were declared
    /// under on the source node) and four directional output ports — dragging an edge from a
    /// directional output port to another node's input port is what creates a
    /// <see cref="NavigationConnection"/> for that direction.
    /// </summary>
    public sealed class NavigationNodeView : Node
    {
        /// <summary> The data this view represents. </summary>
        public NavigationNode Node { get; }

        /// <summary> The single generic input port nodes connect into. </summary>
        public Port InputPort { get; }

        /// <summary> The four directional output ports, keyed by the direction they represent. </summary>
        public IReadOnlyDictionary<NavDirection, Port> OutputPorts => outputPorts;

        private static readonly Color LiveFocusBorderColor = new Color(0.2f, 1f, 0.4f);
        private const float LiveFocusBorderWidth = 3f;

        private readonly Dictionary<NavDirection, Port> outputPorts = new Dictionary<NavDirection, Port>();

        public NavigationNodeView(NavigationNode node)
        {
            Node = node;
            title = node.DisplayName;
            viewDataKey = node.Id;
            SetPosition(new Rect(node.EditorPosition, Vector2.zero));

            InputPort = InstantiatePort(Orientation.Horizontal, PortDirection.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            AddOutputPort(NavDirection.Up);
            AddOutputPort(NavDirection.Down);
            AddOutputPort(NavDirection.Left);
            AddOutputPort(NavDirection.Right);

            RefreshExpandedState();
            RefreshPorts();
        }

        private void AddOutputPort(NavDirection direction)
        {
            Port port = InstantiatePort(Orientation.Horizontal, PortDirection.Output, Port.Capacity.Multi, typeof(bool));
            port.portName = direction.ToString();
            outputContainer.Add(port);
            outputPorts[direction] = port;
        }

        /// <summary> Keeps <see cref="NavigationNode.EditorPosition"/> in sync whenever this view is moved. </summary>
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.SetEditorPosition(newPos.position);
        }

        /// <summary>
        /// Toggles a border highlight marking this node as the live <c>NavigationManager.CurrentNode</c>
        /// in a running Play Mode session — see <see cref="NavigationGraphView.SetLiveFocusedNode"/>.
        /// Purely a debugging aid; has no effect on the authored graph data.
        /// </summary>
        public void SetLiveFocused(bool focused)
        {
            if (focused)
            {
                style.borderTopWidth = LiveFocusBorderWidth;
                style.borderBottomWidth = LiveFocusBorderWidth;
                style.borderLeftWidth = LiveFocusBorderWidth;
                style.borderRightWidth = LiveFocusBorderWidth;
                style.borderTopColor = LiveFocusBorderColor;
                style.borderBottomColor = LiveFocusBorderColor;
                style.borderLeftColor = LiveFocusBorderColor;
                style.borderRightColor = LiveFocusBorderColor;
            }
            else
            {
                style.borderTopWidth = StyleKeyword.Null;
                style.borderBottomWidth = StyleKeyword.Null;
                style.borderLeftWidth = StyleKeyword.Null;
                style.borderRightWidth = StyleKeyword.Null;
                style.borderTopColor = StyleKeyword.Null;
                style.borderBottomColor = StyleKeyword.Null;
                style.borderLeftColor = StyleKeyword.Null;
                style.borderRightColor = StyleKeyword.Null;
            }
        }
    }
}
