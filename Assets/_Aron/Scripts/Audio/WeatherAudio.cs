// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Logic.Audio;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>
    /// Rain and sandstorm ambience. Drop one in a level, set the starting weather, and call SetWeather
    /// from triggers or the level script. Loops crossfade, thunder and gusts are scheduled on top.
    /// </summary>
    public class WeatherAudio : MonoBehaviour
    {
        [SerializeField] private Weather weather = Weather.Clear;
        [SerializeField] private float fadeSeconds = 3f;
        [SerializeField, Range(0f, 1f)] private float level = 1f;
        [Header("Rain")]
        [SerializeField] private bool autoThunder = true;
        [SerializeField] private Vector2 thunderGap = new Vector2(12f, 30f);
        [SerializeField, Range(0f, 1f)] private float nearThunderChance = 0.35f;
        [Header("Sandstorm")]
        [SerializeField] private bool autoGusts = true;
        [SerializeField] private Vector2 gustGap = new Vector2(4f, 10f);

        private static readonly string[] LayerIds = { "rain_light", "rain_heavy", "rain_roof", "sandstorm", "sand_hiss" };

        private WeatherMix mix;
        private AudioSource[] layers;
        private float[] layerVolume;
        private float nextThunder, nextGust;
        private bool underCover;
        // Wind and sea beds sit under the weather loops; the weather director sets their targets.
        private AudioSource windBed, wavesBed;
        private float windTarget, wavesTarget, windNow, wavesNow;

        public static WeatherAudio Current { get; private set; }
        public Weather Weather => weather;
        /// <summary>Off when the level flashes its own lightning and calls PlayThunder itself.</summary>
        public bool AutoThunder { get => autoThunder; set => autoThunder = value; }

        private void Awake()
        {
            mix = new WeatherMix(fadeSeconds);
            layers = new AudioSource[LayerIds.Length];
            layerVolume = new float[LayerIds.Length];
            for (int i = 0; i < LayerIds.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f;
                s.priority = 64;
                layers[i] = s;
                layerVolume[i] = SfxLibrary.Active.TryGet(LayerIds[i], out var e) ? e.volume : 1f;
            }
            windBed = Bed("wind_loop");
            wavesBed = Bed("waves_loop");
            mix.Set(weather, underCover, fadeSeconds);
            mix.Snap();
            ScheduleThunder();
            ScheduleGust();
        }

        private void OnEnable() => Current = this;
        private void OnDisable()
        {
            if (Current == this) Current = null;
            foreach (var s in layers) if (s != null) s.Stop();
            if (windBed != null) windBed.Stop();
            if (wavesBed != null) wavesBed.Stop();
        }

        private AudioSource Bed(string id)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.priority = 96; s.volume = 0f;
            if (SfxLibrary.Active.TryGet(id, out var e)) s.clip = e.Clip(0);
            return s;
        }

        /// <summary>Wind (0..1) and waves (0..1, zero away from water). They ease towards the values over a few seconds.</summary>
        public void SetBeds(float wind, float waves)
        {
            windTarget = Mathf.Clamp01(wind);
            wavesTarget = Mathf.Clamp01(waves);
        }

        private void StepBed(AudioSource s, ref float now, float target, string id, float settings)
        {
            if (s == null) return;
            now = Mathf.MoveTowards(now, target, Time.deltaTime / Mathf.Max(0.1f, fadeSeconds));
            float vol = now * now * (SfxLibrary.Active.TryGet(id, out var e) ? e.volume : 1f) * settings;
            if (vol <= 0.001f) { if (s.isPlaying) s.Stop(); return; }
            if (s.clip == null) return;   // clip not in the build yet: stay silent
            if (!s.isPlaying) { s.time = Random.Range(0f, s.clip.length * 0.9f); s.Play(); }
            s.volume = vol;
        }

        public void SetWeather(Weather next) => SetWeather(next, fadeSeconds);

        public void SetWeather(Weather next, float fade)
        {
            weather = next;
            mix.Set(weather, underCover, fade);
            ScheduleThunder();
            ScheduleGust();
        }

        /// <summary>Under an awning or tin roof: rain drums overhead and the open-air layers duck.</summary>
        public void SetUnderCover(bool covered)
        {
            if (covered == underCover) return;
            underCover = covered;
            mix.Set(weather, underCover, 0.8f);
        }

        /// <summary>Thunder after a delay, e.g. the gap between a lightning flash and its sound.</summary>
        public void PlayThunder(float delay) => StartCoroutine(Thunder(delay, delay < 1.5f));

        public void PlayThunder(float delay, bool near) => StartCoroutine(Thunder(delay, near));

        private IEnumerator Thunder(float delay, bool near)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Sfx.Play(near ? "thunder_near" : "thunder_far");
        }

        private void Update()
        {
            mix.Step(Time.deltaTime);
            float settings = SfxPlayer.SettingsVolume * level;
            for (int i = 0; i < layers.Length; i++)
            {
                float g = mix.Gain((WeatherLayer)i);
                var s = layers[i];
                if (g <= 0f)
                {
                    if (s.isPlaying) s.Stop();
                    continue;
                }
                if (!s.isPlaying)
                {
                    if (s.clip == null && SfxLibrary.Active.TryGet(LayerIds[i], out var e)) s.clip = e.Clip(0);
                    if (s.clip == null) continue;
                    s.time = Random.Range(0f, s.clip.length * 0.9f);
                    s.Play();
                }
                s.volume = g * g * layerVolume[i] * settings;
            }

            StepBed(windBed, ref windNow, windTarget * (underCover ? 0.5f : 1f), "wind_loop", settings);
            StepBed(wavesBed, ref wavesNow, wavesTarget, "waves_loop", settings);

            bool raining = weather == Weather.Rain || weather == Weather.HeavyRain;
            if (autoThunder && raining && Time.time >= nextThunder)
            {
                bool near = Random.value < (weather == Weather.HeavyRain ? nearThunderChance * 1.5f : nearThunderChance);
                Sfx.Play(near ? "thunder_near" : "thunder_far");
                ScheduleThunder();
            }
            if (autoGusts && weather == Weather.Sandstorm && Time.time >= nextGust)
            {
                Sfx.Play("wind_gust");
                ScheduleGust();
            }
        }

        private void ScheduleThunder() => nextThunder = Time.time + Random.Range(thunderGap.x, thunderGap.y);
        private void ScheduleGust() => nextGust = Time.time + Random.Range(gustGap.x, gustGap.y);
    }
}
