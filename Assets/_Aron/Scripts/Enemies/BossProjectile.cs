// (c) Ahmed Alamrani. All rights reserved.
using Aron.Fx;
using Aron.Player;
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>Something a boss lobs: a date crate, a pearl, a lump of lava. Lands where the player was standing.</summary>
    public class BossProjectile : MonoBehaviour
    {
        private Vector3 from, to;
        private float time, t;
        private PlayerController player;

        public static void Throw(Vector3 from, Vector3 to, float flightTime, Color color, GameObject materialSource)
        {
            var go = PrimitiveFactory.Create(PrimitiveType.Sphere);
            go.name = "BossShot";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.7f;
            var r = go.GetComponent<Renderer>();
            var src = materialSource != null ? materialSource.GetComponentInChildren<Renderer>(true) : null;
            if (src != null) r.sharedMaterial = src.sharedMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
            var p = go.AddComponent<BossProjectile>();
            p.from = from; p.to = to; p.time = Mathf.Max(0.3f, flightTime);
        }

        private void Start() { player = FindFirstObjectByType<PlayerController>(); UI.HazardMarker.Attach(gameObject, 0.5f); }

        private void Update()
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / time);
            float arc = Mathf.Max(3f, Vector3.Distance(from, to) * 0.35f);
            transform.position = Vector3.Lerp(from, to, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * arc;
            transform.Rotate(400f * Time.deltaTime, 300f * Time.deltaTime, 0f);
            if (player != null && (player.transform.position + Vector3.up - transform.position).sqrMagnitude < 0.9f) { Land(true); return; }
            if (f >= 1f) Land(player != null && (player.transform.position - to).sqrMagnitude < 1.2f);
        }

        private void Land(bool hit)
        {
            if (hit && player != null) player.Hurt(true);
            FxPlayer.Play("poof", transform.position);
            Destroy(gameObject);
        }
    }
}
