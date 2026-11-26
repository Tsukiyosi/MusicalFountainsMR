using UnityEngine;

namespace MusicalFountains
{
    public sealed class FountainJet : MonoBehaviour
    {
        public LineRenderer stream;
        public LineRenderer ripple;
        public ParticleSystem droplets;
        [Min(0.1f)] public float height = 1.15f;
        [Min(0.05f)] public float reach = 0.4f;
        [Range(0.005f, 0.06f)] public float streamWidth = 0.018f;
        [Range(0, 160)] public int dropsPerSecond = 65;
        public bool IsFlowing => target > 0;
        public float Strength => strength;
        float target, strength;
        readonly Vector3[] points = new Vector3[20];

        void Awake()
        {
            if (stream) stream.enabled = false;
            if (ripple) ripple.enabled = false;
            if (droplets)
            {
                var emission = droplets.emission;
                emission.enabled = false;
                droplets.Play();
            }
        }

        public void SetFlow(bool flowing) { target = flowing ? 1 : 0; }

        void Update()
        {
            strength = Mathf.MoveTowards(strength, target, Time.deltaTime * (target > strength ? 8 : 4));
            if (stream)
            {
                stream.enabled = strength > 0.01f;
                for (int i = 0; i < points.Length; i++)
                {
                    float t = i / (float)(points.Length - 1);
                    points[i] = new Vector3(Mathf.Sin(t * Mathf.PI) * Mathf.Sin(Time.time * 2 + transform.localPosition.x) * 0.016f,
                        4 * height * strength * t * (1 - t), reach * t);
                }
                stream.positionCount = points.Length;
                stream.SetPositions(points);
                stream.widthMultiplier = streamWidth * Mathf.Sqrt(strength);
            }
            if (droplets)
            {
                var emission = droplets.emission;
                emission.enabled = target > 0 && strength > 0.05f;
                emission.rateOverTime = dropsPerSecond * strength;
            }
            if (ripple)
            {
                ripple.enabled = strength > 0.02f;
                float size = 0.7f + Mathf.Repeat(Time.time * 0.9f, 0.5f);
                ripple.transform.localScale = Vector3.one * size;
                ripple.widthMultiplier = 0.008f * strength;
            }
        }

        void OnDisable()
        {
            target = strength = 0;
            if (droplets) droplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
