using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// Side panel showing editable fields for whatever is currently selected in the
    /// <see cref="NavigationGraphView"/>: a single <see cref="NavigationNode"/>'s display name,
    /// group/page assignment, default/enabled flags and scene references, or a single
    /// <see cref="NavigationConnection"/>'s priority/enabled flag. Shows nothing actionable for
    /// empty or multi-element selections — bulk editing is not part of Phase 3.
    /// </summary>
    public sealed class NavigationGraphInspectorPanel : VisualElement
    {
        private readonly NavigationGraph graph;
        private readonly IMGUIContainer container;

        private NavigationNodeView selectedNodeView;
        private Edge selectedEdge;

        public NavigationGraphInspectorPanel(NavigationGraph graph)
        {
            this.graph = graph;
            container = new IMGUIContainer(DrawSelection);
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 8;
            Add(container);
        }

        /// <summary> Called by the owning window whenever the GraphView selection changes. </summary>
        public void SetSelection(IReadOnlyList<ISelectable> selection)
        {
            selectedNodeView = selection.Count == 1 ? selection[0] as NavigationNodeView : null;
            selectedEdge = selection.Count == 1 ? selection[0] as Edge : null;
            container.MarkDirtyRepaint();
        }

        private void DrawSelection()
        {
            if (selectedNodeView != null)
            {
                DrawNode(selectedNodeView);
            }
            else if (selectedEdge != null && selectedEdge.userData is NavigationConnection connection)
            {
                DrawConnection(connection);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a single node or connection to edit its properties.", MessageType.None);
            }
        }

        private void DrawNode(NavigationNodeView nodeView)
        {
            NavigationNode node = nodeView.Node;
            EditorGUILayout.LabelField("Node", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Display Name", node.DisplayName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Rename Navigation Node");
                node.SetDisplayName(newName);
                nodeView.title = newName;
                MarkDirty();
            }

            DrawGroupPicker(node);
            DrawPagePicker(node);

            EditorGUI.BeginChangeCheck();
            bool isDefault = EditorGUILayout.Toggle("Is Default", node.IsDefault);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Set Default Navigation Node");

                if (isDefault)
                {
                    foreach (NavigationNode other in graph.Nodes)
                    {
                        other.SetDefault(other == node);
                    }
                }
                else
                {
                    node.SetDefault(false);
                }

                MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            bool isEnabled = EditorGUILayout.Toggle("Is Enabled", node.IsEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Toggle Navigation Node Enabled");
                node.SetEnabled(isEnabled);
                MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            var rectTransform = (RectTransform)EditorGUILayout.ObjectField("Rect Transform", node.RectTransform, typeof(RectTransform), true);
            var selectable = (NavigationSelectable)EditorGUILayout.ObjectField("Selectable", node.Selectable, typeof(NavigationSelectable), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Set Navigation Node Scene References");
                node.SetSceneReferences(rectTransform, selectable);
                MarkDirty();
            }
        }

        private void DrawGroupPicker(NavigationNode node)
        {
            List<NavigationGroup> groups = graph.Groups.ToList();
            string[] options = new[] { "(None)" }.Concat(groups.Select(g => g.DisplayName)).ToArray();
            int current = string.IsNullOrEmpty(node.GroupId) ? 0 : groups.FindIndex(g => g.Id == node.GroupId) + 1;

            if (current < 0)
            {
                current = 0;
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Group", current, options);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Set Navigation Node Group");
                node.SetGroup(selected == 0 ? null : groups[selected - 1].Id);
                MarkDirty();
            }
        }

        private void DrawPagePicker(NavigationNode node)
        {
            List<NavigationPage> pages = graph.Pages.ToList();
            string[] options = new[] { "(None)" }.Concat(pages.Select(p => p.DisplayName)).ToArray();
            int current = string.IsNullOrEmpty(node.PageId) ? 0 : pages.FindIndex(p => p.Id == node.PageId) + 1;

            if (current < 0)
            {
                current = 0;
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Page", current, options);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Set Navigation Node Page");
                node.SetPage(selected == 0 ? null : pages[selected - 1].Id);
                MarkDirty();
            }
        }

        private void DrawConnection(NavigationConnection connection)
        {
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Direction", connection.Direction.ToString());
            EditorGUILayout.LabelField("Auto-Generated", connection.IsAutoGenerated ? "Yes (from Auto Connect)" : "No (hand-drawn)");

            EditorGUI.BeginChangeCheck();
            int priority = EditorGUILayout.IntField("Priority", connection.Priority);
            bool enabled = EditorGUILayout.Toggle("Enabled", connection.IsEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Edit Navigation Connection");
                connection.SetPriority(priority);
                connection.SetEnabled(enabled);
                MarkDirty();
            }
        }

        private void MarkDirty() => NavigationGraphAutoSaver.Touch(graph);
    }
}
