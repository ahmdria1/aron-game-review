// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Audio
{
    /// <summary>One bike's sound: three loops (low, mid, high revs) that crossfade, and how its pitch and whine behave.</summary>
    [Serializable]
    public class BikeSound
    {
        public string id, character, nameEn;
        /// <summary>Clip ids in Resources/AronSfx (loops, no take suffix): engine_scooter_low ...</summary>
        public string low, mid, high;
        /// <summary>Playback speed at idle and at full revs, as a pitch multiplier (the pitch and speed move together).</summary>
        public float pitchIdle = 0.8f, pitchTop = 1.5f;
        /// <summary>Extra high-end volume from the whiny bikes at full revs, 0..1.</summary>
        public float whine;
        public float volume = 0.7f;
        /// <summary>Gears for the cartoon "putt-putt" shift dips, and how fast the revs follow the throttle (per second).</summary>
        public int gears = 3;
        public float rpmRise = 5f, rpmFall = 3f;
    }

    [Serializable]
    public class EngineOneShot
    {
        public string id;
        /// <summary>start, boost, brake, land.</summary>
        public string trigger;
        public int takes = 1;
        public float volume = 1f, cooldown = 0.5f;
        public int maxVoices = 2;
    }

    [Serializable]
    public class EngineAudioFile
    {
        public int version = 1;
        /// <summary>How many engines (the player's first) may sound at once on Low, Medium and High tier phones.</summary>
        public int voicesLow = 3, voicesMedium = 5, voicesHigh = 8;
        public int oneShotPool = 6;
        public BikeSound[] bikes = Array.Empty<BikeSound>();
        public EngineOneShot[] oneShots = Array.Empty<EngineOneShot>();

        public static readonly string[] Triggers = { "start", "boost", "brake", "land" };

        public BikeSound Bike(string id)
        {
            foreach (var b in bikes) if (b.id == id) return b;
            return bikes.Length > 0 ? bikes[0] : null;
        }

        public EngineOneShot OneShot(string trigger)
        {
            foreach (var o in oneShots) if (o.trigger == trigger) return o;
            return null;
        }

        public int Voices(QualityTier t) => t == QualityTier.Low ? voicesLow : t == QualityTier.Medium ? voicesMedium : voicesHigh;

        public List<string> Validate()
        {
            var e = new List<string>();
            var ids = new HashSet<string>();
            foreach (var b in bikes)
            {
                string w = "bike " + b.id;
                if (string.IsNullOrEmpty(b.id) || !ids.Add(b.id)) e.Add(w + ": id");
                if (string.IsNullOrEmpty(b.low) || string.IsNullOrEmpty(b.mid) || string.IsNullOrEmpty(b.high)) e.Add(w + ": three loops");
                if (b.pitchIdle <= 0.3f || b.pitchTop > 2.5f || b.pitchTop <= b.pitchIdle) e.Add(w + ": pitch idle < top, within 0.3..2.5");
                if (b.whine < 0f || b.whine > 1f || b.volume <= 0f || b.volume > 1f) e.Add(w + ": whine and volume 0..1");
                if (b.gears < 1 || b.gears > 6) e.Add(w + ": 1..6 gears");
                if (b.rpmRise <= 0f || b.rpmFall <= 0f) e.Add(w + ": rpm speeds");
            }
            foreach (var need in new[] { "scooter", "dirt_bike", "cruiser", "sport_bike" }) if (!ids.Contains(need)) e.Add("missing bike " + need);
            foreach (var t in Triggers) if (OneShot(t) == null) e.Add("missing one-shot for " + t);
            foreach (var o in oneShots)
            {
                if (string.IsNullOrEmpty(o.id) || Array.IndexOf(Triggers, o.trigger) < 0) e.Add("one-shot " + o.id + ": id/trigger");
                if (o.takes < 1 || o.cooldown < 0f || o.maxVoices < 1) e.Add("one-shot " + o.id + ": takes, cooldown, voices");
            }
            if (!(voicesLow >= 2 && voicesLow <= 3 && voicesLow < voicesMedium && voicesMedium <= voicesHigh && voicesHigh <= 8)) e.Add("voices: low 2..3, then more, up to 8");
            if (oneShotPool < 2) e.Add("one-shot pool");
            return e;
        }
    }

    /// <summary>The numbers behind one engine's sound. No Unity types, so the mapping can be tested.</summary>
    public static class EngineMath
    {
        public const float IdleRpm = 0.12f;

        /// <summary>
        /// Revs 0..1 from speed 0..1 and throttle 0..1. Speed is split into gears; each gear climbs from low to high revs, so changing
        /// up drops the revs (the cartoon putt-putt shift). Standing still idles; throttle in the air still revs.
        /// </summary>
        public static float TargetRpm(float speed01, float throttle, int gears)
        {
            speed01 = Clamp01(speed01); throttle = Clamp01(throttle);
            if (speed01 <= 0.01f) return IdleRpm + (1f - IdleRpm) * 0.45f * throttle;
            int g = Math.Min(gears - 1, (int)(speed01 * gears));
            float inGear = speed01 * gears - g;
            float climb = 0.35f + 0.65f * Clamp01(inGear);
            if (g == gears - 1) climb = 0.35f + 0.65f * Clamp01(inGear * 1.0f);
            float r = IdleRpm + (1f - IdleRpm) * climb;
            return Clamp01(Math.Max(IdleRpm, r * (0.55f + 0.45f * throttle)));
        }

        /// <summary>Moves the revs toward the target, quicker up than down, so they never jump.</summary>
        public static float FollowRpm(float rpm, float target, float rise, float fall, float dt)
        {
            float rate = target > rpm ? rise : fall;
            float step = rate * dt;
            return rpm < target ? Math.Min(target, rpm + step) : Math.Max(target, rpm - step);
        }

        /// <summary>The loop's playback speed (and so its pitch) at these revs. Exponential, so equal rev steps sound like equal pitch steps.</summary>
        public static float Pitch(BikeSound b, float rpm01)
        {
            float r = Clamp01(rpm01);
            return b.pitchIdle * (float)Math.Pow(b.pitchTop / b.pitchIdle, r);
        }

        /// <summary>Volumes of the low, mid and high loops. They crossfade with equal power, so the sum of squares is 1 everywhere.</summary>
        public static void Mix(float rpm01, out float low, out float mid, out float high)
        {
            float r = Clamp01(rpm01) * 2f;   // 0..2: low .. mid .. high
            if (r <= 1f) { low = Cos(r); mid = Sin(r); high = 0f; }
            else { low = 0f; mid = Cos(r - 1f); high = Sin(r - 1f); }
        }

        private static float Cos(float x) => (float)Math.Cos(x * Math.PI / 2);
        private static float Sin(float x) => (float)Math.Sin(x * Math.PI / 2);

        /// <summary>Extra high-end volume of whiny bikes: nothing at low revs, growing with the square of the revs.</summary>
        public static float WhineGain(BikeSound b, float rpm01) => b.whine * Clamp01(rpm01) * Clamp01(rpm01);

        /// <summary>Distance fades the engine; far engines never get below the floor until they are cut by the voice limit.</summary>
        public static float DistanceVolume(float metres, float full = 6f, float silent = 60f)
        {
            if (metres <= full) return 1f;
            if (metres >= silent) return 0f;
            float t = (metres - full) / (silent - full);
            return (1f - t) * (1f - t);
        }

        /// <summary>The brake squeak plays when speed falls fast: a drop of this share of top speed per second.</summary>
        public static bool ShouldSqueak(float speedBefore01, float speedAfter01, float dt, float minSpeed = 0.4f, float dropPerSecond = 0.9f)
            => dt > 0f && speedBefore01 >= minSpeed && (speedBefore01 - speedAfter01) / dt >= dropPerSecond;

        /// <summary>The landing boing needs a real fall; the volume grows with it.</summary>
        public static float BoingVolume(float fallSpeed, float minFall = 4f, float fullFall = 14f)
        {
            if (fallSpeed < minFall) return 0f;
            return 0.35f + 0.65f * Clamp01((fallSpeed - minFall) / (fullFall - minFall));
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>One engine's revs over time.</summary>
    public class EngineState
    {
        public readonly BikeSound bike;
        public float Rpm { get; private set; } = EngineMath.IdleRpm;
        private float prevSpeed;

        public EngineState(BikeSound b) { bike = b; }

        public void Update(float speed01, float throttle, float dt)
        {
            float target = EngineMath.TargetRpm(speed01, throttle, bike.gears);
            Rpm = EngineMath.FollowRpm(Rpm, target, bike.rpmRise, bike.rpmFall, dt);
            prevSpeed = speed01;
        }

        public float Pitch => EngineMath.Pitch(bike, Rpm);
        public float PrevSpeed => prevSpeed;
    }

    /// <summary>
    /// Which engines sound. Every engine is simulated cheaply, but only the nearest few get audio sources (the player's own always does),
    /// by graphics tier: 3 on low-end, 5 on mid, all 8 on high. An engine that already sounds keeps its place unless another is clearly closer.
    /// </summary>
    public static class EngineVoices
    {
        /// <param name="distances">Metres from the listener per engine; engine 0 is the player's.</param>
        /// <param name="previouslyAudible">The result of the last call (may be null). Used so engines don't flicker on and off.</param>
        public static bool[] Audible(IList<float> distances, int limit, bool[] previouslyAudible = null)
        {
            int n = distances.Count;
            var result = new bool[n];
            if (n == 0 || limit <= 0) return result;
            var order = new List<int>();
            for (int i = 1; i < n; i++) order.Add(i);
            order.Sort((a, b) => Score(distances[a], previouslyAudible != null && a < previouslyAudible.Length && previouslyAudible[a])
                .CompareTo(Score(distances[b], previouslyAudible != null && b < previouslyAudible.Length && previouslyAudible[b])));
            result[0] = true;
            int left = limit - 1;
            foreach (var i in order) { if (left <= 0) break; result[i] = true; left--; }
            return result;
        }

        private static float Score(float distance, bool wasAudible) => wasAudible ? distance * 0.85f : distance;
    }

    /// <summary>Gates the one-shot sounds (sputter, backfire, squeak, boing): a cooldown per sound and a pooled voice limit.</summary>
    public class EngineOneShots
    {
        private readonly EngineAudioFile cfg;
        private readonly SfxVoiceLimiter pool;
        private readonly Dictionary<string, float> last = new Dictionary<string, float>();
        private readonly Random rnd;

        public EngineOneShots(EngineAudioFile file, int seed = 1) { cfg = file; pool = new SfxVoiceLimiter(file.oneShotPool); rnd = new Random(seed); }

        /// <returns>The pool voice and the take to play, or voice -1 when it is too soon.</returns>
        public (int voice, int take, float volume) Trigger(string trigger, float now, float duration = 0.6f, float strength = 1f)
        {
            var o = cfg.OneShot(trigger);
            if (o == null) return (-1, 0, 0f);
            if (last.TryGetValue(o.id, out var t) && now - t < o.cooldown) return (-1, 0, 0f);
            last[o.id] = now;
            int voice = pool.Acquire(o.id, now, duration, o.maxVoices);
            return (voice, rnd.Next(o.takes), o.volume * Math.Max(0f, Math.Min(1f, strength)));
        }
    }
}
