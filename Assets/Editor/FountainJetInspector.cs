using MusicalFountains;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(FountainJet))]
public sealed class FountainJetInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();
        if (!Application.isPlaying && (changed || GUILayout.Button("Update water preview")))
            ApplyPreview((FountainJet)target);
    }

    public static void ApplyPreview(FountainJet jet)
    {
        if (!jet || !jet.stream) return;
        Undo.RecordObject(jet.stream, "Update fountain preview");
        int count = Mathf.Max(2, jet.stream.positionCount);
        jet.stream.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            jet.stream.SetPosition(i, new Vector3(0, 4 * jet.height * t * (1 - t), jet.reach * t));
        }
        jet.stream.widthMultiplier = jet.streamWidth;
        jet.stream.enabled = true;
        if (jet.ripple) jet.ripple.transform.localPosition = new Vector3(0, -0.053f, jet.reach);
        if (jet.droplets)
        {
            Undo.RecordObject(jet.droplets, "Update droplet height");
            var main = jet.droplets.main;
            main.startSpeed = Mathf.Sqrt(2 * 9.81f * jet.height);
            main.startLifetime = 2 * Mathf.Sqrt(2 * 9.81f * jet.height) / 9.81f;
        }
        EditorUtility.SetDirty(jet.stream);
        if (jet.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(jet.gameObject.scene);
        SceneView.RepaintAll();
    }
}
