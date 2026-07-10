using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class WeaponSelectPanel : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private PlayableAsset showTimeline;
    [SerializeField] private PlayableAsset hideTimeline;

    public void Refresh(WeaponData data) { }

    public IEnumerator Show()
    {
        director.playableAsset = showTimeline;
        director.Play();
        yield return new WaitWhile(() => director.state == PlayState.Playing);
    }

    public IEnumerator Hide()
    {
        director.playableAsset = hideTimeline;
        director.Play();
        yield return new WaitWhile(() => director.state == PlayState.Playing);
    }
}
