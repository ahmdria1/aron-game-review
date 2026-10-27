// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using System.Collections.Generic;
using Aron.Core;
using Aron.Save;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>
    /// Persistent audio service. Loads clips by id from Resources/Audio/Music/{id}, Resources/Audio/Sfx/{id} and
    /// Resources/Audio/Voice/{id}. The ids the game uses are listed in <see cref="Logic.AudioCues"/>; a sound
    /// is simply not heard if its file is missing (a warning is logged for music).
    /// To move to Addressables later, replace <see cref="LoadClip"/> only.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        private AudioSource music, sfx;
        private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        private string currentTrack;
        private float musicLevel = 1f;
        private Coroutine fade;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[AudioManager]");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { var _ = Instance; }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            ApplyVolumes();
            GameEvents.SfxRequested += PlaySfxInternal;
        }

        private void OnDestroy() { if (instance == this) GameEvents.SfxRequested -= PlaySfxInternal; }

        public void ApplyVolumes()
        {
            var d = SaveSystem.Data;
            musicLevel = d.musicEnabled ? d.musicVolume : 0f;
            if (fade == null) music.volume = musicLevel;
            sfx.volume = d.sfxVolume;
        }

        public static void PlayMusic(string track) => Instance.PlayMusicInternal(track, true);

        /// <summary>Short one-shot cue (victory, game over) that replaces the current music and does not loop.</summary>
        /// <returns>False when the clip is missing, so callers can fall back to a sound effect.</returns>
        public static bool PlayStinger(string track) => Instance.PlayMusicInternal(track, false);
        public static void PlaySfx(string id) => Instance.PlaySfxInternal(id);

        /// <summary>The music track now playing (or just asked for), or null.</summary>
        public static string CurrentTrack => instance != null ? instance.currentTrack : null;

        private bool PlayMusicInternal(string track, bool loop)
        {
            if (string.IsNullOrEmpty(track)) return false;
            if (track == currentTrack) return true;
            var clip = LoadClip("Audio/Music/" + track);
            if (clip == null && Aron.Content.ContentService.IsRemoteTrack(track))
            {
                // A country theme that is not on the phone (yet): the base track for this kind of stage plays instead, never silence.
                string fallback = Aron.Content.ContentService.PlayableTrack(track);
                if (fallback != track && fallback != currentTrack) { track = fallback; clip = LoadClip("Audio/Music/" + track); }
                else if (fallback == currentTrack) return true;
            }
            if (clip == null) { global::Aron.Logic.Diagnostics.Log.Warn("[Audio] Missing music: " + track); return false; }
            currentTrack = track;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(SwapMusic(clip, loop));
            return true;
        }

        private IEnumerator SwapMusic(AudioClip clip, bool loop)
        {
            float outTime = music.isPlaying && loop ? 0.6f : 0.05f;
            float from = music.volume;
            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                music.volume = Mathf.Lerp(from, 0f, t / outTime);
                yield return null;
            }
            music.Stop();
            music.clip = clip;
            music.loop = loop;
            music.volume = musicLevel;
            music.Play();
            fade = null;
        }

        private void PlaySfxInternal(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var clip = LoadClip("Audio/Sfx/" + id);
            if (clip != null) sfx.PlayOneShot(clip);
        }

        /// <summary>An action bark from whoever is being played: Aron's line or Noura's (either may be null).</summary>
        public static void Bark(string aronLine, string nouraLine)
        {
            var team = Heroes.HeroTeam.Instance;
            bool noura = team != null && team.NouraActive;
            string id = noura ? nouraLine : aronLine;
            if (id != null) PlayVoice(id, noura ? "noura" : "aron");
        }

        /// <summary>A spoken line: the clip named in the voice table, else "{id}_{speaker}", else "{id}". Silent if none.</summary>
        public static void PlayVoice(string id, string speaker = null)
        {
            if (Instance != null && !string.IsNullOrEmpty(id)) Instance.PlayVoiceInternal(id, speaker);
        }

        private void PlayVoiceInternal(string id, string speaker)
        {
            if (string.IsNullOrEmpty(id)) return;
            Aron.Core.GameEvents.RaiseVoiceSpoken(id, speaker);
            AudioClip clip = null;
            // A recording named in VoiceLines.json wins; then files named after the line; nothing found is silence.
            string named = Lines.ClipFor(id);
            if (named != null) clip = LoadClip("Audio/Voice/" + named);
            if (clip == null && !string.IsNullOrEmpty(speaker)) clip = LoadClip("Audio/Voice/" + id + "_" + speaker);
            if (clip == null) clip = LoadClip("Audio/Voice/" + id);
            if (clip != null) sfx.PlayOneShot(clip, 1.1f);
        }

        private static Aron.Logic.Audio.VoiceLineFile lines;

        /// <summary>The voice line table from Resources/Audio/VoiceLines.json (empty if the file is missing).</summary>
        public static Aron.Logic.Audio.VoiceLineFile Lines
        {
            get
            {
                if (lines != null) return lines;
                var asset = Resources.Load<TextAsset>("Audio/VoiceLines");
                try { lines = asset != null ? JsonUtility.FromJson<Aron.Logic.Audio.VoiceLineFile>(asset.text) : null; }
                catch (System.Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Audio] VoiceLines.json: " + e.Message); }
                if (lines == null) lines = new Aron.Logic.Audio.VoiceLineFile();
                return lines;
            }
        }

        private string hurriedFrom;

        /// <summary>
        /// The bisht's untouchable spell: its own track if there is one, otherwise the stage music played faster.
        /// Off puts the stage music back as it was.
        /// </summary>
        public static void Hurry(bool on) => Instance.HurryInternal(on);

        private void HurryInternal(bool on)
        {
            if (on)
            {
                if (hurriedFrom != null) return;
                hurriedFrom = currentTrack ?? "";
                if (LoadClip("Audio/Music/" + Logic.AudioCues.InvincibleMusic) != null) PlayMusicInternal(Logic.AudioCues.InvincibleMusic, true);
                else music.pitch = 1.3f;
            }
            else
            {
                if (hurriedFrom == null) return;
                music.pitch = 1f;
                if (currentTrack == Logic.AudioCues.InvincibleMusic) PlayMusicInternal(hurriedFrom, true);
                hurriedFrom = null;
            }
        }

        private AudioClip LoadClip(string path)
        {
            if (cache.TryGetValue(path, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>(path);
            // Country music is not in Resources: it comes from the downloaded group, once that is on the phone.
            const string musicFolder = "Audio/Music/";
            if (c == null && path.StartsWith(musicFolder, System.StringComparison.Ordinal)) c = Aron.Content.ContentService.TryGetMusic(path.Substring(musicFolder.Length));
            if (c != null) cache[path] = c;
            return c;
        }
    }
}
