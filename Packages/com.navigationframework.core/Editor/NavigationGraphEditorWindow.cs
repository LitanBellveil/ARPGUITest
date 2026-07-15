using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
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

        private bool lastKnownDirty;

        /// <summary> Opens (or refocuses) the graph window on <paramref name="target"/>. </summary>
        public static void Open(NavigationGraph target)
        {
            var window = GetWindow<NavigationGraphEditorWindow>();
            window.Load(target);
        }

        /// <summary>
        /// Rebuilds the view for <paramref name="target"/> if a window is already showing it,
        /// without opening a new window or stealing focus. Used after batch operations that mutate
        /// the graph from outside the window (Generate From Scene, Auto Connect) so their result is
        /// visible immediately instead of only after the window is closed and reopened.
        /// </summary>
        public static void RefreshIfOpen(NavigationGraph target)
        {
            foreach (NavigationGraphEditorWindow window in Resources.FindObjectsOfTypeAll<NavigationGraphEditorWindow>())
            {
                if (window.graph == target)
                {
                    window.Load(target);
                }
            }
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

        /// <summary> Keeps the title bar's dirty asterisk in sync with the graph's actual dirty state. </summary>
        private void Update()
        {
            if (graph == null)
            {
                return;
            }

            bool dirty = EditorUtility.IsDirty(graph);

            if (dirty != lastKnownDirty)
            {
                lastKnownDirty = dirty;
                RefreshTitle();
            }
        }

        private void RefreshTitle()
        {
            string suffix = lastKnownDirty ? " *" : string.Empty;
            titleContent = new GUIContent($"Navigation Graph — {graph.name}{suffix}");
        }

        private void Load(NavigationGraph target)
        {
            graph = target;
            lastKnownDirty = EditorUtility.IsDirty(graph);
            RefreshTitle();
            rootVisualElement.Clear();

            var root = new VisualElement();
            root.style.flexGrow = 1;
            rootVisualElement.Add(root);

            root.Add(BuildToolbar());

            var splitView = new TwoPaneSplitView(1, 260, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            root.Add(splitView);

            var graphView = new NavigationGraphView(graph);
            graphView.style.flexGrow = 1;
            splitView.Add(graphView);

            var inspectorPanel = new NavigationGraphInspectorPanel(graph);
            splitView.Add(inspectorPanel);

            graphView.SelectionUpdated += inspectorPanel.SetSelection;
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();

            var saveButton = new ToolbarButton(() => NavigationGraphAutoSaver.SaveNow(graph)) { text = "Save Now" };
            toolbar.Add(saveButton);

            var autoSaveToggle = new ToolbarToggle { label = "Auto Save", value = NavigationGraphAutoSaver.AutoSaveEnabled };
            autoSaveToggle.RegisterValueChangedCallback(evt => NavigationGraphAutoSaver.AutoSaveEnabled = evt.newValue);
            toolbar.Add(autoSaveToggle);

            return toolbar;
        }
    }
}
