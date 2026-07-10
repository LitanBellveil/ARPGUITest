using UnityEngine;
using Unity.Cinemachine;

public class CameraStateSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcNormal;
    [SerializeField] CinemachineCamera vcWeapon;
    [SerializeField] CinemachineCamera vcSetting;
    [SerializeField] CinemachineCamera vcDefault;

    void Start() => SwitchTo(vcNormal);

    public void GoToNormal()  => SwitchTo(vcNormal);
    public void GoToWeapon()  => SwitchTo(vcWeapon);
    public void GoToSetting() => SwitchTo(vcSetting);
    public void GoToDefault() => SwitchTo(vcDefault);

    void SwitchTo(CinemachineCamera target)
    {
        vcNormal.Priority.Value  = 0;
        vcWeapon.Priority.Value  = 0;
        vcSetting.Priority.Value = 0;
        vcDefault.Priority.Value = 0;
        target.Priority.Value    = 10;
    }
}