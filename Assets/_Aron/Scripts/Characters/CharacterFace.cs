// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic;
using UnityEngine;

namespace Aron.Characters
{
    [Serializable]
    public class FaceRig
    {
        public Transform eyeL, eyeR, browL, browR;
        public Transform mouthL, mouthC, mouthR, mouthOpen;
        public Transform blushL, blushR, tearL, tearR, sweat;
    }

    /// <summary>
    /// Poses the little face parts the builder glued onto the head bone. Everything is driven from
    /// <see cref="FacePose"/> so expressions blend smoothly and the eyes still blink on their own.
    /// </summary>
    public class CharacterFace : MonoBehaviour
    {
        [SerializeField] private FaceRig rig = new FaceRig();
        [SerializeField] private float blendSpeed = 12f;

        private struct Rest { public Transform t; public Vector3 pos, scale; public Quaternion rot; }

        private Rest eyeL, eyeR, browL, browR, mouthL, mouthC, mouthR, mouthOpen, blushL, blushR, tearL, tearR, sweat;
        private FacePose current = FacePose.For(Emotion.Neutral), target = FacePose.For(Emotion.Neutral);
        private float nextBlink, blinkStart = -1f;
        private bool captured;

        public Emotion Showing { get; private set; }
        public bool HasRig => rig != null && rig.eyeL != null && rig.eyeR != null;

        public void Assign(FaceRig r) => rig = r;

        private void Awake() => Capture();

        private void Capture()
        {
            if (captured || rig == null) return;
            eyeL = Grab(rig.eyeL); eyeR = Grab(rig.eyeR);
            browL = Grab(rig.browL); browR = Grab(rig.browR);
            mouthL = Grab(rig.mouthL); mouthC = Grab(rig.mouthC); mouthR = Grab(rig.mouthR); mouthOpen = Grab(rig.mouthOpen);
            blushL = Grab(rig.blushL); blushR = Grab(rig.blushR);
            tearL = Grab(rig.tearL); tearR = Grab(rig.tearR); sweat = Grab(rig.sweat);
            nextBlink = Time.time + UnityEngine.Random.Range(1.5f, 4f);
            captured = true;
            Apply(current, 1f);
        }

        private static Rest Grab(Transform t) => t == null ? default : new Rest { t = t, pos = t.localPosition, scale = t.localScale, rot = t.localRotation };

        public void Show(Emotion e, bool instant = false)
        {
            Showing = e;
            target = FacePose.For(e);
            if (instant) current = target;
        }

        private void LateUpdate()
        {
            if (!HasRig) return;
            Capture();
            current = FacePose.Lerp(current, target, 1f - Mathf.Exp(-blendSpeed * Time.deltaTime));

            float lid = 1f;
            if (blinkStart < 0f && Time.time >= nextBlink) blinkStart = Time.time;
            if (blinkStart >= 0f)
            {
                float k = (Time.time - blinkStart) / 0.14f;
                if (k >= 1f) { blinkStart = -1f; nextBlink = Time.time + UnityEngine.Random.Range(2f, 5f); }
                else lid = 1f - Mathf.Sin(k * Mathf.PI) * 0.9f;
            }
            Apply(current, lid);
        }

        private void Apply(FacePose p, float lid)
        {
            Eye(eyeL, p, lid);
            Eye(eyeR, p, lid);
            Brow(browL, p);
            Brow(browR, p);

            Corner(mouthL, p.smile);
            Corner(mouthR, p.smile);
            if (mouthC.t != null) mouthC.t.localPosition = mouthC.pos + Vector3.down * Mathf.Max(0f, p.smile) * 0.04f;
            if (mouthOpen.t != null)
            {
                float o = Mathf.Clamp01(p.mouthOpen);
                mouthOpen.t.localScale = Vector3.Scale(mouthOpen.scale, new Vector3(0.8f + o * 0.2f, o, 1f));
                mouthOpen.t.localPosition = mouthOpen.pos + Vector3.down * (0.12f * o);
            }

            Scale(blushL, p.blush);
            Scale(blushR, p.blush);

            float fall = Mathf.Repeat(Time.time * 0.8f, 1f);
            Drip(tearL, p.tears, fall);
            Drip(tearR, p.tears, Mathf.Repeat(fall + 0.45f, 1f));
            if (sweat.t != null)
            {
                Scale(sweat, Mathf.Clamp01(p.sweat * 1.2f - 0.2f));
                sweat.t.localPosition = sweat.pos + Vector3.down * (Mathf.Sin(Time.time * 3f) * 0.05f + 0.05f);
            }
        }

        private static void Eye(Rest r, FacePose p, float lid)
        {
            if (r.t == null) return;
            r.t.localScale = new Vector3(r.scale.x * p.eyeSize, r.scale.y * p.eyeSize * Mathf.Max(0.08f, p.eyeOpen * lid), r.scale.z);
        }

        private static void Brow(Rest r, FacePose p)
        {
            if (r.t == null) return;
            float side = Mathf.Sign(r.pos.x);
            r.t.localPosition = r.pos + Vector3.up * p.browRaise;
            r.t.localRotation = r.rot * Quaternion.Euler(0f, 0f, -side * p.browTilt);
        }

        private static void Corner(Rest r, float smile)
        {
            if (r.t == null) return;
            float side = Mathf.Sign(r.pos.x);
            r.t.localRotation = r.rot * Quaternion.Euler(0f, 0f, side * smile * 28f);
        }

        private static void Scale(Rest r, float amount)
        {
            if (r.t == null) return;
            r.t.localScale = r.scale * Mathf.Clamp01(amount);
        }

        private static void Drip(Rest r, float amount, float fall)
        {
            if (r.t == null) return;
            float a = Mathf.Clamp01(amount);
            r.t.localScale = r.scale * (a > 0.05f ? a * (1f - fall * 0.4f) : 0f);
            r.t.localPosition = r.pos + Vector3.down * fall * 0.9f;
        }
    }
}
