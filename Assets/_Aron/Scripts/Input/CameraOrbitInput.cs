// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.CameraRig;
using Aron.Logic.Safety;
using Aron.Player;
using Aron.Save;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace Aron.Input
{
    /// <summary>
    /// Turns drags into camera orbit and two-finger pinches into zoom. Rules that keep it from fighting the hero controls:
    /// a finger only orbits if it started in the right half of the screen (left half when left-handed), away from the hero and not on
    /// a button, and has moved past a small slop (so a quick tap is still an attack). Off in one-hand mode. On a desktop: right mouse drag, wheel zoom.
    /// </summary>
    public static class CameraOrbitInput
    {
        private const float SlopInches = 0.12f, GrabInches = 0.55f;

        private struct Finger { public int id; public Vector2 start, pos, prev; public bool orbiting, valid; }
        private static readonly Finger[] fingers = new Finger[2];
        private static float lastPinch;

        public static bool Disabled => !OneHandMode.OrbitAllowed(SaveSystem.Data.access);

        public static void Poll(OrbitState orbit, CameraSettings s, CameraFile f, CameraModeDef mode)
        {
            if (mode == null || !mode.orbit || Disabled) { orbit.EndDrag(); fingers[0].valid = fingers[1].valid = false; return; }
            float dpi = Screen.dpi > 40f ? Screen.dpi : 160f;
            bool left = SaveSystem.Data.access != null && SaveSystem.Data.access.leftHanded;
            int active = 0; bool anyOrbit = false; Vector2 sum = Vector2.zero;
            Vector2 a = default, b = default; bool pinch = false;

#if ENABLE_INPUT_SYSTEM
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
            var touches = ETouch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.phase == ETouchPhase.Began) Begin(t.touchId, t.screenPosition, left);
                Move(t.touchId, t.screenPosition, dpi, ref anyOrbit, ref sum, ref active);
                if (t.phase == ETouchPhase.Ended || t.phase == ETouchPhase.Canceled) End(t.touchId);
            }
#else
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                var t = UnityEngine.Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) Begin(t.fingerId, t.position, left);
                Move(t.fingerId, t.position, dpi, ref anyOrbit, ref sum, ref active);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) End(t.fingerId);
            }
#endif
            if (fingers[0].valid && fingers[1].valid) { pinch = true; a = fingers[0].pos; b = fingers[1].pos; }
            if (pinch)
            {
                float d = Vector2.Distance(a, b);
                if (lastPinch > 0f && OneHandMode.PinchAllowed(SaveSystem.Data.access)) orbit.Pinch(lastPinch, d, mode);
                lastPinch = d;
            }
            else
            {
                lastPinch = 0f;
                if (anyOrbit && active == 1) orbit.Drag(sum.x, sum.y, dpi, s, f, mode);
                else orbit.EndDrag();
            }
            Desktop(orbit, s, f, mode, dpi);
        }

        private static void Begin(int id, Vector2 pos, bool left)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id)) return;
            if (!OrbitState.IsOrbitTouch(pos.x / Mathf.Max(1f, Screen.width), left)) return;
            if (OnHero(pos)) return;
            for (int i = 0; i < fingers.Length; i++)
                if (!fingers[i].valid) { fingers[i] = new Finger { id = id, start = pos, pos = pos, prev = pos, valid = true }; return; }
        }

        private static void Move(int id, Vector2 pos, float dpi, ref bool anyOrbit, ref Vector2 sum, ref int active)
        {
            for (int i = 0; i < fingers.Length; i++)
            {
                if (!fingers[i].valid || fingers[i].id != id) continue;
                fingers[i].prev = fingers[i].pos; fingers[i].pos = pos;
                if (!fingers[i].orbiting && (pos - fingers[i].start).magnitude / dpi > SlopInches) fingers[i].orbiting = true;
                active++;
                if (fingers[i].orbiting) { anyOrbit = true; sum += pos - fingers[i].prev; }
            }
        }

        private static void End(int id)
        {
            for (int i = 0; i < fingers.Length; i++) if (fingers[i].valid && fingers[i].id == id) fingers[i].valid = false;
        }

        private static bool OnHero(Vector2 screen)
        {
            var player = PlayerController.Active; var cam = Camera.main;
            if (player == null || cam == null) return false;
            Vector3 feet = cam.WorldToScreenPoint(player.transform.position);
            Vector3 head = cam.WorldToScreenPoint(player.transform.position + Vector3.up * player.BodyHeight);
            if (feet.z <= 0f) return false;
            Vector2 mid = (feet + head) * 0.5f;
            float r = Mathf.Max(Mathf.Abs(head.y - feet.y) * 0.8f, GrabInches * (Screen.dpi > 40f ? Screen.dpi : 160f) * 0.5f);
            return Vector2.Distance(mid, screen) < r;
        }

        private static void Desktop(OrbitState orbit, CameraSettings s, CameraFile f, CameraModeDef mode, float dpi)
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return;
            if (m.rightButton.isPressed) { var d = m.delta.ReadValue(); orbit.Drag(d.x, d.y, dpi, s, f, mode); }
            float wheel = m.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f) orbit.Pinch(100f, 100f * (1f + Mathf.Sign(wheel) * -0.1f), mode);
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) orbit.Recenter();
#else
            if (UnityEngine.Input.GetMouseButton(1)) orbit.Drag(UnityEngine.Input.GetAxis("Mouse X") * 10f, UnityEngine.Input.GetAxis("Mouse Y") * 10f, dpi, s, f, mode);
            float w = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(w) > 0.01f) orbit.Pinch(100f, 100f * (1f + Mathf.Sign(w) * -0.1f), mode);
            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) orbit.Recenter();
#endif
        }
    }
}
