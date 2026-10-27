// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Player;
using UnityEngine;

namespace Aron.Store
{
    public enum ShopKind { Developer, Grocery }

    /// <summary>
    /// One of Ahmed's real shops on the street. Tapping the front, or walking up to the door, asks whether to
    /// go in. The trigger on this object is the walk-up zone; solid colliders under it are what a tap has to hit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShopFront : MonoBehaviour
    {
        private const float TapReach = 45f;

        [SerializeField] private ShopKind kind;
        [SerializeField] private Renderer doorGlow;
        [SerializeField] private Color glowColor = new Color(0.2f, 0.9f, 1f);
        [SerializeField] private Transform bubble;

        private static readonly List<ShopFront> all = new List<ShopFront>();
        private static readonly RaycastHit[] hits = new RaycastHit[16];

        private MaterialPropertyBlock block;
        private Vector3 bubbleRest;
        private bool playerInside;
        private Camera cam;

        public ShopKind Kind => kind;

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (bubble != null) bubbleRest = bubble.localPosition;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            if (doorGlow != null)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(t * 3f);
                doorGlow.GetPropertyBlock(block);
                block.SetColor("_BaseColor", glowColor * (0.7f + 0.3f * pulse));
                block.SetColor("_EmissionColor", glowColor * (1.5f * pulse));
                doorGlow.SetPropertyBlock(block);
            }
            if (bubble != null)
            {
                bubble.localPosition = bubbleRest + Vector3.up * (Mathf.Sin(t * 2.2f) * 0.12f);
                if (cam == null) cam = Camera.main;
                if (cam != null) bubble.rotation = Quaternion.LookRotation(bubble.position - cam.transform.position, Vector3.up);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playerInside || other.GetComponentInParent<PlayerController>() == null) return;
            playerInside = true;
            Ask();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) playerInside = false;
        }

        public void Ask()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.IsOver || gm.IsPaused || gm.InStore)) return;
            StoreScreen.Ask(kind);
        }

        /// <summary>
        /// Hooked into the touch controls: takes taps that land on a shop front, and every tap while a store is
        /// open, so none of them turn into attacks.
        /// </summary>
        public static bool ClaimTap(float x, float y)
        {
            if (StoreScreen.IsShowing) return true;
            if (all.Count == 0) return false;
            var cam = Camera.main;
            var player = PlayerController.Active;
            if (cam == null || player == null) return false;

            var ray = cam.ScreenPointToRay(new Vector3(x, y, 0f));
            int n = Physics.RaycastNonAlloc(ray, hits, 120f, ~0, QueryTriggerInteraction.Collide);
            ShopFront best = null;
            float bestDist = float.MaxValue;
            float blocker = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(player.transform)) continue;
                var shop = col.GetComponentInParent<ShopFront>();
                if (shop != null)
                {
                    // The walk-up zone lies on the road; a tap there is an attack, not a visit.
                    if (col.isTrigger) continue;
                    if (hits[i].distance < bestDist) { bestDist = hits[i].distance; best = shop; }
                    continue;
                }
                // An enemy or crate in front of the shop gets the hit instead.
                if (col.GetComponentInParent<IDamageable>() != null && hits[i].distance < blocker) blocker = hits[i].distance;
            }
            if (best == null || blocker < bestDist) return false;
            if ((best.transform.position - player.transform.position).sqrMagnitude > TapReach * TapReach) return false;
            best.Ask();
            return true;
        }
    }
}
