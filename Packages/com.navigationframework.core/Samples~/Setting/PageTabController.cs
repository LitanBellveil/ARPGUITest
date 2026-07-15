using System;
using System.Collections.Generic;
using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Demonstrates <see cref="NavigationManager.SwitchToPage"/> for a tabbed settings screen. Each
    /// tab has its own <see cref="NavigationPage"/> and its own content Group; switching tabs
    /// disables the previous tab's Group (via <see cref="NavigationManager.SetGroupEnabled"/>) so
    /// arrow-key navigation can never wander into a hidden tab's content — <c>SwitchToPage</c>
    /// alone only changes which node gets focused, it does not hide/disable anything by itself.
    /// Pages/Groups are looked up by <see cref="NavigationPage.DisplayName"/>/
    /// <see cref="NavigationGroup.DisplayName"/> rather than their GUID <c>Id</c>, since the Graph
    /// Window currently has no way to read a page/group's raw Id back out for typing into this
    /// component's Inspector fields. All five arrays are parallel and indexed by tab.
    /// </summary>
    public class PageTabController : MonoBehaviour
    {
        [SerializeField] private NavigationInputRouter router;
        [SerializeField] private NavigationSelectable[] tabSelectables;
        [SerializeField] private string[] pageNames;
        [SerializeField] private string[] groupNames;
        [SerializeField] private GameObject[] contentPanels;
        [SerializeField] private GameObject[] tabActiveIndicators;

        private string[] pageIds;
        private string[] groupIds;
        private Action[] submitHandlers;
        private int activeTabIndex = -1;

        private void OnEnable()
        {
            submitHandlers = new Action[tabSelectables.Length];

            for (int i = 0; i < tabSelectables.Length; i++)
            {
                int index = i;
                submitHandlers[i] = () => SwitchTab(index);
                tabSelectables[i].Submitted += submitHandlers[i];
            }
        }

        private void OnDisable()
        {
            if (submitHandlers == null)
            {
                return;
            }

            for (int i = 0; i < tabSelectables.Length; i++)
            {
                tabSelectables[i].Submitted -= submitHandlers[i];
            }

            submitHandlers = null;
        }

        private void Start()
        {
            NavigationGraph graph = router.Manager.Graph;
            pageIds = new string[pageNames.Length];
            groupIds = new string[groupNames.Length];

            for (int i = 0; i < pageNames.Length; i++)
            {
                pageIds[i] = FindByDisplayName(graph.Pages, pageNames[i])?.Id;
                groupIds[i] = FindByDisplayName(graph.Groups, groupNames[i])?.Id;

                if (pageIds[i] == null)
                {
                    Debug.LogError($"[NavigationFramework] No NavigationPage named '{pageNames[i]}' found on '{graph.name}'.", this);
                }

                if (groupIds[i] == null)
                {
                    Debug.LogError($"[NavigationFramework] No NavigationGroup named '{groupNames[i]}' found on '{graph.name}'.", this);
                }
            }

            for (int i = 0; i < groupIds.Length; i++)
            {
                router.Manager.SetGroupEnabled(groupIds[i], i == 0);
            }

            SwitchTab(0);
        }

        private static NavigationPage FindByDisplayName(IReadOnlyList<NavigationPage> pages, string displayName)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].DisplayName == displayName)
                {
                    return pages[i];
                }
            }

            return null;
        }

        private static NavigationGroup FindByDisplayName(IReadOnlyList<NavigationGroup> groups, string displayName)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].DisplayName == displayName)
                {
                    return groups[i];
                }
            }

            return null;
        }

        private void SwitchTab(int index)
        {
            if (index == activeTabIndex)
            {
                return;
            }

            if (activeTabIndex >= 0)
            {
                router.Manager.SetGroupEnabled(groupIds[activeTabIndex], false);
            }

            activeTabIndex = index;
            router.Manager.SetGroupEnabled(groupIds[index], true);
            router.Manager.SwitchToPage(pageIds[index]);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < contentPanels.Length; i++)
            {
                contentPanels[i].SetActive(i == activeTabIndex);
                tabActiveIndicators[i].SetActive(i == activeTabIndex);
            }
        }
    }
}
