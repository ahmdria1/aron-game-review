// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Audio;
using Aron.Characters;
using Aron.Config;
using Aron.Core;
using Aron.Dialogue;
using Aron.Fx;
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>
    /// Region boss; in Riyadh it's Dr. Duhaim («د. دحيم»). Loop: stalk -> telegraph -> attack -> stunned.
    /// Attacks are charge, wave (stomp), volley (lobbed shots), leap and dash (three short charges); every one
    /// except the stomp leaves it dizzy, and it can only be hurt while dizzy. Phases from <see cref="BossConfig"/>
    /// change speed and attacks and can summon minions.
    /// </summary>
    public class RiyadhBoss : MonoBehaviour, IDamageable
    {
        public enum BossState { Idle, Stalk, Telegraph, Charge, Shockwave, Stunned, Dead }

        [SerializeField] private BossConfig config;
        [SerializeField] private GameObject shockwavePrefab;
        [SerializeField] private Enemy minionPrefab;
        [SerializeField] private Transform[] minionSpawns;
        [SerializeField] private float arenaRadius = 14f;
        [SerializeField] private DialogueSpeaker speaker;
        [SerializeField] private Animator animator;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private Vector3 lastPos;

        public BossState State { get; private set; } = BossState.Idle;
        public int PhaseIndex { get; private set; } = -1;
        private int health;
        private float invuln;
        private Vector3 arenaCentre;
        private PlayerController player;
        private CharacterEmotion emotion;

        public bool CanBeDamaged => State == BossState.Stunned && invuln <= 0f;
        /// <summary>The boss the player is fighting right now, for the HUD.</summary>
        public static RiyadhBoss Current { get; private set; }
        public string DisplayName => config != null ? config.bossNameAr : "";
        private BossPhase Phase => config.phases[Mathf.Clamp(PhaseIndex, 0, config.phases.Length - 1)];

        private void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<BossConfig>();
            health = config.maxHealth;
            Fx.DropShadow.Ensure(gameObject, 0f, 0f, true);
            UI.HazardMarker.Attach(gameObject, 1.4f);
            arenaCentre = transform.position;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            renderers = GetComponentsInChildren<Renderer>();
            emotion = GetComponentInChildren<CharacterEmotion>();
            lastPos = transform.position;
        }

        /// <summary>Which boss barks to use (the region's boss model, e.g. shark or golem).</summary>
        public string VoiceModel { get; set; } = "scientist";
        private float hurtBarkAt = -10f;

        /// <summary>Called by the arena trigger when the player enters.</summary>
        public void Activate()
        {
            if (State != BossState.Idle) return;
            player = FindFirstObjectByType<PlayerController>();
            Current = this;
            var lvl = GameManager.Instance != null ? GameManager.Instance.Level : null;
            AudioManager.PlayMusic(lvl != null ? lvl.bossMusicTrack : MusicCues.StageBoss);
            GameEvents.RaiseBossEncounterStarted();
            AudioManager.PlayVoice(AudioCues.Boss(VoiceModel, AudioCues.BossTaunt));
            Feel(Emotion.Anger, 2.5f);
            UpdatePhase();
            StartCoroutine(Brain());
        }

        /// <summary>Swaps in another boss before the fight starts (stage scenes build one per region).</summary>
        public void Configure(BossConfig c, Animator anim)
        {
            if (State != BossState.Idle || c == null) return;
            config = c;
            health = c.maxHealth;
            UseBody(anim);
            arenaCentre = transform.position;
        }

        /// <summary>Picks up a freshly built model: its renderers for the hit flash, its face and animator if any.</summary>
        public void UseBody(Animator anim)
        {
            animator = anim;
            renderers = GetComponentsInChildren<Renderer>();
            emotion = GetComponentInChildren<CharacterEmotion>();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private IEnumerator Brain()
        {
            while (State != BossState.Dead)
            {
                State = BossState.Stalk;
                float t = Phase.attackInterval;
                while (t > 0f && State == BossState.Stalk)
                {
                    t -= Time.deltaTime;
                    Face(player.transform.position, 360f);
                    Vector3 to = Flat(player.transform.position - transform.position);
                    if (to.magnitude > 4f) StepTowards(player.transform.position, Phase.moveSpeed);
                    yield return null;
                }

                string attack = PickAttack();
                State = BossState.Telegraph;
                Feel(Emotion.Anger, 0.8f);
                yield return Flash(attack == "wave" ? Color.yellow : attack == "volley" ? config.projectileColor : Color.red, 0.7f);

                switch (attack)
                {
                    case "wave":
                        State = BossState.Shockwave;
                        yield return Hop();
                        SpawnShockwave();
                        FollowCamera.Shake(0.4f);
                        yield return new WaitForSeconds(0.6f);
                        continue; // shockwaves don't leave the boss stunned
                    case "volley":
                        State = BossState.Shockwave;
                        yield return Volley();
                        break;
                    case "leap":
                        State = BossState.Charge;
                        yield return Leap();
                        break;
                    case "dash":
                        State = BossState.Charge;
                        for (int i = 0; i < 3 && State == BossState.Charge; i++)
                        {
                            yield return Charge(arenaRadius * 0.7f, 1.25f);
                            if (i < 2) { Face(player.transform.position, 9999f); yield return new WaitForSeconds(0.25f); }
                        }
                        break;
                    default:
                        State = BossState.Charge;
                        yield return Charge(arenaRadius * 1.8f, 1f);
                        break;
                }

                State = BossState.Stunned;
                GameEvents.PlaySfx("bounce");
                if (speaker != null) speaker.Say(config.stunLineAr, 1.5f);
                Feel(Emotion.Worry, config.stunDuration);
                float s = config.stunDuration * (attack == "volley" ? 0.8f : 1f);
                while (s > 0f && State == BossState.Stunned)
                {
                    s -= Time.deltaTime;
                    transform.Rotate(0f, 200f * Time.deltaTime, 0f);
                    yield return null;
                }
            }
        }

        /// <summary>The old Riyadh fight when a phase lists no attacks: charges, and a stomp now and then.</summary>
        private string PickAttack()
        {
            var list = Phase.attacks;
            if (list == null || list.Length == 0) return Phase.useShockwave && Random.value < 0.4f ? "wave" : "charge";
            int n = 0;
            foreach (var a in list) if (a != "summon") n++;
            if (n == 0) return "charge";
            int pick = Random.Range(0, n);
            foreach (var a in list)
            {
                if (a == "summon") continue;
                if (pick-- == 0) return a;
            }
            return "charge";
        }

        private IEnumerator Charge(float distance, float speedScale)
        {
            Vector3 dir = Flat(player.transform.position - transform.position).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            transform.rotation = Quaternion.LookRotation(dir);
            float travelled = 0f;
            while (travelled < distance)
            {
                float step = Phase.chargeSpeed * speedScale * Time.deltaTime;
                Vector3 next = transform.position + dir * step;
                if (Flat(next - arenaCentre).magnitude > arenaRadius) break; // hit the arena wall
                transform.position = next;
                travelled += step;
                yield return null;
            }
        }

        /// <summary>Lobs a few things at where the player is standing, then stops for breath.</summary>
        private IEnumerator Volley()
        {
            int count = 3 + Mathf.Min(PhaseIndex, 2);
            for (int i = 0; i < count; i++)
            {
                Face(player.transform.position, 9999f);
                Vector3 target = player.transform.position + Flat(Random.insideUnitSphere) * 1.2f;
                if (Flat(target - arenaCentre).magnitude > arenaRadius) target = arenaCentre + Flat(target - arenaCentre).normalized * arenaRadius;
                BossProjectile.Throw(transform.position + Vector3.up * 1.8f * transform.localScale.y, target, 1.1f, config.projectileColor, shockwavePrefab);
                GameEvents.PlaySfx("swing");
                yield return new WaitForSeconds(0.45f);
            }
            yield return new WaitForSeconds(0.4f);
        }

        /// <summary>Jumps onto the player and lands with a small ring; the landing leaves it dizzy.</summary>
        private IEnumerator Leap()
        {
            Vector3 from = transform.position;
            Vector3 off = Vector3.ClampMagnitude(Flat(player.transform.position - arenaCentre), arenaRadius - 1f);
            Vector3 to = new Vector3(arenaCentre.x + off.x, from.y, arenaCentre.z + off.z);
            const float time = 0.9f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float f = t / time;
                transform.position = Vector3.Lerp(from, to, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * 5f;
                yield return null;
            }
            transform.position = to;
            FollowCamera.Shake(0.5f);
            FxPlayer.Play("dust", transform.position);
            SpawnShockwave(0.5f);
        }

        private void Update()
        {
            if (invuln > 0f) invuln -= Time.deltaTime;
            if (animator != null && animator.runtimeAnimatorController != null && Time.deltaTime > 0f)
            {
                float speed = Flat(transform.position - lastPos).magnitude / Time.deltaTime;
                animator.SetFloat(SpeedParam, State == BossState.Stunned ? 0f : speed / 4f);
            }
            lastPos = transform.position;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!CanBeDamaged) return;
            health -= Mathf.Max(1, info.amount);
            invuln = config.invulnerableAfterHit;
            GameEvents.PlaySfx("enemy");
            FxPlayer.Play("hit", transform.position + Vector3.up);
            FollowCamera.Shake(0.35f);
            GameEvents.RaiseBossHealthChanged(Mathf.Clamp01((float)health / config.maxHealth));
            StartCoroutine(Flash(Color.white, 0.15f));
            Feel(Emotion.Anger, 1.2f);
            if (health <= 0) { Die(); return; }
            if (Time.time - hurtBarkAt > 2f) { hurtBarkAt = Time.time; AudioManager.PlayVoice(AudioCues.Boss(VoiceModel, AudioCues.BossHurt)); }
            int before = PhaseIndex;
            UpdatePhase();
            if (PhaseIndex != before) State = BossState.Stalk; // phase change ends stun immediately
        }

        private void Feel(Emotion e, float duration, bool big = false)
        {
            if (emotion != null) emotion.Feel(e, duration, big);
        }

        private void UpdatePhase()
        {
            float frac = (float)health / config.maxHealth;
            int idx = 0;
            for (int i = 0; i < config.phases.Length; i++) if (frac <= config.phases[i].healthThreshold) idx = i;
            if (idx == PhaseIndex) return;
            PhaseIndex = idx;
            GameEvents.RaiseBossPhaseChanged(idx + 1);
            if (speaker != null && !string.IsNullOrEmpty(Phase.tauntLineAr)) speaker.Say(Phase.tauntLineAr, 2.5f);
            if (Phase.summonMinions || System.Array.IndexOf(Phase.attacks ?? new string[0], "summon") >= 0) SummonMinions();
        }

        private void SummonMinions()
        {
            if (minionPrefab == null || minionSpawns == null) return;
            foreach (var sp in minionSpawns) if (sp != null) Instantiate(minionPrefab, sp.position, sp.rotation).gameObject.SetActive(true);
        }

        private void Die()
        {
            State = BossState.Dead;
            StopAllCoroutines();
            if (speaker != null) speaker.Say(config.defeatLineAr, 3f);
            AudioManager.PlayVoice(AudioCues.Boss(VoiceModel, AudioCues.BossDefeat));
            Feel(Emotion.Sadness, 4f, true);
            GameEvents.RaiseBossDefeated();
            if (GameManager.Instance != null) AudioManager.PlayMusic(GameManager.Instance.Level != null ? GameManager.Instance.Level.musicTrack : MusicCues.SaudiNajd);
            StartCoroutine(Shrink());
        }

        private IEnumerator Shrink()
        {
            Vector3 s = transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime) { transform.localScale = s * (1f - t); yield return null; }
            gameObject.SetActive(false);
        }

        private IEnumerator Flash(Color c, float duration)
        {
            if (renderers == null || renderers.Length == 0) { yield return new WaitForSeconds(duration); yield break; }
            if (block == null) block = new MaterialPropertyBlock();
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                bool on = Aron.Logic.Access.FlashSafety.BlinkOn(Aron.Save.SaveSystem.Data.access, t, 8f, 0.5f);
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                foreach (var r in renderers) if (r != null) r.SetPropertyBlock(on ? block : null);
                yield return null;
            }
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(null);
        }

        private IEnumerator Hop()
        {
            Vector3 p0 = transform.position;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { transform.position = p0 + Vector3.up * Mathf.Sin(t / 0.5f * Mathf.PI) * 2.5f; yield return null; }
            transform.position = p0;
        }

        private void SpawnShockwave(float radiusScale = 1f)
        {
            if (shockwavePrefab == null) return;
            float groundY = Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.transform != transform ? hit.point.y : transform.position.y - 1.6f;
            var go = Instantiate(shockwavePrefab, new Vector3(transform.position.x, groundY + 0.05f, transform.position.z), Quaternion.identity);
            go.SetActive(true);
            var sw = go.GetComponent<BossShockwave>();
            if (sw != null) { sw.maxRadius = config.shockwaveRadius * radiusScale; sw.speed = config.shockwaveSpeed; }
        }

        private void StepTowards(Vector3 target, float speed)
        {
            Vector3 d = Flat(target - transform.position);
            transform.position += d.normalized * Mathf.Min(speed * Time.deltaTime, d.magnitude);
        }

        private void Face(Vector3 target, float degPerSec)
        {
            Vector3 d = Flat(target - transform.position);
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), degPerSec * Time.deltaTime);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void OnTriggerStay(Collider other)
        {
            if (State == BossState.Dead || State == BossState.Stunned || State == BossState.Idle) return;
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) p.Hurt(true);
        }
    }
}
