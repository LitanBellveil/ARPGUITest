using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// Adds an "Open Graph Window" button, "Generate From Scene" (Phase 5), and Group/Page
    /// management (add/rename/remove) to the default Inspector for <see cref="NavigationGraph"/>.
    /// Node and connection editing lives entirely in <see cref="NavigationGraphEditorWindow"/> —
    /// groups, pages, and scene generation are edited here instead, since they are graph-wide
    /// actions/metadata rather than something naturally represented as a GraphView node or edge.
    /// </summary>
    [CustomEditor(typeof(NavigationGraph))]
    public sealed class NavigationGraphEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (NavigationGraph)target;

            if (GUILayout.Button("Open Graph Window"))
            {
                NavigationGraphEditorWindow.Open(graph);
            }

            EditorGUILayout.Space();
            DrawGenerateFromScene(graph);
            EditorGUILayout.Space();
            DrawGroups(graph);
            EditorGUILayout.Space();
            DrawPages(graph);
        }

        private void DrawGenerateFromScene(NavigationGraph graph)
        {
            EditorGUILayout.LabelField("Generate From Scene", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var scanRoot = (Transform)EditorGUILayout.ObjectField("Scan Root", graph.GenerateFromSceneRoot, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, "Set Generate From Scene Root");
                graph.SetGenerateFromSceneRoot(scanRoot);
                NavigationGraphAutoSaver.Touch(graph);
            }

            using (new EditorGUI.DisabledScope(scanRoot == null))
            {
                if (GUILayout.Button("Generate From Scene"))
                {
                    NavigationSceneGenerator.GenerateFromScene(graph, scanRoot);
                }
            }
        }

        private void DrawGroups(NavigationGraph graph)
        {
            EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);

            foreach (NavigationGroup group in graph.Groups.ToList())
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.TextField(group.DisplayName);
                bool enabledByDefault = EditorGUILayout.ToggleLeft("Enabled By Default", group.EnabledByDefault, GUILayout.Width(140));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(graph, "Edit Navigation Group");
                    group.SetDisplayName(name);
                    group.SetEnabledByDefault(enabledByDefault);
                    NavigationGraphAutoSaver.Touch(graph);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(graph, "Remove Navigation Group");
                    graph.RemoveGroup(group);
                    NavigationGraphAutoSaver.Touch(graph);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Group"))
            {
                Undo.RecordObject(graph, "Add Navigation Group");
                graph.AddGroup(new NavigationGroup(Guid.NewGuid().ToString(), "New Group"));
                NavigationGraphAutoSaver.Touch(graph);
            }
        }

        private void DrawPages(NavigationGraph graph)
        {
            EditorGUILayout.LabelField("Pages", EditorStyles.boldLabel);

            foreach (NavigationPage page in graph.Pages.ToList())
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.TextField(page.DisplayName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(graph, "Rename Navigation Page");
                    page.SetDisplayName(name);
                    NavigationGraphAutoSaver.Touch(graph);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(graph, "Remove Navigation Page");
                    graph.RemovePage(page);
                    NavigationGraphAutoSaver.Touch(graph);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                var nodes = graph.Nodes.ToList();
                string[] options = new[] { "(None)" }.Concat(nodes.Select(n => n.DisplayName)).ToArray();
                int current = string.IsNullOrEmpty(page.DefaultNodeId) ? 0 : nodes.FindIndex(n => n.Id == page.DefaultNodeId) + 1;

                if (current < 0)
                {
                    current = 0;
                }

                EditorGUI.BeginChangeCheck();
                int selected = EditorGUILayout.Popup("Default Node", current, options);
                var entryMode = (PageEntryMode)EditorGUILayout.EnumPopup("Entry Mode", page.EntryMode);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(graph, "Edit Navigation Page");
                    page.SetDefaultNode(selected == 0 ? null : nodes[selected - 1].Id);
                    page.SetEntryMode(entryMode);
                    NavigationGraphAutoSaver.Touch(graph);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Page"))
            {
                Undo.RecordObject(graph, "Add Navigation Page");
                graph.AddPage(new NavigationPage(Guid.NewGuid().ToString(), "New Page"));
                NavigationGraphAutoSaver.Touch(graph);
            }
        }
    }
}
