// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Characters;
using Aron.Config;
using Aron.Core;
using Aron.Fx;
using Aron.Level;
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>Patrols between two points; chases the player when close (if config allows).</summary>
    public class Enemy : MonoBehaviour, IDamageable
    {
        public enum State { Patrol, Chase, Return, Dead }

        [SerializeField] protected EnemyConfig config;
        [SerializeField] private Vector3 patrolOffset = new Vector3(4f, 0f, 0f);
        [SerializeField] private GameObject datePrefab;
        [SerializeField] private Animator animator;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int HurtParam = Animator.StringToHash("Hurt");
        private static readonly int DieParam = Animator.StringToHash("Die");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public State CurrentState { get; private set; }
        protected int health;
        private Vector3 a, b, home;
        private bool toB = true;
        private Transform player;
        private PlayerController playerCtl;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float stagger, moved;
        private Vector3 knockback;
        private CharacterEmotion emotion;

        public bool CanBeDamaged => CurrentState != State.Dead && stagger <= 0f;

        /// <summary>Live enemies, for the falcon to pick from.</summary>
        public static readonly List<Enemy> All = new List<Enemy>();

        /// <summary>For enemies placed from code before they wake up.</summary>
        public void SetPatrol(Vector3 offset) => patrolOffset = offset;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        protected virtual void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<EnemyConfig>();
            health = config.health;
            Fx.DropShadow.Ensure(gameObject);
            home = a = transform.position;
            b = a + patrolOffset;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            renderers = GetComponentsInChildren<Renderer>();
            emotion = GetComponentInChildren<CharacterEmotion>();
        }

        protected virtual void Start()
        {
            playerCtl = FindFirstObjectByType<PlayerController>();
            UI.HazardMarker.Attach(gameObject);
            if (playerCtl != null) player = playerCtl.transform;
        }

        protected virtual void Update()
        {
            if (CurrentState == State.Dead || player == null) return;
            moved = 0f;
            if (stagger > 0f)
            {
                stagger -= Time.deltaTime;
                transform.position += knockback * Time.deltaTime;
                knockback = Vector3.Lerp(knockback, Vector3.zero, Time.deltaTime * 8f);
                UpdateAnimator();
                return;
            }
            float dist = Vector3.Distance(Flat(transform.position), Flat(player.position));
            bool sameHeight = Mathf.Abs(player.position.y - transform.position.y) < 3f;

            switch (CurrentState)
            {
                case State.Patrol:
                    MoveTo(toB ? b : a, config.patrolSpeed);
                    if (Vector3.Distance(Flat(transform.position), Flat(toB ? b : a)) < 0.2f) toB = !toB;
                    if (config.canChase && dist < config.detectRadius && sameHeight)
                    {
                        CurrentState = State.Chase;
                        if (emotion != null) emotion.Feel(Emotion.Anger, 1.5f);
                    }
                    break;
                case State.Chase:
                    MoveTo(player.position, config.chaseSpeed);
                    if (dist > config.loseRadius || !sameHeight || Vector3.Distance(transform.position, home) > config.loseRadius * 1.5f)
                        CurrentState = State.Return;
                    break;
                case State.Return:
                    MoveTo(home, config.patrolSpeed);
                    if (Vector3.Distance(Flat(transform.position), Flat(home)) < 0.3f) CurrentState = State.Patrol;
                    break;
            }
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat(SpeedParam, moved / Mathf.Max(0.01f, config.chaseSpeed));
        }

        private void MoveTo(Vector3 target, float speed)
        {
            Vector3 dir = Flat(target) - Flat(transform.position);
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.position += Vector3.ClampMagnitude(dir.normalized * speed * Time.deltaTime, dir.magnitude);
            moved = speed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), config.turnSpeed * Time.deltaTime);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public virtual void TakeDamage(DamageInfo info)
        {
            if (!CanBeDamaged) return;
            // Like the old platformers: one stomp or one hit from the agal or the handbag and it's gone.
            health -= Mathf.Max(health, info.amount);
            if (health <= 0) { Kill(); return; }
            GameEvents.PlaySfx("hit");
            Vector3 away = Flat(transform.position - info.sourcePosition);
            knockback = (away.sqrMagnitude > 0.001f ? away.normalized : -transform.forward) * 6f;
            stagger = 0.35f;
            if (emotion != null) emotion.Feel(health == 1 ? Emotion.Fear : Emotion.Anger, 1.4f);
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(HurtParam);
            StartCoroutine(HitFlash());
        }

        private System.Collections.IEnumerator HitFlash()
        {
            if (block == null) block = new MaterialPropertyBlock();
            var tint = new Color(1f, 0.25f, 0.2f);
            block.SetColor(BaseColorId, tint);
            block.SetColor(ColorId, tint);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(block);
            yield return new WaitForSeconds(0.12f);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(null);
        }

        protected virtual void Kill()
        {
            CurrentState = State.Dead;
            if (emotion != null) emotion.Feel(Emotion.Sadness, 2f);
            GameEvents.PlaySfx("enemy");
            FxPlayer.Play("poof", transform.position + Vector3.up * 0.8f);
            FollowCamera.Shake(0.2f);
            for (int i = 0; i < config.datesDropped; i++)
            {
                if (datePrefab != null) Instantiate(datePrefab, transform.position + Vector3.up + Random.insideUnitSphere * 0.5f, Quaternion.identity).SetActive(true);
                else if (GameManager.Instance != null) GameManager.Instance.AddDates(1);
            }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            StartCoroutine(SquashAndDestroy());
        }

        private System.Collections.IEnumerator SquashAndDestroy()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(DieParam);
                yield return new WaitForSeconds(0.6f);
            }
            Vector3 s = transform.localScale;
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                transform.localScale = new Vector3(s.x * (1f + t * 2f), s.y * (1f - t * 3.5f), s.z * (1f + t * 2f));
                yield return null;
            }
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other) => TouchPlayer(other);
        private void OnTriggerStay(Collider other) => TouchPlayer(other);

        private void TouchPlayer(Collider other)
        {
            if (CurrentState == State.Dead) return;
            var p = other.GetComponentInParent<PlayerController>();
            if (p == null) return;
            float above = p.transform.position.y - transform.position.y;
            bool invincible = p.Bisht != null && p.Bisht.Invincible;
            switch (Aron.Logic.HeroContact.Resolve(p.IsFalling, above, p.IsAttacking, invincible, p.IsInvulnerable))
            {
                case Aron.Logic.ContactResult.EnemyDefeated:
                    bool stomp = p.IsFalling && above > Aron.Logic.HeroContact.StompHeight;
                    TakeDamage(new DamageInfo(1, stomp ? DamageKind.Stomp : DamageKind.Spin, p.transform.position, p.gameObject));
                    if (stomp) { p.StompBounce(); GameEvents.PlaySfx(Aron.Logic.AudioCues.EnemyStomp); Aron.Audio.AudioManager.Bark(Aron.Logic.AudioCues.VoAronStomp, null); }
                    break;
                case Aron.Logic.ContactResult.HeroHurt:
                    p.Hurt();
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 start = Application.isPlaying ? a : transform.position;
            Gizmos.color = Color.yellow; Gizmos.DrawLine(start, start + patrolOffset);
            if (config != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, config.detectRadius); }
        }
    }
}
