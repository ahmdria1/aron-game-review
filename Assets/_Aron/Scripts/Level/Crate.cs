// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Audio;
using Aron.Core;
using Aron.Fx;
using Aron.Player;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>Breakable crate. Basic = dates, Bounce = springs player (breaks only by attack), Life = +1 life,
    /// Checkpoint = the lantern box (breaks on touch too, saves the spot and leaves a lit lantern),
    /// Bisht = the golden bisht (a wooden box with gold trim).</summary>
    public class Crate : MonoBehaviour, IDamageable
    {
        public enum CrateType { Basic, Bounce, Life, Checkpoint, Bisht }

        [SerializeField] private CrateType type = CrateType.Basic;
        [SerializeField] private int dates = 5;
        [SerializeField] private Transform visual;

        private bool broken;
        public CrateType Type => type;
        public bool CanBeDamaged => !broken;

        public void Setup(CrateType t, int dateCount)
        {
            type = t; dates = dateCount;
            Dress();
        }

        private void Start()
        {
            Dress();
            Fx.DropShadow.Ensure(gameObject);
        }

        private bool dressed;

        private void Dress()
        {
            if (dressed) return;
            dressed = true;
            if (type == CrateType.Bisht) GoldTrim.Apply(this);
            if (type == CrateType.Checkpoint)
            {
                Lantern.Mark(this);
                if (GetComponent<TouchBreak>() == null) gameObject.AddComponent<TouchBreak>();
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (broken) return;
            if (type == CrateType.Bounce && info.kind == DamageKind.Stomp)
            {
                var p = info.instigator != null ? info.instigator.GetComponent<PlayerController>() : null;
                if (p != null) p.BounceCrate();   // one height for every spring crate (Game Feel > crateBounceHeight)
                if (visual != null) StartCoroutine(Squash());
                FxPlayer.Play("dust", transform.position + Vector3.up);
                return; // spring crates survive stomps
            }
            Break(info);
        }

        private void Break(DamageInfo info)
        {
            broken = true;
            Vector3 centre = transform.position + Vector3.up * 0.5f;
            Sfx.Play("crate_break", centre);
            FxPlayer.Play("crate", centre);
            GameEvents.RaiseCrateBroken(centre);
            if (type == CrateType.Life) FxPlayer.Play("coin", centre);
            if (type == CrateType.Bisht) { FxPlayer.Play("sparkle", centre); GameEvents.PlaySfx(Logic.AudioCues.BishtBox); }
            FollowCamera.Shake(0.12f);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                switch (type)
                {
                    case CrateType.Basic: case CrateType.Bounce: gm.AddDates(dates); break;
                    case CrateType.Life: gm.AddLife(); break;
                    case CrateType.Checkpoint:
                        gm.SetCheckpoint(transform.position + Vector3.up * 0.5f);
                        Lantern.Light(transform.parent, transform.position);
                        break;
                }
                gm.BoxBroken(type == CrateType.Bisht);
            }
            if (type == CrateType.Bisht)
            {
                var p = info.instigator != null ? info.instigator.GetComponentInParent<PlayerController>() : null;
                if (p == null) p = PlayerController.Active;
                if (p != null && p.Bisht != null) p.Bisht.Collect();
            }
            Destroy(gameObject);
        }

        private IEnumerator Squash()
        {
            Vector3 rest = visual.localScale;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / 0.3f * Mathf.PI) * (1f - t / 0.3f);
                visual.localScale = Vector3.Scale(rest, new Vector3(1f + k * 0.35f, 1f - k * 0.45f, 1f + k * 0.35f));
                yield return null;
            }
            visual.localScale = rest;
        }
    }
}
