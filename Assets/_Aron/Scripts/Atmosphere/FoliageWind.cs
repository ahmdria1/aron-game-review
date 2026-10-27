// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Audio;
using Aron.Logic;
using Aron.Logic.Sky;
using Aron.Save;
using UnityEngine;

namespace Aron.Atmosphere
{
    public enum FoliageKind { Palm, Leaves }

    /// <summary>
    /// Sets the global wind for the foliage shader (<c>AronFoliageWind.shader</c>) every frame from the weather state, and keeps the
    /// list of palms and trees the rustle sound listens for. <see cref="WeatherDirector"/> adds one per stage and hands it the wind,
    /// dust and rain each tick; the numbers come from <see cref="FoliageWindMath"/> and Resources/Config/foliage_wind.json.
    /// Reduce motion uses the same share as the wind push on the hero (<c>reduceMotionWindShare</c> in weather_gameplay.json).
    /// </summary>
    public class FoliageWind : MonoBehaviour
    {
        private static readonly int WindId = Shader.PropertyToID("_AronFoliageWind");
        private static readonly int FlutterId = Shader.PropertyToID("_AronFoliageFlutter");
        private const string LowKeyword = "ARON_FOLIAGE_LOW";

        private struct Spot { public Transform T; public FoliageKind Kind; }
        private static readonly List<Spot> Spots = new List<Spot>();

        private static FoliageWindFile file;
        public static FoliageWindFile File
        {
            get
            {
                if (file != null) return file;
                var t = Resources.Load<TextAsset>("Config/foliage_wind");
                try { file = t != null ? JsonUtility.FromJson<FoliageWindFile>(t.text) : null; } catch (System.Exception) { file = null; }
                if (file == null || file.Validate().Count > 0) file = new FoliageWindFile();
                return file;
            }
        }

        /// <summary>The landmark factory registers the root of every grove and palm row it builds (for the rustle sound).</summary>
        public static void Register(Transform t, FoliageKind kind)
        {
            if (t != null) Spots.Add(new Spot { T = t, Kind = kind });
        }

        public static void Clear() => Spots.Clear();
        public static int Count => Spots.Count;

        /// <summary>Distance in metres to the nearest registered palm or tree grove of this kind, and which one (null if none).</summary>
        public static float Nearest(Vector3 from, FoliageKind kind, out Transform which)
        {
            which = null;
            float best = float.MaxValue;
            for (int i = Spots.Count - 1; i >= 0; i--)
            {
                var s = Spots[i];
                if (s.T == null) { Spots.RemoveAt(i); continue; }   // an unloaded stage
                if (s.Kind != kind || !s.T.gameObject.activeInHierarchy) continue;
                float d = (s.T.position - from).sqrMagnitude;
                if (d < best) { best = d; which = s.T; }
            }
            return which == null ? float.MaxValue : Mathf.Sqrt(best);
        }

        public QualityTier Tier = QualityTier.Medium;
        public static FoliageWind Current { get; private set; }
        public float Wind => wind;
        public float Gust { get; private set; }
        public FoliageWindParams Params { get; private set; }

        private float wind, dust, rain;
        private float windTarget, dustTarget, rainTarget;
        private Vector2 dir = new Vector2(-0.8f, 0.35f);
        private bool first = true;

        /// <summary>Called by the weather director at its tick rate. Wind, dust and rain are 0..1; the vector is the downwind direction on the ground.</summary>
        public void SetWeather(float windStrength, float dustAmount, float rainAmount, Vector2 downwind)
        {
            windTarget = Mathf.Clamp01(windStrength); dustTarget = Mathf.Clamp01(dustAmount); rainTarget = Mathf.Clamp01(rainAmount);
            if (downwind.sqrMagnitude > 1e-6f) dir = downwind.normalized;
            if (first) { wind = windTarget; dust = dustTarget; rain = rainTarget; first = false; }
        }

        private void Awake()
        {
            Current = this;
            if (FoliageWindMath.UsesCheapPath(Tier)) Shader.EnableKeyword(LowKeyword); else Shader.DisableKeyword(LowKeyword);
            if (GetComponent<FoliageAudio>() == null) gameObject.AddComponent<FoliageAudio>();
        }

        private void Update()
        {
            // Ease towards the weather so a change of weather builds up over a few seconds instead of jumping.
            float step = Time.deltaTime * 0.35f;
            wind = Mathf.MoveTowards(wind, windTarget, step);
            dust = Mathf.MoveTowards(dust, dustTarget, step);
            rain = Mathf.MoveTowards(rain, rainTarget, step);

            var gp = WeatherGameplayRuntime.File;
            bool reduce = SaveSystem.Data != null && Aron.Logic.Access.FlashSafety.MotionReduced(SaveSystem.Data.access);
            var p = FoliageWindMath.Compute(File, Tier, wind, dust, rain, dir.x, dir.y, reduce, gp.reduceMotionWindShare, Time.timeAsDouble, gp.gustPeriodSeconds);
            Params = p;
            Gust = p.Gust;
            Shader.SetGlobalVector(WindId, new Vector4(p.DirX, p.DirZ, p.Sway, p.SwayHz));
            Shader.SetGlobalVector(FlutterId, new Vector4(p.Flutter, p.FlutterHz, p.Gust, File.gustShare));
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            // A calm world for any scene that has no weather director.
            Shader.SetGlobalVector(WindId, Vector4.zero);
            Shader.SetGlobalVector(FlutterId, Vector4.zero);
        }
    }
}
