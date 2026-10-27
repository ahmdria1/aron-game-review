// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Fx;
using Aron.Input;
using Aron.Localization;
using Aron.Logic.Heroes;
using Aron.Player;
using Aron.UI;
using UnityEngine;

namespace Aron.Heroes
{
    /// <summary>A basket of dates tucked away somewhere only teamwork or Noura gets to.</summary>
    public class DateBasket : MonoBehaviour
    {
        public static readonly List<DateBasket> Open = new List<DateBasket>();
        [SerializeField] private int dates = 6;
        public void Setup(int n) => dates = n;

        private void OnEnable() => Open.Add(this);
        private void OnDisable() => Open.Remove(this);

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null) GameManager.Instance.AddDates(dates);
            FxPlayer.Play("coin", transform.position + Vector3.up * 0.5f);
            GameEvents.PlaySfx("coin");
            gameObject.SetActive(false);
        }
    }

    /// <summary>Barred door Noura picks open when she walks up to it. Aron gets told who can.</summary>
    public class LockedGate : MonoBehaviour
    {
        [SerializeField] private Transform bars;
        private bool open, hinted;
        public void Setup(Transform b) => bars = b;

        private bool keyTrip;

        private void Update()
        {
            var team = HeroTeam.Instance;
            if (open || team == null || bars == null) return;
            float dActive = Vector3.Distance(team.transform.position, transform.position);
            var h = team.ActiveHero;
            bool canPick = h != null && h.opensLocks;
            // Someone who cannot pick the lock still has the falcon: it flies up for the key that hangs above the gate.
            if (!canPick && !keyTrip && dActive < Coop.Reach + 3f)
            {
                var falcon = Object.FindFirstObjectByType<Animals.FalconCompanion>();
                if (falcon != null && falcon.FetchKey(transform.position + Vector3.up * 3.6f, OpenWithKey)) { keyTrip = true; GameUI.Banner(Strings.Get("falcon_key"), 1.6f); }
            }
            if (canPick && Coop.Ready(CoopKind.LockedGate, 99f, dActive, team.Active))
            {
                open = true;
                GameEvents.PlaySfx("checkpoint");
                FxPlayer.Play("sparkle", bars.position);
                GameUI.Banner(Strings.Get("lock_open"), 1.4f);
            }
            else if (!canPick && !hinted && dActive < Coop.Reach)
            {
                hinted = true;
                GameUI.Banner(Strings.Get("lock_hint"), 2f);
            }
        }

        private void OpenWithKey()
        {
            if (open) return;
            open = true;
            GameEvents.PlaySfx("checkpoint");
            FxPlayer.Play("sparkle", bars.position);
            GameUI.Banner(Strings.Get("falcon_unlocked"), 1.4f);
        }

        private void LateUpdate()
        {
            if (!open || bars == null || !bars.gameObject.activeSelf) return;
            bars.localPosition += Vector3.down * 2.5f * Time.deltaTime;
            if (bars.localPosition.y < -2.4f) bars.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// A slot only Noura fits through. The walls do the work (she is slimmer); this only tells Aron about it once.
    /// </summary>
    public class NarrowGap : MonoBehaviour
    {
        [SerializeField] private float width = 0.8f;
        private bool hinted;
        public void Setup(float w) => width = w;

        private void Update()
        {
            var team = HeroTeam.Instance;
            if (hinted || team == null) return;
            var h = team.ActiveHero;
            if (h == null || Coop.Fits(h, width)) return;
            if (Vector3.Distance(team.transform.position, transform.position) > Coop.Reach) return;
            hinted = true;
            GameUI.Banner(Strings.Get("gap_hint"), 2f);
        }
    }

    /// <summary>Planks that are only there for someone who sees hidden paths (Noura).</summary>
    public class SecretRoute : MonoBehaviour
    {
        private Renderer[] rends;
        private Collider[] cols;
        private bool shown = true;
        public static readonly List<SecretRoute> All = new List<SecretRoute>();

        private void Awake()
        {
            rends = GetComponentsInChildren<Renderer>(true);
            cols = GetComponentsInChildren<Collider>(true);
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>The falcon's scouting sparkle: marks the path for someone who cannot see it (it stays hidden and unwalkable).</summary>
        public void Ping() => FxPlayer.Play("sparkle", transform.position + Vector3.up);

        private void Update()
        {
            var team = HeroTeam.Instance;
            var h = team != null ? team.ActiveHero : null;
            bool see = h != null && h.seesSecrets;
            if (see == shown) return;
            shown = see;
            foreach (var r in rends) if (r != null) r.enabled = see;
            foreach (var c in cols) if (c != null && !c.isTrigger) c.enabled = see;
            if (see) FxPlayer.Play("sparkle", transform.position + Vector3.up);
        }
    }

    /// <summary>
    /// A ledge too high for one. Standing at its foot with the partner close, a flick up gets a lift:
    /// Aron boosts Noura up, or Noura pulls Aron up.
    /// </summary>
    public class CoopLedge : MonoBehaviour
    {
        [SerializeField] private Vector3 top = new Vector3(0f, 3.4f, 0f);
        private bool hintedWait, hintedGo;
        public void Setup(Vector3 t) => top = t;

        private void Update()
        {
            var team = HeroTeam.Instance;
            if (team == null || team.Partner == null) return;
            float dA = Vector3.Distance(team.transform.position, transform.position);
            if (dA > Coop.Reach) return;
            float dP = Vector3.Distance(team.Partner.position, transform.position);
            var kind = team.NouraActive ? CoopKind.BoostUp : CoopKind.PullUp;
            // Distances by who is who: Aron's first.
            float aron = team.NouraActive ? dP : dA, noura = team.NouraActive ? dA : dP;
            if (!Coop.Ready(kind, aron, noura, team.Active))
            {
                if (!hintedWait) { hintedWait = true; GameUI.Banner(Strings.Get("ledge_wait"), 1.8f); }
                return;
            }
            if (!hintedGo) { hintedGo = true; GameUI.Banner(Strings.Get("ledge_hint"), 1.8f); }
            var intent = InputRouter.Instance != null ? InputRouter.Instance.Current : default;
            if (!intent.jumpPressed) return;
            var pc = team.GetComponent<PlayerController>();
            pc.Teleport(transform.TransformPoint(top));
            FxPlayer.Play("dust", transform.position);
            GameEvents.PlaySfx("doublejump");
        }
    }
}
