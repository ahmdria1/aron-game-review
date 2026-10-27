// (c) Ahmed Alamrani. All rights reserved.
using Aron.Config;
using Aron.Input;
using Aron.Logic.GameFeel;
using Aron.Logic.CameraRig;
using Aron.Save;
using Aron.Stages;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// The game camera. Four modes (Resources/Config/camera.json, picked per stage by its type or its own override):
    /// free third person you can orbit with a drag in the right half of the screen and pinch to zoom, a fixed Crash-style chase view,
    /// a race view that widens with speed, and a boss-arena view that keeps hero and boss both in frame. After about 1.5 s without
    /// orbit input the free camera swings back behind the hero; <see cref="Recenter"/> does it at once. A sphere cast keeps it out of walls.
    /// On top of that, the Game Feel layer (docs/game_feel.md) adds look-ahead in the run direction, a vertical dead band that ignores small hops, and the boss
    /// weighting; its numbers come from <see cref="FeelSettings"/> and are tuned with the hidden panel.
    /// All of the maths is in Aron.Logic.CameraRig and Aron.Logic.GameFeel; shake strength, invert-Y and orbit speed come from the saved camera settings.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public const string ConfigPath = "Config/camera";

        [SerializeField] private Transform target;
        [SerializeField] private LayerMask obstacleMask = 0;
        [SerializeField] private float wallRadius = 0.25f;

        /// <summary>A target that moves farther than this in one frame was teleported (respawn, checkpoint): the camera jumps with it instead of flying through the level.</summary>
        public const float TeleportSnapMetres = 8f;
        private readonly RaycastHit[] wallHits = new RaycastHit[16];
        private readonly Collider[] overlapBuf = new Collider[16];

        private static FollowCamera current;
        private static CameraFile config;

        private Vector3 vel, basePos;
        private float trackedY, fovVel, curLen = -1f;
        private readonly LookAheadModel lookAhead = new LookAheadModel();
        private readonly VerticalFollowModel vertical = new VerticalFollowModel();
        private bool snapped;
        private string modeId = "";
        private CameraModeDef mode;
        private readonly OrbitState orbit = new OrbitState();
        private readonly ShakeModel shake = new ShakeModel();
        private Vector3 lastTarget;
        private float speed01;
        private Camera cam;

        public static CameraFile Config
        {
            get
            {
                if (config == null)
                {
                    var t = Resources.Load<TextAsset>(ConfigPath);
                    config = t != null ? JsonUtility.FromJson<CameraFile>(t.text) : new CameraFile();
                    if (config.Validate().Count > 0) global::Aron.Logic.Diagnostics.Log.Warn("camera.json: " + string.Join("; ", config.Validate()));
                }
                return config;
            }
        }

        /// <summary>True while the camera can be orbited (free mode and one-hand mode off): the recenter button shows then.</summary>
        public static bool CanOrbit => current != null && current.mode != null && current.mode.orbit && !CameraOrbitInput.Disabled;

        public void SetTarget(Transform t) { target = t; snapped = false; }

        public static void Shake(float amount)
        {
            if (current == null) return;
            var d = SaveSystem.Data;
            current.shake.Add(amount, Aron.Logic.Access.FlashSafety.ShakeScale(d.access, d.camera));
        }

        /// <summary>The recenter button.</summary>
        public static void Recenter() { if (current != null) current.orbit.Recenter(); }

        private void OnEnable() { current = this; cam = GetComponent<Camera>(); }
        private void OnDisable() { if (current == this) current = null; }

        private void PickMode()
        {
            var st = StageSession.Stage;
            string id = Config.ModeFor(st != null ? st.type : "normal", st != null ? st.camera : "");
            if (id == modeId && mode != null) return;
            modeId = id; mode = Config.Mode(id) ?? Config.Mode("free");
            orbit.Reset();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            PickMode();
            if (mode == null) return;
            float dt = Time.deltaTime;
            var settings = SaveSystem.Data.camera ?? new CameraSettings();

            Vector3 p = target.position;
            if (!snapped) { trackedY = p.y; vertical.Reset(p.y); lookAhead.Reset(); basePos = transform.position; lastTarget = p; curLen = -1f; snapped = true; }
            bool teleported = snapped && (p - lastTarget).sqrMagnitude > TeleportSnapMetres * TeleportSnapMetres;
            if (teleported) { trackedY = p.y; vertical.Reset(p.y); lookAhead.Reset(); speed01 = 0f; vel = Vector3.zero; curLen = -1f; }
            else if (dt > 0f) speed01 = Mathf.Lerp(speed01, Mathf.Clamp01((p - lastTarget).magnitude / dt / 14f), 0.1f);
            lastTarget = p;

            CameraOrbitInput.Poll(orbit, settings, Config, mode);
            orbit.Tick(dt, settings, Config);

            // Follow height lazily: small hops stay inside a dead band and do not bob the screen; a new floor is settled onto; a long drop catches up.
            var fv = FeelSettings.V;
            var pc = PlayerController.Active;
            bool chase = modeId == "chase";
            bool onGround = pc == null || pc.IsGrounded;
            trackedY = vertical.Step(fv, p.y, onGround, dt, chase ? fv.camChaseVertWindowMult : 1f);

            // Look a little way along the run (the chase camera less, the boss and race ones not at all: they frame by other rules).
            V2 lead = default;
            if (pc != null && (modeId == "free" || chase))
            {
                var v = pc.Velocity;
                lead = lookAhead.Step(fv, new V2(v.x, v.z), fv.runSpeed * fv.sprintMultiplier, dt, chase ? fv.camChaseLookAheadMult : 1f);
            }

            float distance = mode.distance * orbit.Zoom, height = mode.height * orbit.Zoom;
            float blend = Mathf.Clamp01(Mathf.Abs(orbit.Yaw) / 30f);   // when orbited, follow in x exactly so the hero stays centred
            float lateral = Mathf.Lerp(mode.lateralFollow, 1f, blend);
            Vector3 anchor = new Vector3(p.x * lateral + lead.X * lateral, trackedY, p.z + lead.Z);
            Vector3 focus = new Vector3(p.x + lead.X, trackedY + 1.2f, p.z + lead.Z + mode.lookAhead * (1f - blend));

            if (modeId == "boss" && RiyadhBossAccess.TryGet(out var bp))
            {
                CameraMath.FrameTwo(mode, p.x, p.z, bp.x, bp.z, out _, out _, out var d);
                BossFraming.Focus(fv, p.x, p.z, bp.x, bp.z, out var fx, out var fz);   // leans toward the hero; the distance still shows both
                float gap = Mathf.Sqrt((bp.x - p.x) * (bp.x - p.x) + (bp.z - p.z) * (bp.z - p.z));
                focus = new Vector3(fx, trackedY + 1.2f + BossFraming.Lift(fv, gap), fz); anchor = new Vector3(fx * lateral, trackedY, fz);
                distance = d;
            }
            else if (CameraFile.IsRaceLike(modeId)) distance = CameraMath.RaceDistance(mode, speed01);

            focus = ClampFocus(p + Vector3.up * 1.2f, focus);

            float pitch = CameraMath.Pitch(mode, orbit.Pitch);
            float len = Mathf.Sqrt(distance * distance + height * height);
            float basePitch = mode.BasePitch;
            if (modeId != "free") { len = Mathf.Sqrt(distance * distance + mode.height * mode.height); pitch = basePitch; }
            CameraMath.Offset(orbit.Yaw, pitch, len, out var ox, out var oy, out var oz);
            Vector3 desired = anchor + new Vector3(ox, oy, oz);

            if (teleported) basePos = desired;
            else basePos = Vector3.SmoothDamp(basePos, desired, ref vel, mode.smoothTime);

            // Walls: a sphere cast from the hero to the camera. Moves in at once, drifts back out slowly.
            Vector3 toCam = basePos - focus;
            float full = toCam.magnitude;
            Vector3 pos = basePos;
            if (full > 0.01f)
            {
                Vector3 dir = toCam / full;
                float hit = -1f;
                hit = NearestWall(focus, dir, full);
                float want = CameraMath.WallDistance(full, hit, Config.wallMargin, Config.minDistance);
                curLen = curLen < 0f ? want : CameraMath.SmoothDistance(curLen, want, dt, Config.wallInSpeed, Config.wallOutSpeed);
                curLen = Mathf.Min(curLen, full);
                pos = focus + dir * curLen;
            }

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(focus - pos, Vector3.up);
            Quaternion shakeRoll = Quaternion.identity;
            if (shake.Trauma > 0f)
            {
                float s = shake.Intensity;
                float t = Time.time * 25f;
                pos += transform.right * (Mathf.PerlinNoise(t, 0f) - 0.5f) * s * ShakeModel.PositionMetres
                     + transform.up * (Mathf.PerlinNoise(0f, t) - 0.5f) * s * ShakeModel.PositionMetres;
                shakeRoll = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, t) - 0.5f) * s * ShakeModel.RollDegrees);
                shake.Tick(dt);
            }
            // After the shake, so a tremor can never push the lens into a wall either.
            pos = PullOutOfGeometry(pos, focus);
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(focus - pos, Vector3.up) * shakeRoll;

            if (cam != null)
            {
                float fov = CameraFile.IsRaceLike(modeId) ? CameraMath.RaceFov(mode, speed01) : mode.fov;
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, fov, ref fovVel, 0.3f);
            }

        }

        /// <summary>
        /// Distance from the focus to the nearest wall or floor along the camera boom, or -1. A sphere cast, so the lens is kept a wall radius off the surface.
        /// The mask defaults to every normal layer (the serialized one was never set, so the camera used to ignore walls). Triggers, the hero himself,
        /// anything that can be hit and loose rigidbodies (crates, carts, animals) never push the camera.
        /// </summary>
        private float NearestWall(Vector3 from, Vector3 dir, float length)
        {
            int n = Physics.SphereCastNonAlloc(from, wallRadius, dir, wallHits, length, WallMask, QueryTriggerInteraction.Ignore);
            float best = -1f;
            for (int i = 0; i < n; i++)
            {
                var h = wallHits[i];
                if (h.distance <= 0f) continue;   // a sphere that starts inside a collider reports distance 0: that one is behind the lens already
                if (!IsWall(h.collider)) continue;
                if (best < 0f || h.distance < best) best = h.distance;
            }
            return best;
        }

        /// <summary>
        /// The point the camera looks at sits a little ahead of the hero (and lags behind a fall). If a wall or a slab lies between his head and that point
        /// (he dropped into a pit beside the next segment), the point would be inside solid ground and the camera could never be placed in the open around it:
        /// stop it at the wall instead.
        /// </summary>
        private Vector3 ClampFocus(Vector3 head, Vector3 focus)
        {
            Vector3 d = focus - head;
            float len = d.magnitude;
            if (len < 0.05f) return focus;
            Vector3 dir = d / len;
            int n = Physics.SphereCastNonAlloc(head, 0.2f, dir, wallHits, len, WallMask, QueryTriggerInteraction.Ignore);
            float best = -1f;
            for (int i = 0; i < n; i++)
            {
                var h = wallHits[i];
                if (h.distance <= 0f || !IsWall(h.collider)) continue;
                if (best < 0f || h.distance < best) best = h.distance;
            }
            return best < 0f ? focus : head + dir * Mathf.Max(0f, best - 0.1f);
        }

        private int WallMask => obstacleMask.value != 0 ? obstacleMask.value : Physics.DefaultRaycastLayers;

        private bool IsWall(Collider c)
        {
            if (c == null || c.isTrigger) return false;
            if (target != null && c.transform.IsChildOf(target)) return false;   // not the root: the hero may sit under the world root
            if (c.GetComponentInParent<PlayerController>() != null) return false;
            if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return false;
            return c.GetComponentInParent<Aron.Core.IDamageable>() == null;
        }

        /// <summary>The last line of defence: if the lens ended up inside a wall or a floor (a thin mesh the cast cannot see from behind, a stale position), walk it toward the hero until it is clear.</summary>
        private Vector3 PullOutOfGeometry(Vector3 pos, Vector3 focus)
        {
            float lens = Mathf.Max(wallRadius * 0.6f, cam != null ? cam.nearClipPlane * 1.5f : 0f);
            for (int step = 0; step < 24; step++)
            {
                int n = Physics.OverlapSphereNonAlloc(pos, lens, overlapBuf, WallMask, QueryTriggerInteraction.Ignore);
                bool inside = false;
                for (int i = 0; i < n && !inside; i++) inside = IsWall(overlapBuf[i]);
                if (!inside) break;
                Vector3 to = focus - pos;
                if (to.magnitude <= Config.minDistance) break;
                pos += to.normalized * Mathf.Min(0.25f, to.magnitude - Config.minDistance);
            }
            if (TouchesWall(pos, lens)) pos = FreeSpotAround(pos, focus, lens);
            return pos;
        }

        private bool TouchesWall(Vector3 pos, float lens)
        {
            int n = Physics.OverlapSphereNonAlloc(pos, lens, overlapBuf, WallMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (IsWall(overlapBuf[i])) return true;
            return false;
        }

        /// <summary>
        /// Still inside after walking toward the hero (the hero is under a slab, in a pit, or the geometry is one big solid): look for the nearest open spot
        /// around the hero that he can be seen from, turning away from the stuck direction a step at a time and going higher before closer.
        /// </summary>
        private Vector3 FreeSpotAround(Vector3 stuck, Vector3 focus, float lens)
        {
            Vector3 dir = stuck - focus;
            float len = Mathf.Max(dir.magnitude, Config.minDistance * 2f);
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.back;
            float[] dists = { len, len * 0.6f, Mathf.Max(Config.minDistance * 1.5f, 1f) };
            float[] lifts = { 0f, 25f, 50f, 75f, -25f, -50f, -75f };   // up first; down when the hero is under an overhang
            for (int d = 0; d < dists.Length; d++)
                for (int l = 0; l < lifts.Length; l++)
                    for (int a = 0; a < 12; a++)
                    {
                        float yaw = (a % 2 == 0 ? 1 : -1) * 30f * ((a + 1) / 2);
                        Vector3 flat = Quaternion.Euler(0f, yaw, 0f) * new Vector3(dir.x, 0f, dir.z).normalized;
                        if (flat.sqrMagnitude < 0.5f) flat = Vector3.back;
                        Vector3 v = (Quaternion.AngleAxis(lifts[l], Vector3.Cross(flat, Vector3.up).normalized) * flat).normalized;
                        Vector3 cand = focus + v * dists[d];
                        if (TouchesWall(cand, lens)) continue;
                        if (NearestWall(focus, v, dists[d]) >= 0f) continue;   // the hero must be visible from there
                        return cand;
                    }
            return stuck;
        }
    }

    /// <summary>Finds the boss's position without the camera knowing about boss internals.</summary>
    public static class RiyadhBossAccess
    {
        public static bool TryGet(out Vector3 pos)
        {
            var b = Aron.Enemies.RiyadhBoss.Current;
            pos = b != null ? b.transform.position : default;
            return b != null;
        }
    }
}
