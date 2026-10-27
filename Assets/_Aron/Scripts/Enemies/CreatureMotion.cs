// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>
    /// Keeps a boss built from primitives alive without an animator: legs and arms swing while it moves, tails
    /// and fins sway, jaws snap, hovering parts bob and helpers circle.
    /// </summary>
    public class CreatureMotion : MonoBehaviour
    {
        public enum Kind { Stride, Sway, Chomp, Hover, Spin }

        private class Joint
        {
            public Transform t;
            public Kind kind;
            public Vector3 axis;
            public float amount, rate, phase;
            public Quaternion rest;
            public Vector3 restPos;
        }

        private readonly List<Joint> joints = new List<Joint>();
        private Vector3 lastPos;
        private float stride, clock, moving;

        public void Add(Transform t, Kind kind, Vector3 axis, float amount, float rate, float phase = 0f)
        {
            if (t == null) return;
            joints.Add(new Joint { t = t, kind = kind, axis = axis, amount = amount, rate = rate, phase = phase, rest = t.localRotation, restPos = t.localPosition });
        }

        private void OnEnable() => lastPos = transform.position;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 d = transform.position - lastPos;
            d.y = 0f;
            lastPos = transform.position;
            float speed = d.magnitude / dt;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 3f), dt * 4f);
            stride += dt * (2f + Mathf.Min(speed, 12f) * 0.9f);
            clock += dt;

            foreach (var j in joints)
            {
                if (j.t == null) continue;
                switch (j.kind)
                {
                    case Kind.Stride:
                        j.t.localRotation = j.rest * Quaternion.AngleAxis(Mathf.Sin(stride * j.rate + j.phase) * j.amount * moving, j.axis);
                        break;
                    case Kind.Sway:
                        j.t.localRotation = j.rest * Quaternion.AngleAxis(Mathf.Sin(clock * j.rate + j.phase) * j.amount * (1f + moving), j.axis);
                        break;
                    case Kind.Chomp:
                        j.t.localRotation = j.rest * Quaternion.AngleAxis(Mathf.Max(0f, Mathf.Sin(clock * j.rate + j.phase)) * j.amount, j.axis);
                        break;
                    case Kind.Hover:
                        j.t.localPosition = j.restPos + j.axis * (Mathf.Sin(clock * j.rate + j.phase) * j.amount);
                        break;
                    case Kind.Spin:
                        j.t.localRotation = j.rest * Quaternion.AngleAxis(clock * j.rate + j.phase, j.axis);
                        break;
                }
            }
        }
    }
}
