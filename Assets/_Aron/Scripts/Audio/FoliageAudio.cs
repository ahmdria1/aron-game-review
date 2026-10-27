// (c) Ahmed Alamrani. All rights reserved.
using Aron.Atmosphere;
using Aron.Logic.Sky;
using Aron.Player;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>
    /// The rustle that goes with the swaying: for palms and for other trees, a light, a moderate and a strong loop that crossfade as the
    /// wind rises, with volume and pitch following the wind and the distance to the nearest palm or tree, plus gust whooshes and an
    /// occasional frond creak in a gust. Sits next to <see cref="FoliageWind"/>.
    /// The mapping is <see cref="FoliageWindMath"/> (tested); this class only plays it. A low-end phone plays one loop per kind at a time,
    /// and reduce motion softens the gust sounds along with the gusts themselves (they follow the scaled gust).
    /// </summary>
    public class FoliageAudio : MonoBehaviour
    {
        private static readonly string[] PalmIds = { "palm_rustle_light", "palm_rustle_moderate", "palm_rustle_strong" };
        private static readonly string[] LeavesIds = { "leaves_rustle_light", "leaves_rustle_medium", "leaves_rustle_strong" };
        private const string CreakId = "frond_creak", GustId = "foliage_gust";

        private readonly AudioSource[] palm = new AudioSource[3], leaves = new AudioSource[3];
        private readonly float[] palmNow = new float[3], leavesNow = new float[3];
        private float palmTarget, leavesTarget, pitch = 1f;
        private float nextLook, nextCreak, nextGust;
        private Transform creakAt;
        private float creakDistance = float.MaxValue;
        private FoliageWind wind;

        private void Awake()
        {
            wind = GetComponent<FoliageWind>();
            for (int i = 0; i < 3; i++) { palm[i] = Make(PalmIds[i]); leaves[i] = Make(LeavesIds[i]); }
            nextCreak = Time.time + FoliageWindMath.NextCreakGap(FoliageWind.File, Random.value);
            nextGust = Time.time + FoliageWindMath.NextGustSoundGap(FoliageWind.File, Random.value);
        }

        private AudioSource Make(string id)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.priority = 112; s.volume = 0f;
            if (SfxLibrary.Active.TryGet(id, out var e)) s.clip = e.Clip(0);
            return s;
        }

        private void OnDisable()
        {
            for (int i = 0; i < 3; i++)
            {
                if (palm[i] != null) palm[i].Stop();
                if (leaves[i] != null) leaves[i].Stop();
            }
        }

        private void Update()
        {
            if (wind == null || palm[0] == null) return;
            var f = FoliageWind.File;

            // Where the listener is: the hero if there is one, otherwise the camera.
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + 0.2f;
                var p = PlayerController.Active;
                var cam = Camera.main;
                Vector3 at = p != null ? p.transform.position : cam != null ? cam.transform.position : Vector3.zero;
                float dp = FoliageWind.Nearest(at, FoliageKind.Palm, out var palmT);
                float dl = FoliageWind.Nearest(at, FoliageKind.Leaves, out var leafT);
                palmTarget = FoliageWindMath.RustleVolume(f, wind.Wind, dp);
                leavesTarget = FoliageWindMath.RustleVolume(f, wind.Wind, dl);
                pitch = FoliageWindMath.RustlePitch(f, wind.Wind);
                bool palmCloser = dp <= dl;
                creakAt = palmCloser ? palmT : leafT;
                creakDistance = Mathf.Min(dp, dl);
            }

            float settings = SfxPlayer.SettingsVolume;
            var mix = FoliageWindMath.LayerMix(f, wind.Wind, FoliageWindMath.UsesCheapPath(wind.Tier));
            var share = new[] { mix.Light, mix.Mid, mix.Strong };
            for (int i = 0; i < 3; i++)
            {
                Step(palm[i], ref palmNow[i], palmTarget * share[i], PalmIds[i], settings);
                Step(leaves[i], ref leavesNow[i], leavesTarget * share[i], LeavesIds[i], settings);
            }

            if (creakAt == null) return;
            float dt = 0.2f;
            if (Time.time >= nextGust)
            {
                if (FoliageWindMath.GustSoundDue(f, wind.Gust, creakDistance, Random.value, dt))
                {
                    Sfx.Play(GustId, creakAt.position, f.gustSoundVolume);
                    nextGust = Time.time + FoliageWindMath.NextGustSoundGap(f, Random.value);
                }
                else nextGust = Time.time + dt;
            }
            if (Time.time >= nextCreak)
            {
                if (FoliageWindMath.CreakDue(f, wind.Gust, creakDistance, Random.value, dt))
                {
                    Sfx.Play(CreakId, creakAt.position, f.creakVolume);
                    nextCreak = Time.time + FoliageWindMath.NextCreakGap(f, Random.value);
                }
                else nextCreak = Time.time + dt;
            }
        }

        private void Step(AudioSource s, ref float now, float target, string id, float settings)
        {
            if (s == null) return;
            now = Mathf.MoveTowards(now, target, Time.deltaTime * 0.6f);
            float vol = now * (SfxLibrary.Active.TryGet(id, out var e) ? e.volume : 1f) * settings;
            if (vol <= 0.002f) { if (s.isPlaying) s.Stop(); return; }
            if (s.clip == null) return;
            if (!s.isPlaying) { s.time = Random.Range(0f, s.clip.length * 0.9f); s.Play(); }
            s.volume = vol;
            s.pitch = pitch;
        }
    }
}
