// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>
    /// Sound ids and how each one plays. Clips live in Resources/AronSfx as {id}.ogg for loops and
    /// {id}_1.ogg, {id}_2.ogg ... for takes. A SfxLibrary asset saved as Resources/AronSfx/SfxLibrary
    /// overrides the built-in table, so levels can be tuned without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Aron/Sfx Library", fileName = "SfxLibrary")]
    public class SfxLibrary : ScriptableObject
    {
        public const string Folder = "AronSfx/";

        [Serializable]
        public class Entry
        {
            public string id;
            [Min(0)] public int takes = 1;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.5f)] public float volumeJitter = 0.1f;
            [Range(0f, 0.5f)] public float pitchJitter = 0.06f;
            [Range(0f, 1f)] public float spatial = 0.6f;
            [Min(1)] public int maxVoices = 3;
            public bool loop;

            [NonSerialized] public AudioClip[] clips;

            public Entry() { }

            public Entry(string id, int takes, float volume, float pitchJitter, int maxVoices, float spatial = 0.6f, bool loop = false)
            {
                this.id = id; this.takes = takes; this.volume = volume; this.pitchJitter = pitchJitter;
                this.maxVoices = maxVoices; this.spatial = spatial; this.loop = loop;
            }

            public int ClipCount => loop ? 1 : Mathf.Max(1, takes);

            public AudioClip Clip(int take)
            {
                if (clips == null)
                {
                    clips = new AudioClip[ClipCount];
                    for (int i = 0; i < clips.Length; i++)
                        clips[i] = Resources.Load<AudioClip>(Folder + (loop ? id : id + "_" + (i + 1)));
                }
                return clips[Mathf.Clamp(take, 0, clips.Length - 1)];
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> lookup;
        private static SfxLibrary active;

        public static SfxLibrary Active
        {
            get
            {
                if (active == null)
                {
                    active = Resources.Load<SfxLibrary>(Folder + "SfxLibrary");
                    if (active == null)
                    {
                        active = CreateInstance<SfxLibrary>();
                        active.entries = Defaults();
                    }
                }
                return active;
            }
        }

        public bool TryGet(string id, out Entry entry)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Entry>();
                foreach (var e in entries) if (e != null && !string.IsNullOrEmpty(e.id)) lookup[e.id] = e;
            }
            return lookup.TryGetValue(id, out entry);
        }

        public IReadOnlyList<Entry> Entries => entries;

        private void OnValidate() => lookup = null;

        public static List<Entry> Defaults() => new List<Entry>
        {
            new Entry("rain_light", 1, 0.8f, 0f, 1, 0f, true),
            new Entry("rain_heavy", 1, 0.9f, 0f, 1, 0f, true),
            new Entry("rain_roof", 1, 0.7f, 0f, 1, 0f, true),
            new Entry("sandstorm", 1, 0.9f, 0f, 1, 0f, true),
            new Entry("sand_hiss", 1, 0.6f, 0f, 1, 0f, true),
            new Entry("thunder_near", 3, 1f, 0.05f, 2, 0f),
            new Entry("thunder_far", 2, 0.8f, 0.06f, 2, 0f),
            new Entry("wind_gust", 3, 0.7f, 0.1f, 2, 0f),
            new Entry("wind_loop", 1, 0.6f, 0f, 1, 0f, true),
            new Entry("waves_loop", 1, 0.7f, 0f, 1, 0f, true),
            new Entry("palm_rustle_light", 1, 0.5f, 0f, 1, 0f, true),
            new Entry("palm_rustle_moderate", 1, 0.55f, 0f, 1, 0f, true),
            new Entry("palm_rustle_strong", 1, 0.6f, 0f, 1, 0f, true),
            new Entry("leaves_rustle_light", 1, 0.45f, 0f, 1, 0f, true),
            new Entry("leaves_rustle_medium", 1, 0.5f, 0f, 1, 0f, true),
            new Entry("leaves_rustle_strong", 1, 0.55f, 0f, 1, 0f, true),
            new Entry("foliage_gust", 4, 0.7f, 0.08f, 2, 0.4f),
            new Entry("frond_creak", 3, 0.6f, 0.08f, 2, 0.8f),
            new Entry("puddle_step", 3, 0.55f, 0.12f, 2, 0.5f),
            new Entry("crate_break", 4, 0.9f, 0.08f, 4),
            new Entry("jar_break", 3, 0.85f, 0.08f, 3),
            new Entry("glass_break", 4, 0.8f, 0.07f, 3),
            new Entry("metal_break", 3, 0.9f, 0.06f, 3),
            new Entry("wall_crumble", 3, 0.95f, 0.06f, 2),
            new Entry("explosion", 2, 1f, 0.07f, 2, 0.4f),
            new Entry("boss_hit", 3, 1f, 0.06f, 3, 0.3f),
            new Entry("passport_stamp", 2, 0.9f, 0.05f, 2),
        };
    }
}
