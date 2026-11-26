using System;
using System.IO;
using MusicalFountains;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class FountainRecorderSetup
{
    const string AssetsRoot = "Assets/MusicalFountains";

    [MenuItem("Tools/Musical Fountains/Add Recording Controls")]
    public static void AddControls()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before editing");
        var stage = UnityEngine.Object.FindAnyObjectByType<FountainStage>();
        if (!stage) throw new InvalidOperationException("Open the fountain scene first");
        if (stage.GetComponentInChildren<FountainRecorder>()) throw new InvalidOperationException("Recording controls already exist; edit the saved scene objects");
        var dark = AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot + "/Materials/Basalt.mat");
        var steel = AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot + "/Materials/BrushedMetal.mat");
        var panel = new GameObject("Recording — Record, Stop, Playback");
        panel.transform.SetParent(stage.transform, false);
        panel.transform.localPosition = new Vector3(0, 1.04f, -0.76f);
        panel.transform.localEulerAngles = new Vector3(-25, 0, 0);
        Undo.RegisterCreatedObjectUndo(panel, "Add recording controls");
        var recorder = panel.AddComponent<FountainRecorder>();
        recorder.keys = stage.keys;
        recorder.maxRecordSeconds = 30;
        recorder.statusLabel = MakeText("Запишите свою мелодию · до 30 секунд", panel.transform, new Vector3(0, 0.22f, 0.035f), 0.009f, new Vector3(25, 0, 0));
        Cube("Recorder shelf", panel.transform, new Vector3(0, -0.015f, 0), new Vector3(1.16f, 0.055f, 0.22f), dark);
        Cube("Left bracket", panel.transform, new Vector3(-0.48f, -0.09f, 0.015f), new Vector3(0.035f, 0.15f, 0.12f), steel);
        Cube("Right bracket", panel.transform, new Vector3(0.48f, -0.09f, 0.015f), new Vector3(0.035f, 0.15f, 0.12f), steel);
        var labels = new[] { "ЗАПИСЬ", "СТОП", "ВОСПРОИЗВЕСТИ" };
        var names = new[] { "Record", "Stop", "Playback" };
        var colors = new[] { new Color(0.95f, 0.27f, 0.31f), new Color(0.75f, 0.8f, 0.84f), new Color(0.25f, 0.83f, 0.75f) };
        for (int i = 0; i < 3; i++)
        {
            string materialPath = AssetsRoot + "/Materials/Transport_" + names[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", colors[i]);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var root = new GameObject(names[i] + " button");
            root.transform.SetParent(panel.transform, false);
            root.transform.localPosition = new Vector3((i - 1) * 0.36f, 0.025f, 0);
            var button = root.AddComponent<FountainTransportButton>();
            button.recorder = recorder;
            button.action = (FountainTransportAction)i;
            button.color = colors[i];
            var pivot = new GameObject("Press cap pivot"); pivot.transform.SetParent(root.transform, false);
            button.cap = pivot.transform;
            button.capRenderer = Cube("Colored cap", pivot.transform, Vector3.zero, new Vector3(0.31f, 0.035f, 0.135f), material).GetComponent<Renderer>();
            var label = MakeText(labels[i], pivot.transform, new Vector3(0, 0.020f, 0), i == 2 ? 0.0048f : 0.0065f, new Vector3(90, 0, 0));
            label.color = Color.white;
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(0.32f, 0.085f, 0.17f); collider.isTrigger = true;
            EditorUtility.SetDirty(button);
        }
        EditorUtility.SetDirty(recorder);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("[MusicalFountains] Recording controls saved. Maximum duration: 30 seconds. Chords and note releases are captured.");
    }

    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    static TextMesh MakeText(string value, Transform parent, Vector3 position, float size, Vector3 rotation)
    {
        var go = new GameObject(value); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localEulerAngles = rotation;
        var text = go.AddComponent<TextMesh>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = 64; text.characterSize = size;
        text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(0.75f, 0.83f, 0.87f);
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = text.font.material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return text;
    }
}
