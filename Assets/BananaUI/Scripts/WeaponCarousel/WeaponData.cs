using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon Carousel/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite icon;
    [TextArea] public string description;
}
