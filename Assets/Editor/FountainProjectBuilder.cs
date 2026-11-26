using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicalFountains;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class FountainProjectBuilder
{
    const string Root = "Assets/MusicalFountains";
    const string ScenePath = "Assets/Scenes/MusicalFountains.unity";
    static readonly string[] Notes = { "До", "Ре", "Ми", "Фа", "Соль", "Ля", "Си", "До" };
    static readonly string[] Pitches = { "C4", "D4", "E4", "F4", "G4", "A4", "B4", "C5" };
    static readonly double[] Frequencies = { 261.625565, 293.664768, 329.627557, 349.228231, 391.995436, 440, 493.883301, 523.251131 };
    static readonly string[] Hex = { "4DD8DF", "48BAD5", "6096E5", "9B83E4", "CD88D7", "EE9C9B", "EBC078", "AAD68A" };
    static Mesh cylinder;
    static Material dark, rim, steel, water, textMaterial;
    static Font font;

    [MenuItem("Tools/Musical Fountains/Create Editable Prototype")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building the scene");
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open " + ScenePath + " before creating the prototype");
        if (UnityEngine.Object.FindAnyObjectByType<FountainStage>())
            throw new InvalidOperationException("A fountain already exists. Edit its scene objects; this command will not overwrite them.");
        foreach (string dir in new[] { "Models", "Materials", "Prefabs", "Audio", "Input", "Textures" })
            Directory.CreateDirectory(Root + "/" + dir);
        AssetDatabase.Refresh();
        cylinder = CreateCylinder();
        dark = Material("Basalt", new Color(0.075f, 0.095f, 0.12f), false);
        rim = Material("Ceramic", new Color(0.64f, 0.7f, 0.72f), false);
        steel = Material("BrushedMetal", new Color(0.27f, 0.34f, 0.38f), false);
        water = Material("WaterSurface", new Color(0.025f, 0.17f, 0.205f), true);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMaterial = font.material;
        var camera = Camera.main;
        if (!camera) throw new InvalidOperationException("The scene must contain the PICO XR camera");
        camera.allowHDR = false;
        camera.nearClipPlane = 0.05f;
        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) light.shadows = LightShadows.None;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.63f, 0.69f);

        var world = new GameObject("Musical Fountains — EDIT THIS GROUP");
        Undo.RegisterCreatedObjectUndo(world, "Create musical fountain");
        world.transform.position = new Vector3(0, 0, 2.1f);
        var stage = world.AddComponent<FountainStage>();
        stage.playerCamera = camera;
        stage.keys = new FountainKey[8];
        var pool = new GameObject("Basin");
        pool.transform.SetParent(world.transform, false);
        Shape("Base", PrimitiveType.Cube, pool.transform, new Vector3(0, 0.09f, 0.18f), new Vector3(2.42f, 0.18f, 0.96f), dark);
        Shape("Water", PrimitiveType.Cube, pool.transform, new Vector3(0, 0.185f, 0.18f), new Vector3(2.28f, 0.018f, 0.8f), water);
        Shape("Front rim", PrimitiveType.Cube, pool.transform, new Vector3(0, 0.21f, -0.28f), new Vector3(2.44f, 0.08f, 0.055f), rim);
        Shape("Back rim", PrimitiveType.Cube, pool.transform, new Vector3(0, 0.21f, 0.64f), new Vector3(2.44f, 0.08f, 0.055f), rim);
        Shape("Left rim", PrimitiveType.Cube, pool.transform, new Vector3(-1.19f, 0.21f, 0.18f), new Vector3(0.055f, 0.08f, 0.9f), rim);
        Shape("Right rim", PrimitiveType.Cube, pool.transform, new Vector3(1.19f, 0.21f, 0.18f), new Vector3(0.055f, 0.08f, 0.9f), rim);
        PrefabUtility.SaveAsPrefabAssetAndConnect(pool, Root + "/Prefabs/Basin.prefab", InteractionMode.AutomatedAction);

        var desk = new GameObject("Keyboard console");
        desk.transform.SetParent(world.transform, false);
        Shape("Console top", PrimitiveType.Cube, desk.transform, new Vector3(0, 0.82f, -0.86f), new Vector3(2.24f, 0.09f, 0.48f), dark);
        Shape("Left support", PrimitiveType.Cube, desk.transform, new Vector3(-0.88f, 0.4f, -0.86f), new Vector3(0.065f, 0.8f, 0.2f), steel);
        Shape("Right support", PrimitiveType.Cube, desk.transform, new Vector3(0.88f, 0.4f, -0.86f), new Vector3(0.065f, 0.8f, 0.2f), steel);
        var noteKeys = new GameObject("Keys — note clips and jet references");
        noteKeys.transform.SetParent(world.transform, false);
        var jets = new GameObject("Jets — height, reach and particles");
        jets.transform.SetParent(world.transform, false);
        var dropletTexture = CreateDropTexture();
        for (int i = 0; i < 8; i++)
        {
            ColorUtility.TryParseHtmlString("#" + Hex[i], out Color color);
            var accent = Material("Note_" + Pitches[i], color, true);
            var streamMaterial = Material("Stream_" + Pitches[i], Color.Lerp(color, Color.white, 0.5f), true);
            var dropsMaterial = ParticleMaterial("Drops_" + Pitches[i], color, dropletTexture);
            float x = (i - 3.5f) * 0.255f;
            var jetObject = new GameObject("Jet " + (i + 1) + " — " + Pitches[i]);
            jetObject.transform.SetParent(jets.transform, false);
            jetObject.transform.localPosition = new Vector3(x, 0.255f, -0.1f);
            var jet = jetObject.AddComponent<FountainJet>();
            jet.height = 0.95f + i * 0.055f;
            var nozzle = new GameObject("Nozzle model");
            nozzle.transform.SetParent(jetObject.transform, false);
            Cylinder("Socket", nozzle.transform, Vector3.down * 0.022f, new Vector3(0.085f, 0.045f, 0.085f), steel);
            Cylinder("Lit ring", nozzle.transform, Vector3.zero, new Vector3(0.075f, 0.012f, 0.075f), accent);
            Cylinder("Outlet", nozzle.transform, new Vector3(0, 0.01f, 0), new Vector3(0.04f, 0.018f, 0.04f), dark);
            PrefabUtility.SaveAsPrefabAssetAndConnect(nozzle, Root + "/Prefabs/Nozzle_" + Pitches[i] + ".prefab", InteractionMode.AutomatedAction);
            jet.stream = Line("Water arc", jetObject.transform, streamMaterial, 20, 0.018f, false);
            for (int p = 0; p < 20; p++)
            {
                float t = p / 19f;
                jet.stream.SetPosition(p, new Vector3(0, 4 * jet.height * t * (1 - t), jet.reach * t));
            }
            jet.ripple = Line("Ripple", jetObject.transform, accent, 24, 0.008f, false);
            jet.ripple.loop = true;
            jet.ripple.transform.localPosition = new Vector3(0, -0.053f, jet.reach);
            for (int p = 0; p < 24; p++)
            {
                float a = p * Mathf.PI * 2 / 24;
                jet.ripple.SetPosition(p, new Vector3(Mathf.Cos(a) * 0.075f, 0, Mathf.Sin(a) * 0.075f));
            }
            jet.droplets = CreateParticles(jetObject.transform, dropsMaterial, color, jet.height);
            Shape("Note light", PrimitiveType.Cube, pool.transform, new Vector3(x, 0.11f, -0.313f), new Vector3(0.15f, 0.012f, 0.008f), accent);

            var keyObject = new GameObject("Key " + (i + 1) + " — " + Notes[i] + " / " + Pitches[i]);
            keyObject.transform.SetParent(noteKeys.transform, false);
            keyObject.transform.localPosition = new Vector3(x, 0.895f, -0.86f);
            var key = keyObject.AddComponent<FountainKey>();
            key.noteName = Pitches[i];
            key.color = color;
            key.noteClip = WriteNote(Pitches[i], Frequencies[i]);
            var voice = keyObject.GetComponent<AudioSource>();
            voice.playOnAwake = false; voice.loop = true; voice.clip = key.noteClip; voice.volume = 0; voice.spatialBlend = 0;
            var collider = keyObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.19f, 0.085f, 0.23f);
            collider.center = new Vector3(0, 0.01f, 0);
            collider.isTrigger = true;
            Cylinder("Button base", keyObject.transform, new Vector3(0, -0.012f, 0), new Vector3(0.19f, 0.03f, 0.19f), steel);
            var capPivot = new GameObject("Press cap pivot"); capPivot.transform.SetParent(keyObject.transform, false); capPivot.transform.localPosition = new Vector3(0, 0.014f, 0);
            var cap = Cylinder("Press cap", capPivot.transform, Vector3.zero, new Vector3(0.165f, 0.035f, 0.165f), accent);
            key.cap = capPivot.transform;
            key.capRenderer = cap.GetComponent<Renderer>();
            Text(Notes[i].ToUpperInvariant(), capPivot.transform, new Vector3(0, 0.020f, 0), 0.008f, Color.white, new Vector3(90, 0, 0));
            Text((i + 1) + "  /  " + Pitches[i], keyObject.transform, new Vector3(0, 0.009f, -0.145f), 0.0072f, new Color(0.7f, 0.77f, 0.8f), new Vector3(90, 0, 0));
            PrefabUtility.SaveAsPrefabAssetAndConnect(keyObject, Root + "/Prefabs/Key_" + Pitches[i] + ".prefab", InteractionMode.AutomatedAction);
            key.jet = jet;
            PrefabUtility.RecordPrefabInstancePropertyModifications(key);
            stage.keys[i] = key;
        }
        Text("A Q U A  /  N O T E S", world.transform, new Vector3(0, 1.8f, 0.55f), 0.030f, new Color(0.8f, 0.88f, 0.92f), Vector3.zero);
        var help = Text("Курок / касание — играть   ·   Y — переставить", desk.transform, new Vector3(0, 0.872f, -0.675f), 0.0118f, new Color(0.6f, 0.69f, 0.74f), new Vector3(90, 0, 0));
        var interactionObject = new GameObject("Fountain Interaction — controllers and desktop preview");
        var controls = interactionObject.AddComponent<FountainControls>();
        controls.stage = stage;
        controls.playerCamera = camera;
        controls.trackingSpace = camera.transform.parent;
        controls.instructionText = help;
        controls.controllerActions = CreateInputActions();
        controls.leftTip = Shape("Left controller tip", PrimitiveType.Cube, interactionObject.transform, Vector3.zero, Vector3.one * 0.022f, Material("LeftPointer", new Color(0.3f, 0.85f, 0.95f), true)).transform;
        controls.rightTip = Shape("Right controller tip", PrimitiveType.Cube, interactionObject.transform, Vector3.zero, Vector3.one * 0.022f, Material("RightPointer", new Color(0.95f, 0.65f, 0.35f), true)).transform;
        controls.leftTip.gameObject.SetActive(false); controls.rightTip.gameObject.SetActive(false);
        controls.leftRay = Line("Left aim ray", interactionObject.transform, AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/LeftPointer.mat"), 2, 0.003f, true);
        controls.rightRay = Line("Right aim ray", interactionObject.transform, AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/RightPointer.mat"), 2, 0.003f, true);
        controls.leftRay.enabled = controls.rightRay.enabled = false;
        EditorUtility.SetDirty(stage);
        EditorUtility.SetDirty(controls);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = world;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 0.8f, 2.1f), Quaternion.Euler(22, 0, 0), 3.5f);
        Debug.Log("[MusicalFountains] Editable scene, 8 note WAVs, low-poly models and prefabs created.");
        VerifyScene();
    }

    static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return go;
    }

    static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = cylinder;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    static TextMesh Text(string value, Transform parent, Vector3 position, float size, Color color, Vector3 rotation)
    {
        var go = new GameObject(value); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localEulerAngles = rotation;
        var text = go.AddComponent<TextMesh>(); text.text = value; text.font = font; text.fontSize = 64; text.characterSize = size; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = color;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = textMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return text;
    }

    static Material Material(string name, Color color, bool unlit)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        material.name = name; material.SetColor("_BaseColor", color);
        if (!unlit) { material.SetFloat("_Smoothness", 0.38f); material.SetFloat("_Metallic", name == "BrushedMetal" ? 0.5f : 0); }
        AssetDatabase.CreateAsset(material, path); return material;
    }

    static Material ParticleMaterial(string name, Color color, Texture2D texture)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.name = name; material.SetColor("_BaseColor", Color.Lerp(color, Color.white, 0.6f)); material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, Root + "/Materials/" + name + ".mat"); return material;
    }

    static LineRenderer Line(string name, Transform parent, Material material, int count, float width, bool worldSpace)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = worldSpace; line.positionCount = count; line.widthMultiplier = width;
        line.numCapVertices = 2; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        return line;
    }

    static ParticleSystem CreateParticles(Transform parent, Material material, Color color, float height)
    {
        var go = new GameObject("Water droplets"); go.transform.SetParent(parent, false); go.transform.localEulerAngles = new Vector3(-85, 0, 0);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = false; main.loop = true; main.duration = 2; main.maxParticles = 100;
        main.startLifetime = 2 * Mathf.Sqrt(2 * 9.81f * height) / 9.81f; main.startSpeed = Mathf.Sqrt(2 * 9.81f * height); main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f); main.startColor = Color.Lerp(color, Color.white, 0.65f); main.gravityModifier = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 2; shape.radius = 0.007f;
        var emission = ps.emission; emission.rateOverTime = 65; emission.enabled = false;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.3f));
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    static Texture2D CreateDropTexture()
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float d = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
            texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - d) * 2)));
        }
        texture.Apply(); string path = Root + "/Textures/WaterDrop.png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Mesh CreateCylinder(int sides = 16)
    {
        string path = Root + "/Models/Cylinder" + sides + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing) return existing;
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f), q = new Vector3(Mathf.Cos(b) * 0.5f, 0, Mathf.Sin(b) * 0.5f);
            int start = vertices.Count;
            vertices.Add(p + Vector3.down * 0.5f); vertices.Add(q + Vector3.down * 0.5f); vertices.Add(q + Vector3.up * 0.5f); vertices.Add(p + Vector3.up * 0.5f);
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            start = vertices.Count; vertices.Add(Vector3.up * 0.5f); vertices.Add(p + Vector3.up * 0.5f); vertices.Add(q + Vector3.up * 0.5f);
            triangles.AddRange(new[] { start, start + 2, start + 1 });
            start = vertices.Count; vertices.Add(Vector3.down * 0.5f); vertices.Add(p + Vector3.down * 0.5f); vertices.Add(q + Vector3.down * 0.5f);
            triangles.AddRange(new[] { start, start + 1, start + 2 });
        }
        var mesh = new Mesh { name = "Low-poly cylinder / " + sides + " sides / " + sides * 4 + " triangles" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    static AudioClip WriteNote(string pitch, double frequency)
    {
        const int sampleRate = 48000, samples = 96000;
        double loopFrequency = Math.Round(frequency * 2) / 2;
        string path = Root + "/Audio/" + pitch + ".wav";
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
            for (int i = 0; i < samples; i++)
            {
                double phase = 2 * Math.PI * loopFrequency * i / sampleRate;
                double value = (Math.Sin(phase) + 0.22 * Math.Sin(phase * 2) + 0.08 * Math.Sin(phase * 3)) * 0.55;
                writer.Write((short)(value * short.MaxValue));
            }
        }
        AssetDatabase.ImportAsset(path);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path); importer.forceToMono = true;
        var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = settings; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static InputActionAsset CreateInputActions()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>(); var map = new InputActionMap("Fountains"); asset.AddActionMap(map);
        foreach (string hand in new[] { "Left", "Right" })
        {
            string prefix = "<XRController>{" + hand + "Hand}/";
            map.AddAction(hand + "Position", InputActionType.Value, prefix + "pointerPosition", expectedControlLayout: "Vector3");
            map.AddAction(hand + "Rotation", InputActionType.Value, prefix + "pointerRotation", expectedControlLayout: "Quaternion");
            map.AddAction(hand + "Tracked", InputActionType.Value, prefix + "isTracked", expectedControlLayout: "Button");
            map.AddAction(hand + "Trigger", InputActionType.Value, prefix + "trigger", expectedControlLayout: "Axis");
        }
        map.AddAction("Recenter", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        string path = Root + "/Input/FountainControls.inputactions"; File.WriteAllText(path, asset.ToJson()); UnityEngine.Object.DestroyImmediate(asset); AssetDatabase.ImportAsset(path);
        var result = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        EditorBuildSettings.AddConfigObject("com.unity.input.settings.actions", result, true);
        return result;
    }

    [MenuItem("Tools/Musical Fountains/Validate Prototype")]
    public static void VerifyScene()
    {
        var stage = UnityEngine.Object.FindAnyObjectByType<FountainStage>();
        if (!stage || stage.keys == null || stage.keys.Length != 8) throw new Exception("Expected eight keys");
        if (stage.keys.Select(k => k.jet).Distinct().Count() != 8) throw new Exception("Each key must reference a different jet");
        var output = new List<string>();
        for (int i = 0; i < stage.keys.Length; i++)
        {
            var key = stage.keys[i];
            if (!key || !key.noteClip || !key.jet || !key.cap || !key.capRenderer) throw new Exception("Broken key references: " + i);
            if (key.GetComponent<AudioSource>().playOnAwake) throw new Exception("Audio must not play before a key press");
            var data = new float[key.noteClip.samples]; if (!key.noteClip.GetData(data, 0)) throw new Exception("Cannot read note audio: " + key.noteName);
            double energy = 0; float peak = 0; int crossings = 0;
            for (int s = 1; s < data.Length; s++) { energy += data[s] * data[s]; peak = Mathf.Max(peak, Mathf.Abs(data[s])); if (data[s - 1] <= 0 && data[s] > 0) crossings++; }
            double hz = crossings / key.noteClip.length;
            if (Math.Abs(hz - Frequencies[i]) > 1 || peak >= 0.99f || energy < 1) throw new Exception("Invalid audio waveform: " + key.noteName);
            key.SetPressed(30, true); key.SetPressed(31, true); key.SetPressed(30, false);
            if (!key.IsPressed || !key.jet.IsFlowing) throw new Exception("Two-hand hold failed");
            key.SetPressed(31, false);
            if (key.IsPressed || key.jet.IsFlowing) throw new Exception("Note release failed");
            output.Add(key.noteName + ": " + hz.ToString("F1") + " Hz, PCM audio / references / two-owner hold PASS");
        }
        int triangles = stage.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh).Sum(m => m.sharedMesh.triangles.Length / 3);
        output.Add("Static model triangles: " + triangles);
        if (triangles > 12000) throw new Exception("Prototype exceeded the geometry budget");
        Directory.CreateDirectory("D:/Unity/build-work/musical-fountains");
        File.WriteAllLines("D:/Unity/build-work/musical-fountains/validation.txt", output);
        Debug.Log("[MusicalFountains] Validation passed. " + triangles + " static model triangles. Eight note waveforms and two-hand holds verified.");
    }

    [MenuItem("Tools/Musical Fountains/Polish Prototype Layout")]
    public static void PolishScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before editing");
        var stage = UnityEngine.Object.FindAnyObjectByType<FountainStage>();
        if (!stage) throw new InvalidOperationException("Open the fountain scene first");
        foreach (var key in stage.keys)
        {
            if (key.cap.name != "Press cap") continue;
            FountainJet linkedJet = key.jet;
            PrefabUtility.UnpackPrefabInstance(key.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var oldCap = key.cap;
            var label = oldCap.GetComponentInChildren<TextMesh>();
            var pivot = new GameObject("Press cap pivot"); pivot.transform.SetParent(key.transform, false); pivot.transform.localPosition = oldCap.localPosition;
            oldCap.SetParent(pivot.transform, false); oldCap.localPosition = Vector3.zero;
            if (label) { label.transform.SetParent(pivot.transform, false); label.transform.localPosition = new Vector3(0, 0.020f, 0); label.transform.localEulerAngles = new Vector3(90, 0, 0); label.transform.localScale = Vector3.one; label.characterSize = 0.008f; }
            key.cap = pivot.transform;
            foreach (var text in key.GetComponentsInChildren<TextMesh>()) if (text != label) text.characterSize = 0.0072f;
            key.jet = null;
            PrefabUtility.SaveAsPrefabAssetAndConnect(key.gameObject, Root + "/Prefabs/Key_" + key.noteName + ".prefab", InteractionMode.AutomatedAction);
            key.jet = linkedJet; PrefabUtility.RecordPrefabInstancePropertyModifications(key);
        }
        foreach (var text in stage.GetComponentsInChildren<TextMesh>())
        {
            if (text.text.StartsWith("A Q U A")) text.characterSize = 0.030f;
            if (text.text.StartsWith("Курок")) text.characterSize = 0.0118f;
        }
        foreach (var jet in stage.GetComponentsInChildren<FountainJet>())
        {
            jet.droplets.transform.localEulerAngles = new Vector3(-85, 0, 0);
            var main = jet.droplets.main; main.startLifetime = 2 * Mathf.Sqrt(2 * 9.81f * jet.height) / 9.81f;
        }
        var controls = UnityEngine.Object.FindAnyObjectByType<FountainControls>();
        var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube); var cubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(tempCube);
        foreach (var tip in new[] { controls.leftTip, controls.rightTip }) { tip.GetComponent<MeshFilter>().sharedMesh = cubeMesh; tip.localScale = Vector3.one * 0.022f; }
        foreach (string name in new[] { "BloomCube", "VisualScripting SceneVariables", "Global Volume" })
        {
            var go = GameObject.Find(name); if (go) Undo.DestroyObjectImmediate(go);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = stage.gameObject;
        if (SceneView.lastActiveSceneView) { SceneView.lastActiveSceneView.drawGizmos = false; SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0.85f, 2.1f), Quaternion.Euler(24, -15, 0), 2.5f); }
        VerifyScene();
    }

    [MenuItem("Tools/Musical Fountains/Make Fountain Circular")]
    public static void MakeCircular()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before editing");
        var stage = UnityEngine.Object.FindAnyObjectByType<FountainStage>();
        if (!stage) throw new InvalidOperationException("Open the fountain scene first");
        var pool = stage.transform.Find("Basin");
        if (PrefabUtility.IsPartOfPrefabInstance(pool.gameObject)) PrefabUtility.UnpackPrefabInstance(pool.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        for (int i = pool.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(pool.GetChild(i).gameObject);
        cylinder = CreateCylinder(32);
        dark = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Basalt.mat");
        rim = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Ceramic.mat");
        water = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/WaterSurface.mat");
        steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/BrushedMetal.mat");
        const float centerZ = 0.15f;
        Cylinder("Round basin", pool, new Vector3(0, 0.09f, centerZ), new Vector3(2.24f, 0.18f, 2.24f), dark);
        Cylinder("Round water surface", pool, new Vector3(0, 0.185f, centerZ), new Vector3(2.05f, 0.018f, 2.05f), water);
        MeshObject("Circular ceramic rim", pool, new Vector3(0, 0.215f, centerZ), RingMesh("CircularRim32", 1.025f, 1.13f, 0.07f, -180, 360, 32), rim);
        for (int i = 0; i < stage.keys.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / 8;
            var jet = stage.keys[i].jet;
            var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            jet.transform.localPosition = direction * 0.65f + new Vector3(0, 0.255f, centerZ);
            jet.transform.localRotation = Quaternion.LookRotation(-direction, Vector3.up);
            var accent = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Note_" + Pitches[i] + ".mat");
            Cylinder("Rim light " + Pitches[i], pool, direction * 1.08f + new Vector3(0, 0.254f, centerZ), new Vector3(0.04f, 0.006f, 0.04f), accent);
            float keyAngle = Mathf.Lerp(-56, 56, i / 7f);
            float a = keyAngle * Mathf.Deg2Rad;
            var key = stage.keys[i];
            key.transform.localPosition = new Vector3(Mathf.Sin(a) * 1.33f, 0.895f, -Mathf.Cos(a) * 1.33f + centerZ);
            key.transform.localRotation = Quaternion.Euler(0, -keyAngle, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(key.transform);
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(pool.gameObject, Root + "/Prefabs/Basin.prefab", InteractionMode.AutomatedAction);
        var desk = stage.transform.Find("Keyboard console");
        foreach (string name in new[] { "Console top", "Curved console top", "Left support", "Right support" })
        {
            var child = desk.Find(name); if (child) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        MeshObject("Curved console top", desk, new Vector3(0, 0.82f, centerZ), RingMesh("KeyboardArc32", 1.16f, 1.51f, 0.09f, -65, 130, 32), dark);
        foreach (int side in new[] { -1, 1 })
        {
            float a = side * 49 * Mathf.Deg2Rad;
            Shape(side < 0 ? "Left support" : "Right support", PrimitiveType.Cube, desk, new Vector3(Mathf.Sin(a) * 1.33f, 0.4f, -Mathf.Cos(a) * 1.33f + centerZ), new Vector3(0.07f, 0.8f, 0.18f), steel);
        }
        var controls = UnityEngine.Object.FindAnyObjectByType<FountainControls>();
        if (controls.instructionText)
        {
            controls.instructionText.transform.SetParent(stage.transform, false);
            controls.instructionText.transform.localPosition = new Vector3(0, 1.62f, 0.62f);
            controls.instructionText.transform.localEulerAngles = Vector3.zero;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = pool.gameObject;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 0.8f, 2.15f), Quaternion.Euler(28, -20, 0), 2.8f);
        VerifyScene();
        Debug.Log("[MusicalFountains] Circular basin, eight radial jets and curved keyboard saved.");
    }

    static GameObject MeshObject(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    static Mesh RingMesh(string name, float inner, float outer, float height, float start, float span, int segments)
    {
        string path = Root + "/Models/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh) return mesh;
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        Action<Vector3, Vector3, Vector3, Vector3> quad = (a, b, c, d) =>
        {
            int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d }); triangles.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
        };
        Vector3 up = Vector3.up * height * 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float a = (start + span * i / segments) * Mathf.Deg2Rad, b = (start + span * (i + 1) / segments) * Mathf.Deg2Rad;
            Vector3 da = new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)), db = new Vector3(Mathf.Sin(b), 0, -Mathf.Cos(b));
            quad(da * inner + up, da * outer + up, db * outer + up, db * inner + up);
            quad(da * outer - up, db * outer - up, db * outer + up, da * outer + up);
            quad(db * inner - up, da * inner - up, da * inner + up, db * inner + up);
            quad(db * inner - up, db * outer - up, da * outer - up, da * inner - up);
            if (Mathf.Abs(span) < 359 && i == 0) quad(da * inner - up, da * outer - up, da * outer + up, da * inner + up);
            if (Mathf.Abs(span) < 359 && i == segments - 1) quad(db * outer - up, db * inner - up, db * inner + up, db * outer + up);
        }
        mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
}
