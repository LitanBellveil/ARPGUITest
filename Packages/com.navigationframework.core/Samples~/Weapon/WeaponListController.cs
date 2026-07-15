using System;
using NavigationFramework;
using UnityEngine;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Demonstrates a second state layered on top of focus: "equipped" persists after focus moves
    /// away, unlike <see cref="NavigationFocusVisual"/>'s transient highlight. Submitting a slot
    /// (Enter, via <see cref="NavigationSelectable.Submitted"/>) equips it and shows
    /// <see cref="equippedIndicators"/>' matching entry; every other slot's indicator is hidden.
    /// </summary>
    public class WeaponListController : MonoBehaviour
    {
        [SerializeField] private NavigationSelectable[] weaponSlots;
        [SerializeField] private GameObject[] equippedIndicators;

        private Action[] submitHandlers;
        private int equippedIndex = -1;

        private void OnEnable()
        {
            submitHandlers = new Action[weaponSlots.Length];

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                int index = i;
                submitHandlers[i] = () => Equip(index);
                weaponSlots[i].Submitted += submitHandlers[i];
            }

            RefreshIndicators();
        }

        private void OnDisable()
        {
            if (submitHandlers == null)
            {
                return;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                weaponSlots[i].Submitted -= submitHandlers[i];
            }

            submitHandlers = null;
        }

        private void Equip(int index)
        {
            equippedIndex = index;
            RefreshIndicators();
        }

        private void RefreshIndicators()
        {
            for (int i = 0; i < equippedIndicators.Length; i++)
            {
                if (equippedIndicators[i] != null)
                {
                    equippedIndicators[i].SetActive(i == equippedIndex);
                }
            }
        }
    }
}
