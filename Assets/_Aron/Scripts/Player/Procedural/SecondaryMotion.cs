// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Config;
using Aron.Logic.GameFeel;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Spring-and-verlet secondary motion for the shemagh tails and the thobe or abaya hem. It looks for bone chains named like
    /// <c>ShemaghTail_L_0 → _1 → _2</c> or <c>ThobeHem_0 → ...</c> (see <see cref="SecondaryMotionRules"/>; a bone with "sleeve" in its name is never touched, the sleeves stay
    /// rolled, and Noura's <c>AbayaHem</c> chains swing less so wind never lifts it). After the animation has posed the bones, each chain is stepped by
    /// <see cref="VerletChain"/> and the bones are aimed at the simulated joints. The greybox rig has no such bones, so this does nothing there until a rig with them is dropped in.
    /// Runs only inside the procedural budget (off on the Low tier).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SecondaryMotion : MonoBehaviour
    {
        private class Chain
        {
            public Transform[] bones;
            public VerletChain sim;
            public float maxAngle;
            public bool snapped;
        }

        private readonly List<Chain> chains = new List<Chain>();
        private PlayerController player;
        private ProceduralAnimator proc;

        public int ChainCount => chains.Count;

        public void Rebuild(Transform root, PlayerController pc)
        {
            player = pc;
            proc = GetComponent<ProceduralAnimator>();
            chains.Clear();
            if (root == null) return;
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (!SecondaryMotionRules.Matches(t.name)) continue;
                // a chain starts at a matching bone whose parent is not matching
                if (t.parent != null && SecondaryMotionRules.Matches(t.parent.name)) continue;
                var list = new List<Transform> { t };
                var cur = t;
                while (true)
                {
                    Transform next = null;
                    for (int i = 0; i < cur.childCount; i++) if (SecondaryMotionRules.Matches(cur.GetChild(i).name)) { next = cur.GetChild(i); break; }
                    if (next == null) break;
                    list.Add(next); cur = next;
                }
                if (list.Count < 2) continue;
                float seg = Vector3.Distance(list[0].position, list[1].position);
                chains.Add(new Chain
                {
                    bones = list.ToArray(),
                    sim = new VerletChain(list.Count, Mathf.Max(0.01f, seg)),
                    maxAngle = SecondaryMotionRules.MaxAngle(FeelSettings.V, t.name)
                });
            }
        }

        private void LateUpdate()
        {
            if (chains.Count == 0 || proc == null || !proc.Budget.Cloth) return;
            float dt = Time.deltaTime;
            if (dt <= 0f || Time.timeScale <= 0f) return;
            dt = Mathf.Min(dt, 1f / 20f);
            var v = FeelSettings.V;
            float amp = proc.Budget.Amplitude;
            Vector3 wind = player != null ? player.WindPush : Vector3.zero;
            foreach (var c in chains)
            {
                int n = c.bones.Length;
                // the pose the animation (and the body motion) gave the bones this frame
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = c.bones[i].position;
                    c.sim.SetRest(i, p.x, p.y, p.z);
                    // the length can change with scale (squash), so it follows the pose
                    if (i > 0) c.sim.Length[i] = Mathf.Max(0.005f, Vector3.Distance(c.bones[i - 1].position, p));
                }
                if (!c.snapped) { c.sim.Snap(); c.snapped = true; }
                c.sim.Step(v, dt, wind.x, wind.z, amp, c.maxAngle);
                // aim each bone at the simulated next joint (root bone stays on the animation)
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 simDir = new Vector3(c.sim.X[i + 1] - c.sim.X[i], c.sim.Y[i + 1] - c.sim.Y[i], c.sim.Z[i + 1] - c.sim.Z[i]);
                    // the direction the bone points right now, after its parents were turned (root to tip, so each bone sees its parent's change)
                    Vector3 cur = c.bones[i + 1].position - c.bones[i].position;
                    if (cur.sqrMagnitude < 1e-8f || simDir.sqrMagnitude < 1e-8f) continue;
                    c.bones[i].rotation = Quaternion.FromToRotation(cur, simDir) * c.bones[i].rotation;
                }
            }
        }

        private void OnDisable() { foreach (var c in chains) c.snapped = false; }
    }
}
