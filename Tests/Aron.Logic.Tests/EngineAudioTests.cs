// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Audio;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class EngineAudioTests
    {
        private static string Root()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "AronSfx"))) return Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
            Assert.Fail("Resources not found"); return null;
        }

        private static EngineAudioFile File_() =>
            JsonSerializer.Deserialize<EngineAudioFile>(System.IO.File.ReadAllText(Path.Combine(Root(), "Audio", "engine_audio.json")), new JsonSerializerOptions { IncludeFields = true });

        [Test]
        public void ConfigIsValidAndEveryClipExists()
        {
            var f = File_();
            Assert.That(f.Validate(), Is.Empty);
            foreach (var b in f.bikes)
                foreach (var id in new[] { b.low, b.mid, b.high })
                    Assert.That(System.IO.File.Exists(Path.Combine(Root(), "AronSfx", id + ".ogg")), Is.True, id);
            foreach (var o in f.oneShots)
                for (int i = 1; i <= o.takes; i++)
                    Assert.That(System.IO.File.Exists(Path.Combine(Root(), "AronSfx", o.id + "_" + i + ".ogg")), Is.True, o.id + "_" + i);
        }

        [Test]
        public void FourBikesWithTheirOwnCharacter()
        {
            var f = File_();
            Assert.That(f.bikes.Length, Is.EqualTo(4));
            var scooter = f.Bike("scooter"); var cruiser = f.Bike("cruiser"); var sport = f.Bike("sport_bike"); var dirt = f.Bike("dirt_bike");
            Assert.That(scooter.pitchIdle, Is.GreaterThan(cruiser.pitchIdle), "scooter buzzes high, cruiser blubs low");
            Assert.That(sport.whine, Is.GreaterThan(0.5f)); Assert.That(cruiser.whine, Is.EqualTo(0f));
            Assert.That(sport.pitchTop / sport.pitchIdle, Is.GreaterThan(cruiser.pitchTop / cruiser.pitchIdle), "sport bike climbs the most");
            Assert.That(dirt.character, Does.Contain("rattl"));
        }

        [Test]
        public void PitchRisesWithRevsAndStaysInsideTheBikeRange()
        {
            var b = File_().Bike("sport_bike");
            Assert.That(EngineMath.Pitch(b, 0f), Is.EqualTo(b.pitchIdle).Within(1e-4f));
            Assert.That(EngineMath.Pitch(b, 1f), Is.EqualTo(b.pitchTop).Within(1e-4f));
            float prev = 0f;
            for (float r = 0f; r <= 1.0001f; r += 0.05f) { float p = EngineMath.Pitch(b, r); Assert.That(p, Is.GreaterThan(prev)); prev = p; }
            Assert.That(EngineMath.Pitch(b, -3f), Is.EqualTo(b.pitchIdle).Within(1e-4f));
            Assert.That(EngineMath.Pitch(b, 9f), Is.EqualTo(b.pitchTop).Within(1e-4f));
            // exponential: equal rev steps are equal pitch ratios
            Assert.That(EngineMath.Pitch(b, 0.5f) / EngineMath.Pitch(b, 0.25f), Is.EqualTo(EngineMath.Pitch(b, 0.75f) / EngineMath.Pitch(b, 0.5f)).Within(1e-3f));
        }

        [Test]
        public void CrossfadeKeepsEqualPowerAndMovesFromLowToHigh()
        {
            for (float r = 0f; r <= 1.0001f; r += 0.02f)
            {
                EngineMath.Mix(r, out var l, out var m, out var h);
                Assert.That(l * l + m * m + h * h, Is.EqualTo(1f).Within(1e-4f), "r=" + r);
                Assert.That(Math.Min(l, h), Is.EqualTo(0f), "low and high never play together");
            }
            EngineMath.Mix(0f, out var l0, out _, out _); EngineMath.Mix(0.5f, out _, out var m5, out _); EngineMath.Mix(1f, out _, out _, out var h1);
            Assert.That(l0, Is.EqualTo(1f).Within(1e-4f)); Assert.That(m5, Is.EqualTo(1f).Within(1e-4f)); Assert.That(h1, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void WhineGrowsWithRevsAndOnlyOnWhinyBikes()
        {
            var f = File_();
            Assert.That(EngineMath.WhineGain(f.Bike("sport_bike"), 0.2f), Is.LessThan(EngineMath.WhineGain(f.Bike("sport_bike"), 0.9f)));
            Assert.That(EngineMath.WhineGain(f.Bike("cruiser"), 1f), Is.EqualTo(0f));
            Assert.That(EngineMath.WhineGain(f.Bike("sport_bike"), 1f), Is.GreaterThan(EngineMath.WhineGain(f.Bike("scooter"), 1f)));
        }

        [Test]
        public void RevsIdleWhenStillClimbWithSpeedAndDropOnAGearChange()
        {
            Assert.That(EngineMath.TargetRpm(0f, 0f, 3), Is.EqualTo(EngineMath.IdleRpm).Within(1e-4f));
            Assert.That(EngineMath.TargetRpm(0f, 1f, 3), Is.GreaterThan(EngineMath.IdleRpm), "revving on the spot");
            Assert.That(EngineMath.TargetRpm(0.3f, 1f, 3), Is.GreaterThan(EngineMath.TargetRpm(0.1f, 1f, 3)));
            float before = EngineMath.TargetRpm(0.3329f, 1f, 3), after = EngineMath.TargetRpm(0.3335f, 1f, 3);
            Assert.That(after, Is.LessThan(before - 0.2f), "shifting up drops the revs: putt-putt");
            for (float s = 0f; s <= 1f; s += 0.01f) Assert.That(EngineMath.TargetRpm(s, 1f, 4), Is.InRange(0f, 1f));
        }

        [Test]
        public void RevsFollowTheTargetWithoutJumpingAndFasterUpThanDown()
        {
            float up = EngineMath.FollowRpm(0.2f, 1f, 5f, 3f, 0.1f), down = EngineMath.FollowRpm(0.8f, 0f, 5f, 3f, 0.1f);
            Assert.That(up, Is.EqualTo(0.7f).Within(1e-4f)); Assert.That(down, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(EngineMath.FollowRpm(0.99f, 1f, 5f, 3f, 1f), Is.EqualTo(1f), "never overshoots");
            var e = new EngineState(File_().Bike("dirt_bike"));
            for (int i = 0; i < 300; i++) e.Update(1f, 1f, 0.016f);
            float high = e.Rpm;
            Assert.That(high, Is.GreaterThan(0.6f));
            Assert.That(e.Pitch, Is.GreaterThan(e.bike.pitchIdle));
        }

        [Test]
        public void OnlyTheNearestEnginesSoundAndTheEngineNumberZeroAlwaysDoes()
        {
            var f = File_();
            var d = new float[] { 40f, 5f, 12f, 3f, 80f, 20f, 9f, 30f };
            var low = EngineVoices.Audible(d, f.Voices(QualityTier.Low));
            Assert.That(low.Count(x => x), Is.EqualTo(3)); Assert.That(low[0], Is.True, "the player's engine always sounds");
            Assert.That(low[3] && low[1], Is.True, "the two nearest rivals");
            Assert.That(EngineVoices.Audible(d, f.Voices(QualityTier.Medium)).Count(x => x), Is.EqualTo(5));
            Assert.That(EngineVoices.Audible(d, f.Voices(QualityTier.High)).Count(x => x), Is.EqualTo(8));
            Assert.That(EngineVoices.Audible(d, 0).Count(x => x), Is.EqualTo(0));
            Assert.That(EngineVoices.Audible(new float[0], 3), Is.Empty);
        }

        [Test]
        public void AnEngineThatAlreadySoundsDoesNotFlickerForASliverOfDistance()
        {
            var d = new float[] { 0f, 10f, 10.5f, 30f };
            var first = EngineVoices.Audible(d, 2);
            Assert.That(first[1], Is.True);
            var d2 = new float[] { 0f, 10.4f, 10.3f, 30f };    // engine 2 is now a hair closer
            var second = EngineVoices.Audible(d2, 2, first);
            Assert.That(second[1], Is.True, "engine 1 keeps its voice");
            var d3 = new float[] { 0f, 20f, 8f, 30f };         // clearly closer: swaps
            Assert.That(EngineVoices.Audible(d3, 2, first)[2], Is.True);
        }

        [Test]
        public void OneShotsHaveCooldownsAndAPooledVoiceLimit()
        {
            var f = File_(); var o = new EngineOneShots(f, 3);
            var a = o.Trigger("boost", 0f);
            Assert.That(a.voice, Is.GreaterThanOrEqualTo(0)); Assert.That(a.take, Is.InRange(0, 1));
            Assert.That(o.Trigger("boost", 0.2f).voice, Is.EqualTo(-1), "cooldown");
            Assert.That(o.Trigger("boost", 1.0f).voice, Is.GreaterThanOrEqualTo(0));
            Assert.That(o.Trigger("nonsense", 5f).voice, Is.EqualTo(-1));
            var s1 = o.Trigger("start", 10f); var s2 = o.Trigger("start", 10.1f);
            Assert.That(s1.voice, Is.GreaterThanOrEqualTo(0)); Assert.That(s2.voice, Is.EqualTo(-1));
            Assert.That(o.Trigger("land", 20f, 0.6f, 0.5f).volume, Is.EqualTo(f.OneShot("land").volume * 0.5f).Within(1e-4f));
        }

        [Test]
        public void BrakeSqueakAndLandingBoingFireOnlyWhenTheyShould()
        {
            Assert.That(EngineMath.ShouldSqueak(0.8f, 0.3f, 0.3f), Is.True);
            Assert.That(EngineMath.ShouldSqueak(0.8f, 0.78f, 0.3f), Is.False, "just easing off");
            Assert.That(EngineMath.ShouldSqueak(0.2f, 0f, 0.1f), Is.False, "too slow to squeal");
            Assert.That(EngineMath.ShouldSqueak(0.9f, 0f, 0f), Is.False);
            Assert.That(EngineMath.BoingVolume(2f), Is.EqualTo(0f));
            Assert.That(EngineMath.BoingVolume(14f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(EngineMath.BoingVolume(9f), Is.InRange(0.4f, 0.9f));
        }

        [Test]
        public void DistanceFadesTheEngine()
        {
            Assert.That(EngineMath.DistanceVolume(0f), Is.EqualTo(1f));
            Assert.That(EngineMath.DistanceVolume(30f), Is.InRange(0.01f, 0.99f));
            Assert.That(EngineMath.DistanceVolume(100f), Is.EqualTo(0f));
        }
    }
}
