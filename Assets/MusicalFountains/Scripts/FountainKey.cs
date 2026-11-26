using System;
using UnityEngine;

namespace MusicalFountains
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FountainKey : FountainButton
    {
        [Header("Note and fountain — editable scene references")]
        public string noteName = "C4";
        public AudioClip noteClip;
        public FountainJet jet;
        public Transform cap;
        public Renderer capRenderer;
        public Color color = Color.cyan;
        [Range(0, 0.3f)] public float volume = 0.12f;
        [Min(0.005f)] public float attackSeconds = 0.018f;
        [Min(0.02f)] public float releaseSeconds = 0.12f;
        [Range(0, 0.04f)] public float pressTravel = 0.014f;
        public bool IsPressed => owners != 0;
        public const int PlaybackOwner = 29;
        public bool IsManuallyPressed => (owners & ~(1u << PlaybackOwner)) != 0;
        public event Action<FountainKey, bool> ManualPressedChanged;
        public int PressCount { get; private set; }
        AudioSource voice;
        MaterialPropertyBlock block;
        Vector3 restingPosition;
        uint owners, hoverOwners;
        float envelope;
        bool initialized;

        void Awake()
        {
            initialized = true;
            voice = GetComponent<AudioSource>();
            voice.clip = noteClip;
            voice.loop = true;
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            voice.volume = 0;
            voice.priority = 32;
            block = new MaterialPropertyBlock();
            if (cap) restingPosition = cap.localPosition;
        }

        public override void SetPressed(int owner, bool pressed)
        {
            if (owner < 0 || owner > 31) return;
            bool wasPressed = IsPressed;
            bool wasManual = IsManuallyPressed;
            uint bit = 1u << owner;
            owners = pressed ? owners | bit : owners & ~bit;
            if (IsPressed && !wasPressed)
            {
                PressCount++;
                if (voice && !voice.isPlaying) voice.Play();
            }
            if (jet) jet.SetFlow(IsPressed);
            if (wasManual != IsManuallyPressed) ManualPressedChanged?.Invoke(this, IsManuallyPressed);
        }

        public override void SetHovered(int owner, bool hovered)
        {
            uint bit = 1u << Mathf.Clamp(owner, 0, 31);
            hoverOwners = hovered ? hoverOwners | bit : hoverOwners & ~bit;
        }

        public void ReleaseAll()
        {
            bool wasManual = IsManuallyPressed;
            owners = hoverOwners = 0;
            if (jet) jet.SetFlow(false);
            if (wasManual) ManualPressedChanged?.Invoke(this, false);
        }

        void Update()
        {
            float goal = IsPressed ? 1 : 0;
            envelope = Mathf.MoveTowards(envelope, goal, Time.deltaTime / (goal > envelope ? attackSeconds : releaseSeconds));
            if (voice)
            {
                voice.volume = envelope * volume;
                if (!IsPressed && envelope <= 0 && voice.isPlaying) voice.Stop();
            }
            if (cap) cap.localPosition = restingPosition + Vector3.down * pressTravel * envelope;
            if (capRenderer)
            {
                Color c = Color.Lerp(color * 0.45f, color, Mathf.Max(envelope, hoverOwners != 0 ? 0.55f : 0));
                c.a = 1;
                block.SetColor("_BaseColor", c);
                capRenderer.SetPropertyBlock(block);
            }
        }

        void OnDisable()
        {
            if (!initialized) return;
            ReleaseAll();
            envelope = 0;
            if (voice) { voice.Stop(); voice.volume = 0; }
            if (cap) cap.localPosition = restingPosition;
        }
    }
}
