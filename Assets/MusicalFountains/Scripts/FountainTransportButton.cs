using UnityEngine;

namespace MusicalFountains
{
    public enum FountainTransportAction { Record, Stop, Play }

    public sealed class FountainTransportButton : FountainButton
    {
        public FountainRecorder recorder;
        public FountainTransportAction action;
        public Transform cap;
        public Renderer capRenderer;
        public Color color = Color.white;
        uint owners, hoverOwners;
        Vector3 restingPosition;
        MaterialPropertyBlock block;
        bool initialized;

        void Awake()
        {
            initialized = true;
            block = new MaterialPropertyBlock();
            if (cap) restingPosition = cap.localPosition;
        }

        public override void SetPressed(int owner, bool pressed)
        {
            if (owner < 0 || owner > 31) return;
            bool before = owners != 0;
            uint bit = 1u << owner;
            owners = pressed ? owners | bit : owners & ~bit;
            if (before || owners == 0 || !recorder) return;
            switch (action)
            {
                case FountainTransportAction.Record: recorder.StartRecording(); break;
                case FountainTransportAction.Stop: recorder.Stop(); break;
                case FountainTransportAction.Play: recorder.StartPlayback(); break;
            }
        }

        public override void SetHovered(int owner, bool hovered)
        {
            uint bit = 1u << Mathf.Clamp(owner, 0, 31);
            hoverOwners = hovered ? hoverOwners | bit : hoverOwners & ~bit;
        }

        void Update()
        {
            bool active = recorder && (action == FountainTransportAction.Record ? recorder.IsRecording : action == FountainTransportAction.Play ? recorder.IsPlaying : false);
            bool available = recorder && (action != FountainTransportAction.Play || recorder.HasRecording || recorder.IsRecording);
            float light = active ? 1 : !available ? 0.2f : owners != 0 || hoverOwners != 0 ? 0.9f : 0.55f;
            if (cap) cap.localPosition = Vector3.Lerp(cap.localPosition, restingPosition + Vector3.down * (owners != 0 ? 0.01f : 0), Time.deltaTime * 18);
            if (capRenderer)
            {
                Color tint = color * light; tint.a = 1;
                block.SetColor("_BaseColor", tint); capRenderer.SetPropertyBlock(block);
            }
        }

        void OnDisable()
        {
            owners = hoverOwners = 0;
            if (initialized && cap) cap.localPosition = restingPosition;
        }
    }
}
