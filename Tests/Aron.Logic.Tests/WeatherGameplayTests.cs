// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Sky;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class WeatherGameplayTests
    {
        private static WeatherGameplayFile F() => TestData.Read<WeatherGameplayFile>("Config", "weather_gameplay.json");

        [Test] public void TheShippedFileIsValid() => Assert.That(F().Validate(), Is.Empty);

        [Test]
        public void SandstormLowersVisibilityButNeverBelowTheFloor()
        {
            var f = F();
            float clear = WeatherGameplay.Visibility(f, WeatherLook.Of(WeatherKind.Clear));
            float storm = WeatherGameplay.Visibility(f, WeatherLook.Of(WeatherKind.Sandstorm));
            Assert.That(storm, Is.LessThan(clear));
            Assert.That(storm, Is.GreaterThanOrEqualTo(f.minVisibility));
            foreach (WeatherKind k in System.Enum.GetValues(typeof(WeatherKind)))
                Assert.That(WeatherGameplay.Visibility(f, WeatherLook.Of(k)), Is.GreaterThanOrEqualTo(f.minVisibility), k.ToString());
        }

        [Test]
        public void SandstormWindPushesInGustsAndNeverTooHard()
        {
            var f = F(); var look = WeatherLook.Of(WeatherKind.Sandstorm);
            var pushes = Enumerable.Range(0, 120).Select(i => WeatherGameplay.WindPush(f, look, i * 0.1, false, false)).ToList();
            Assert.That(pushes.Max(), Is.GreaterThan(pushes.Min() + 0.5f), "gusts come and go");
            Assert.That(pushes.Max(), Is.LessThanOrEqualTo(f.maxWindPush + 1e-4f));
            Assert.That(pushes.Min(), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void LightWindDoesNothingAndRidingOrReducedMotionCalmsIt()
        {
            var f = F();
            Assert.That(WeatherGameplay.WindPush(f, WeatherLook.Of(WeatherKind.Clear), 3.0, false, false), Is.EqualTo(0f));
            Assert.That(WeatherGameplay.WindPush(f, WeatherLook.Of(WeatherKind.Rain), 3.0, false, false), Is.EqualTo(0f));
            var s = WeatherLook.Of(WeatherKind.Sandstorm);
            float walk = WeatherGameplay.WindPush(f, s, 1.5, false, false);
            Assert.That(WeatherGameplay.WindPush(f, s, 1.5, true, false), Is.LessThan(walk));
            Assert.That(WeatherGameplay.WindPush(f, s, 1.5, false, true), Is.LessThan(walk));
        }

        [Test]
        public void RainIsSlipperyAndPuddlesSlowYouDown()
        {
            var f = F(); var rain = WeatherLook.Of(WeatherKind.Rain); var dry = WeatherLook.Of(WeatherKind.Clear);
            Assert.That(WeatherGameplay.Traction(f, dry, false), Is.EqualTo(1f));
            Assert.That(WeatherGameplay.Traction(f, rain, false), Is.LessThan(1f));
            Assert.That(WeatherGameplay.Traction(f, rain, true), Is.LessThan(WeatherGameplay.Traction(f, rain, false)));
            Assert.That(WeatherGameplay.Traction(f, rain, true), Is.GreaterThanOrEqualTo(0.25f), "slippery, never ice");
            Assert.That(WeatherGameplay.SpeedInPuddle(f, true), Is.LessThan(1f));
            Assert.That(WeatherGameplay.SpeedInPuddle(f, false), Is.EqualTo(1f));
        }

        [Test]
        public void HeatOnlyAppliesInDrySunnyDaylightInHotClimates()
        {
            var f = F(); var clear = WeatherLook.Of(WeatherKind.Clear);
            Assert.That(WeatherGameplay.HeatStrength(f, "desert", clear, true), Is.GreaterThan(0.8f));
            Assert.That(WeatherGameplay.HeatStrength(f, "desert", clear, false), Is.EqualTo(0f), "night is cool");
            Assert.That(WeatherGameplay.HeatStrength(f, "desert", WeatherLook.Of(WeatherKind.Rain), true), Is.EqualTo(0f));
            Assert.That(WeatherGameplay.HeatStrength(f, "oceanic", clear, true), Is.EqualTo(0f));
            Assert.That(WeatherGameplay.HeatStrength(f, "desert", clear, true), Is.GreaterThan(WeatherGameplay.HeatStrength(f, "tropical", clear, true)));
        }

        [Test]
        public void HeatBuildsInTheSunSlowsALittleAndShadeFixesIt()
        {
            var f = F(); var h = new HeatStress(f);
            for (int i = 0; i < 400; i++) h.Tick(0.1f, 1f, false, false);   // 40 s in full sun
            Assert.That(h.Level, Is.EqualTo(1f).Within(1e-4));
            Assert.That(h.SpeedScale, Is.EqualTo(f.heatMinSpeed).Within(1e-4));
            Assert.That(h.ShowHint, Is.True);
            for (int i = 0; i < 70; i++) h.Tick(0.1f, 1f, true, false);   // 7 s in shade
            Assert.That(h.Level, Is.EqualTo(0f).Within(1e-4));
            Assert.That(h.SpeedScale, Is.EqualTo(1f));
        }

        [Test]
        public void DrinkingAlsoCoolsAndAMildDayBuildsLess()
        {
            var f = F(); var a = new HeatStress(f); var b = new HeatStress(f);
            for (int i = 0; i < 100; i++) { a.Tick(0.1f, 1f, false, false); b.Tick(0.1f, 0.5f, false, false); }
            Assert.That(b.Level, Is.LessThan(a.Level));
            float before = a.Level; a.Tick(1f, 1f, false, true);
            Assert.That(a.Level, Is.LessThan(before));
        }

        [Test]
        public void HeatNeverSlowsPastTheFloor()
        {
            var f = F(); var h = new HeatStress(f);
            for (int i = 0; i < 5000; i++) h.Tick(0.1f, 1f, false, false);
            Assert.That(h.SpeedScale, Is.GreaterThanOrEqualTo(0.85f));
        }
    
        [Test]
        public void RacesTimeTrialsAndBossesAreNeverPushedOrWilted()
        {
            Assert.That(WeatherGameplay.Applies(PlayMode.Story), Is.True);
            Assert.That(WeatherGameplay.Applies(PlayMode.FreePlay), Is.True);
            Assert.That(WeatherGameplay.Applies(PlayMode.Race), Is.False);
            Assert.That(WeatherGameplay.Applies(PlayMode.TimeTrial), Is.False);
            Assert.That(WeatherGameplay.Applies(PlayMode.Boss), Is.False);
            Assert.That(WeatherGameplay.Daylight(DayPhase.Noon), Is.True);
            Assert.That(WeatherGameplay.Daylight(DayPhase.Night), Is.False);
        }
    }
}
