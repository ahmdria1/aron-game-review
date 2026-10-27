// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Online;
using Aron.Logic.Races;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class RaceSetupTests
    {
        private static RaceSetupFile F() => TestData.Read<RaceSetupFile>("Races", "race_setup.json");
        private static RaceFile Tracks() => TestData.Read<RaceFile>("Races", "tracks.json");

        [Test] public void TheShippedFileIsValid() => Assert.That(F().Validate(), Is.Empty);

        [Test]
        public void OfflineTheDefaultIsTheGhostRace()
        {
            var f = F();
            Assert.That(RaceSetup.DefaultMode(f, false, 0), Is.EqualTo(RaceMode.Ghost));
            Assert.That(RaceSetup.DefaultMode(f, false, 5), Is.EqualTo(RaceMode.Ghost), "no connection, nobody else to race");
            Assert.That(RaceSetup.DefaultMode(f, true, 1), Is.EqualTo(RaceMode.Ghost), "online but alone");
            Assert.That(RaceSetup.DefaultMode(f, true, 2), Is.EqualTo(RaceMode.Ranked));
        }

        private static Ghost Run(RaceTrack t, float time)
        {
            var g = new Ghost { track = t.id, time = time };
            for (int i = 0; i < 40; i++) g.Add(new GhostFrame { z = i * 5f });
            return g;
        }

        [Test]
        public void TheFirstRaceGetsAPaceGhostThenYourBestThenAFriendWins()
        {
            var f = F(); var t = Tracks().tracks[0];
            Assert.That(RaceSetup.Choose(f, t, "normal", null, null).pick, Is.EqualTo(GhostPick.Pace));
            var own = Run(t, 70f);
            Assert.That(RaceSetup.Choose(f, t, "normal", own, null).pick, Is.EqualTo(GhostPick.OwnBest));
            var code = Run(t, 65f).ShareCode();
            var c = RaceSetup.Choose(f, t, "normal", own, code);
            Assert.That(c.pick, Is.EqualTo(GhostPick.Friend));
            Assert.That(c.ghost.time, Is.EqualTo(65f).Within(0.01));
        }

        [Test]
        public void ABadOrForeignCodeFallsBackWithoutAnError()
        {
            var f = F(); var tracks = Tracks().tracks; var t = tracks[0]; var other = tracks[1];
            var own = Run(t, 70f);
            Assert.That(RaceSetup.Choose(f, t, "normal", own, "not a code!!").pick, Is.EqualTo(GhostPick.OwnBest));
            Assert.That(RaceSetup.Choose(f, t, "normal", own, Run(other, 50f).ShareCode()).pick, Is.EqualTo(GhostPick.OwnBest), "a code from another track");
            Assert.That(RaceSetup.Choose(f, t, "normal", Run(other, 50f), "").pick, Is.EqualTo(GhostPick.Pace), "own best is for another track");
        }

        [Test]
        public void ThePaceGhostFitsEveryTrackAndGetsHarderWithDifficulty()
        {
            var f = F();
            foreach (var t in Tracks().tracks)
            {
                float easy = RaceSetup.PaceSeconds(f, t, "easy"), normal = RaceSetup.PaceSeconds(f, t, "normal"), hard = RaceSetup.PaceSeconds(f, t, "hard");
                Assert.That(easy, Is.GreaterThan(normal), t.id); Assert.That(normal, Is.GreaterThan(hard), t.id);
                Assert.That(easy, Is.LessThan(Ghost.MaxFrames / (float)Ghost.Hz), t.id + " fits the ghost format");
                var g = RaceSetup.PaceGhost(f, t, "normal");
                Assert.That(g.frames.Last().z, Is.EqualTo(t.TotalLength).Within(0.01), t.id);
                Assert.That(g.frames.Count, Is.GreaterThan(10));
                var back = Ghost.Decode(g.Encode());
                Assert.That(back, Is.Not.Null, t.id);
            }
        }
    }
}
