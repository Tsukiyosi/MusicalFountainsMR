using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MusicalFountains
{
    [Serializable]
    public sealed class FountainNoteEvent
    {
        public float time;
        public int note;
        public bool pressed;
        public FountainNoteEvent(float time, int note, bool pressed) { this.time = time; this.note = note; this.pressed = pressed; }
    }

    [Serializable]
    public sealed class FountainPerformance
    {
        public int version = 1;
        public float duration;
        public List<FountainNoteEvent> events = new List<FountainNoteEvent>();
    }

    public sealed class FountainRecorder : MonoBehaviour
    {
        public FountainKey[] keys;
        public TextMesh statusLabel;
        [Range(1, 30)] public float maxRecordSeconds = 30;
        public bool saveLastRecording = true;
        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool HasRecording => lastRecording != null && lastRecording.events.Count > 0;
        public FountainPerformance LastRecording => lastRecording;
        public float RecordingTime => IsRecording ? Mathf.Clamp((float)(Now - recordingStarted), 0, Limit) : 0;
        public float PlaybackTime => IsPlaying ? Mathf.Clamp((float)(Now - playbackStarted), 0, lastRecording.duration) : 0;
        public string StoragePath
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath(Path.Combine(Application.dataPath, "../UserSettings/LastFountainRecording.json"));
#else
                return Path.Combine(Application.persistentDataPath, "LastFountainRecording.json");
#endif
            }
        }

        static double Now => Time.unscaledTimeAsDouble;
        float Limit => Mathf.Clamp(maxRecordSeconds, 1, 30);
        FountainPerformance lastRecording, working;
        bool[] recordedHeld;
        double recordingStarted, playbackStarted, nextStatus;
        int playbackIndex;
        string notice;
        bool initialized;

        void Awake()
        {
            initialized = true;
            if (saveLastRecording) LoadLastRecording();
            UpdateStatus();
        }

        void OnEnable()
        {
            if (keys == null) return;
            foreach (var key in keys) if (key) key.ManualPressedChanged += OnManualNote;
        }

        public void StartRecording()
        {
            if (IsRecording || keys == null || keys.Length == 0) return;
            StopPlayback();
            working = new FountainPerformance();
            recordedHeld = new bool[keys.Length];
            recordingStarted = Now;
            IsRecording = true;
            notice = null;
            for (int i = 0; i < keys.Length; i++)
                if (keys[i] && keys[i].IsManuallyPressed) AddEvent(i, true, 0);
            UpdateStatus();
        }

        void OnManualNote(FountainKey key, bool pressed)
        {
            if (!IsRecording) return;
            float elapsed = (float)(Now - recordingStarted);
            if (elapsed >= Limit) { FinishRecording(Limit); return; }
            int note = Array.IndexOf(keys, key);
            if (note >= 0) AddEvent(note, pressed, Mathf.Max(0, elapsed));
        }

        void AddEvent(int note, bool pressed, float at)
        {
            if (recordedHeld[note] == pressed) return;
            working.events.Add(new FountainNoteEvent(at, note, pressed));
            recordedHeld[note] = pressed;
        }

        public void Stop()
        {
            if (IsRecording) FinishRecording(Mathf.Clamp((float)(Now - recordingStarted), 0, Limit));
            StopPlayback();
            UpdateStatus();
        }

        void FinishRecording(float duration)
        {
            if (!IsRecording) return;
            IsRecording = false;
            working.duration = duration;
            for (int i = 0; i < recordedHeld.Length; i++) if (recordedHeld[i]) AddEvent(i, false, duration);
            if (working.events.Count > 0)
            {
                lastRecording = working;
                notice = null;
                if (saveLastRecording) SaveLastRecording();
            }
            else notice = HasRecording ? "Нет новых нот · предыдущая запись сохранена" : "Нет нот · попробуйте ещё раз";
            working = null;
            UpdateStatus();
        }

        public void StartPlayback()
        {
            if (IsRecording) FinishRecording(Mathf.Clamp((float)(Now - recordingStarted), 0, Limit));
            StopPlayback();
            if (!HasRecording) { notice = "Сначала запишите ноты"; UpdateStatus(); return; }
            notice = null;
            playbackStarted = Now;
            playbackIndex = 0;
            IsPlaying = true;
            AdvancePlayback(0);
            UpdateStatus();
        }

        void StopPlayback()
        {
            IsPlaying = false;
            if (keys != null) foreach (var key in keys) if (key) key.SetPressed(FountainKey.PlaybackOwner, false);
        }

        void AdvancePlayback(float elapsed)
        {
            while (playbackIndex < lastRecording.events.Count && lastRecording.events[playbackIndex].time <= elapsed)
            {
                var step = lastRecording.events[playbackIndex++];
                if (keys[step.note]) keys[step.note].SetPressed(FountainKey.PlaybackOwner, step.pressed);
            }
            if (elapsed >= lastRecording.duration) StopPlayback();
        }

        void Update()
        {
            if (IsRecording && Now - recordingStarted >= Limit) FinishRecording(Limit);
            if (IsPlaying) AdvancePlayback((float)(Now - playbackStarted));
            if (Now >= nextStatus) { nextStatus = Now + 0.1; UpdateStatus(); }
        }

        void UpdateStatus()
        {
            if (!statusLabel) return;
            if (IsRecording)
            {
                statusLabel.text = "ЗАПИСЬ  " + RecordingTime.ToString("00.0") + " / " + Limit.ToString("0") + " с";
                statusLabel.color = new Color(1, 0.4f, 0.42f);
            }
            else if (IsPlaying)
            {
                statusLabel.text = "ПОВТОР  " + PlaybackTime.ToString("00.0") + " / " + lastRecording.duration.ToString("00.0") + " с";
                statusLabel.color = new Color(0.35f, 0.9f, 0.85f);
            }
            else
            {
                statusLabel.text = notice ?? (HasRecording ? "Последняя запись: " + lastRecording.duration.ToString("00.0") + " с" : "Запишите свою мелодию · до 30 секунд");
                statusLabel.color = new Color(0.75f, 0.83f, 0.87f);
            }
        }

        public void SaveLastRecording()
        {
            if (!HasRecording) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                File.WriteAllText(StoragePath, JsonUtility.ToJson(lastRecording));
            }
            catch (Exception e) { Debug.LogWarning("[FountainRecorder] Could not save recording: " + e.Message); }
        }

        public void LoadLastRecording()
        {
            try
            {
                if (!File.Exists(StoragePath)) return;
                var clip = JsonUtility.FromJson<FountainPerformance>(File.ReadAllText(StoragePath));
                if (!IsValid(clip)) { Debug.LogWarning("[FountainRecorder] Ignoring an invalid recording"); return; }
                lastRecording = clip;
            }
            catch (Exception e) { Debug.LogWarning("[FountainRecorder] Could not load recording: " + e.Message); }
        }

        bool IsValid(FountainPerformance clip)
        {
            if (clip == null || clip.version != 1 || clip.events == null || clip.events.Count == 0 || clip.events.Count > 50000 ||
                float.IsNaN(clip.duration) || float.IsInfinity(clip.duration) || clip.duration < 0 || clip.duration > 30 || keys == null) return false;
            float previous = 0;
            foreach (var e in clip.events)
            {
                if (e == null || e.note < 0 || e.note >= keys.Length || !keys[e.note] || float.IsNaN(e.time) || float.IsInfinity(e.time) || e.time < previous || e.time > clip.duration) return false;
                previous = e.time;
            }
            return true;
        }

        void OnApplicationFocus(bool focus) { if (!focus && initialized) Stop(); }
        void OnApplicationPause(bool paused) { if (paused && initialized) Stop(); }
        void OnDisable()
        {
            if (initialized) Stop();
            if (keys != null) foreach (var key in keys) if (key) key.ManualPressedChanged -= OnManualNote;
        }
    }
}
