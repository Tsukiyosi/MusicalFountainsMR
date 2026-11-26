using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace MusicalFountains
{
    public sealed class FountainStage : MonoBehaviour
    {
        public FountainKey[] keys;
        public Camera playerCamera;
        [Tooltip("Disable to keep exactly the position designed in the scene.")]
        public bool placeInFrontOnStart = true;
        [Range(1.3f, 4)] public float distanceFromPlayer = 2.1f;

        IEnumerator Start()
        {
            if (!placeInFrontOnStart || !playerCamera) yield break;
            if (Application.platform == RuntimePlatform.Android)
            {
                float end = Time.realtimeSinceStartup + 5;
                while (Time.realtimeSinceStartup < end)
                {
                    var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                    if (head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked) break;
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
            PlaceInFront();
        }

        public void PlaceInFront()
        {
            if (!playerCamera) return;
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            Vector3 position = playerCamera.transform.position + forward * distanceFromPlayer;
            position.y = Application.platform == RuntimePlatform.Android && playerCamera.transform.parent
                ? playerCamera.transform.parent.position.y : 0;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }

        public void ReleaseAll()
        {
            if (keys == null) return;
            foreach (var key in keys) if (key) key.ReleaseAll();
        }

        void OnApplicationFocus(bool focused) { if (!focused) ReleaseAll(); }
        void OnApplicationPause(bool paused) { if (paused) ReleaseAll(); }
        void OnDisable() { ReleaseAll(); }
    }
}
