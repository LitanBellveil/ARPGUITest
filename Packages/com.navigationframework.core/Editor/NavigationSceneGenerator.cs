using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NavigationFramework.Editor
{
    /// <summary>
    /// Scans a scene hierarchy for <see cref="NavigationSelectable"/> components and creates/updates
    /// matching <see cref="NavigationNode"/> entries in a <see cref="NavigationGraph"/>. Purely
    /// additive: existing nodes are matched by their <see cref="NavigationNode.Selectable"/>
    /// reference and only have their scene references refreshed — never their display name,
    /// group/page assignment, connections, or <see cref="NavigationNode.EditorPosition"/> — so
    /// re-running this after hand-editing the graph in the Graph Window never clobbers that work.
    /// Nodes are never removed for selectables that can no longer be found; delete those manually
    /// in the Graph Window if that's what's needed.
    /// </summary>
    public static class NavigationSceneGenerator
    {
        /// <summary> Scans <paramref name="scanRoot"/> (inactive children included) and updates <paramref name="graph"/> in place. </summary>
        public static void GenerateFromScene(NavigationGraph graph, Transform scanRoot)
        {
            if (graph == null || scanRoot == null)
            {
                return;
            }

            var existingBySelectable = new Dictionary<NavigationSelectable, NavigationNode>();

            foreach (NavigationNode node in graph.Nodes)
            {
                if (node.Selectable != null)
                {
                    existingBySelectable[node.Selectable] = node;
                }
            }

            NavigationSelectable[] found = scanRoot.GetComponentsInChildren<NavigationSelectable>(true);

            int created = 0;
            int updated = 0;

            Undo.RecordObject(graph, "Generate Navigation Graph From Scene");

            foreach (NavigationSelectable selectable in found)
            {
                RectTransform rectTransform = selectable.RectTransform != null
                    ? selectable.RectTransform
                    : selectable.transform as RectTransform;

                if (existingBySelectable.TryGetValue(selectable, out NavigationNode existingNode))
                {
                    existingNode.SetSceneReferences(rectTransform, selectable);
                    updated++;
                    continue;
                }

                var node = new NavigationNode(Guid.NewGuid().ToString(), selectable.gameObject.name);
                node.SetSceneReferences(rectTransform, selectable);
                node.SetEditorPosition(ToEditorPosition(rectTransform));
                graph.AddNode(node);
                created++;
            }

            NavigationGraphAutoSaver.Touch(graph);

            Debug.Log($"[NavigationFramework] Generate From Scene on '{scanRoot.name}': created {created}, updated {updated} (scanned {found.Length}).", scanRoot);
        }

        private static Vector2 ToEditorPosition(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            // Flip Y: uGUI's anchoredPosition is Y-up, but the Graph Window's canvas is Y-down —
            // without this, generated nodes would appear vertically mirrored from the real UI.
            Vector2 anchored = rectTransform.anchoredPosition;
            return new Vector2(anchored.x, -anchored.y);
        }
    }
}
