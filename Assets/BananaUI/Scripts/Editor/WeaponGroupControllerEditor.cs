using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponGroupController))]
public class WeaponGroupControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Test Controls ──", EditorStyles.boldLabel);

        WeaponGroupController ctrl = (WeaponGroupController)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("需要進入 Play Mode 才能使用切換按鈕。", MessageType.Info);
            GUI.enabled = false;
        }

        string label = ctrl.isShowingWeapons
            ? "切換 → 隱藏 Weapons / 顯示 Weapons2"
            : "切換 → 顯示 Weapons / 隱藏 Weapons2";

        if (GUILayout.Button(label, GUILayout.Height(35)))
            ctrl.ToggleWeapons();

        GUI.enabled = true;
    }
}
