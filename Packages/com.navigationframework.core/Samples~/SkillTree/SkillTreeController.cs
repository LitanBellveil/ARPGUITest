using System;
using System.Collections.Generic;
using System.Linq;
using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    [Serializable]
    public class SkillDefinition
    {
        public string id;
        public string parentId;
        public string displayName;
    }

    /// <summary>
    /// Generates a small branching skill tree at runtime and connects it with parent/child
    /// (Up/Down) and sibling (Left/Right) edges, instead of the nearest-neighbor grid Auto Connect
    /// produces — a branching tree isn't a grid, and these nodes don't exist in a
    /// <see cref="NavigationGraph"/> asset for Auto Connect to see anyway (same reasoning as
    /// Inventory/Carousel). A parent with several children gets one Down connection per child,
    /// ranked by declaration order via <see cref="NavigationConnection"/>'s <c>priority</c> — Down
    /// from the parent always lands on the first child, and Left/Right connections between
    /// siblings reach the rest. Positions are a simple per-depth row layout (each depth is one row,
    /// evenly spaced) — good enough for a small demo tree, not a general tree-layout algorithm.
    /// </summary>
    public class SkillTreeController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private RectTransform treeParent;
        [SerializeField] private NavigationSelectable nodePrefab;
        [SerializeField] private List<SkillDefinition> skills;
        [SerializeField] private float rowHeight = 140f;
        [SerializeField] private float columnWidth = 160f;

        private readonly Dictionary<string, NavigationNode> nodesById = new Dictionary<string, NavigationNode>();

        private void Start()
        {
            Dictionary<string, int> depthById = ComputeDepths();

            foreach (IGrouping<int, SkillDefinition> row in skills.GroupBy(s => depthById[s.id]))
            {
                int depth = row.Key;
                List<SkillDefinition> rowSkills = row.ToList();
                float startX = -(rowSkills.Count - 1) * columnWidth * 0.5f;

                for (int i = 0; i < rowSkills.Count; i++)
                {
                    SkillDefinition skill = rowSkills[i];
                    NavigationSelectable instance = Instantiate(nodePrefab, treeParent);
                    instance.name = skill.displayName;
                    instance.RectTransform.anchoredPosition = new Vector2(startX + i * columnWidth, -depth * rowHeight);

                    NavigationNode node = router.Manager.RegisterDynamicNode(
                        null, null, instance, instance.RectTransform, skill.displayName);

                    nodesById[skill.id] = node;
                }
            }

            ConnectTree();

            SkillDefinition rootSkill = skills.FirstOrDefault(s => string.IsNullOrEmpty(s.parentId));

            if (rootSkill != null)
            {
                router.Manager.SelectNode(nodesById[rootSkill.id].Id);
            }
        }

        private Dictionary<string, int> ComputeDepths()
        {
            var depthById = new Dictionary<string, int>();
            Dictionary<string, SkillDefinition> byId = skills.ToDictionary(s => s.id);

            int DepthOf(string id)
            {
                if (depthById.TryGetValue(id, out int cached))
                {
                    return cached;
                }

                SkillDefinition skill = byId[id];
                int depth = string.IsNullOrEmpty(skill.parentId) ? 0 : DepthOf(skill.parentId) + 1;
                depthById[id] = depth;
                return depth;
            }

            foreach (SkillDefinition skill in skills)
            {
                DepthOf(skill.id);
            }

            return depthById;
        }

        private void ConnectTree()
        {
            foreach (SkillDefinition skill in skills)
            {
                if (string.IsNullOrEmpty(skill.parentId))
                {
                    continue;
                }

                nodesById[skill.id].AddConnection(new NavigationConnection(nodesById[skill.parentId].Id, Direction.Up));
            }

            foreach (IGrouping<string, SkillDefinition> siblingGroup in skills
                .Where(s => !string.IsNullOrEmpty(s.parentId))
                .GroupBy(s => s.parentId))
            {
                List<SkillDefinition> siblings = siblingGroup.ToList();
                NavigationNode parent = nodesById[siblingGroup.Key];

                for (int i = 0; i < siblings.Count; i++)
                {
                    NavigationNode child = nodesById[siblings[i].id];
                    parent.AddConnection(new NavigationConnection(child.Id, Direction.Down, siblings.Count - i));

                    if (i > 0)
                    {
                        NavigationNode previous = nodesById[siblings[i - 1].id];
                        previous.AddConnection(new NavigationConnection(child.Id, Direction.Right));
                        child.AddConnection(new NavigationConnection(previous.Id, Direction.Left));
                    }
                }
            }
        }
    }
}
