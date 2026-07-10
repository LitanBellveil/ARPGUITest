using UnityEngine;
using UnityEngine.VFX;

public class WeaponDissolveEvent : MonoBehaviour
{
    public VisualEffect dissolveVFX;

    void OnEnable()
    {
        // OnEnable 比 Start 更早，VFX Active 時會呼叫
        dissolveVFX.Stop();
        dissolveVFX.pause = true;
    }

    public void TriggerDissolve()
    {
        dissolveVFX.pause = false;
        dissolveVFX.Play();
        dissolveVFX.SendEvent("OnPlay");
    }
}