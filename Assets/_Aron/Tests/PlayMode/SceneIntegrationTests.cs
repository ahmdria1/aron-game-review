// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aron.Atmosphere;
using Aron.Config;
using Aron.Core;
using Aron.Enemies;
using Aron.Input;
using Aron.Level;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Localization;
using Aron.Player;
using Aron.Save;
using Aron.UI;
using NUnit.Framework;
using TMPro;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Aron.Tests.PlayMode
{
    /// <summary>Scripted intent for tests: one-frame presses, held jump, a steady stick.</summary>
    internal sealed class ScriptedInput : IIntentSource
    {
        public Vector2 Move;
        public bool JumpHeld;
        private bool jumpOnce, attackOnce, pauseOnce;
        public void PressJump() => jumpOnce = true;
        public void PressAttack() => attackOnce = true;
        public void PressPause() => pauseOnce = true;

        public PlayerIntent Poll()
        {
            var i = new PlayerIntent { move = Move, jumpPressed = jumpOnce, jumpHeld = JumpHeld || jumpOnce, attackPressed = attackOnce, pausePressed = pauseOnce };
            i.fingerDown = i.jumpHeld;
            jumpOnce = attackOnce = pauseOnce = false;
            return i;
        }
    }

    /// <summary>
    /// Boots the real builder-generated Level01_Riyadh scene (run Aron.EditorTools.CI.Prepare first, see tools/unity-verify.sh)
    /// and plays it with scripted input. Any Error, Exception or Assert in the log fails the test (Unity's default).
    /// </summary>
    public class SceneIntegrationTests
    {
        private string savePath;
        private ScriptedInput input;
        private PlayerController player;

        [UnitySetUp]
        public IEnumerator Boot()
        {
            savePath = Path.Combine(Application.temporaryCachePath, "aron_scene_test_save.json");
            WipeSave();
            SaveSystem.UsePath(savePath);
            Time.timeScale = 1f;

            var load = SceneManager.LoadSceneAsync(SceneNames.Riyadh, LoadSceneMode.Single);
            Assert.IsNotNull(load, "Level01_Riyadh is not in Build Settings; run the builder first");
            yield return load;
            yield return null;
            yield return null;

            Assert.IsNotNull(InputRouter.Instance, "scene has no InputRouter");
            input = new ScriptedInput();
            InputRouter.Instance.AddSource(input);
            player = PlayerController.Active;
            // Let the character controller settle onto the ground.
            for (int i = 0; i < 40; i++) yield return null;
        }

        [TearDown]
        public void Teardown()
        {
            Time.timeScale = 1f;
            SaveSystem.UsePath(null);
            WipeSave();
        }

        private void WipeSave()
        {
            if (savePath == null) return;
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(savePath), Path.GetFileName(savePath) + "*")) File.Delete(f);
        }

        [UnityTest]
        public IEnumerator PlayerSpawnsAndStandsOnTheGround()
        {
            Assert.IsNotNull(player, "no active PlayerController");
            Assert.IsNotNull(GameManager.Instance);
            Assert.Greater(GameManager.Instance.Lives, 0);
            Assert.IsTrue(player.IsGrounded, "player did not land");
            Assert.That(player.transform.position.y, Is.InRange(-0.5f, 1.5f));
            Assert.IsNotNull(Camera.main, "no main camera");
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpReachesTheConfiguredApex()
        {
            float startY = player.transform.position.y;
            float maxY = startY;
            input.PressJump();
            for (int i = 0; i < 180; i++)
            {
                yield return null;
                // Hold jump while rising so the variable-height cut does not shorten the arc.
                input.JumpHeld = player.Velocity.y > 0f;
                maxY = Mathf.Max(maxY, player.transform.position.y);
                if (i > 10 && player.IsGrounded) break;
            }
            float apex = maxY - startY;
            float expected = FeelSettings.V.jumpHeight;
            Assert.AreEqual(expected, apex, 0.35f, $"apex {apex:0.00} m, configured {expected:0.00} m");
        }

        [UnityTest]
        public IEnumerator SpinBreaksACrate()
        {
            // Jump, and at the top of the arc put a crate right beside the player and spin.
            input.PressJump();
            input.JumpHeld = true;
            float limit = Time.time + 2f;
            while (Time.time < limit && !(player.Velocity.y <= 0f && !player.IsGrounded)) yield return null;
            Assert.IsFalse(player.IsGrounded, "player never reached the top of the jump");
            input.JumpHeld = false;

            var crateGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crateGo.name = "TestCrate";
            crateGo.transform.position = player.transform.position + player.transform.forward * 1.0f + Vector3.up * 0.3f;
            var crate = crateGo.AddComponent<Crate>();
            crate.Setup(Crate.CrateType.Basic, 3);
            yield return null;
            int before = GameManager.Instance.Dates;

            input.PressAttack();                      // attack in the air = spin
            float until = Time.time + 1.0f;
            bool sawSpin = false;
            while (Time.time < until && crate != null && crate.CanBeDamaged)
            {
                yield return null;
                sawSpin |= player.IsSpinning;
            }
            if (crate != null && crate.CanBeDamaged)
                Assert.Fail($"crate survived the spin (spin seen: {sawSpin}, player {player.transform.position}, crate {crate.transform.position})");
            Assert.AreEqual(before + 3, GameManager.Instance.Dates, "broken crate should pay out its dates");
        }

        /// <summary>Spin right after take-off (the attack comes this many seconds after the jump press, 0 = the same frame): a crate 1.2 m away must break. Measured in time, not frames, so slow and fast frame rates test the same thing.</summary>
        [UnityTest]
        public IEnumerator EarlyJumpSpinBreaksACrateInRange([Values(0f, 0.05f, 0.1f)] float secondsAfterJump)
        {
            Vector3 origin = player.transform.position;
            var crateGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crateGo.name = "TestCrate";
            crateGo.transform.position = origin + player.transform.forward * 1.2f + Vector3.up * 0.5f;
            var crate = crateGo.AddComponent<Crate>();
            crate.Setup(Crate.CrateType.Basic, 3);
            yield return null;
            int before = GameManager.Instance.Dates;

            input.PressJump();
            input.JumpHeld = true;
            float tJump = Time.time;
            while (Time.time - tJump < secondsAfterJump) yield return null;
            input.PressAttack();
            float tPress = Time.time; string trace = "";
            float until = Time.time + 1.0f;
            bool sawSpin = false, sawCrateInSphere = false; int maxInSphere = 0; float lowestY = float.MaxValue, highestY = float.MinValue;
            while (Time.time < until && crate != null && crate.CanBeDamaged)
            {
                yield return null;
                if (player.IsSpinning) { sawSpin = true; if (trace.Length < 400) trace += $"[t{Time.time - tPress:0.000} y{player.transform.position.y:0.00}]"; }
                lowestY = Mathf.Min(lowestY, player.transform.position.y); highestY = Mathf.Max(highestY, player.transform.position.y);
                if (player.IsSpinning && crate != null)
                {
                    var all = Physics.OverlapSphere(player.transform.position + Vector3.up * 0.6f, 1.4f, ~0, QueryTriggerInteraction.Collide);
                    maxInSphere = Mathf.Max(maxInSphere, all.Length);
                    foreach (var c in all) if (c.gameObject == crateGo) sawCrateInSphere = true;
                }
            }
            input.JumpHeld = false;
            Assert.IsTrue(crate == null || !crate.CanBeDamaged,
                $"a spin {secondsAfterJump:0.00} s after take-off did not break a crate 1.2 m away (player {player.transform.position}, spin seen {sawSpin}, crate inside the spin sphere {sawCrateInSphere}, most colliders in the sphere {maxInSphere}, height {lowestY:0.00}..{highestY:0.00}, spin frames {trace})");
            Assert.AreEqual(before + 3, GameManager.Instance.Dates);
        }

        [UnityTest]
        public IEnumerator SpinLeavesACrateOutOfReachAlone()
        {
            var crateGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crateGo.name = "FarCrate";
            crateGo.transform.position = player.transform.position + player.transform.forward * 3.2f + Vector3.up * 0.5f;
            var crate = crateGo.AddComponent<Crate>();
            crate.Setup(Crate.CrateType.Basic, 3);
            yield return null;
            input.PressJump(); input.JumpHeld = true;
            yield return null; yield return null;
            input.PressAttack();
            for (int i = 0; i < 40; i++) yield return null;
            input.JumpHeld = false;
            Assert.IsTrue(crate != null && crate.CanBeDamaged, "a spin must not reach a crate well outside its radius");
        }

        /// <summary>A ring of tall walls high above the street: the hero is moved into it (as a respawn does) and the camera must stay inside the ring.</summary>
        [UnityTest]
        public IEnumerator CameraStaysInsideWallsAfterARespawnTeleport()
        {
            var cam = Camera.main;
            var root = new GameObject("TestWallRing");
            Vector3 centre = new Vector3(0f, 300f, 0f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = centre + Vector3.down * 0.5f; floor.transform.localScale = new Vector3(40f, 1f, 40f);
            const float ringRadius = 4f;
            for (int k = 0; k < 4; k++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.transform.SetParent(root.transform);
                var dir = Quaternion.Euler(0f, k * 90f, 0f) * Vector3.forward;
                w.transform.position = centre + dir * (ringRadius + 0.5f) + Vector3.up * 10f;
                w.transform.rotation = Quaternion.Euler(0f, k * 90f, 0f);
                w.transform.localScale = new Vector3(ringRadius * 2f + 2f, 20f, 1f);
            }
            yield return null;

            player.Respawn(centre + Vector3.up * 0.1f);   // the same call a death uses: far from where the camera was
            var hits = new Collider[16];
            float radius = Mathf.Max(0.15f, cam.nearClipPlane * 1.5f);
            int violations = 0; string first = null;
            for (int i = 0; i < 150; i++)
            {
                yield return null;
                int n = Physics.OverlapSphereNonAlloc(cam.transform.position, radius, hits, ~0, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < n; h++)
                {
                    if (hits[h].transform.IsChildOf(player.transform)) continue;
                    violations++;
                    var hc = hits[h];
                    if (first == null) first = $"frame {i}: camera inside '{PathOf(hc.transform)}' ({hc.GetType().Name}, bounds {hc.bounds}, layer {hc.gameObject.layer}, rb {(hc.attachedRigidbody != null)}) at {cam.transform.position}, player at {player.transform.position}, camera looks {cam.transform.forward}";
                }
            }
            Object.Destroy(root);
            Assert.AreEqual(0, violations, first);
            var focus = player.transform.position + Vector3.up * 1.2f;
            Assert.IsFalse(Physics.Linecast(cam.transform.position, focus, out var block, ~0, QueryTriggerInteraction.Ignore) && !block.transform.IsChildOf(player.transform),
                "the camera must see the hero once settled");
        }

        [UnityTest]
        public IEnumerator StompKillsAnEnemyAndBounces()
        {
            var enemy = new GameObject("TestEnemy").AddComponent<Enemy>();
            var col = enemy.gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.height = 1.6f; col.radius = 0.5f; col.center = new Vector3(0, 0.8f, 0);
            enemy.transform.position = player.transform.position + player.transform.forward * 3f;
            yield return null;

            player.Teleport(enemy.transform.position + Vector3.up * 3f);
            float maxVy = float.MinValue;
            for (int i = 0; i < 90 && enemy != null && enemy.CurrentState != Enemy.State.Dead; i++) yield return null;
            Assert.IsTrue(enemy == null || enemy.CurrentState == Enemy.State.Dead, "stomp did not kill the enemy");
            for (int i = 0; i < 6; i++) { yield return null; maxVy = Mathf.Max(maxVy, player.Velocity.y); }
            Assert.Greater(maxVy, 1f, "player should bounce upwards after a stomp");
        }

        [UnityTest]
        public IEnumerator CollectingADateAddsPoints()
        {
            var go = new GameObject("TestDate");
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true; sphere.radius = 1.2f;
            go.AddComponent<DateCollectible>();
            go.transform.position = player.transform.position + Vector3.up * 0.9f;
            int dates = GameManager.Instance.Dates;
            int coins = SaveSystem.Data.coins;
            for (int i = 0; i < 20 && go != null; i++) yield return null;
            Assert.IsTrue(go == null, "date was not collected");
            Assert.AreEqual(dates + 1, GameManager.Instance.Dates);
            Assert.AreEqual(coins + 1, SaveSystem.Data.coins);
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheWorldAndResumeRestoresIt()
        {
            var gm = GameManager.Instance;
            input.PressPause();
            yield return null;
            yield return null;
            Assert.IsTrue(gm.IsPaused, "pause intent did not pause");
            Assert.AreEqual(0f, Time.timeScale);

            Vector3 at = player.transform.position;
            input.Move = Vector2.up;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(at, player.transform.position, "player moved while paused");

            input.Move = Vector2.zero;
            gm.SetPaused(false);
            Assert.IsFalse(gm.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            input.Move = Vector2.up;
            for (int i = 0; i < 30; i++) yield return null;
            Assert.Greater((player.transform.position - at).magnitude, 0.2f, "player did not move after resume");
        }

        [UnityTest]
        public IEnumerator SaveLoadRoundTripsThroughDisk()
        {
            var d = SaveSystem.Data;
            d.coins = 777; d.totalDates = 42; d.language = "en"; d.selectedCharacterId = "noura";
            SaveSystem.Save();
            Assert.IsTrue(File.Exists(savePath));
            SaveSystem.UsePath(savePath);         // drops the in-memory copy
            var back = SaveSystem.Data;
            Assert.AreEqual(777, back.coins);
            Assert.AreEqual(42, back.totalDates);
            Assert.AreEqual("en", back.language);
            Assert.AreEqual("noura", back.selectedCharacterId);
            Assert.AreEqual(new SaveData().version, back.version);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArabicUiTextRendersInEveryLanguageWithoutErrors()
        {
            var canvas = UIFactory.CreateCanvas("TestArabicUi", 500);
            var root = UIFactory.SafeRoot(canvas);
            string saved = Strings.Language;
            var labels = new List<TextMeshProUGUI>();
            try
            {
                foreach (var code in UiLanguages.Codes)
                {
                    Strings.Language = code;
                    var t = UIFactory.Label(root, "L_" + code, Strings.Get("play") + " — رحلة أرون ١٢٣", 40f, Color.white);
                    ArabicTMP.SetText(t, "رحلة أرون: التمر ٥٠ والأرواح ٣");
                    labels.Add(t);
                }
                yield return null;
                yield return null;
                foreach (var t in labels)
                {
                    t.ForceMeshUpdate();
                    Assert.Greater(t.textInfo.characterCount, 5, t.name + " rendered no characters");
                }
                Assert.IsNotNull(ArabicTMP.Font, "Arabic font asset missing");
                Assert.IsTrue(labels[0].isRightToLeftText, "Arabic text should be set right-to-left");
            }
            finally { Strings.Language = saved; Object.Destroy(canvas.gameObject); }
        }

        [UnityTest]
        public IEnumerator FoliageWindRunsAcrossWeatherChanges()
        {
            var go = new GameObject("TestFoliageWind");
            var wind = go.AddComponent<FoliageWind>();
            Assert.AreSame(wind, FoliageWind.Current);
            foreach (var (w, dust, rain) in new[] { (0f, 0f, 0f), (1f, 0f, 0f), (0.6f, 1f, 0.4f), (1f, 1f, 1f) })
            {
                wind.SetWeather(w, dust, rain, new Vector2(-0.8f, 0.35f));
                for (int i = 0; i < 20; i++) yield return null;
                Assert.IsFalse(float.IsNaN(wind.Gust) || float.IsInfinity(wind.Gust));
                Assert.IsFalse(float.IsNaN(wind.Params.Sway));
            }
            Object.Destroy(go);
            yield return null;
            Assert.IsNull(FoliageWind.Current);
        }

        [UnityTest]
        public IEnumerator CameraNeverEntersSolidGeometryOnAScriptedWalk()
        {
            var cam = Camera.main;
            Assert.IsNotNull(cam);
            float radius = Mathf.Max(0.15f, cam.nearClipPlane * 1.5f);
            var hits = new Collider[16];
            input.Move = Vector2.up;
            int violations = 0, fell = 0; string first = null;
            for (int i = 0; i < 360; i++)
            {
                if (i == 60 || i == 180) { input.PressJump(); }
                if (i == 120) input.Move = new Vector2(0.25f, 0.9f);
                if (i == 240) input.Move = new Vector2(-0.25f, 0.9f);
                yield return null;
                if (player.transform.position.y < -1f) fell++;   // a fall and its respawn are camera questions too
                int n = Physics.OverlapSphereNonAlloc(cam.transform.position, radius, hits, ~0, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < n; h++)
                {
                    if (hits[h].transform.IsChildOf(player.transform)) continue;
                    violations++;
                    var hc = hits[h];
                    if (first == null) first = $"frame {i}: camera inside '{PathOf(hc.transform)}' ({hc.GetType().Name}, bounds {hc.bounds}, layer {hc.gameObject.layer}, rb {(hc.attachedRigidbody != null)}, trigger {hc.isTrigger}, damageable {hc.GetComponentInParent<Aron.Core.IDamageable>() != null}, playerChild {hc.GetComponentInParent<PlayerController>() != null}) at {cam.transform.position}, player at {player.transform.position}, looks {cam.transform.forward}";
                }
            }
            Assert.AreEqual(0, violations, first + $" (frames with player below the street: {fell})");
        }

        private static string PathOf(Transform t)
        {
            string s = t.name;
            for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s;
            return s;
        }

        [UnityTest, Performance]
        public IEnumerator SceneRunsSixHundredFramesCleanAndFrameTimesAreRecorded()
        {
            input.Move = new Vector2(0f, 0.6f);
            using (Measure.Frames().WarmupCount(10).MeasurementCount(600).Scope("Level01_Riyadh frames"))
            {
                for (int i = 0; i < 610; i++) yield return null;
            }
            // Reaching here means 600 frames ran with no exception, Error or Assert in the log.
            Assert.IsNotNull(player);
            Assert.IsFalse(GameManager.Instance.IsOver, "run ended unexpectedly");
        }
    }
}
