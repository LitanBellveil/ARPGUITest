using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// The editor window that hosts a <see cref="NavigationGraphView"/> for one
    /// <see cref="NavigationGraph"/> asset, plus a side inspector for the currently selected node
    /// or connection. Opened via the asset's Inspector button (<see cref="NavigationGraphEditor"/>)
    /// or by double-clicking the asset. A single window instance is reused across graphs, mirroring
    /// how most single-purpose Unity tool windows behave.
    /// </summary>
    public sealed class NavigationGraphEditorWindow : EditorWindow
    {
        [SerializeField] private NavigationGraph graph;

        /// <summary> Opens (or refocuses) the graph window on <paramref name="target"/>. </summary>
        public static void Open(NavigationGraph target)
        {
            var window = GetWindow<NavigationGraphEditorWindow>();
            window.titleContent = new GUIContent($"Navigation Graph — {target.name}");
            window.Load(target);
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            if (!(EditorUtility.InstanceIDToObject(instanceId) is NavigationGraph asset))
            {
                return false;
            }

            Open(asset);
            return true;
        }

        private void OnEnable()
        {
            if (graph != null)
            {
                Load(graph);
            }
        }

        private void Load(NavigationGraph target)
        {
            graph = target;
            rootVisualElement.Clear();

            var splitView = new TwoPaneSplitView(1, 260, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            rootVisualElement.Add(splitView);

            var graphView = new NavigationGraphView(graph);
            graphView.style.flexGrow = 1;
            splitView.Add(graphView);

            var inspectorPanel = new NavigationGraphInspectorPanel(graph);
            splitView.Add(inspectorPanel);

            graphView.SelectionUpdated += inspectorPanel.SetSelection;
        }
    }
}
