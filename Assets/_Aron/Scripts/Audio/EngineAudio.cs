// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Audio;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>
    /// The funny putt-putt engine of one bike. Three looping clips (low, mid, high revs) crossfade by the revs, and the pitch and
    /// speed climb with them. Put one on each bike (the player's is index 0 of the group), give it speed and throttle each frame,
    /// and call OnStart, OnBoost, OnLand (brake squeak is automatic). All the maths lives in Aron.Logic.Audio.EngineMath.
    /// The clips are in Resources/AronSfx (see docs/engine_sounds.md).
    /// </summary>
    public class EngineAudio : MonoBehaviour
    {
        public const string ConfigPath = "Audio/engine_audio";

        [SerializeField] private string bikeId = "scooter";
        [SerializeField] private bool isPlayer;

        /// <summary>Set by the bike's controller each frame: speed as a share of top speed, throttle 0..1, and whether it is in the air.</summary>
        public float Speed01 { get; set; }
        public float Throttle { get; set; }
        public float FallSpeed { get; set; }

        private static EngineAudioFile config;
        private static readonly List<EngineAudio> all = new List<EngineAudio>();
        private static float lastSelect = -1f;
        private static bool[] audible = new bool[0];
        private static EngineOneShots oneShots;
        private static AudioSource[] shotPool;
        private static int shotNext;

        private BikeSound bike;
        private EngineState state;
        private AudioSource low, mid, high;
        private bool wasAudible;
        private float prevSpeed;

        public static EngineAudioFile Config
        {
            get
            {
                if (config == null)
                {
                    var t = Resources.Load<TextAsset>(ConfigPath);
                    config = t != null ? JsonUtility.FromJson<EngineAudioFile>(t.text) : new EngineAudioFile();
                    if (config.Validate().Count > 0) global::Aron.Logic.Diagnostics.Log.Warn("engine_audio.json: " + string.Join("; ", config.Validate()));
                }
                return config;
            }
        }

        public void SetBike(string id, bool player) { bikeId = id; isPlayer = player; Setup(); }

        private void OnEnable() { Setup(); all.Add(this); }
        private void OnDisable() { all.Remove(this); Silence(); }

        private void Setup()
        {
            bike = Config.Bike(bikeId);
            if (bike == null) { enabled = false; return; }
            state = new EngineState(bike);
        }

        private AudioSource Loop(string clipId)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = Resources.Load<AudioClip>(SfxLibrary.Folder + clipId);
            s.loop = true; s.playOnAwake = false; s.volume = 0f; s.spatialBlend = isPlayer ? 0f : 0.9f;
            s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 6f; s.maxDistance = 60f;
            // each engine starts at a different point so identical bikes do not phase together
            if (s.clip != null) { s.time = Random.value * s.clip.length * 0.99f; s.Play(); }
            return s;
        }

        private void Silence()
        {
            foreach (var s in new[] { low, mid, high }) if (s != null) { s.Stop(); Destroy(s); }
            low = mid = high = null; wasAudible = false;
        }

        private void Update()
        {
            if (state == null) return;
            float dt = Time.deltaTime;
            state.Update(Speed01, Throttle, dt);

            // brake squeak
            if (dt > 0f && EngineMath.ShouldSqueak(prevSpeed, Speed01, dt) && isPlayer) Shot("brake", transform.position, 1f);
            prevSpeed = Speed01;

            SelectVoices();
            int idx = all.IndexOf(this);
            bool on = idx >= 0 && idx < audible.Length && audible[idx];
            if (on && !wasAudible) { low = Loop(bike.low); mid = Loop(bike.mid); high = Loop(bike.high); wasAudible = true; }
            else if (!on && wasAudible) Silence();
            if (!on) return;

            EngineMath.Mix(state.Rpm, out var l, out var m, out var h);
            float master = bike.volume * Settings.EngineVolume() * (isPlayer ? 1f : EngineMath.DistanceVolume(Distance()));
            h *= 1f + EngineMath.WhineGain(bike, state.Rpm);
            float pitch = state.Pitch;
            Apply(low, l * master, pitch); Apply(mid, m * master, pitch); Apply(high, Mathf.Min(1f, h) * master, pitch);
        }

        private static void Apply(AudioSource s, float volume, float pitch) { if (s == null) return; s.volume = Mathf.Clamp01(volume); s.pitch = pitch; }

        private float Distance()
        {
            var cam = Camera.main;
            return cam != null ? Vector3.Distance(cam.transform.position, transform.position) : 0f;
        }

        /// <summary>Re-pick the audible engines a few times a second, not every frame: nearest N by graphics tier, the player first.</summary>
        private static void SelectVoices()
        {
            if (Time.unscaledTime - lastSelect < 0.2f) return;
            lastSelect = Time.unscaledTime;
            all.Sort((a, b) => b.isPlayer.CompareTo(a.isPlayer));
            var d = new float[all.Count];
            for (int i = 0; i < d.Length; i++) d[i] = all[i].isPlayer ? 0f : all[i].Distance();
            audible = EngineVoices.Audible(d, Config.Voices(Aron.Core.DeviceSettings.Tier), audible.Length == d.Length ? audible : null);
        }

        // ---- one-shots (start sputter, boost backfire, landing boing) ----

        public void OnStart() { Shot("start", transform.position, 1f); }
        public void OnBoost() { Shot("boost", transform.position, 1f); }
        public void OnLand(float fallSpeed) { float v = EngineMath.BoingVolume(fallSpeed); if (v > 0f) Shot("land", transform.position, v); }

        private void Shot(string trigger, Vector3 pos, float strength)
        {
            if (!isPlayer && Distance() > 35f) return;
            EnsurePool();
            var r = oneShots.Trigger(trigger, Time.unscaledTime, 0.8f, strength);
            if (r.voice < 0) return;
            var o = Config.OneShot(trigger);
            var clip = Resources.Load<AudioClip>(SfxLibrary.Folder + o.id + "_" + (r.take + 1));
            if (clip == null) return;
            var s = shotPool[shotNext++ % shotPool.Length];
            s.transform.position = pos;
            s.spatialBlend = isPlayer ? 0f : 0.9f;
            s.pitch = 1f + Random.Range(-0.05f, 0.05f);
            s.PlayOneShot(clip, Mathf.Clamp01(r.volume * Settings.EngineVolume()));
        }

        private static void EnsurePool()
        {
            if (oneShots != null && shotPool != null && shotPool[0] != null) return;
            oneShots = new EngineOneShots(Config, Random.Range(1, 9999));
            var root = new GameObject("[EngineOneShots]"); DontDestroyOnLoad(root);
            shotPool = new AudioSource[Config.oneShotPool];
            for (int i = 0; i < shotPool.Length; i++)
            {
                var c = new GameObject("Shot" + i); c.transform.SetParent(root.transform, false);
                shotPool[i] = c.AddComponent<AudioSource>(); shotPool[i].playOnAwake = false;
            }
        }

        private static class Settings
        {
            public static float EngineVolume() => Aron.Save.SaveSystem.Data.sfxVolume;
        }
    }
}
