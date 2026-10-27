// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Sky;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class FoliageWindTests
    {
        private static FoliageWindFile F() => TestData.Read<FoliageWindFile>("Config", "foliage_wind.json");
        private static WeatherGameplayFile G() => TestData.Read<WeatherGameplayFile>("Config", "weather_gameplay.json");

        [Test] public void TheShippedFileIsValid() => Assert.That(F().Validate(), Is.Empty);

        [Test]
        public void WindMapsToSwayRisingSmoothlyAndNeverAboveTheCeiling()
        {
            var f = F();
            float prev = -1f;
            for (int i = 0; i <= 20; i++)
            {
                float a = FoliageWindMath.SwayAmplitude(f, i / 20f, 0f, 0f, false, 0.4f);
                Assert.That(a, Is.GreaterThanOrEqualTo(prev), "monotonic at " + i);
                Assert.That(a, Is.LessThanOrEqualTo(f.maxTotalSway + 1e-5f));
                prev = a;
            }
            Assert.That(FoliageWindMath.SwayAmplitude(f, 0f, 0f, 0f, false, 0.4f), Is.EqualTo(f.idleSway).Within(1e-6f), "a calm day still breathes a little");
            Assert.That(FoliageWindMath.SwayAmplitude(f, 1f, 0f, 0f, false, 0.4f), Is.EqualTo(f.maxSway).Within(1e-6f));
            // Out-of-range wind is clamped, not extrapolated.
            Assert.That(FoliageWindMath.SwayAmplitude(f, 5f, 1f, 1f, false, 0.4f), Is.LessThanOrEqualTo(f.maxTotalSway + 1e-5f));
            Assert.That(FoliageWindMath.SwayAmplitude(f, -1f, 0f, 0f, false, 0.4f), Is.EqualTo(f.idleSway).Within(1e-6f));
        }

        [Test]
        public void SandstormAndRainSwayHarderThanClearAtTheSameWind()
        {
            var f = F();
            float clear = FoliageWindMath.SwayAmplitude(f, 0.6f, 0f, 0f, false, 0.4f);
            Assert.That(FoliageWindMath.SwayAmplitude(f, 0.6f, 1f, 0f, false, 0.4f), Is.GreaterThan(clear));
            Assert.That(FoliageWindMath.SwayAmplitude(f, 0.6f, 0f, 1f, false, 0.4f), Is.GreaterThan(clear));
            foreach (WeatherKind k in System.Enum.GetValues(typeof(WeatherKind)))
            {
                var l = WeatherLook.Of(k);
                Assert.That(FoliageWindMath.SwayAmplitude(f, l.Wind, l.Dust, l.Rain, false, 0.4f), Is.LessThanOrEqualTo(f.maxTotalSway + 1e-5f), k.ToString());
            }
            var storm = WeatherLook.Of(WeatherKind.Sandstorm);
            Assert.That(FoliageWindMath.SwayAmplitude(f, storm.Wind, storm.Dust, storm.Rain, false, 0.4f),
                Is.GreaterThan(FoliageWindMath.SwayAmplitude(f, WeatherLook.Of(WeatherKind.Clear).Wind, 0f, 0f, false, 0.4f) * 5f));
        }

        [Test]
        public void ReduceMotionScalesSwayFlutterAndGustByTheSameShareAsTheWindPush()
        {
            var f = F(); var g = G();
            float share = g.reduceMotionWindShare;
            Assert.That(share, Is.EqualTo(0.4f).Within(1e-6f), "the shared setting");
            float full = FoliageWindMath.SwayAmplitude(f, 0.8f, 0f, 0f, false, share);
            float calm = FoliageWindMath.SwayAmplitude(f, 0.8f, 0f, 0f, true, share);
            Assert.That(calm, Is.EqualTo(full * share).Within(1e-6f));
            Assert.That(FoliageWindMath.FlutterAmplitude(f, 0.8f, 0f, 0f, true, share), Is.EqualTo(FoliageWindMath.FlutterAmplitude(f, 0.8f, 0f, 0f, false, share) * share).Within(1e-6f));
            var a = FoliageWindMath.Compute(f, QualityTier.High, 0.9f, 0f, 0f, 1f, 0f, false, share, 1.0, g.gustPeriodSeconds);
            var b = FoliageWindMath.Compute(f, QualityTier.High, 0.9f, 0f, 0f, 1f, 0f, true, share, 1.0, g.gustPeriodSeconds);
            Assert.That(b.Sway, Is.LessThan(a.Sway)); Assert.That(b.Flutter, Is.LessThan(a.Flutter)); Assert.That(b.Gust, Is.LessThanOrEqualTo(a.Gust));
        }

        [Test]
        public void TheLowTierUsesTheCheapSinglePathWithNoFlutterOrGust()
        {
            var f = F(); var g = G();
            Assert.That(FoliageWindMath.UsesCheapPath(QualityTier.Low), Is.True);
            Assert.That(FoliageWindMath.UsesCheapPath(QualityTier.Medium), Is.False);
            Assert.That(FoliageWindMath.UsesCheapPath(QualityTier.High), Is.False);
            Assert.That(FoliageWindMath.FlutterEnabled(QualityTier.Low), Is.False);
            var low = Enumerable.Range(0, 60).Select(i => FoliageWindMath.Compute(f, QualityTier.Low, 1f, 1f, 0f, 1f, 0f, false, 0.4f, i * 0.25, g.gustPeriodSeconds)).ToList();
            Assert.That(low.All(p => p.Flutter == 0f && p.Gust == 0f && p.Tier == 0), Is.True);
            Assert.That(low.All(p => p.Sway > 0f), Is.True, "it still sways");
            var mid = FoliageWindMath.Compute(f, QualityTier.Medium, 1f, 0f, 0f, 1f, 0f, false, 0.4f, 1.0, g.gustPeriodSeconds);
            var high = FoliageWindMath.Compute(f, QualityTier.High, 1f, 0f, 0f, 1f, 0f, false, 0.4f, 1.0, g.gustPeriodSeconds);
            Assert.That(mid.Flutter, Is.GreaterThan(0f)); Assert.That(high.Tier, Is.EqualTo(2)); Assert.That(mid.Tier, Is.EqualTo(1));
        }

        [Test]
        public void TheWindDirectionIsNormalisedAndFallsBackWhenZero()
        {
            var f = F();
            var p = FoliageWindMath.Compute(f, QualityTier.Medium, 0.5f, 0f, 0f, -4.8f, 2.1f, false, 0.4f, 0.0, 6f);
            Assert.That(p.DirX * p.DirX + p.DirZ * p.DirZ, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(p.DirX, Is.LessThan(0f));
            var z = FoliageWindMath.Compute(f, QualityTier.Medium, 0.5f, 0f, 0f, 0f, 0f, false, 0.4f, 0.0, 6f);
            Assert.That(z.DirX, Is.EqualTo(1f)); Assert.That(z.DirZ, Is.EqualTo(0f));
        }

        [Test]
        public void SwayWeightRisesFromTrunkToTip()
        {
            var f = F();
            Assert.That(FoliageWindMath.Weight(f, 0f), Is.EqualTo(0f));
            Assert.That(FoliageWindMath.Weight(f, 1f), Is.EqualTo(1f).Within(1e-6f));
            float prev = -1f;
            for (int i = 0; i <= 10; i++) { float w = FoliageWindMath.Weight(f, i / 10f); Assert.That(w, Is.GreaterThanOrEqualTo(prev)); prev = w; }
            Assert.That(FoliageWindMath.Weight(f, 0.3f), Is.LessThan(0.3f), "the base barely moves");
        }

        [Test]
        public void GustsPulseOnlyInRealWindAndStayInRange()
        {
            var f = F(); var g = G();
            Assert.That(Enumerable.Range(0, 100).Max(i => FoliageWindMath.GustPulse(f, i * 0.1, g.gustPeriodSeconds, 0.1f)), Is.EqualTo(0f), "calm: no gusts");
            var pulses = Enumerable.Range(0, 400).Select(i => FoliageWindMath.GustPulse(f, i * 0.05, g.gustPeriodSeconds, 1f)).ToList();
            Assert.That(pulses.Min(), Is.GreaterThanOrEqualTo(0f)); Assert.That(pulses.Max(), Is.LessThanOrEqualTo(1f));
            Assert.That(pulses.Max() - pulses.Min(), Is.GreaterThan(0.5f), "they come and go");
            var weaker = Enumerable.Range(0, 400).Max(i => FoliageWindMath.GustPulse(f, i * 0.05, g.gustPeriodSeconds, 0.4f));
            Assert.That(weaker, Is.LessThan(pulses.Max()));
        }

        [Test]
        public void SwayAndFlutterSpeedUpWithTheWind()
        {
            var f = F();
            Assert.That(FoliageWindMath.SwayFrequency(f, 1f), Is.GreaterThan(FoliageWindMath.SwayFrequency(f, 0f)));
            Assert.That(FoliageWindMath.FlutterFrequency(f, 1f), Is.GreaterThan(FoliageWindMath.FlutterFrequency(f, 0f)));
            Assert.That(FoliageWindMath.FlutterAmplitude(f, 1f, 0f, 0f, false, 0.4f), Is.GreaterThan(FoliageWindMath.FlutterAmplitude(f, 0f, 0f, 0f, false, 0.4f)));
        }

        [Test]
        public void RustleVolumeFollowsWindAndFadesWithDistance()
        {
            var f = F();
            Assert.That(FoliageWindMath.RustleVolume(f, 0f, 1f), Is.EqualTo(0f), "no wind, no rustle");
            Assert.That(FoliageWindMath.RustleVolume(f, f.rustleMinWind, 1f), Is.EqualTo(0f));
            float prev = 0f;
            for (int i = 1; i <= 10; i++)
            {
                float v = FoliageWindMath.RustleVolume(f, i / 10f, 2f);
                Assert.That(v, Is.GreaterThanOrEqualTo(prev)); Assert.That(v, Is.LessThanOrEqualTo(1f)); prev = v;
            }
            Assert.That(FoliageWindMath.RustleVolume(f, 1f, f.fullVolumeDistance), Is.EqualTo(f.rustleMaxVolume).Within(1e-5f));
            float last = float.MaxValue;
            for (float d = f.fullVolumeDistance; d <= f.audibleDistance; d += 1f)
            {
                float v = FoliageWindMath.RustleVolume(f, 0.8f, d);
                Assert.That(v, Is.LessThanOrEqualTo(last)); last = v;
            }
            Assert.That(FoliageWindMath.RustleVolume(f, 1f, f.audibleDistance), Is.EqualTo(0f));
            Assert.That(FoliageWindMath.RustleVolume(f, 1f, 500f), Is.EqualTo(0f));
        }

        [Test]
        public void PitchRisesWithWindAndCreaksNeedAGustAndSomeFoliageNearby()
        {
            var f = F();
            Assert.That(FoliageWindMath.RustlePitch(f, 1f), Is.GreaterThan(FoliageWindMath.RustlePitch(f, 0f)));
            Assert.That(FoliageWindMath.RustlePitch(f, 0f), Is.EqualTo(f.pitchCalm).Within(1e-6f));
            Assert.That(FoliageWindMath.CreakDue(f, 0.2f, 3f, 0.0001f, 1f), Is.False, "no gust, no creak");
            Assert.That(FoliageWindMath.CreakDue(f, 1f, f.creakMaxDistance + 1f, 0.0001f, 1f), Is.False, "too far");
            Assert.That(FoliageWindMath.CreakDue(f, 1f, 3f, 0.0001f, 1f), Is.True, "close, in a full gust, unlucky roll");
            Assert.That(FoliageWindMath.CreakDue(f, 1f, 3f, 0.999f, 0.2f), Is.False, "a high roll skips");
            // A harder gust creaks more often.
            int soft = Enumerable.Range(0, 1000).Count(i => FoliageWindMath.CreakDue(f, f.creakMinGust + 0.05f, 3f, i / 1000f, 1f));
            int hard = Enumerable.Range(0, 1000).Count(i => FoliageWindMath.CreakDue(f, 1f, 3f, i / 1000f, 1f));
            Assert.That(hard, Is.GreaterThan(soft));
            Assert.That(FoliageWindMath.NextCreakGap(f, 0f), Is.EqualTo(f.creakGapMin)); Assert.That(FoliageWindMath.NextCreakGap(f, 1f), Is.EqualTo(f.creakGapMax));
        }

        [Test]
        public void StrengthLoopsCrossfadeWithEqualPowerAsTheWindRises()
        {
            var f = F();
            var calm = FoliageWindMath.LayerMix(f, 0.05f, false);
            Assert.That(calm.Light, Is.EqualTo(1f).Within(1e-5f)); Assert.That(calm.Mid, Is.EqualTo(0f).Within(1e-5f)); Assert.That(calm.Strong, Is.EqualTo(0f).Within(1e-5f));
            var steady = FoliageWindMath.LayerMix(f, (f.layerMidAt + f.layerStrongAt) / 2f, false);
            Assert.That(steady.Mid, Is.EqualTo(1f).Within(1e-5f));
            var gale = FoliageWindMath.LayerMix(f, 1f, false);
            Assert.That(gale.Strong, Is.EqualTo(1f).Within(1e-5f)); Assert.That(gale.Light + gale.Mid, Is.EqualTo(0f).Within(1e-5f));
            float prevLight = 2f, prevStrong = -1f;
            for (int i = 0; i <= 100; i++)
            {
                var m = FoliageWindMath.LayerMix(f, i / 100f, false);
                Assert.That(m.Light * m.Light + m.Mid * m.Mid + m.Strong * m.Strong, Is.EqualTo(1f).Within(1e-4f), "equal power at " + i);
                Assert.That(m.Light, Is.LessThanOrEqualTo(prevLight + 1e-6f), "the light loop only fades out");
                Assert.That(m.Strong, Is.GreaterThanOrEqualTo(prevStrong - 1e-6f), "the strong loop only fades in");
                prevLight = m.Light; prevStrong = m.Strong;
            }
            // At the crossover points the two neighbours are equally loud.
            var a = FoliageWindMath.LayerMix(f, f.layerMidAt, false); Assert.That(a.Light, Is.EqualTo(a.Mid).Within(1e-4f));
            var b = FoliageWindMath.LayerMix(f, f.layerStrongAt, false); Assert.That(b.Mid, Is.EqualTo(b.Strong).Within(1e-4f));
            // Out-of-range wind is clamped.
            Assert.That(FoliageWindMath.LayerMix(f, -3f, false).Light, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(FoliageWindMath.LayerMix(f, 9f, false).Strong, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ALowEndPhonePlaysOneStrengthLoopAtATime()
        {
            var f = F();
            for (int i = 0; i <= 100; i++)
            {
                var m = FoliageWindMath.LayerMix(f, i / 100f, true);
                Assert.That(new[] { m.Light, m.Mid, m.Strong }.Count(v => v > 0f), Is.EqualTo(1), "one voice at " + i);
                Assert.That(m.Light + m.Mid + m.Strong, Is.EqualTo(1f).Within(1e-6f));
            }
            Assert.That(FoliageWindMath.LayerMix(f, 0.2f, true).Light, Is.EqualTo(1f));
            Assert.That(FoliageWindMath.LayerMix(f, 0.5f, true).Mid, Is.EqualTo(1f));
            Assert.That(FoliageWindMath.LayerMix(f, 0.9f, true).Strong, Is.EqualTo(1f));
        }

        [Test]
        public void GustWhooshesNeedARealGustAndSomeFoliageNearby()
        {
            var f = F();
            Assert.That(FoliageWindMath.GustSoundDue(f, f.gustSoundMinGust - 0.01f, 3f, 0.0001f, 1f), Is.False, "no gust, no whoosh");
            Assert.That(FoliageWindMath.GustSoundDue(f, 1f, f.gustSoundMaxDistance + 1f, 0.0001f, 1f), Is.False, "too far");
            Assert.That(FoliageWindMath.GustSoundDue(f, 1f, 3f, 0.0001f, 1f), Is.True);
            Assert.That(FoliageWindMath.GustSoundDue(f, 1f, 3f, 0.9999f, 0.2f), Is.False, "a high roll skips");
            int soft = Enumerable.Range(0, 1000).Count(i => FoliageWindMath.GustSoundDue(f, f.gustSoundMinGust + 0.05f, 3f, i / 1000f, 1f));
            int hard = Enumerable.Range(0, 1000).Count(i => FoliageWindMath.GustSoundDue(f, 1f, 3f, i / 1000f, 1f));
            Assert.That(hard, Is.GreaterThan(soft), "a harder gust is more likely to whoosh");
            Assert.That(FoliageWindMath.NextGustSoundGap(f, 0f), Is.EqualTo(f.gustSoundGapMin)); Assert.That(FoliageWindMath.NextGustSoundGap(f, 1f), Is.EqualTo(f.gustSoundGapMax));
        }

        [Test]
        public void TheShaderTheSoundsAndTheHooksAreAllThere()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestData.Resources(), ".."));
            string shader = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Resources", "Shaders", "AronFoliageWind.shader"));
            Assert.That(shader, Does.Contain("Shader \"Aron/FoliageWind\""));
            Assert.That(shader, Does.Contain("ARON_FOLIAGE_LOW")); Assert.That(shader, Does.Contain("multi_compile_instancing"));
            Assert.That(shader, Does.Contain("CBUFFER_START(UnityPerMaterial)"), "SRP Batcher needs the material block");
            Assert.That(shader, Does.Contain("ShadowCaster"));
            string hlsl = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Resources", "Shaders", "AronFoliageWind.hlsl"));
            foreach (var w in new[] { "_AronFoliageWind", "_AronFoliageFlutter", "_MASK_VERTEXCOLOR", "_MASK_UV2" }) Assert.That(hlsl, Does.Contain(w), w);
            foreach (var clip in new[] { "palm_rustle_light", "palm_rustle_moderate", "palm_rustle_strong", "leaves_rustle_light", "leaves_rustle_medium", "leaves_rustle_strong", "foliage_gust_1", "foliage_gust_2", "foliage_gust_3", "foliage_gust_4", "frond_creak_1", "frond_creak_2", "frond_creak_3" })
                Assert.That(System.IO.File.Exists(System.IO.Path.Combine(root, "Audio", "SFX", "Resources", "AronSfx", clip + ".ogg")), Is.True, clip);
            string lib = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Scripts", "Audio", "SfxLibrary.cs"));
            foreach (var id in new[] { "\"palm_rustle_light\"", "\"palm_rustle_moderate\"", "\"palm_rustle_strong\"", "\"leaves_rustle_light\"", "\"leaves_rustle_medium\"", "\"leaves_rustle_strong\"", "\"foliage_gust\"", "\"frond_creak\"" }) Assert.That(lib, Does.Contain(id), id);
            string imp = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Editor", "SfxImport.cs"));
            foreach (var loop in new[] { "/palm_rustle_light.", "/palm_rustle_moderate.", "/palm_rustle_strong.", "/leaves_rustle_light.", "/leaves_rustle_medium.", "/leaves_rustle_strong." }) Assert.That(imp, Does.Contain(loop), loop);
            string factory = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Scripts", "Stages", "LandmarkFactory.cs"));
            Assert.That(factory, Does.Contain("FoliageWind.Register")); Assert.That(factory, Does.Contain("FoliageFrustum"));
        }
    }
}
