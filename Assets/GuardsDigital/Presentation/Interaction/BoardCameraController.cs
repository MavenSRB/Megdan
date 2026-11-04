using System;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>Local board navigation only. The spotlight may temporarily borrow the same camera.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class BoardCameraController : MonoBehaviour
    {
        public const float MinimumPitch = 20f;
        private Camera view;
        private Func<Vector2, bool> blocksPointer;
        private Func<bool> suspended;
        private VisualElement hud;
        private Vector3 homePosition, pivot;
        private Quaternion homeRotation;
        private float homeSize, homeFar, pitch = 90f, yaw, size, distance;
        private Bounds boardBounds;
        private object mapIdentity;
        private bool hasHome, rotating, focused = true;
        public bool HasManualPose { get; private set; }
        public float Elevation => HasManualPose ? pitch : 90f;

        public void Configure(Func<Vector2, bool> pointerBlocker, Func<bool> isSuspended, VisualElement hudRoot)
        {
            view = GetComponent<Camera>();
            blocksPointer = pointerBlocker;
            suspended = isSuspended;
            hud = hudRoot;
            if (!hasHome) AcceptHomeFrame(null, new Bounds(new Vector3(view.transform.position.x, 0, view.transform.position.z), Vector3.one * 80f));
        }

        /// <summary>Map framing remains the owner of the whole-board home, including HUD safe-area fitting.</summary>
        public void AcceptHomeFrame(object identity, Bounds bounds)
        {
            view ??= GetComponent<Camera>();
            bool newMap = mapIdentity != null && !ReferenceEquals(mapIdentity, identity);
            mapIdentity = identity;
            boardBounds = bounds;
            homePosition = view.transform.position;
            homeRotation = view.transform.rotation;
            homeSize = view.orthographicSize;
            homeFar = view.farClipPlane;
            hasHome = true;
            if (newMap) HasManualPose = false;
            if (HasManualPose) ReapplyView(); else ReadHome();
        }

        private void ReadHome()
        {
            pivot = new Vector3(homePosition.x, 0f, homePosition.z);
            pitch = 90f;
            var right = homeRotation * Vector3.right;
            yaw = Mathf.Atan2(-right.z, right.x) * Mathf.Rad2Deg;
            distance = Mathf.Max(20f, homePosition.y);
            size = homeSize;
        }

        public void ResetView()
        {
            if (!hasHome || suspended?.Invoke() == true) return;
            HasManualPose = false;
            rotating = false;
            view.transform.SetPositionAndRotation(homePosition, homeRotation);
            view.orthographicSize = homeSize;
            view.farClipPlane = homeFar;
            ReadHome();
        }

        public void Navigate(Vector2 pan, Vector2 orbitDegrees, float zoomSteps, float deltaTime, bool fast = false)
        {
            if (!hasHome || suspended?.Invoke() == true || !focused) return;
            if (pan == Vector2.zero && orbitDegrees == Vector2.zero && Mathf.Approximately(zoomSteps, 0f)) return;
            HasManualPose = true;
            yaw += orbitDegrees.x;
            pitch = Mathf.Clamp(pitch - orbitDegrees.y, MinimumPitch, 90f);
            size = Mathf.Clamp(size * Mathf.Pow(0.88f, zoomSteps), homeSize * 0.12f, homeSize * 1.7f);
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var movement = rotation * new Vector3(pan.x, 0f, pan.y);
            pivot += Vector3.ClampMagnitude(movement, 1f) * (size * 0.9f * Mathf.Max(0f, deltaTime) * (fast ? 2.4f : 1f));
            pivot.x = Mathf.Clamp(pivot.x, boardBounds.min.x - 2f, boardBounds.max.x + 2f);
            pivot.z = Mathf.Clamp(pivot.z, boardBounds.min.z - 2f, boardBounds.max.z + 2f);
            ReapplyView();
        }

        public void ReapplyView()
        {
            if (!hasHome || !HasManualPose) return;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            view.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
            view.orthographicSize = size;
            view.farClipPlane = Mathf.Max(homeFar, 300f);
        }

        private bool TextEntryFocused()
        {
            for (var element = hud?.focusController?.focusedElement as VisualElement; element != null; element = element.parent)
                if (element is TextField) return true;
            return false;
        }

        private void Update()
        {
            if (!focused || !hasHome || suspended?.Invoke() == true || TextEntryFocused())
            { rotating = false; return; }
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard?.homeKey.wasPressedThisFrame == true) { ResetView(); return; }
            var pan = keyboard == null ? Vector2.zero : new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            var orbit = Vector2.zero;
            float zoom = 0f;
            if (mouse != null)
            {
                bool overHud = blocksPointer?.Invoke(mouse.position.ReadValue()) == true;
                if (mouse.middleButton.wasPressedThisFrame && !overHud) rotating = true;
                if (!mouse.middleButton.isPressed) rotating = false;
                if (rotating) orbit = mouse.delta.ReadValue() * 0.2f;
                if (!overHud) zoom = mouse.scroll.ReadValue().y / 60f;
            }
            Navigate(pan, orbit, zoom, Time.unscaledDeltaTime,
                keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed));
        }

        private void OnApplicationFocus(bool value) { focused = value; if (!value) rotating = false; }
        private void OnDisable() => rotating = false;
    }
}
