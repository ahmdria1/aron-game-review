// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Core;
using Aron.Enemies;
using Aron.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aron.Tests.PlayMode
{
    public class CombatTests
    {
        private GameObject managerGo;

        [SetUp]
        public void SetUp()
        {
            managerGo = new GameObject("GameManager");
            managerGo.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.scene.IsValid() && go.transform.parent == null && go.name != "Code-based tests runner") Object.DestroyImmediate(go);
        }

        private static DamageInfo Hit(DamageKind kind) => new DamageInfo(1, kind, Vector3.zero, null);

        [UnityTest]
        public IEnumerator BreakingABasicCrateAwardsDates()
        {
            var crateGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var crate = crateGo.AddComponent<Crate>();
            crate.Setup(Crate.CrateType.Basic, 5);
            yield return null;

            int before = GameManager.Instance.Dates;
            crate.TakeDamage(Hit(DamageKind.Melee));
            yield return null;

            Assert.AreEqual(before + 5, GameManager.Instance.Dates);
            Assert.IsTrue(crateGo == null, "crate should be destroyed");
        }

        [UnityTest]
        public IEnumerator LifeCrateAddsALife()
        {
            var crate = new GameObject("Crate").AddComponent<Crate>();
            crate.Setup(Crate.CrateType.Life, 0);
            yield return null;
            int lives = GameManager.Instance.Lives;
            crate.TakeDamage(Hit(DamageKind.Spin));
            Assert.AreEqual(lives + 1, GameManager.Instance.Lives);
        }

        [UnityTest]
        public IEnumerator SpringCrateSurvivesStomp()
        {
            var crate = new GameObject("Crate").AddComponent<Crate>();
            crate.Setup(Crate.CrateType.Bounce, 1);
            yield return null;
            crate.TakeDamage(Hit(DamageKind.Stomp));
            Assert.IsTrue(crate.CanBeDamaged);
            crate.TakeDamage(Hit(DamageKind.Melee));
            Assert.IsFalse(crate.CanBeDamaged);
        }

        [UnityTest]
        public IEnumerator EnemyDiesFromOneHitAndStopsTakingDamage()
        {
            var enemy = new GameObject("Enemy").AddComponent<Enemy>();
            yield return null;
            Assert.IsTrue(enemy.CanBeDamaged);
            enemy.TakeDamage(Hit(DamageKind.Melee));
            Assert.AreEqual(Enemy.State.Dead, enemy.CurrentState);
            Assert.IsFalse(enemy.CanBeDamaged);
        }

        [UnityTest]
        public IEnumerator BossIgnoresHitsUnlessStunned()
        {
            var boss = new GameObject("Boss").AddComponent<RiyadhBoss>();
            yield return null;
            Assert.IsFalse(boss.CanBeDamaged);
            boss.TakeDamage(Hit(DamageKind.Melee));
            Assert.AreEqual(RiyadhBoss.BossState.Idle, boss.State);
        }
    }
}
