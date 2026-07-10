using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScrambleTextAnimation))]
public class ScrambleTextAnimationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Test Controls ──", EditorStyles.boldLabel);

        ScrambleTextAnimation scramble = (ScrambleTextAnimation)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("需要進入 Play Mode 才能預覽播放。", MessageType.Info);
            GUI.enabled = false;
        }

        if (GUILayout.Button("Play Preview", GUILayout.Height(35)))
            scramble.PlayScramble();

        if (GUILayout.Button("Stop", GUILayout.Height(25)))
            scramble.Stop();

        GUI.enabled = true;
    }
}
