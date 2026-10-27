// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Data;
using Aron.Input;
using Aron.Localization;
using Aron.Logic;
using Aron.Player;
using Aron.Save;
using Aron.Stages;
using Aron.UI;
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>
    /// Runs the companion abilities at the levels bought in the store. Nothing new to press:
    ///  - camel: double tap on Aron while riding sprints on the camel's breath; the camel also soaks up hits and
    ///    brings extra coins;
    ///  - horse: the tap that makes the horse charge also gives a burst of speed that smashes barriers and walls,
    ///    and hits slide off during it;
    ///  - falcon: its swoop and fetch get quicker and reach further.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class CompanionAbilityRunner : MonoBehaviour
    {
        private PlayerController player;
        private CompanionLevel camel, horse, falcon;
        private SandSprint sprint;
        private HitShield shield;
        private HorseMoves moves;
        private readonly DateCarry carry = new DateCarry();
        private MountKind lastKind = MountKind.None;
        private bool wasCharging, addingBonus;
        private int lastDates;
        private readonly Collider[] hits = new Collider[16];

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            Load();
        }

        /// <summary>Re-reads the levels (after an upgrade in the store).</summary>
        public void Load()
        {
            var file = GameData.Companions;
            var data = SaveSystem.Data;
            camel = CompanionUpgrades.Current(file, data, CompanionIds.Camel);
            horse = CompanionUpgrades.Current(file, data, CompanionIds.Horse);
            falcon = CompanionUpgrades.Current(file, data, CompanionIds.Falcon);
            sprint = new SandSprint(camel);
            shield = new HitShield(0);
            moves = new HorseMoves(horse);
            var f = FindFirstObjectByType<FalconCompanion>();
            if (f != null)
            {
                f.Brain.SwoopCooldown = falcon.swoopCooldown;
                f.Brain.FetchRange = falcon.fetchRange;
                f.Brain.SwoopRange = Mathf.Max(f.Brain.SwoopRange, falcon.fetchRange + 2f);
                f.Scouting = new FalconScout(falcon);
            }
        }

        private void OnEnable()
        {
            player = GetComponent<PlayerController>();
            player.AbsorbHit = Absorb;
            GameEvents.DatesChanged += OnDates;
        }

        private void OnDisable()
        {
            if (player != null) { player.AbsorbHit = null; player.SpeedBoost = 1f; }
            GameEvents.DatesChanged -= OnDates;
            GameUI.Ability(null);
        }

        private MountKind Kind
        {
            get
            {
                var r = player.Rider;
                return r != null && r.IsMounted ? r.State.Kind : MountKind.None;
            }
        }

        private bool Absorb()
        {
            var k = Kind;
            if (k == MountKind.Horse && moves.Dodging(Time.time)) return true;
            return k == MountKind.Camel && shield.Absorb();
        }

        private void OnDates(int total)
        {
            int delta = total - lastDates;
            lastDates = total;
            if (addingBonus || delta <= 0 || Kind != MountKind.Camel) return;
            int extra = carry.Bonus(delta, camel.dateBonus);
            if (extra <= 0 || GameManager.Instance == null) return;
            addingBonus = true;
            GameManager.Instance.AddDates(extra);
            addingBonus = false;
        }

        private void Update()
        {
            float now = Time.time, dt = Time.deltaTime;
            var kind = Kind;
            if (kind != lastKind)
            {
                if (kind == MountKind.Camel) shield.Refill(camel.shieldHits);
                lastKind = kind;
            }
            var intent = InputRouter.Instance != null ? InputRouter.Instance.Current : default;
            float boost = 1f;

            if (kind == MountKind.Camel)
            {
                boost = sprint.Tick(dt, intent.sprintHeld, OnSand());
                GameUI.Ability(Strings.Format("ability_camel", Mathf.RoundToInt(sprint.Fraction * 100f), shield.Left));
            }
            else sprint.Tick(dt, false, false);

            if (kind == MountKind.Horse)
            {
                bool charging = player.Rider.IsCharging;
                if (charging && !wasCharging) moves.Charge.TryStart(now);
                wasCharging = charging;
                boost = moves.SpeedMultiplier(now);
                if (moves.BreaksBarriers(now)) Smash();
                GameUI.Ability(moves.Charge.Ready(now) ? Strings.Get("ability_horse_ready")
                    : Strings.Format("ability_horse_wait", Mathf.CeilToInt((1f - moves.Charge.Fraction(now)) * horse.chargeCooldown)));
            }
            else wasCharging = false;

            if (kind == MountKind.None) GameUI.Ability(null);
            player.SpeedBoost = boost;
        }

        private bool OnSand()
        {
            var b = StageBuilder.Current;
            if (b == null || b.Stage == null) return true;
            return b.Stage.terrain == "desert" || b.Stage.terrain == "oasis";
        }

        private void Smash()
        {
            var c = transform.position + transform.forward * 1.4f + Vector3.up * 0.8f;
            int n = Physics.OverlapSphereNonAlloc(c, 1.6f, hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var b = hits[i].GetComponentInParent<Breakable>();
                if (b != null) b.Smash();
            }
        }
    }
}
