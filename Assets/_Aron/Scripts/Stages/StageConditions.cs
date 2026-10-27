// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Audio;
using Aron.Core;
using Aron.Enemies;
using Aron.Fx;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Audio;
using Aron.Logic.Regions;
using Aron.Player;
using Aron.UI;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>Light, weather and stage type (action waves, timed escape, chase) for a built stage.</summary>
    public static class StageConditions
    {
        public static void Apply(StageBuilder b, StageArt art, StageDef s, Light sun, Color sky, Color ground)
        {
            var layout = b.Layout;
            var host = b.World.gameObject;
            SetLight(s.time, s.weather, sun, sky);
            // Sky, clouds, rain and snow, lightning, wet ground, water and wind. Added before the stage streams in so
            // water surfaces get their material; it takes over the light and fog SetLight just laid down.
            Atmosphere.WeatherDirector.Attach(b, art, s, b.Region, sun, sky, ground);
            if (s.time == "night" || s.time == "sunset") Lamps(art, b.World, layout, s.time == "night");
            if (s.time == "night" && b.Player != null) Glow(b.Player);

            if (s.weather == "rain") host.AddComponent<RainWeather>().Init(b, art);
            else if (s.weather == "sandstorm") host.AddComponent<SandstormWeather>().Init(b, ground);

            Ambushes(b, art, layout, s.type == "action");
            if (s.type == "action") Escape(b, layout);
            if (ChaseHazard.Wanted(layout)) host.AddComponent<ChaseHazard>().Init(b, art, s);
        }

        private static void SetLight(string time, string weather, Light sun, Color sky)
        {
            Color sunColor, ambient, background;
            float intensity, pitch;
            switch (time)
            {
                case "sunset":
                    sunColor = new Color(1f, 0.62f, 0.38f); intensity = 0.9f; pitch = 12f;
                    ambient = new Color(0.55f, 0.42f, 0.42f); background = Color.Lerp(sky, new Color(0.98f, 0.55f, 0.35f), 0.6f);
                    break;
                case "night":
                    sunColor = new Color(0.55f, 0.65f, 0.95f); intensity = 0.28f; pitch = 40f;
                    ambient = new Color(0.16f, 0.18f, 0.3f); background = new Color(0.04f, 0.06f, 0.14f);
                    break;
                default:
                    sunColor = new Color(1f, 0.96f, 0.88f); intensity = 1.15f; pitch = 50f;
                    ambient = new Color(0.62f, 0.62f, 0.66f); background = sky;
                    break;
            }
            if (weather == "rain") { intensity *= 0.6f; ambient *= 0.75f; background = Color.Lerp(background, new Color(0.35f, 0.38f, 0.42f), 0.6f); }
            if (weather == "sandstorm") { sunColor = Color.Lerp(sunColor, new Color(1f, 0.7f, 0.4f), 0.5f); intensity *= 0.7f; background = new Color(0.8f, 0.6f, 0.38f); }

            if (sun != null)
            {
                sun.color = sunColor; sun.intensity = intensity;
                sun.transform.rotation = Quaternion.Euler(pitch, time == "sunset" ? 250f : -30f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = weather != "clear" || time == "night";
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = background;
            RenderSettings.fogStartDistance = weather == "sandstorm" ? 8f : 25f;
            RenderSettings.fogEndDistance = weather == "sandstorm" ? 45f : time == "night" ? 90f : 120f;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = background; }
        }

        private static void Lamps(StageArt art, Transform world, StageLayout layout, bool night)
        {
            // Street lamps and hanging lanterns. Only a handful carry a real light on weaker phones; the rest glow.
            int lightsLeft = DeviceSettings.Tier == QualityTier.High ? 10 : DeviceSettings.Tier == QualityTier.Medium ? 5 : 0;
            var warm = new Color(1f, 0.78f, 0.45f);
            int i = 0;
            foreach (var g in layout.Of(PieceKind.Ground))
            {
                for (float z = g.Start + 2f; z < g.End - 1f; z += 12f, i++)
                {
                    float side = i % 2 == 0 ? 1f : -1f;
                    var lamp = StageArt.Group("Lamp", world, new Vector3(side * (layout.HalfWidth + 0.35f), g.y + 0.8f, z));
                    art.Cyl(lamp, Vector3.zero, 0.07f, 2.6f, new Color(0.2f, 0.18f, 0.16f));
                    art.Box(lamp, new Vector3(-side * 0.35f, 2.55f, 0f), new Vector3(0.7f, 0.06f, 0.06f), new Color(0.2f, 0.18f, 0.16f));
                    var bulb = art.Ball(lamp, new Vector3(-side * 0.65f, 2.35f, 0f), Vector3.one * 0.35f, warm);
                    Emissive(bulb, warm * (night ? 2.5f : 1.2f));
                    if (night && lightsLeft-- > 0)
                    {
                        var l = new GameObject("Light").AddComponent<Light>();
                        l.transform.SetParent(lamp, false);
                        l.transform.localPosition = new Vector3(-side * 0.65f, 2.2f, 0f);
                        l.type = LightType.Point; l.color = warm; l.range = 9f; l.intensity = 2.2f;
                        l.shadows = LightShadows.None;
                    }
                }
            }
        }

        private static void Glow(Transform player)
        {
            // Aron carries a soft light at night so the path right in front of him stays readable.
            var l = new GameObject("AronGlow").AddComponent<Light>();
            l.transform.SetParent(player, false);
            l.transform.localPosition = new Vector3(0f, 2.2f, 0.6f);
            l.type = LightType.Point; l.color = new Color(1f, 0.9f, 0.7f); l.range = 7f; l.intensity = 1.6f;
            l.shadows = LightShadows.None;
        }

        internal static void Emissive(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var m = r.material;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
        }

        private static void Ambushes(StageBuilder b, StageArt art, StageLayout layout, bool barrels)
        {
            var host = b.World;
            // Each ambush piece is a trigger across the track; enemies drop in just ahead when he runs through.
            foreach (var p in layout.Of(PieceKind.Ambush))
            {
                float z = SafeZ(layout, p.z);
                var t = StageArt.Group("Ambush", host, new Vector3(0f, layout.GroundAt(z) + 1f, z));
                var box = t.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true; box.size = new Vector3(layout.HalfWidth * 2f, 4f, 2f);
                t.gameObject.AddComponent<ActionWave>().Init(b, Mathf.Max(2, (int)p.sizeA));
                if (!barrels) continue;
                for (int k = 0; k < 3; k++)
                {
                    float bz = SafeZ(layout, z + 4f + k * 3f);
                    float gy = layout.GroundAt(bz);
                    if (float.IsNaN(gy)) continue;
                    Barrel(art, host, new Vector3((k - 1) * layout.HalfWidth * 0.6f, gy, bz));
                }
            }
        }

        private static void Escape(StageBuilder b, StageLayout layout)
        {
            var host = b.World;
            Piece goal = default; bool found = false;
            foreach (var p in layout.Of(PieceKind.Goal)) { goal = p; found = true; }
            if (!found) return;
            float start = SafeZ(layout, Mathf.Max(layout.Length * 0.72f, goal.z - 90f));
            var esc = StageArt.Group("EscapeStart", host, new Vector3(0f, layout.GroundAt(start) + 1f, start));
            var ec = esc.gameObject.AddComponent<BoxCollider>();
            ec.isTrigger = true; ec.size = new Vector3(layout.HalfWidth * 2f, 4f, 2f);
            float seconds = Mathf.Ceil((goal.z - start) / 4.2f) + 6f;
            esc.gameObject.AddComponent<EscapeTimer>().Init(seconds);
        }

        /// <summary>Nearest z at or after <paramref name="z"/> that has ground under it.</summary>
        internal static float SafeZ(StageLayout layout, float z)
        {
            for (float d = 0f; d < 30f; d += 0.5f)
                if (!float.IsNaN(layout.GroundAt(z + d))) return z + d;
            return z;
        }

        private static void Barrel(StageArt art, Transform parent, Vector3 pos)
        {
            var g = StageArt.Group("Barrel", parent, pos);
            var red = new Color(0.75f, 0.14f, 0.1f);
            art.Cyl(g, Vector3.zero, 0.45f, 1.1f, red);
            art.Cyl(g, new Vector3(0f, 0.25f, 0f), 0.47f, 0.08f, new Color(0.15f, 0.15f, 0.15f));
            art.Cyl(g, new Vector3(0f, 0.8f, 0f), 0.47f, 0.08f, new Color(0.15f, 0.15f, 0.15f));
            var col = g.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.55f, 0f); col.radius = 0.45f; col.height = 1.1f;
            g.gameObject.AddComponent<ExplosiveBarrel>();
        }
    }

    /// <summary>Keeps a transform over the player (weather emitters).</summary>
    public class FollowPlayer : MonoBehaviour
    {
        public Vector3 offset;
        private void LateUpdate()
        {
            var p = PlayerController.Active;
            if (p != null) transform.position = p.transform.position + offset;
        }
    }

    internal static class Particles
    {
        public static ParticleSystem Make(string name, Material mat, int max, float rate, Vector3 box, Vector3 velocity, float size, Color c, float stretch)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.maxParticles = max; main.startLifetime = 1.6f; main.startSpeed = 0f; main.startSize = size;
            main.startColor = c; main.simulationSpace = ParticleSystemSimulationSpace.World; main.loop = true;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = velocity.x; vel.y = velocity.y; vel.z = velocity.z;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            if (stretch > 0f) { r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = stretch; }
            ps.Play();
            return ps;
        }
    }

    /// <summary>
    /// An authored rain stage: a slippery track and puddles that splash. The rain itself, the lightning (which keeps
    /// to the reduce-flashing setting) and the thunder come from <see cref="Atmosphere.WeatherDirector"/>.
    /// </summary>
    public class RainWeather : MonoBehaviour
    {
        public const float WetTraction = 0.45f;
        private float nextStep;
        private readonly List<Vector4> puddles = new List<Vector4>();   // x, y, z, radius

        public void Init(StageBuilder b, StageArt art)
        {
            var rnd = new System.Random(b.Stage.Seed);
            var layout = b.Layout;
            foreach (var g in layout.Of(PieceKind.Ground))
                for (float z = g.Start + 3f; z < g.End - 2f; z += 7f + (float)rnd.NextDouble() * 6f)
                {
                    float x = ((float)rnd.NextDouble() * 2f - 1f) * (layout.HalfWidth - 1f);
                    float r = 0.7f + (float)rnd.NextDouble() * 0.8f;
                    var puddle = art.Cyl(b.World, new Vector3(x, g.y + 0.01f, z), r, 0.02f, new Color(0.3f, 0.36f, 0.45f));
                    puddle.name = "Puddle";
                    Object.Destroy(puddle.GetComponent<Collider>());
                    puddles.Add(new Vector4(x, g.y, z, r));
                }
            GameUI.Banner(Strings.Get("rain_in"));
        }

        private void Update()
        {
            var p = PlayerController.Active;
            if (p == null) return;
            p.Traction = p.IsRiding ? 0.7f : WetTraction;
            Splash(p);
            Atmosphere.WeatherGameplayRuntime.PuddleSpeed = Logic.Sky.WeatherGameplay.SpeedInPuddle(Atmosphere.WeatherGameplayRuntime.File, InPuddle(p));
        }

        private bool InPuddle(PlayerController p)
        {
            var pos = p.transform.position;
            foreach (var q in puddles)
            {
                float dx = pos.x - q.x, dz = pos.z - q.z;
                if (Mathf.Abs(pos.y - q.y) <= 0.4f && dx * dx + dz * dz <= q.w * q.w) return true;
            }
            return false;
        }

        private void Splash(PlayerController p)
        {
            var v = p.Velocity; v.y = 0f;
            if (Time.time < nextStep || v.sqrMagnitude < 4f || p.IsFalling) return;
            var pos = p.transform.position;
            foreach (var q in puddles)
            {
                float dx = pos.x - q.x, dz = pos.z - q.z;
                if (Mathf.Abs(pos.y - q.y) > 0.4f || dx * dx + dz * dz > q.w * q.w) continue;
                Sfx.Play("puddle_step", pos, 0.8f);
                nextStep = Time.time + Mathf.Clamp(1.6f / Mathf.Sqrt(v.sqrMagnitude), 0.18f, 0.4f);
                return;
            }
        }

        private void OnDestroy()
        {
            var p = PlayerController.Active;
            if (p != null) p.Traction = 1f;
        }
    }

    /// <summary>Thick dust and low visibility; the camel walks through it unbothered.</summary>
    public class SandstormWeather : MonoBehaviour
    {
        public void Init(StageBuilder b, Color ground)
        {
            var tier = DeviceSettings.Tier;
            int max = tier == QualityTier.Low ? 200 : tier == QualityTier.Medium ? 500 : 1100;
            var ps = Particles.Make("Sand", b.ParticleMaterial, max, max / 1.2f, new Vector3(2f, 6f, 30f),
                new Vector3(-14f, 0.5f, -1f), 0.18f, new Color(ground.r, ground.g * 0.9f, ground.b * 0.8f, 0.5f), 1.5f);
            ps.transform.SetParent(transform, false);
            ps.gameObject.AddComponent<FollowPlayer>().offset = new Vector3(14f, 2.5f, 6f);
            var mix = GetComponent<WeatherAudio>();
            if (mix == null) mix = gameObject.AddComponent<WeatherAudio>();
            mix.SetWeather(Weather.Sandstorm);
            GameUI.Banner(Strings.Get("storm_in"));
        }
    }

    /// <summary>Walk into it and a group of enemies jumps out ahead.</summary>
    public class ActionWave : MonoBehaviour
    {
        private StageBuilder builder;
        private int count;
        private bool fired;

        public void Init(StageBuilder b, int n) { builder = b; count = n; }

        private void OnTriggerEnter(Collider other)
        {
            if (fired || other.GetComponentInParent<PlayerController>() == null) return;
            fired = true;
            var layout = builder.Layout;
            for (int i = 0; i < count; i++)
            {
                float z = StageConditions.SafeZ(layout, transform.localPosition.z + 6f + i * 2.5f);
                float y = layout.GroundAt(z);
                if (float.IsNaN(y)) continue;
                float x = (i % 2 == 0 ? 1f : -1f) * (layout.HalfWidth - 1.2f);
                var e = builder.SpawnPatrol(new Vector3(x, y, z), -x * 1.6f);
                if (e != null) FxPlayer.Play("poof", e.transform.position);
            }
            GameUI.Banner(Strings.Get("wave_in"), 1.8f);
            GameEvents.PlaySfx("enemy");
        }
    }

    /// <summary>Red barrel: hit it and it blows, taking nearby enemies with it. Too close and Aron gets hurt too.</summary>
    public class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        public const float Radius = 3.2f, PlayerRadius = 1.8f;
        private bool gone;
        public bool CanBeDamaged => !gone;

        public void TakeDamage(DamageInfo info) => Explode();

        private void Explode()
        {
            if (gone) return;
            gone = true;
            var c = transform.position + Vector3.up * 0.5f;
            FxPlayer.Play("crate", c);
            FxPlayer.Play("dust", c);
            Sfx.Play("explosion", c);
            foreach (var e in new List<Enemy>(Enemy.All))
                if (e != null && e.CanBeDamaged && (e.transform.position - c).sqrMagnitude < Radius * Radius)
                    e.TakeDamage(new DamageInfo(99, DamageKind.Hazard, c, gameObject));
            foreach (var hit in Physics.OverlapSphere(c, Radius))
            {
                var other = hit.GetComponentInParent<ExplosiveBarrel>();
                if (other != null && other != this) other.Invoke(nameof(Explode), 0.15f);
                var brk = hit.GetComponentInParent<Breakable>();
                if (brk != null) brk.Smash();
            }
            var p = PlayerController.Active;
            if (p != null && (p.transform.position - c).sqrMagnitude < PlayerRadius * PlayerRadius) p.Hurt(true);
            Destroy(gameObject);
        }
    }

    /// <summary>From here to the goal against the clock. Run out and Aron goes back to the checkpoint.</summary>
    public class EscapeTimer : MonoBehaviour
    {
        private float seconds, endAt = -1f;
        private int shown = -1;

        public void Init(float s) => seconds = s;

        private void OnEnable() { GameEvents.LevelCompleted += Stop; GameEvents.PlayerDied += Stop; }
        private void OnDisable() { GameEvents.LevelCompleted -= Stop; GameEvents.PlayerDied -= Stop; GameUI.Timer(null); }

        private void Stop() { endAt = -1f; shown = -1; GameUI.Timer(null); }

        private void OnTriggerEnter(Collider other)
        {
            if (endAt > 0f || other.GetComponentInParent<PlayerController>() == null) return;
            endAt = Time.time + seconds;
            GameUI.Banner(Strings.Get("escape_go"));
        }

        private void Update()
        {
            if (endAt < 0f) return;
            int left = Mathf.CeilToInt(endAt - Time.time);
            if (left != shown) { shown = left; GameUI.Timer(Strings.Format("escape_time", Mathf.Max(0, left))); }
            if (left > 0) return;
            Stop();
            GameUI.Banner(Strings.Get("escape_fail"));
            var p = PlayerController.Active;
            if (p != null) p.Die(DeathCause.Hazard);
        }
    }

    /// <summary>
    /// A boulder that rolls after him through each chase section of the stage (ChaseRun pieces). It starts a few
    /// metres behind as he enters, keeps a little under his speed and drops away at the end of the section.
    /// </summary>
    public class ChaseHazard : MonoBehaviour
    {
        public const float StartGap = 16f, Radius = 2.2f, NearMiss = 1.6f;
        private StageBuilder builder;
        private Transform ball;
        private float speed, z, waitUntil = -1f, nearMissAt = -10f;
        private readonly List<Vector2> runs = new List<Vector2>();
        private int current = -1;
        private bool running;

        public static bool Wanted(StageLayout layout)
        {
            foreach (var _ in layout.Of(PieceKind.ChaseRun)) return true;
            return false;
        }

        public void Init(StageBuilder b, StageArt art, StageDef s)
        {
            builder = b;
            speed = PlayerSpeed() * Mathf.Lerp(0.72f, 0.88f, (s.difficulty - 1) / 9f) * Logic.Access.AssistRules.ChaseSpeed(Save.SaveSystem.Data.access);
            foreach (var p in b.Layout.Of(PieceKind.ChaseRun))
                runs.Add(new Vector2(p.z - p.sizeB * 0.5f, p.z + p.sizeB * 0.5f));
            ball = StageArt.Group("Roller", b.World, Vector3.zero);
            var body = art.Ball(ball, Vector3.zero, Vector3.one * Radius * 2f, new Color(0.55f, 0.42f, 0.32f));
            art.Box(ball, Vector3.zero, new Vector3(Radius * 2.05f, 0.35f, 0.35f), new Color(0.4f, 0.28f, 0.2f));
            art.Box(ball, Vector3.zero, new Vector3(0.35f, Radius * 2.05f, 0.35f), new Color(0.4f, 0.28f, 0.2f));
            Object.Destroy(body.GetComponent<Collider>());
            UI.HazardMarker.Attach(ball.gameObject, 1.6f);
            ball.gameObject.SetActive(false);
        }

        private static float PlayerSpeed()
        {
            return Aron.Config.FeelSettings.V.runSpeed;
        }

        private void OnEnable() { GameEvents.PlayerDied += OnDied; GameEvents.LevelCompleted += OnDone; }
        private void OnDisable() { GameEvents.PlayerDied -= OnDied; GameEvents.LevelCompleted -= OnDone; }
        private void OnDied() { Stop(); waitUntil = Time.time + 2.2f; }
        private void OnDone() { Stop(); runs.Clear(); }

        private void Stop()
        {
            running = false;
            current = -1;
            if (ball != null) ball.gameObject.SetActive(false);
        }

        private int RunAt(float pz)
        {
            for (int i = 0; i < runs.Count; i++) if (pz >= runs[i].x && pz < runs[i].y - 3f) return i;
            return -1;
        }

        private void Place()
        {
            float gy = builder.Layout.GroundAt(Mathf.Max(0.5f, z));
            if (float.IsNaN(gy)) gy = 0f;
            ball.localPosition = new Vector3(0f, gy + Radius, z);
        }

        private void Update()
        {
            var p = PlayerController.Active;
            if (p == null || ball == null || p.IsDead) return;
            float pz = p.transform.position.z;
            if (!running)
            {
                if (Time.time < waitUntil) return;
                int i = RunAt(pz);
                if (i < 0) return;
                current = i;
                z = Mathf.Max(runs[i].x - 2f, pz - StartGap);
                running = true;
                ball.gameObject.SetActive(true);
                Place();
                GameUI.Banner(Strings.Get("chase_run"), 1.8f);
                GameEvents.PlaySfx(AudioCues.ChaseStart);
                AudioManager.Bark(AudioCues.VoAronHelp, AudioCues.VoChase);
            }
            // Past the end of the section the boulder rolls off into the sand.
            if (pz > runs[current].y || z > runs[current].y - 2f) { Stop(); waitUntil = Time.time + 0.5f; return; }
            float catchUp = pz - z > StartGap * 1.6f ? 1.5f : 1f;
            z += speed * catchUp * Time.deltaTime;
            Place();
            ball.Rotate(Vector3.right, speed * catchUp * Time.deltaTime / Radius * Mathf.Rad2Deg, Space.Self);
            var d = p.transform.position - ball.position;
            d.y = Mathf.Max(0f, d.y - Radius * 0.4f);
            float reach = Radius + 0.3f;
            if (d.sqrMagnitude < reach * reach && !p.IsInvulnerable) { p.Die(DeathCause.Enemy); return; }
            if (d.sqrMagnitude < (reach + NearMiss) * (reach + NearMiss) && Time.time - nearMissAt > 6f)
            {
                nearMissAt = Time.time;
                AudioManager.Bark(AudioCues.VoAronNearMiss, null);
            }
        }
    }
}
