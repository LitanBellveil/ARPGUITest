using System.Collections.Generic;
using UnityEditor;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// Debounced auto-save for <see cref="NavigationGraph"/> assets. Editing code calls
    /// <see cref="Touch"/> instead of <c>EditorUtility.SetDirty</c> alone; after
    /// <see cref="AutoSaveDelaySeconds"/> of no further edits to that graph, it is flushed to disk
    /// with <c>AssetDatabase.SaveAssetIfDirty</c>. Every pending graph is also force-flushed right
    /// before entering Play Mode, so an edit made just before pressing Play is never left only in
    /// memory — see the Phase 3 doc note on why an unsaved graph's scene references can silently
    /// break under a scene reload.
    /// </summary>
    [InitializeOnLoad]
    public static class NavigationGraphAutoSaver
    {
        private const double AutoSaveDelaySeconds = 2.0;
        private const string AutoSaveEnabledPrefKey = "NavigationFramework.AutoSaveEnabled";

        private static readonly Dictionary<NavigationGraph, double> pendingSaves = new Dictionary<NavigationGraph, double>();

        /// <summary> Whether edits are auto-saved after a short delay. Persisted per-user via <c>EditorPrefs</c>. </summary>
        public static bool AutoSaveEnabled
        {
            get => EditorPrefs.GetBool(AutoSaveEnabledPrefKey, true);
            set => EditorPrefs.SetBool(AutoSaveEnabledPrefKey, value);
        }

        static NavigationGraphAutoSaver()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Marks <paramref name="graph"/> dirty and, if <see cref="AutoSaveEnabled"/>, (re)schedules
        /// a debounced save — repeated edits within the delay window collapse into a single save.
        /// </summary>
        public static void Touch(NavigationGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            EditorUtility.SetDirty(graph);

            if (AutoSaveEnabled)
            {
                pendingSaves[graph] = EditorApplication.timeSinceStartup + AutoSaveDelaySeconds;
            }
        }

        /// <summary> Immediately saves <paramref name="graph"/> to disk if dirty, bypassing the debounce delay. </summary>
        public static void SaveNow(NavigationGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            pendingSaves.Remove(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
        }

        private static void OnEditorUpdate()
        {
            if (pendingSaves.Count == 0)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            List<NavigationGraph> ready = null;

            foreach (KeyValuePair<NavigationGraph, double> entry in pendingSaves)
            {
                if (now >= entry.Value)
                {
                    (ready ??= new List<NavigationGraph>()).Add(entry.Key);
                }
            }

            if (ready == null)
            {
                return;
            }

            foreach (NavigationGraph graph in ready)
            {
                pendingSaves.Remove(graph);

                if (graph != null)
                {
                    AssetDatabase.SaveAssetIfDirty(graph);
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || pendingSaves.Count == 0)
            {
                return;
            }

            foreach (NavigationGraph graph in pendingSaves.Keys)
            {
                if (graph != null)
                {
                    AssetDatabase.SaveAssetIfDirty(graph);
                }
            }

            pendingSaves.Clear();
        }
    }
}
