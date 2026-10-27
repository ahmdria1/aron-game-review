// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.CameraRig
{
    /// <summary>Camera choices the player can make, saved with the game.</summary>
    [Serializable]
    public class CameraSettings
    {
        /// <summary>Screen-shake strength, 0 (none) to 100 (full).</summary>
        public int shake = 100;
        /// <summary>Dragging up looks up when false (the default), down when true.</summary>
        public bool invertY;
        /// <summary>Orbit speed multiplier, 0.5..2.</summary>
        public float orbitSpeed = 1f;
        public bool autoRecenter = true;

        public const int ReduceMotionShakeCap = 25;

        /// <summary>0..1 multiplier for shake. Reduce-motion caps it at a quarter.</summary>
        public float ShakeScale(bool reduceMotion)
        {
            int s = Math.Max(0, Math.Min(100, shake));
            if (reduceMotion) s = Math.Min(s, ReduceMotionShakeCap);
            return s / 100f;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (shake < 0 || shake > 100) e.Add("shake 0..100");
            if (orbitSpeed < 0.5f || orbitSpeed > 2f) e.Add("orbit speed 0.5..2");
            return e;
        }
    }

    /// <summary>How the camera behaves in one mode. Angles in degrees, distances in metres.</summary>
    [Serializable]
    public class CameraModeDef
    {
        /// <summary>free (third person you can orbit), chase (fixed behind the hero, Crash-style), race, boss.</summary>
        public string id;
        public float distance = 7f, height = 4.2f, lookAhead = 3f, lateralFollow = 0.6f, smoothTime = 0.15f;
        public float fov = 60f;
        /// <summary>Drag is allowed to turn the camera (free only).</summary>
        public bool orbit, zoom;
        /// <summary>Orbit limits around the default view.</summary>
        public float maxYaw = 150f, pitchDown = 15f, pitchUp = 35f;
        public float zoomMin = 0.6f, zoomMax = 1.5f;
        /// <summary>Race: extra field of view at full speed and how far the camera backs off.</summary>
        public float fovSpeedBoost, distanceSpeedBoost;
        /// <summary>Boss arena: the most the framing may pull back to keep both fighters in view, and how much of the gap it uses.</summary>
        public float frameExtra, frameGapFactor;

        /// <summary>The default look-down angle from the height and distance.</summary>
        public float BasePitch => (float)(Math.Atan2(height, distance) * 180.0 / Math.PI);
    }

    [Serializable]
    public class CameraFile
    {
        public static readonly string[] Modes = { "free", "chase", "race", "boss", "camel_race" };
        /// <summary>The race camera and the camel race camera both widen and back off with speed (see <see cref="CameraMath.RaceFov"/>).</summary>
        public static bool IsRaceLike(string modeId) => modeId == "race" || modeId == "camel_race";
        public static readonly string[] StageTypes = { "normal", "action", "chase", "race", "boss" };

        /// <summary>No orbit input for this long and the camera swings back behind the hero.</summary>
        public float recenterSeconds = 1.5f;
        /// <summary>How long the swing back takes.</summary>
        public float recenterDuration = 0.6f;
        /// <summary>Degrees of turn per centimetre of finger drag at orbit speed 1.</summary>
        public float degreesPerCm = 55f;
        /// <summary>Gap kept to a wall, and how close the camera may ever come to the hero.</summary>
        public float wallMargin = 0.3f, minDistance = 1.4f;
        /// <summary>How fast the camera moves in toward a wall and back out (per second); in is fast, out is slow, so it never clips or jitters.</summary>
        public float wallInSpeed = 40f, wallOutSpeed = 4f;
        public CameraModeDef[] modes = Array.Empty<CameraModeDef>();
        /// <summary>"stageType=mode", e.g. "chase=chase".</summary>
        public string[] stageModes = Array.Empty<string>();

        public CameraModeDef Mode(string id)
        {
            foreach (var m in modes) if (m.id == id) return m;
            return null;
        }

        /// <summary>Mode for a stage: its own override when set and known, else by its type.</summary>
        public string ModeFor(string stageType, string overrideMode = "")
        {
            if (!string.IsNullOrEmpty(overrideMode) && Mode(overrideMode) != null) return overrideMode;
            foreach (var s in stageModes)
            {
                int eq = s.IndexOf('=');
                if (eq > 0 && s.Substring(0, eq) == stageType) return s.Substring(eq + 1);
            }
            return "free";
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            foreach (var m in Modes) if (Mode(m) == null) e.Add("missing mode " + m);
            foreach (var m in modes)
            {
                string w = "mode " + m.id;
                if (Array.IndexOf(Modes, m.id) < 0) e.Add(w + ": unknown");
                if (m.distance < 2f || m.distance > 20f || m.height < 0f || m.height > 15f) e.Add(w + ": distance/height");
                if (m.fov < 40f || m.fov > 90f) e.Add(w + ": fov 40..90");
                if (m.orbit && (m.maxYaw <= 0f || m.maxYaw > 180f || m.pitchDown < 0f || m.pitchUp < 0f || m.pitchDown + m.pitchUp > 80f)) e.Add(w + ": orbit limits");
                if (m.zoom && (m.zoomMin <= 0.3f || m.zoomMax > 2f || m.zoomMin >= m.zoomMax)) e.Add(w + ": zoom range");
                if (m.smoothTime < 0f) e.Add(w + ": smooth");
            }
            var free = Mode("free"); var chase = Mode("chase");
            if (free != null && (!free.orbit || !free.zoom)) e.Add("free mode orbits and zooms");
            if (chase != null && (chase.orbit || chase.zoom)) e.Add("the chase camera is fixed");
            foreach (var t in StageTypes)
            {
                string m = ModeFor(t);
                if (Mode(m) == null) e.Add("stage type " + t + " has no mode");
            }
            if (ModeFor("normal") != "free" || ModeFor("action") != "free") e.Add("normal and action stages use the free camera");
            if (ModeFor("chase") != "chase" || ModeFor("race") != "race" || ModeFor("boss") != "boss") e.Add("chase, race and boss stages use their own camera");
            if (recenterSeconds < 1f || recenterSeconds > 2.5f) e.Add("recenter about 1.5 s");
            if (recenterDuration <= 0f || degreesPerCm <= 0f || wallMargin < 0.1f || minDistance <= 0.5f || wallInSpeed <= wallOutSpeed) e.Add("numbers");
            return e;
        }
    }

    /// <summary>What a drag, a pinch or the recenter button did, as the view relative to the default one.</summary>
    public class OrbitState
    {
        public float Yaw, Pitch, Zoom = 1f;
        /// <summary>Seconds since the last orbit input.</summary>
        public float Idle;
        public bool Dragging;
        public bool Recentering { get; private set; }

        public bool IsDefault => Math.Abs(Yaw) < 0.05f && Math.Abs(Pitch) < 0.05f;

        /// <summary>Whether a finger starting at x01 (0..1 across the screen) may orbit: the right half, or the left for left-handed play.</summary>
        public static bool IsOrbitTouch(float x01, bool leftHanded) => leftHanded ? x01 <= 0.5f : x01 >= 0.5f;

        /// <summary>Drag in pixels. Converted by dpi so a thumb-width drag turns the same on every phone.</summary>
        public void Drag(float dxPixels, float dyPixels, float dpi, CameraSettings s, CameraFile f, CameraModeDef mode)
        {
            if (mode == null || !mode.orbit) return;
            if (dpi < 40f) dpi = 160f;
            float perPixel = f.degreesPerCm * s.orbitSpeed / (dpi / 2.54f);
            Yaw += dxPixels * perPixel;
            // dragging up tilts the view up unless inverted
            Pitch += (s.invertY ? 1f : -1f) * dyPixels * perPixel;
            Clamp(mode);
            Idle = 0f; Dragging = true; Recentering = false;
        }

        /// <summary>Finger distance before and after: spreading zooms in (smaller distance), pinching zooms out.</summary>
        public void Pinch(float prevDistance, float curDistance, CameraModeDef mode)
        {
            if (mode == null || !mode.zoom || prevDistance <= 1f || curDistance <= 1f) return;
            Zoom = Math.Max(mode.zoomMin, Math.Min(mode.zoomMax, Zoom * prevDistance / curDistance));
            Idle = 0f;
        }

        /// <summary>The button: swing back behind the hero now.</summary>
        public void Recenter() { Recentering = true; Dragging = false; }

        public void EndDrag() { Dragging = false; }

        /// <summary>Call each frame. Returns true while the view is still easing back.</summary>
        public bool Tick(float dt, CameraSettings s, CameraFile f)
        {
            if (Dragging) { Idle = 0f; return false; }
            Idle += dt;
            bool auto = s.autoRecenter && Idle >= f.recenterSeconds && !IsDefault;
            if (!Recentering && !auto) return false;
            // ease out: cover the angle in recenterDuration, fast at first
            float k = 1f - (float)Math.Exp(-dt * 5f / f.recenterDuration);
            Yaw += (0f - Yaw) * k; Pitch += (0f - Pitch) * k;
            if (Math.Abs(Yaw) < 0.2f && Math.Abs(Pitch) < 0.2f) { Yaw = 0f; Pitch = 0f; Recentering = false; return false; }
            return true;
        }

        /// <summary>A new stage or camera mode starts from the default view.</summary>
        public void Reset() { Yaw = 0f; Pitch = 0f; Zoom = 1f; Idle = 0f; Dragging = false; Recentering = false; }

        private void Clamp(CameraModeDef m)
        {
            Yaw = Math.Max(-m.maxYaw, Math.Min(m.maxYaw, Yaw));
            Pitch = Math.Max(-m.pitchUp, Math.Min(m.pitchDown, Pitch));   // negative pitch offset looks up from lower
        }
    }

    public static class CameraMath
    {
        /// <summary>
        /// Where the camera sits relative to what it looks at. Yaw 0 is straight behind (-z), pitch is the look-down angle in degrees.
        /// </summary>
        public static void Offset(float yawDeg, float pitchDeg, float distance, out float x, out float y, out float z)
        {
            double yaw = yawDeg * Math.PI / 180.0, pitch = pitchDeg * Math.PI / 180.0;
            x = (float)(Math.Sin(yaw) * Math.Cos(pitch) * distance);
            y = (float)(Math.Sin(pitch) * distance);
            z = (float)(-Math.Cos(yaw) * Math.Cos(pitch) * distance);
        }

        /// <summary>Total look-down angle: the mode's default plus the orbit offset, never below ground level or past straight down.</summary>
        public static float Pitch(CameraModeDef m, float orbitPitch) => Math.Max(-5f, Math.Min(80f, m.BasePitch + orbitPitch));

        /// <summary>The distance to use when a wall sits between hero and camera: stop short of it by the margin, never closer than the minimum.</summary>
        public static float WallDistance(float desired, float hitDistance, float margin, float minDistance)
        {
            if (hitDistance < 0f) return desired;
            return Math.Min(desired, Math.Max(minDistance, hitDistance - margin));
        }

        /// <summary>Fast in, slow out: the camera jumps in front of a wall at once and drifts back out once it is clear.</summary>
        public static float SmoothDistance(float current, float target, float dt, float inSpeed, float outSpeed)
        {
            float speed = target < current ? inSpeed : outSpeed;
            float k = 1f - (float)Math.Exp(-speed * dt);
            return current + (target - current) * k;
        }

        /// <summary>Race camera: field of view and distance grow with speed (0..1).</summary>
        public static float RaceFov(CameraModeDef m, float speed01) => m.fov + m.fovSpeedBoost * Clamp01(speed01);
        public static float RaceDistance(CameraModeDef m, float speed01) => m.distance + m.distanceSpeedBoost * Clamp01(speed01);

        /// <summary>
        /// Boss arena framing: look at a point between the hero and the boss, weighted toward the hero, and back off as they drift apart
        /// so both stay in view.
        /// </summary>
        public static void FrameTwo(CameraModeDef m, float heroX, float heroZ, float bossX, float bossZ, out float focusX, out float focusZ, out float distance)
        {
            const float weightHero = 0.6f;
            focusX = heroX * weightHero + bossX * (1f - weightHero);
            focusZ = heroZ * weightHero + bossZ * (1f - weightHero);
            float gap = (float)Math.Sqrt((heroX - bossX) * (heroX - bossX) + (heroZ - bossZ) * (heroZ - bossZ));
            distance = m.distance + Math.Min(m.frameExtra, gap * m.frameGapFactor);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>
    /// Screen shake as trauma: hits add to it, it fades by itself, and the strength follows the settings slider. The runtime turns the
    /// <see cref="Intensity"/> into a position and roll offset.
    /// </summary>
    public class ShakeModel
    {
        public const float PositionMetres = 0.6f, RollDegrees = 6f, DecayPerSecond = 1.8f;
        public float Trauma { get; private set; }

        public void Add(float amount, float scale)
        {
            if (scale <= 0f || amount <= 0f) return;
            Trauma = Math.Min(1f, Trauma + amount * Math.Max(0f, Math.Min(1f, scale)));
        }

        public void Tick(float dt) { Trauma = Math.Max(0f, Trauma - dt * DecayPerSecond); }

        /// <summary>Squared, so small bumps are small and big hits are big.</summary>
        public float Intensity => Trauma * Trauma;

        public void Clear() { Trauma = 0f; }
    }
}
