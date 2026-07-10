using System.Collections;
using UnityEngine;

public class WeaponGroupController : MonoBehaviour
{
    [Header("Weapons References")]
    public GameObject weapons;
    public Animator weaponsAnimator;

    [Header("Weapons2 References")]
    public GameObject weapons2;
    public Animator weapons2Animator;

    [Header("Animation Names")]
    public string showAnimation = "Weapon_Default_Show";
    public string hideAnimation = "Weapon_Default_Hide";

    [HideInInspector]
    public bool isShowingWeapons = true;

    private void Start()
    {
        weapons.SetActive(true);
        weapons2.SetActive(false);
        isShowingWeapons = true;
    }

    public void ToggleWeapons()
    {
        if (isShowingWeapons)
        {
            weapons2.SetActive(true);
            weapons2Animator.Play(showAnimation);
            weaponsAnimator.Play(hideAnimation);
            StartCoroutine(DeactivateAfterAnim(weapons, weaponsAnimator));
            isShowingWeapons = false;
        }
        else
        {
            weapons.SetActive(true);
            weaponsAnimator.Play(showAnimation);
            weapons2Animator.Play(hideAnimation);
            StartCoroutine(DeactivateAfterAnim(weapons2, weapons2Animator));
            isShowingWeapons = true;
        }
    }

    private IEnumerator DeactivateAfterAnim(GameObject obj, Animator anim)
    {
        yield return null;
        yield return null;
        float length = anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        if (obj != null)
            obj.SetActive(false);
    }
}
