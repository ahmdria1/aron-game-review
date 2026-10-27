// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Enemies;
using Aron.Fx;
using Aron.Heroes;
using Aron.UI;
using Aron.Level;
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>
    /// The falcon. Flies off Aron's shoulder, now and then fetches a coin that is close by, dives at the nearest
    /// guard when he attacks (with a cooldown), and lands on his arm when he stands still.
    /// </summary>
    public class FalconCompanion : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float flySpeed = 15f;
        [SerializeField] private Vector3 shoulder = new Vector3(0.55f, 0.45f, -0.2f);

        private static readonly int PerchedId = Animator.StringToHash("Perched");

        private readonly FalconBrain brain = new FalconBrain();
        private readonly List<Spot> spots = new List<Spot>();
        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<DateCollectible> coins = new List<DateCollectible>();
        private PlayerController player;
        private Transform arm;
        private Transform target;
        private Enemy prey;
        private DateCollectible coin;
        private bool hooked;

        public FalconBrain Brain => brain;
        /// <summary>The scouting run (set from the companion level by <see cref="CompanionAbilityRunner"/>); null until then.</summary>
        public FalconScout Scouting { get; set; }
        private System.Action keyDone;
        private Vector3 keyPos;

        /// <summary>
        /// Fly to a key out of Aron's reach and bring it back; <paramref name="done"/> fires when it arrives at the key.
        /// False while the falcon is busy, on cooldown or the key is too far.
        /// </summary>
        public bool FetchKey(Vector3 where, System.Action done)
        {
            if (!FindPlayer() || keyDone != null) return false;
            if (!brain.TryFetchKey(Time.time, ToSpot(player.transform.position), ToSpot(where))) return false;
            keyPos = where; keyDone = done; target = null; coin = null; prey = null;
            return true;
        }

        private void OnEnable() => GameEvents.PlayerAttacked += OnAttack;
        private void OnDisable() => GameEvents.PlayerAttacked -= OnAttack;

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            Fx.DropShadow.Ensure(gameObject, 0.3f);
        }

        private bool FindPlayer()
        {
            if (player != null) return true;
            player = PlayerController.Active;
            if (player == null) return false;
            arm = Find(player.transform, "LowerArm.L") ?? Find(player.transform, "Fist.L");
            transform.position = Home();
            return true;
        }

        private void OnAttack()
        {
            if (!FindPlayer()) return;
            enemies.Clear();
            spots.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.CanBeDamaged) continue;
                enemies.Add(e);
                spots.Add(ToSpot(e.transform.position));
            }
            int i = brain.TrySwoop(Time.time, ToSpot(player.transform.position), spots);
            if (i < 0) return;
            prey = enemies[i];
            target = prey.transform;
            hooked = false;
            GameEvents.PlaySfx("swing");
        }

        // The scouting run: now and then a sparkle goes up over every hidden path ahead, so a player who cannot see them knows where to look.
        private readonly List<float> secretZ = new List<float>();
        private readonly List<SecretRoute> secrets = new List<SecretRoute>();

        private void TryScout()
        {
            if (Scouting == null || SecretRoute.All.Count == 0) return;
            secrets.Clear(); secretZ.Clear();
            foreach (var r in SecretRoute.All) { if (r == null) continue; secrets.Add(r); secretZ.Add(r.transform.position.z); }
            var found = Scouting.Reveal(Time.time, player.transform.position.z, secretZ);
            if (found.Count == 0) return;
            foreach (int i in found) secrets[i].Ping();
            GameUI.Banner(Localization.Strings.Get("falcon_scouted"), 1.6f);
        }

        private void TryFetch()
        {
            if (!brain.CanFetch(Time.time)) return;
            coins.Clear();
            spots.Clear();
            foreach (var c in DateCollectible.All)
            {
                if (c == null) continue;
                coins.Add(c);
                spots.Add(ToSpot(c.transform.position));
            }
            int i = brain.TryFetch(Time.time, ToSpot(player.transform.position), spots);
            if (i < 0) return;
            coin = coins[i];
            target = coin.transform;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || !FindPlayer()) return;
            var v = player.Velocity;
            v.y = 0f;
            bool still = v.sqrMagnitude < 0.05f && !player.IsRiding;
            brain.Tick(Time.time, still);
            // A respawn or a long ride can leave him far behind; he catches up at once rather than flying a kilometre.
            if ((transform.position - Home()).sqrMagnitude > 40f * 40f) transform.position = Home();
            if (brain.State == FalconState.Hover) { TryScout(); TryFetch(); }

            switch (brain.State)
            {
                case FalconState.Swooping:
                case FalconState.Fetching:
                    if (keyDone != null)
                    {
                        if (FlyTo(keyPos, flySpeed * 1.4f, 0.6f)) { var done = keyDone; keyDone = null; FxPlayer.Play("sparkle", keyPos); GameEvents.PlaySfx("coin"); brain.Arrived(Time.time); done(); }
                        break;
                    }
                    if (target == null) { brain.Arrived(Time.time); break; }
                    Vector3 aim = target.position + Vector3.up * (brain.State == FalconState.Swooping ? 1f : 0.3f);
                    if (FlyTo(aim, flySpeed * 1.4f, 0.6f)) Strike();
                    break;
                case FalconState.Returning:
                    if (FlyTo(Home(), flySpeed, 0.4f)) brain.BackHome();
                    break;
                case FalconState.Perched:
                    var perch = Perch();
                    transform.position = Vector3.Lerp(transform.position, perch, 1f - Mathf.Exp(-14f * Time.deltaTime));
                    Face(Aron().forward);
                    break;
                default:
                    var home = Home() + Vector3.up * Mathf.Sin(Time.time * 2.2f) * 0.12f;
                    transform.position = Vector3.Lerp(transform.position, home, 1f - Mathf.Exp(-6f * Time.deltaTime));
                    Face(Aron().forward);
                    break;
            }
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool(PerchedId, brain.State == FalconState.Perched);
        }

        private void Strike()
        {
            if (brain.State == FalconState.Swooping && prey != null && prey.CanBeDamaged && !hooked)
            {
                hooked = true;
                prey.TakeDamage(new DamageInfo(1, DamageKind.Melee, transform.position, gameObject));
                FxPlayer.Play("hit", transform.position);
            }
            else if (brain.State == FalconState.Fetching && coin != null) coin.Collect();
            prey = null; coin = null; target = null;
            brain.Arrived(Time.time);
        }

        private bool FlyTo(Vector3 point, float speed, float reach)
        {
            Vector3 d = point - transform.position;
            if (d.sqrMagnitude <= reach * reach) return true;
            transform.position = Vector3.MoveTowards(transform.position, point, speed * Time.deltaTime);
            Face(d);
            return false;
        }

        private void Face(Vector3 dir)
        {
            dir.y *= 0.3f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        /// <summary>The falcon stays with Aron: the played hero, or the partner while Noura is being played.</summary>
        private Transform Aron()
        {
            var team = Heroes.HeroTeam.Instance;
            if (team != null && team.NouraActive && team.Partner != null) return team.Partner;
            return player.transform;
        }

        private Vector3 Home()
        {
            var t = Aron();
            float h = player.BodyHeight;
            return t.position + t.right * shoulder.x + Vector3.up * (h + shoulder.y - 0.3f) + t.forward * shoulder.z;
        }

        private Vector3 Perch() => arm != null && Aron() == player.transform ? arm.position + Vector3.up * 0.1f : Home() - Vector3.up * 0.4f;

        private static Spot ToSpot(Vector3 p) => new Spot(p.x, p.y, p.z);

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }
    }
}
