// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Audio;
using Aron.Core;
using Aron.Logic;
using Aron.Logic.Races;
using Aron.Logic.Regions;
using Aron.Logic.Sky;
using Aron.Player;
using Aron.Save;
using Aron.Stages;
using UnityEngine;
using PlayMode = Aron.Logic.Sky.PlayMode;

namespace Aron.Atmosphere
{
    /// <summary>
    /// Runs the weather and sky for a stage. Picks the play mode (story, free play, race, time trial, boss), asks
    /// <see cref="WeatherPolicy"/> what weather that mode may have, and drives the sky dome, sun and moon, fog,
    /// clouds, rain and snow, lightning, wet ground, water, wind and the ambience beds from it.
    /// The sun and moon heights, the moon's phase and the way its lit edge faces are the real ones for today's date
    /// at the stage's place; only the compass bearing is turned so the sun or moon sits where the camera can see it.
    /// </summary>
    public class WeatherDirector : MonoBehaviour
    {
        /// <summary>Set by the race and time-trial screens before the stage loads; cleared when the stage ends.</summary>
        public static PlayMode? ModeOverride;
        public static WeatherDirector Current { get; private set; }
        /// <summary>Set only by the hidden performance benchmark: rain is forced on so every run is measured in the same weather.</summary>
        public static bool BenchmarkRain;

        private static WeatherTable table;
        private static RaceFile races;
        public static WeatherTable Table => table ?? (table = LoadJson<WeatherTable>("Weather/weather") ?? new WeatherTable());

        public PlayMode Mode { get; private set; }
        public WeatherState State { get; private set; }
        public WeatherLook Look { get; private set; }
        public float Wetness { get; private set; }
        public DayPhase Phase { get; private set; }
        public MoonPhase Moon { get; private set; }

        private StageBuilder builder;
        private StageArt art;
        private StageDef stage;
        private RegionDef region;
        private CountryClimate climate;
        private Light sun;
        private Color regionSky, ground;
        private WeatherQuality quality;
        private QualityTier tier;
        private WeatherSettings settings;
        private string trackWeather = "";
        private float lat, lon, baseWind;
        private DateTime skyStart;
        private float startTime, nextTick, lastTick;
        private bool nearWater, authoredRain, authoredSand, setTraction;

        private SkyDome dome;
        private RainCurtain rain, snow;
        private ParticleSystem rainFallback, dust, splashes;
        private CloudBillboards clouds;
        private WetSurfaces wet;
        private WeatherAudio audioMix;
        private FoliageWind foliage;
        private Material water, pond;
        private PlanarReflection planar;
        private Light flashLight;
        private float baseSunIntensity;
        private Color baseAmbient;
        private WeatherKind lastMain = (WeatherKind)(-1);

        private List<Strike> strikes = new List<Strike>();
        private int nextStrike;
        private float stormStart = -1f;

        private static readonly int WindGlobal = Shader.PropertyToID("_AronWind");

        // ---------------------------------------------------------------- set-up

        /// <summary>Added by <c>StageConditions.Apply</c> before the stage streams in, so water picks up its material.</summary>
        public static WeatherDirector Attach(StageBuilder b, StageArt art, StageDef s, RegionDef r, Light sun, Color sky, Color ground)
        {
            var d = b.World.gameObject.AddComponent<WeatherDirector>();
            d.Init(b, art, s, r, sun, sky, ground);
            return d;
        }

        private void Init(StageBuilder b, StageArt a, StageDef s, RegionDef r, Light light, Color sky, Color groundColor)
        {
            Current = this;
            builder = b; art = a; stage = s; region = r; sun = light; regionSky = sky; ground = groundColor;
            tier = DeviceSettings.Tier;
            quality = WeatherQuality.For(tier);
            var data = SaveSystem.Data;
            if (data.weather == null) data.weather = new WeatherSettings();
            settings = data.weather;
            Mode = ModeFor(s, data);
            string country = r != null && !string.IsNullOrEmpty(r.country) ? r.country : "saudi";
            string regionId = r != null ? r.id : "";
            climate = Table.Country(country);
            Table.Place(country, regionId, out lat, out lon);
            baseWind = Table.WindFor(country, regionId);
            if (s.IsRace)
            {
                if (races == null) races = LoadJson<RaceFile>("Races/tracks");
                trackWeather = races?.Track(s.race)?.weather ?? "clear";
            }
            authoredRain = s.weather == "rain";
            authoredSand = s.weather == "sandstorm";
            nearWater = s.terrain == "coast" || s.terrain == "river";

            // Sky clock: today's date at the stage's place, at the stage's authored time of day.
            double offset = climate != null ? climate.utcOffset : 3.0;
            var localDate = DateTime.UtcNow.AddHours(offset).Date;
            skyStart = Astronomy.StageInstant(s.time, localDate, offset, lat, lon);
            startTime = Time.time;

            // Water first: the stage builds its shoulders and harbours right after this.
            var profile = Table.Water(Table.WaterFor(country, regionId, s.terrain));
            var still = Table.Water("river");
            var q = new QualityTierSettings { VertexWaves = quality.VertexWaves, Reflections = quality.ReflectionProbe };
            water = WaterLook.Make(profile, q, false);
            pond = WaterLook.Make(still ?? profile, q, true);
            art.WaterMaterial = water;
            art.PondMaterial = pond;
            art.WaterVertexWaves = quality.VertexWaves;
            if (nearWater && water != null && WeatherQuality.PlanarReflections(tier, settings))
                planar = PlanarReflection.Create(transform, -0.29f, water, pond);

            dome = SkyDome.Create();
            if (quality.VolumetricClouds) clouds = CloudBillboards.Create(transform, quality.CloudLayers);
            wet = gameObject.AddComponent<WetSurfaces>();
            wet.Init(b, a, quality.WetSpecular, quality.PuddleRipples ? (tier == QualityTier.High ? 50 : 26) : 0);
            gameObject.AddComponent<WindSway>().Tier = tier;
            foliage = gameObject.AddComponent<FoliageWind>();
            foliage.Tier = tier;

            audioMix = GetComponent<WeatherAudio>();
            if (audioMix == null) audioMix = gameObject.AddComponent<WeatherAudio>();
            audioMix.AutoThunder = false;   // thunder follows our own strikes

            if (sun != null) baseSunIntensity = sun.intensity;
            baseAmbient = RenderSettings.ambientLight;
            if (quality.LightningLight)
            {
                flashLight = new GameObject("LightningLight").AddComponent<Light>();
                flashLight.transform.SetParent(transform, false);
                flashLight.type = LightType.Directional;
                flashLight.color = new Color(0.8f, 0.85f, 1f);
                flashLight.intensity = 0f;
                flashLight.shadows = LightShadows.None;
                flashLight.transform.rotation = Quaternion.Euler(60f, 20f, 0f);
                flashLight.enabled = false;
            }

            gameObject.AddComponent<WeatherGameplayRuntime>().Init(this, climate != null ? climate.climate : "");
            Tick(true);
            // Rain that is already falling when the stage opens has already wet the ground.
            Wetness = Look.Wetness > 0f && (Look.Rain > 0f) ? 1f : 0f;
        }

        /// <summary>Which rules apply to this stage. Race and time-trial screens can force their mode.</summary>
        public static PlayMode ModeFor(StageDef s, SaveData data)
        {
            if (ModeOverride.HasValue) return ModeOverride.Value;
            if (s == null) return PlayMode.Story;
            if (s.IsRace) return PlayMode.Race;
            if (s.boss) return PlayMode.Boss;
            return data != null && StageProgress.IsDone(data, s.id) ? PlayMode.FreePlay : PlayMode.Story;
        }

        private static T LoadJson<T>(string path) where T : class
        {
            var t = Resources.Load<TextAsset>(path);
            if (t == null) { global::Aron.Logic.Diagnostics.Log.Warn("[Weather] missing Resources/" + path + ".json"); return null; }
            try { return JsonUtility.FromJson<T>(t.text); }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Weather] bad " + path + ": " + e.Message); return null; }
        }

        // ---------------------------------------------------------------- per frame

        private void Update()
        {
            if (Time.time >= nextTick) Tick(false);
            Lightning();
        }

        private void Tick(bool first)
        {
            float dt = first ? 0f : Time.time - lastTick;
            lastTick = Time.time;
            nextTick = Time.time + 1f / Mathf.Max(1f, quality.UpdateHz);
            string country = climate != null ? climate.country : "saudi";
            var now = DateTime.UtcNow;
            var dynamicNow = WeatherSchedule.At(Table, country, region != null ? region.id : "", now);
            State = WeatherPolicy.Resolve(Mode, settings, stage.weather, trackWeather, dynamicNow);
            var look = State.Look;
            // Hard authored stages pour harder.
            if (authoredRain && stage.difficulty >= 6) look.Rain = Mathf.Max(look.Rain, 0.9f);
            // Authored rain stages always had the odd flash; they keep it, now under the reduce-flashing rule.
            if (authoredRain) look.Lightning = Mathf.Max(look.Lightning, 0.5f);
            if (BenchmarkRain) { look.Rain = Mathf.Max(look.Rain, 0.85f); look.Snow = 0f; }
            Look = look;
            Wetness = Logic.Sky.Wetness.Step(Wetness, look.Rain, look.Snow, dt);

            float wind = Mathf.Clamp01(baseWind * 0.6f + look.Wind * 0.8f);
            Shader.SetGlobalFloat(WindGlobal, wind);
            var windVec = new Vector3(-0.8f, 0f, 0.35f) * (wind * 6f);
            if (foliage != null) foliage.SetWeather(wind, look.Dust, look.Rain, new Vector2(windVec.x, windVec.z));

            Sky(look, wind);
            Fog(look);
            Precipitation(look, windVec);
            Dust(look, wind);
            WaterLook.Step(water, wind, look.Rain, quality.PuddleRipples);
            WaterLook.Step(pond, wind * 0.4f, look.Rain, quality.PuddleRipples);
            wet.Apply(Wetness, look.Rain, !authoredRain && quality.PuddleRipples);
            Ambience(look);
            Traction();

            if (State.Main != lastMain)
            {
                lastMain = State.Main;
                if (look.Lightning > 0f) PlanStorm(look.Lightning); else { strikes.Clear(); stormStart = -1f; }
                // Reflections pick up the new sky; cheap enough once per change on medium and up.
                if (quality.ReflectionProbe && dome.Live) DynamicGI.UpdateEnvironment();
            }
        }

        // ---------------------------------------------------------------- sky

        private void Sky(WeatherLook look, float wind)
        {
            var instant = skyStart.AddSeconds(Time.time - startTime);
            var sunPos = Astronomy.Sun(instant, lat, lon);
            var moonPos = Astronomy.MoonPosition(instant, lat, lon);
            Moon = Astronomy.Moon(instant);
            Phase = Astronomy.PhaseAt(instant, lat, lon);
            bool moonUp = moonPos.Altitude > 0;
            var sky = SkyLook.For(sunPos.Altitude, look.CloudCover, Mathf.Max(look.Haze, look.Dust * 0.8f), (float)Moon.Illumination, moonUp);

            // Turn the compass so the main light of the moment is somewhere useful on screen.
            double yaw;
            if (sunPos.Altitude > -4) yaw = sunPos.Azimuth - (sunPos.Altitude < 12 ? 20.0 : 150.0);
            else if (moonUp) yaw = moonPos.Azimuth - 30.0;
            else yaw = sunPos.Azimuth - 150.0;
            var sunDir = Dir(sunPos.Altitude, sunPos.Azimuth - yaw);
            var moonDir = Dir(moonPos.Altitude, moonPos.Azimuth - yaw);

            Color zenith = new Color(sky.ZenithR, sky.ZenithG, sky.ZenithB);
            Color horizon = new Color(sky.HorizonR, sky.HorizonG, sky.HorizonB);
            Color sunCol = new Color(sky.SunR, sky.SunG, sky.SunB);
            // A touch of the region's own sky colour by day keeps each country's look.
            float day = Mathf.Clamp01((float)sunPos.Altitude / 10f);
            zenith = Color.Lerp(zenith, regionSky, 0.2f * day);
            // Overcast skies go grey.
            var grey = new Color(0.55f, 0.57f, 0.6f) * Mathf.Max(0.08f, sky.SunIntensity + 0.15f);
            zenith = Color.Lerp(zenith, grey, look.CloudCover * 0.7f);
            horizon = Color.Lerp(horizon, grey * 1.1f, look.CloudCover * 0.5f);
            var dustCol = new Color(ground.r, ground.g * 0.85f, ground.b * 0.65f) * Mathf.Max(0.25f, sky.SunIntensity);
            Color hazeCol = Color.Lerp(horizon, dustCol, Mathf.Clamp01(look.Dust + look.Haze * 0.4f));
            HorizonColor = Color.Lerp(horizon, hazeCol, Mathf.Clamp01(look.Haze + look.Dust));

            float lightLevel = Mathf.Max(0.05f, sky.SunIntensity);
            var frame = new SkyDome.Frame
            {
                Zenith = zenith,
                Horizon = horizon,
                Ground = Color.Lerp(ground * 0.45f, horizon * 0.5f, 0.4f),
                Sun = sunCol,
                SunDir = sunDir,
                MoonDir = moonDir,
                MoonLit = (float)Moon.Illumination,
                MoonLimb = (float)(Astronomy.BrightLimbFromUp(instant, lat, lon) * Mathf.Deg2Rad),
                MoonVisible = moonUp ? Mathf.Clamp01((float)moonPos.Altitude / 3f) * (1f - 0.9f * look.CloudCover) * (1f - 0.6f * look.Haze) : 0f,
                Stars = sky.Stars * (1f - look.Fog),
                MilkyWay = sky.MilkyWay * (1f - look.Fog),
                StarSpin = (float)(LocalSiderealDegrees(instant, lon) - yaw) * Mathf.Deg2Rad,
                Cloud = look.CloudCover,
                CloudLit = Color.Lerp(new Color(0.25f, 0.27f, 0.33f), Color.Lerp(Color.white, sunCol, 0.35f), lightLevel / 1.15f),
                CloudShade = Color.Lerp(new Color(0.08f, 0.09f, 0.12f), new Color(0.5f, 0.53f, 0.6f), lightLevel / 1.15f) * (1f - 0.35f * look.Rain),
                HazeColor = hazeCol,
                Haze = Mathf.Clamp01(look.Haze * 0.6f + look.Dust * 0.8f + look.Fog * 0.5f),
                CloudDrift = new Vector2(0.002f + 0.01f * wind, 0.0008f),
                CloudLayer = true,
                Flash = Mathf.Max(0f, shownFlash) * 0.5f,
            };
            dome.Apply(frame, Camera.main);
            if (clouds != null)
                clouds.Apply(look.CloudCover, sunDir, frame.CloudLit, frame.CloudShade, frame.Haze, hazeCol, new Vector3(-0.8f, 0f, 0.35f) * (2f + 8f * wind), Time.time - lastTickSky);
            lastTickSky = Time.time;

            // Key light: the sun by day, the moon (dim and blue) by night, never fully dark so the run stays readable.
            if (sun != null)
            {
                Vector3 keyDir = sunPos.Altitude > -4 || !moonUp ? sunDir : moonDir;
                if (keyDir.y < 0.12f) keyDir = new Vector3(keyDir.x, 0.12f, keyDir.z).normalized;
                sun.transform.rotation = Quaternion.LookRotation(-keyDir);
                float night = Mathf.Clamp01((float)(-sunPos.Altitude - 4.0) / 8f);
                var moonLight = new Color(0.55f, 0.65f, 0.95f);
                float moonLevel = 0.22f + 0.12f * (moonUp ? (float)Moon.Illumination : 0f);
                sun.color = Color.Lerp(sunCol, moonLight, night);
                baseSunIntensity = Mathf.Lerp(sky.SunIntensity, moonLevel, night) * (1f - 0.35f * look.Rain) * (1f - 0.3f * look.Dust);
                sun.intensity = baseSunIntensity;
            }
            var flat = new Color(0.62f, 0.62f, 0.66f) * Mathf.Clamp(sky.SunIntensity / 1.15f, 0.25f, 1f);
            var ambient = Color.Lerp(flat, (zenith + horizon) * 0.5f, 0.3f);
            ambient = new Color(Mathf.Max(ambient.r, 0.16f), Mathf.Max(ambient.g, 0.18f), Mathf.Max(ambient.b, 0.3f)) * (1f - 0.25f * look.Rain);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            baseAmbient = ambient;
            RenderSettings.ambientLight = ambient;
            lightLevelNow = lightLevel;
        }

        private float lastTickSky, lightLevelNow = 1f;
        private Color HorizonColor = Color.gray;

        /// <summary>Unity direction for an altitude and a bearing (degrees). Bearing 0 is +z, 90 is +x.</summary>
        private static Vector3 Dir(double altitude, double bearing)
        {
            float a = (float)altitude * Mathf.Deg2Rad, b = (float)bearing * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Sin(a), Mathf.Cos(b) * Mathf.Cos(a));
        }

        private static double LocalSiderealDegrees(DateTime utc, double longitude)
        {
            double d = Astronomy.JulianDay(utc) - 2451545.0;
            double g = 280.46061837 + 360.98564736629 * d + longitude;
            g %= 360.0;
            return g < 0 ? g + 360.0 : g;
        }

        private void Fog(WeatherLook look)
        {
            float thick = Mathf.Clamp01(Mathf.Max(look.Fog, look.Dust * 0.95f, look.Haze * 0.45f, look.Rain * 0.35f, look.Snow * 0.4f));
            bool night = Phase == DayPhase.Night;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = HorizonColor * (night ? 0.6f : 1f);
            RenderSettings.fogStartDistance = Mathf.Lerp(60f, 5f, thick);
            RenderSettings.fogEndDistance = Mathf.Min(Mathf.Lerp(230f, 38f, thick), night ? 110f : 230f);
        }

        // ---------------------------------------------------------------- rain, snow, dust

        private void Precipitation(WeatherLook look, Vector3 windVec)
        {
            float light = Mathf.Clamp(lightLevelNow + 0.25f, 0.3f, 1.3f);
            if (look.Rain > 0.01f && rain == null && rainFallback == null)
            {
                rain = RainCurtain.Create(transform, quality.RainDrops, false);
                if (rain == null && builder.ParticleMaterial != null)
                {
                    // No custom shader in this build: the old particle rain.
                    rainFallback = Particles.Make("Rain", builder.ParticleMaterial, quality.RainDrops / 2, quality.RainDrops / 3f, new Vector3(22f, 0.5f, 26f),
                        new Vector3(0.6f, -16f, 0f), 0.05f, new Color(0.75f, 0.82f, 0.95f, 0.55f), 3f);
                    rainFallback.transform.SetParent(transform, false);
                    rainFallback.gameObject.AddComponent<FollowPlayer>().offset = new Vector3(0f, 12f, 6f);
                }
            }
            if (rain != null) { rain.Amount = look.Rain; rain.SetWind(windVec); rain.SetLight(light); }
            if (rainFallback != null) { var em = rainFallback.emission; em.rateOverTime = quality.RainDrops / 3f * look.Rain; }

            if (look.Snow > 0.01f && snow == null) snow = RainCurtain.Create(transform, quality.SnowFlakes, true);
            if (snow != null) { snow.Amount = look.Snow; snow.SetWind(windVec * 0.5f); snow.SetLight(light); }

            if (quality.Splashes > 0 && look.Rain > 0.05f && splashes == null && builder.ParticleMaterial != null)
            {
                splashes = Particles.Make("Splashes", builder.ParticleMaterial, quality.Splashes * 4, quality.Splashes * 6f, new Vector3(14f, 0.05f, 18f),
                    new Vector3(0f, 1.2f, 0f), 0.07f, new Color(0.85f, 0.9f, 1f, 0.5f), 0f);
                var main = splashes.main;
                main.startLifetime = 0.22f;
                splashes.transform.SetParent(transform, false);
                splashes.gameObject.AddComponent<FollowPlayer>().offset = new Vector3(0f, 0.08f, 5f);
            }
            if (splashes != null) { var em = splashes.emission; em.rateOverTime = quality.Splashes * 6f * look.Rain; }
        }

        private void Dust(WeatherLook look, float wind)
        {
            // Authored sandstorm stages bring their own dust; elsewhere blowing sand in strong desert wind.
            if (authoredSand || builder.ParticleMaterial == null) return;
            bool sandy = stage.terrain == "desert" || stage.terrain == "oasis";
            float amount = Mathf.Max(look.Dust, sandy ? Mathf.Clamp01((wind - 0.6f) * 1.2f) : 0f);
            if (amount <= 0.01f && dust == null) return;
            int max = tier == QualityTier.Low ? 200 : tier == QualityTier.Medium ? 500 : 1100;
            if (dust == null)
            {
                dust = Particles.Make("Dust", builder.ParticleMaterial, max, 0f, new Vector3(2f, 6f, 30f), new Vector3(-14f, 0.5f, -1f), 0.18f,
                    new Color(ground.r, ground.g * 0.9f, ground.b * 0.8f, 0.5f), 1.5f);
                dust.transform.SetParent(transform, false);
                dust.gameObject.AddComponent<FollowPlayer>().offset = new Vector3(14f, 2.5f, 6f);
            }
            var em = dust.emission;
            em.rateOverTime = max / 1.2f * amount;
        }

        // ---------------------------------------------------------------- lightning

        private void PlanStorm(float intensity)
        {
            int seed = (stage.id ?? "").GetHashCode() ^ (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute);
            strikes = Logic.Sky.Lightning.Plan(seed, 1800f, intensity);
            nextStrike = 0;
            stormStart = Time.time;
        }

        private void Lightning()
        {
            if (stormStart < 0f || strikes.Count == 0) { ApplyFlash(0f); return; }
            float t = Time.time - stormStart;
            bool reduce = Aron.Logic.Access.FlashSafety.FlashReduced(SaveSystem.Data.access);
            float flash = 0f;
            for (int i = Mathf.Max(0, nextStrike - 1); i < strikes.Count && strikes[i].Time <= t; i++)
            {
                var s = strikes[i];
                if (i >= nextStrike)
                {
                    nextStrike = i + 1;
                    if (audioMix != null) audioMix.PlayThunder(s.ThunderAt - s.Time, s.Near);
                }
                flash = Mathf.Max(flash, Logic.Sky.Lightning.Flash(s, t - s.Time, reduce));
            }
            ApplyFlash(flash * Look.Lightning);
        }

        private float shownFlash = -1f;
        private void ApplyFlash(float flash)
        {
            // Light and ambient are cheap to set; do it every frame so a tick in mid-flash cannot cut it short.
            if (!Mathf.Approximately(flash, shownFlash))
            {
                shownFlash = flash;
                dome?.SetFlash(flash * 0.5f);
                if (flashLight != null) { flashLight.enabled = flash > 0.001f; flashLight.intensity = flash; }
            }
            RenderSettings.ambientLight = baseAmbient * (1f + flash * 0.6f);
            if (sun != null) sun.intensity = baseSunIntensity * (1f + (flashLight == null ? flash * 0.5f : 0f));
        }

        // ---------------------------------------------------------------- audio and play

        private void Ambience(WeatherLook look)
        {
            if (audioMix == null) return;
            if (!authoredSand)
            {
                var mix = AmbienceBeds.MixWeather(State.Main);
                if (authoredRain && stage.difficulty >= 6 && mix == Logic.Audio.Weather.Rain) mix = Logic.Audio.Weather.HeavyRain;
                if (audioMix.Weather != mix) audioMix.SetWeather(mix);
            }
            var beds = AmbienceBeds.For(look, nearWater, false);
            audioMix.SetBeds(beds.Wind, beds.Waves);
        }

        private void Traction()
        {
            // Authored rain stages set their own grip; only the optional free-play effect is handled here.
            if (authoredRain) return;
            var p = PlayerController.Active;
            if (p == null) return;
            float grip = WeatherPolicy.Traction(Mode, settings, State);
            if (grip < 0.999f) { p.Traction = p.IsRiding ? Mathf.Max(grip, 0.8f) : grip; setTraction = true; }
            else if (setTraction) { p.Traction = 1f; setTraction = false; }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (setTraction && PlayerController.Active != null) PlayerController.Active.Traction = 1f;
            Shader.SetGlobalFloat(WindGlobal, 0f);
            dome?.Dispose();
            if (water != null) Destroy(water);
            if (pond != null) Destroy(pond);
        }
    }
}
