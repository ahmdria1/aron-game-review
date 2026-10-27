// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Fx;
using Aron.Logic.Heroes;
using UnityEngine;

namespace Aron.Heroes
{
    /// <summary>Noura's handbag flying out and back. Hits anything on the way, both ways.</summary>
    public class HandbagThrow : MonoBehaviour
    {
        private Boomerang path;
        private Transform owner;
        private Vector3 dir;
        private readonly Collider[] hits = new Collider[12];
        private readonly System.Collections.Generic.HashSet<Object> struck = new System.Collections.Generic.HashSet<Object>();
        private bool wasReturning;

        public static void Launch(HeroTeam team, Boomerang path, Vector3 dir)
        {
            var go = new GameObject("Handbag");
            var b = go.AddComponent<HandbagThrow>();
            b.path = path; b.owner = team.transform; b.dir = dir;
            var body = PrimitiveFactory.Create(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.34f, 0.26f, 0.13f);
            var strap = PrimitiveFactory.Create(PrimitiveType.Cylinder);
            Destroy(strap.GetComponent<Collider>());
            strap.transform.SetParent(go.transform, false);
            strap.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            strap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            strap.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.material.color = new Color(0.55f, 0.12f, 0.3f);
            go.transform.position = team.transform.position + Vector3.up * 1.1f;
            GameEvents.PlaySfx("spin");
            Audio.AudioManager.PlayVoice(Aron.Logic.AudioCues.VoNouraBagThrow, "noura");
        }

        private void Update()
        {
            if (owner == null) { Destroy(gameObject); return; }
            float d = path.Distance(Time.time);
            if (!path.Out) { Destroy(gameObject); return; }
            bool back = path.Returning(Time.time);
            if (back && !wasReturning) struck.Clear();   // can hit the same thing again on the way back
            wasReturning = back;
            transform.position = owner.position + Vector3.up * 1.1f + dir * d;
            transform.Rotate(0f, 900f * Time.deltaTime, 0f, Space.World);

            int n = Physics.OverlapSphereNonAlloc(transform.position, 0.6f, hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                if (hits[i].transform.IsChildOf(owner)) continue;
                var dmg = hits[i].GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.CanBeDamaged) continue;
                var key = (Object)(dmg as Component);
                if (key == null || !struck.Add(key)) continue;
                dmg.TakeDamage(new DamageInfo(1, DamageKind.Melee, transform.position, owner.gameObject));
                FxPlayer.Play("hit", transform.position);
            }
        }
    }
}
