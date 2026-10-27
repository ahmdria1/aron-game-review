// (c) Ahmed Alamrani. All rights reserved.
using Aron.Config;
using Aron.Logic.GameFeel;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Animator IK pass for a humanoid rig: feet planted on slopes and stairs, the pelvis dropping to the lower foot, and the head and eyes turning to
    /// the point <see cref="ProceduralAnimator"/> picked. Lives on the object with the Animator (OnAnimatorIK is only called there) and needs "IK Pass" ticked on
    /// the base layer of the animator controller. Feet are only moved while the clip has them planted (their height above the root is small),
    /// and only on the High tier. Without a humanoid avatar (the greybox) none of this runs.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ProceduralIk : MonoBehaviour
    {
        private Animator animator;
        private ProceduralAnimator proc;
        private PlayerController player;
        private float leftLift, rightLift, pelvis;
        private readonly RaycastHit[] hits = new RaycastHit[6];

        public void Bind(ProceduralAnimator p, PlayerController pc) { proc = p; player = pc; animator = GetComponent<Animator>(); }

        private void OnAnimatorIK(int layer)
        {
            if (proc == null || player == null || animator == null) return;
            var v = FeelSettings.V;
            var b = proc.Budget;
            float dt = Time.deltaTime;

            if (b.LookAt && proc.Look.Weight > 0.01f)
            {
                float w = proc.Look.Weight;
                animator.SetLookAtWeight(w, 0.1f * w, LookAtModel.HeadShare(v) * w, v.lookEyeWeight * w, 0.6f);
                animator.SetLookAtPosition(proc.LookPoint);
            }
            else animator.SetLookAtWeight(0f);

            bool feet = b.FootIk && player.IsGrounded && !player.IsRiding && !player.IsSliding && v.footIkWeight > 0f;
            if (!feet)
            {
                // ease the corrections out instead of snapping when the feet leave the ground
                float k = 1f - Mathf.Exp(-v.footIkSmooth * dt);
                leftLift = Mathf.Lerp(leftLift, 0f, k); rightLift = Mathf.Lerp(rightLift, 0f, k); pelvis = Mathf.Lerp(pelvis, 0f, k);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f); animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f); animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
                return;
            }

            float rootY = player.transform.position.y;
            Vector3 lf = animator.GetIKPosition(AvatarIKGoal.LeftFoot), rf = animator.GetIKPosition(AvatarIKGoal.RightFoot);
            float gl = Ground(lf, out Vector3 nl), gr = Ground(rf, out Vector3 nr);
            var res = FootIk.Solve(v, gl, gr, rootY);
            float k2 = 1f - Mathf.Exp(-v.footIkSmooth * dt);
            leftLift = Mathf.Lerp(leftLift, res.LeftLift, k2);
            rightLift = Mathf.Lerp(rightLift, res.RightLift, k2);
            pelvis = Mathf.Lerp(pelvis, res.PelvisDrop, 1f - Mathf.Exp(-v.pelvisSmooth * dt));

            Foot(AvatarIKGoal.LeftFoot, lf, leftLift, nl, rootY, v);
            Foot(AvatarIKGoal.RightFoot, rf, rightLift, nr, rootY, v);
            animator.bodyPosition += Vector3.up * pelvis;
        }

        private void Foot(AvatarIKGoal goal, Vector3 animated, float lift, Vector3 normal, float rootY, FeelValues v)
        {
            // a foot in the swing phase (well above the root) is left to the clip; a planted one gets the full correction
            float planted = FootIk.PlantedWeight(animated.y - rootY);
            animator.SetIKPositionWeight(goal, planted);
            animator.SetIKPosition(goal, animated + Vector3.up * lift);
            animator.SetIKRotationWeight(goal, planted * 0.8f);
            animator.SetIKRotation(goal, Quaternion.FromToRotation(Vector3.up, normal) * animator.GetIKRotation(goal));
        }

        private float Ground(Vector3 foot, out Vector3 normal)
        {
            var v = FeelSettings.V;
            normal = Vector3.up;
            int n = Physics.RaycastNonAlloc(foot + Vector3.up * 0.5f, Vector3.down, hits, 0.5f + v.footIkRayLength, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; float y = float.NaN;
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider.transform.IsChildOf(player.transform)) continue;
                if (hits[i].distance < best) { best = hits[i].distance; y = hits[i].point.y; normal = hits[i].normal; }
            }
            return y;
        }
    }
}
