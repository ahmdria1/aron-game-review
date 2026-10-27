// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Audio;
using Aron.Save;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>Short-hand for game code: Sfx.Play("crate_break", position).</summary>
    public static class Sfx
    {
        public static void Play(string id) => SfxPlayer.Instance.Play(id, null, 1f);
        public static void Play(string id, Vector3 position) => SfxPlayer.Instance.Play(id, position, 1f);
        public static void Play(string id, Vector3 position, float volume) => SfxPlayer.Instance.Play(id, position, volume);
        public static void PlayDelayed(string id, float delay, float volume = 1f) => SfxPlayer.Instance.PlayDelayed(id, delay, volume);
    }

    /// <summary>
    /// Pooled one-shot player. Every take gets a little pitch and volume jitter, each id has a voice cap,
    /// and the whole pool follows the sfx volume from the save file.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private int voices = 16;

        private static SfxPlayer instance;
        private AudioSource[] pool;
        private float[] baseVolume;
        private SfxVoiceLimiter limiter;
        private readonly SfxVariantPicker picker = new SfxVariantPicker();
        private Transform listener;

        public static SfxPlayer Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[SfxPlayer]");
                    instance = go.AddComponent<SfxPlayer>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>Volume from settings, 0 when muted.</summary>
        public static float SettingsVolume => SaveSystem.Data.sfxVolume;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            pool = new AudioSource[voices];
            baseVolume = new float[voices];
            limiter = new SfxVoiceLimiter(voices);
            for (int i = 0; i < voices; i++)
            {
                var child = new GameObject("Voice" + i);
                child.transform.SetParent(transform, false);
                var s = child.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 6f;
                s.maxDistance = 45f;
                s.dopplerLevel = 0f;
                pool[i] = s;
            }
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        private void LateUpdate()
        {
            float v = SettingsVolume;
            for (int i = 0; i < pool.Length; i++)
                if (pool[i].isPlaying) pool[i].volume = baseVolume[i] * v;
        }

        public void Play(string id, Vector3? position, float volume)
        {
            if (string.IsNullOrEmpty(id)) return;
            Aron.Core.GameEvents.RaiseSoundPlayed(id);
            if (!SfxLibrary.Active.TryGet(id, out var e)) return;
            float settings = SettingsVolume;
            if (settings <= 0f || volume <= 0f) return;
            var clip = e.Clip(picker.Next(id, e.ClipCount));
            if (clip == null) return;

            float pitch = 1f + picker.Range(-e.pitchJitter, e.pitchJitter);
            int slot = limiter.Acquire(id, Time.unscaledTime, clip.length / pitch, e.maxVoices);
            var s = pool[slot];
            s.Stop();
            s.clip = clip;
            s.loop = false;
            s.pitch = pitch;
            baseVolume[slot] = Mathf.Clamp01(e.volume * volume * (1f - picker.Range(0f, e.volumeJitter)));
            s.volume = baseVolume[slot] * settings;
            if (position.HasValue)
            {
                s.transform.position = position.Value;
                s.spatialBlend = e.spatial;
            }
            else
            {
                s.transform.localPosition = Vector3.zero;
                s.spatialBlend = 0f;
            }
            s.Play();
        }

        public void PlayDelayed(string id, float delay, float volume)
        {
            if (delay <= 0f) { Play(id, null, volume); return; }
            StartCoroutine(Delayed(id, delay, volume));
        }

        private System.Collections.IEnumerator Delayed(string id, float delay, float volume)
        {
            yield return new WaitForSeconds(delay);
            Play(id, null, volume);
        }
    }
}
