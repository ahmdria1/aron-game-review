// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.CameraRig;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class CameraTests
    {
        private static CameraFile Cfg()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (!Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            return JsonSerializer.Deserialize<CameraFile>(File.ReadAllText(Path.Combine(dir.FullName, "Assets/_Aron/Resources/Config/camera.json")), new JsonSerializerOptions { IncludeFields = true });
        }

        [Test] public void ConfigIsValid() => Assert.That(Cfg().Validate(), Is.Empty);
        [Test] public void DefaultSettingsAreValid() => Assert.That(new CameraSettings().Validate(), Is.Empty);

        [Test]
        public void EveryStageTypeGetsTheRightMode()
        {
            var f = Cfg();
            Assert.That(f.ModeFor("normal"), Is.EqualTo("free")); Assert.That(f.ModeFor("action"), Is.EqualTo("free"));
            Assert.That(f.ModeFor("chase"), Is.EqualTo("chase")); Assert.That(f.ModeFor("race"), Is.EqualTo("race")); Assert.That(f.ModeFor("boss"), Is.EqualTo("boss"));
            Assert.That(f.ModeFor("normal", "chase"), Is.EqualTo("chase"), "a stage can choose its own");
            Assert.That(f.ModeFor("normal", "nonsense"), Is.EqualTo("free"));
            Assert.That(f.ModeFor("unknown"), Is.EqualTo("free"));
        }

        [Test]
        public void EveryShippedStageResolvesToAKnownMode()
        {
            var f = Cfg();
            var cat = Aron.Logic.Tests.TestData.Catalog();
            foreach (var r in cat.regions) foreach (var s in r.stages) Assert.That(f.Mode(f.ModeFor(s.type, s.camera)), Is.Not.Null, s.id);
        }

        [Test]
        public void OnlyTheRightHalfOrbitsAndLeftHandedMirrors()
        {
            Assert.That(OrbitState.IsOrbitTouch(0.8f, false), Is.True); Assert.That(OrbitState.IsOrbitTouch(0.2f, false), Is.False);
            Assert.That(OrbitState.IsOrbitTouch(0.2f, true), Is.True); Assert.That(OrbitState.IsOrbitTouch(0.8f, true), Is.False);
        }

        [Test]
        public void DragOrbitsClampsAndHonoursInvertY()
        {
            var f = Cfg(); var free = f.Mode("free"); var s = new CameraSettings();
            var o = new OrbitState();
            o.Drag(160f, 0f, 320f, s, f, free);   // 160 px at 320 dpi = 1.27 cm
            Assert.That(o.Yaw, Is.EqualTo(f.degreesPerCm * 160f / (320f / 2.54f)).Within(0.01f));
            o.Drag(100000f, 0f, 320f, s, f, free); Assert.That(o.Yaw, Is.EqualTo(free.maxYaw));
            var up = new OrbitState(); up.Drag(0f, 200f, 320f, s, f, free);
            Assert.That(up.Pitch, Is.LessThan(0f), "dragging up looks up");
            var inv = new OrbitState(); inv.Drag(0f, 200f, 320f, new CameraSettings { invertY = true }, f, free);
            Assert.That(inv.Pitch, Is.GreaterThan(0f), "inverted");
            var big = new OrbitState(); big.Drag(0f, 100000f, 320f, new CameraSettings { invertY = true }, f, free); Assert.That(big.Pitch, Is.EqualTo(free.pitchDown));
            var small = new OrbitState(); small.Drag(0f, -100000f, 320f, new CameraSettings { invertY = true }, f, free); Assert.That(small.Pitch, Is.EqualTo(-free.pitchUp));
        }

        [Test]
        public void DragSpeedFollowsTheSliderAndTheScreenDensity()
        {
            var f = Cfg(); var m = f.Mode("free");
            var a = new OrbitState(); a.Drag(100f, 0f, 320f, new CameraSettings { orbitSpeed = 1f }, f, m);
            var b = new OrbitState(); b.Drag(100f, 0f, 320f, new CameraSettings { orbitSpeed = 2f }, f, m);
            var c = new OrbitState(); c.Drag(100f, 0f, 640f, new CameraSettings { orbitSpeed = 1f }, f, m);
            Assert.That(b.Yaw, Is.EqualTo(a.Yaw * 2f).Within(0.01f)); Assert.That(c.Yaw, Is.EqualTo(a.Yaw / 2f).Within(0.01f), "same finger travel in cm turns the same");
        }

        [Test]
        public void OnlyTheFreeCameraOrbitsAndZooms()
        {
            var f = Cfg(); var s = new CameraSettings();
            foreach (var id in new[] { "chase", "race", "boss" })
            {
                var o = new OrbitState(); o.Drag(300f, 300f, 320f, s, f, f.Mode(id)); o.Pinch(100f, 50f, f.Mode(id));
                Assert.That(o.Yaw, Is.EqualTo(0f), id); Assert.That(o.Pitch, Is.EqualTo(0f), id); Assert.That(o.Zoom, Is.EqualTo(1f), id);
            }
        }

        [Test]
        public void PinchZoomsInAndOutWithinLimits()
        {
            var f = Cfg(); var m = f.Mode("free"); var o = new OrbitState();
            o.Pinch(100f, 200f, m); Assert.That(o.Zoom, Is.EqualTo(0.5f).Within(0.001f).Or.EqualTo(m.zoomMin), "fingers apart: closer");
            o.Pinch(200f, 100f, m); Assert.That(o.Zoom, Is.GreaterThan(m.zoomMin));
            for (int i = 0; i < 30; i++) o.Pinch(100f, 50f, m);
            Assert.That(o.Zoom, Is.EqualTo(m.zoomMax)); for (int i = 0; i < 60; i++) o.Pinch(50f, 100f, m); Assert.That(o.Zoom, Is.EqualTo(m.zoomMin));
            var z = new OrbitState(); z.Pinch(0f, 100f, m); Assert.That(z.Zoom, Is.EqualTo(1f));
        }

        [Test]
        public void AutoRecentersAfterAboutOneAndAHalfSeconds()
        {
            var f = Cfg(); var m = f.Mode("free"); var s = new CameraSettings(); var o = new OrbitState();
            o.Drag(200f, 100f, 320f, s, f, m); float y0 = o.Yaw;
            o.EndDrag();
            for (float t = 0; t < 1.4f; t += 0.02f) o.Tick(0.02f, s, f);
            Assert.That(o.Yaw, Is.EqualTo(y0), "still waiting");
            for (float t = 0; t < 0.4f; t += 0.02f) o.Tick(0.02f, s, f);
            Assert.That(Math.Abs(o.Yaw), Is.LessThan(Math.Abs(y0)), "started to swing back");
            for (float t = 0; t < 1.5f; t += 0.02f) o.Tick(0.02f, s, f);
            Assert.That(o.Yaw, Is.EqualTo(0f)); Assert.That(o.Pitch, Is.EqualTo(0f));
        }

        [Test]
        public void HoldingTheFingerStopsRecenteringAndSettingsCanTurnItOff()
        {
            var f = Cfg(); var m = f.Mode("free"); var s = new CameraSettings();
            var o = new OrbitState(); o.Drag(200f, 0f, 320f, s, f, m);
            for (int i = 0; i < 300; i++) o.Tick(0.02f, s, f);
            Assert.That(o.Yaw, Is.Not.EqualTo(0f), "still dragging");
            var off = new CameraSettings { autoRecenter = false }; var p = new OrbitState(); p.Drag(200f, 0f, 320f, off, f, m); p.EndDrag();
            for (int i = 0; i < 300; i++) p.Tick(0.02f, off, f);
            Assert.That(p.Yaw, Is.Not.EqualTo(0f));
            p.Recenter(); for (int i = 0; i < 100; i++) p.Tick(0.02f, off, f);
            Assert.That(p.Yaw, Is.EqualTo(0f), "the button always works");
        }

        [Test]
        public void RecenterButtonSwingsBackAtOnce()
        {
            var f = Cfg(); var m = f.Mode("free"); var s = new CameraSettings(); var o = new OrbitState();
            o.Drag(300f, 100f, 320f, s, f, m); o.EndDrag(); float y = o.Yaw;
            o.Recenter(); o.Tick(0.1f, s, f);
            Assert.That(Math.Abs(o.Yaw), Is.LessThan(Math.Abs(y) * 0.8f));
            for (int i = 0; i < 100; i++) o.Tick(0.02f, s, f);
            Assert.That(o.IsDefault, Is.True); Assert.That(o.Recentering, Is.False);
        }

        [Test]
        public void CameraPositionIsBehindAtYawZeroAndSwingsAround()
        {
            CameraMath.Offset(0f, 0f, 10f, out var x, out var y, out var z);
            Assert.That(x, Is.EqualTo(0f).Within(1e-4f)); Assert.That(y, Is.EqualTo(0f).Within(1e-4f)); Assert.That(z, Is.EqualTo(-10f).Within(1e-4f));
            CameraMath.Offset(90f, 0f, 10f, out x, out _, out z); Assert.That(x, Is.EqualTo(10f).Within(1e-3f)); Assert.That(z, Is.EqualTo(0f).Within(1e-3f));
            CameraMath.Offset(0f, 30f, 10f, out _, out y, out z); Assert.That(y, Is.EqualTo(5f).Within(1e-3f)); Assert.That(Math.Abs(z), Is.LessThan(10f));
            CameraMath.Offset(37f, 22f, 8f, out x, out y, out z); Assert.That(Math.Sqrt(x * x + y * y + z * z), Is.EqualTo(8f).Within(1e-3f), "always the same distance away");
            var m = Cfg().Mode("free");
            Assert.That(CameraMath.Pitch(m, 0f), Is.EqualTo(m.BasePitch).Within(1e-4f));
            Assert.That(CameraMath.Pitch(m, -500f), Is.EqualTo(-5f)); Assert.That(CameraMath.Pitch(m, 500f), Is.EqualTo(80f));
        }

        [Test]
        public void WallsPullTheCameraInAndItDriftsBackOut()
        {
            var f = Cfg();
            Assert.That(CameraMath.WallDistance(7f, -1f, f.wallMargin, f.minDistance), Is.EqualTo(7f));
            Assert.That(CameraMath.WallDistance(7f, 4f, f.wallMargin, f.minDistance), Is.EqualTo(3.7f).Within(1e-4f));
            Assert.That(CameraMath.WallDistance(7f, 0.5f, f.wallMargin, f.minDistance), Is.EqualTo(f.minDistance), "never inside the hero");
            Assert.That(CameraMath.WallDistance(3f, 5f, f.wallMargin, f.minDistance), Is.EqualTo(3f), "the wall is behind the camera");
            float d = 7f;
            for (int i = 0; i < 5; i++) d = CameraMath.SmoothDistance(d, 3.7f, 0.016f, f.wallInSpeed, f.wallOutSpeed);
            Assert.That(d, Is.LessThan(5.0f), "moves in fast");
            float inD = 3.7f; for (int i = 0; i < 5; i++) inD = CameraMath.SmoothDistance(inD, 7f, 0.016f, f.wallInSpeed, f.wallOutSpeed);
            Assert.That(inD - 3.7f, Is.LessThan((7f - d) * 0.5f + 1f), "and back out slowly");
        }

        [Test]
        public void ShakeFollowsTheSliderFromNothingToFull()
        {
            var full = new CameraSettings { shake = 100 }; var half = new CameraSettings { shake = 50 }; var none = new CameraSettings { shake = 0 };
            Assert.That(full.ShakeScale(false), Is.EqualTo(1f)); Assert.That(half.ShakeScale(false), Is.EqualTo(0.5f)); Assert.That(none.ShakeScale(false), Is.EqualTo(0f));
            Assert.That(full.ShakeScale(true), Is.EqualTo(0.25f), "reduce motion caps it");
            Assert.That(new CameraSettings { shake = 10 }.ShakeScale(true), Is.EqualTo(0.1f));
            var a = new ShakeModel(); a.Add(0.4f, full.ShakeScale(false));
            var b = new ShakeModel(); b.Add(0.4f, half.ShakeScale(false));
            var c = new ShakeModel(); c.Add(0.4f, none.ShakeScale(false));
            Assert.That(a.Intensity, Is.EqualTo(0.16f).Within(1e-4f)); Assert.That(b.Intensity, Is.EqualTo(0.04f).Within(1e-4f)); Assert.That(c.Intensity, Is.EqualTo(0f));
            for (int i = 0; i < 100; i++) a.Tick(0.016f);
            Assert.That(a.Trauma, Is.EqualTo(0f), "fades by itself");
            var big = new ShakeModel(); big.Add(5f, 1f); Assert.That(big.Trauma, Is.EqualTo(1f));
        }

        [Test]
        public void RaceCameraWidensAndBacksOffWithSpeed()
        {
            var m = Cfg().Mode("race");
            Assert.That(CameraMath.RaceFov(m, 1f), Is.GreaterThan(CameraMath.RaceFov(m, 0f))); Assert.That(CameraMath.RaceDistance(m, 1f), Is.GreaterThan(CameraMath.RaceDistance(m, 0f)));
            Assert.That(CameraMath.RaceFov(m, 9f), Is.EqualTo(CameraMath.RaceFov(m, 1f)));
        }

        [Test]
        public void BossFramingKeepsBothInViewAndBacksOffAsTheyDriftApart()
        {
            var m = Cfg().Mode("boss");
            CameraMath.FrameTwo(m, 0f, 0f, 0f, 10f, out var fx, out var fz, out var d1);
            Assert.That(fz, Is.EqualTo(4f).Within(1e-4f), "weighted toward the hero"); Assert.That(fx, Is.EqualTo(0f));
            CameraMath.FrameTwo(m, 0f, 0f, 0f, 40f, out _, out _, out var d2);
            Assert.That(d2, Is.GreaterThan(d1)); Assert.That(d2, Is.LessThanOrEqualTo(m.distance + m.frameExtra + 1e-4f));
            CameraMath.FrameTwo(m, 3f, 3f, 3f, 3f, out _, out _, out var d0); Assert.That(d0, Is.EqualTo(m.distance));
        }
    }
}
