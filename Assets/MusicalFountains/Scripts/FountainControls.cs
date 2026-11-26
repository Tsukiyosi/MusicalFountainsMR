using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace MusicalFountains
{
    public sealed class FountainControls : MonoBehaviour
    {
        public FountainStage stage;
        public Camera playerCamera;
        public Transform trackingSpace;
        public InputActionAsset controllerActions;
        public TextMesh instructionText;
        public Transform leftTip, rightTip;
        public LineRenderer leftRay, rightRay;
        [Range(0.01f, 0.08f)] public float touchRadius = 0.027f;
        [Range(1, 12)] public float rayDistance = 6;
        public bool desktopPreview = true;
        readonly Collider[] contacts = new Collider[12];
        readonly Key[] pianoKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
        readonly FountainButton[] rayKeys = new FountainButton[2];
        readonly FountainButton[] touchKeys = new FountainButton[2];
        InputActionMap map;
        InputAction[] positions, rotations, tracked, triggers;
        InputAction recenter;
        FountainButton mouseKey;

        void OnEnable()
        {
            if (!controllerActions) return;
            map = controllerActions.FindActionMap("Fountains", true);
            positions = new[] { map.FindAction("LeftPosition"), map.FindAction("RightPosition") };
            rotations = new[] { map.FindAction("LeftRotation"), map.FindAction("RightRotation") };
            tracked = new[] { map.FindAction("LeftTracked"), map.FindAction("RightTracked") };
            triggers = new[] { map.FindAction("LeftTrigger"), map.FindAction("RightTrigger") };
            recenter = map.FindAction("Recenter");
            map.Enable();
        }

        void Start()
        {
            if (Application.platform != RuntimePlatform.Android && desktopPreview && playerCamera)
            {
                var pose = playerCamera.GetComponent<TrackedPoseDriver>();
                if (pose) pose.enabled = false;
                var arCamera = playerCamera.GetComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();
                if (arCamera) arCamera.enabled = false;
                playerCamera.transform.SetPositionAndRotation(new Vector3(0, 1.65f, -0.4f), Quaternion.Euler(13, 0, 0));
                playerCamera.clearFlags = CameraClearFlags.SolidColor;
                playerCamera.backgroundColor = new Color(0.055f, 0.065f, 0.085f);
                if (instructionText) instructionText.text = "1–8 / мышь — ноты   ·   WASD — подойти   ·   R — переставить";
            }
        }

        void Update()
        {
            if (!stage || !playerCamera) return;
            UpdateHand(0, leftTip, leftRay, XRNode.LeftHand);
            UpdateHand(1, rightTip, rightRay, XRNode.RightHand);
            if (recenter != null && recenter.WasPressedThisFrame()) stage.PlaceInFront();
            if (Application.platform != RuntimePlatform.Android && desktopPreview) UpdateDesktop();
        }

        void UpdateHand(int hand, Transform tip, LineRenderer ray, XRNode node)
        {
            bool valid = map != null && tracked[hand].ReadValue<float>() > 0.5f;
            if (tip) tip.gameObject.SetActive(valid);
            if (ray) ray.enabled = valid;
            if (!valid)
            {
                ChangeKey(ref rayKeys[hand], null, hand, false, true);
                ChangeKey(ref touchKeys[hand], null, hand + 2, false, false);
                return;
            }
            Vector3 localPosition = positions[hand].ReadValue<Vector3>();
            Quaternion localRotation = rotations[hand].ReadValue<Quaternion>();
            if (localRotation.x == 0 && localRotation.y == 0 && localRotation.z == 0 && localRotation.w == 0) localRotation = Quaternion.identity;
            Vector3 origin = trackingSpace.TransformPoint(localPosition);
            Quaternion rotation = trackingSpace.rotation * localRotation;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 tipPosition = origin + forward * 0.09f;
            if (tip) tip.SetPositionAndRotation(tipPosition, rotation);
            var hitKey = Cast(new Ray(origin, forward), out Vector3 end);
            bool pressed = triggers[hand].ReadValue<float>() > 0.55f;
            bool newPress = pressed && hitKey && rayKeys[hand] != hitKey;
            ChangeKey(ref rayKeys[hand], hitKey, hand, pressed, true);
            if (ray) { ray.SetPosition(0, origin); ray.SetPosition(1, end); }
            FountainButton touched = null;
            int count = Physics.OverlapSphereNonAlloc(tipPosition, touchRadius, contacts, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                touched = contacts[i].GetComponentInParent<FountainButton>();
                if (touched) break;
            }
            bool newTouch = touched && touchKeys[hand] != touched;
            ChangeKey(ref touchKeys[hand], touched, hand + 2, touched, false);
            if (newPress || newTouch) InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, 0.2f, 0.025f);
        }

        FountainButton Cast(Ray ray, out Vector3 end)
        {
            end = ray.GetPoint(rayDistance);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Collide))
            {
                end = hit.point;
                return hit.collider.GetComponentInParent<FountainButton>();
            }
            return null;
        }

        static void ChangeKey(ref FountainButton current, FountainButton next, int owner, bool pressed, bool hover)
        {
            if (current && current != next)
            {
                current.SetPressed(owner, false);
                if (hover) current.SetHovered(owner, false);
            }
            current = next;
            if (!current) return;
            current.SetPressed(owner, pressed);
            if (hover) current.SetHovered(owner, true);
        }

        void UpdateDesktop()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < stage.keys.Length && i < pianoKeys.Length; i++)
                    stage.keys[i].SetPressed(4, keyboard[pianoKeys[i]].isPressed);
                if (keyboard.rKey.wasPressedThisFrame) stage.PlaceInFront();
                Vector3 move = new Vector3((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    0, (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                playerCamera.transform.position += Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0) * move * (Time.deltaTime * 1.4f);
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * 0.12f;
                Vector3 euler = playerCamera.transform.eulerAngles;
                float pitch = euler.x > 180 ? euler.x - 360 : euler.x;
                playerCamera.transform.rotation = Quaternion.Euler(Mathf.Clamp(pitch - delta.y, -65, 65), euler.y + delta.x, 0);
            }
            FountainButton key = Cast(playerCamera.ScreenPointToRay(mouse.position.ReadValue()), out _);
            ChangeKey(ref mouseKey, key, 5, mouse.leftButton.isPressed, true);
        }

        void OnDisable()
        {
            if (stage) stage.ReleaseAll();
            map?.Disable();
            mouseKey = null;
            for (int i = 0; i < 2; i++) { rayKeys[i] = null; touchKeys[i] = null; }
        }
    }
}
