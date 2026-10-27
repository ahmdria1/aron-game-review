// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using Aron.Dialogue;
using Aron.Logic;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Plays the comic act from <see cref="DeathActs"/> on the hero's model, then calls back for the respawn.
    /// Everything it adds (stars, the rolling agal, the shemagh, bubbles, Noura's handbag) is built from simple
    /// shapes and cleaned up afterwards; the model itself is put back as it was.
    /// </summary>
    public class DeathShow : MonoBehaviour
    {
        private static readonly Color Black = new Color(0.05f, 0.05f, 0.05f);
        private static readonly Color Red = new Color(0.82f, 0.12f, 0.12f);
        private static readonly Color White = new Color(0.95f, 0.95f, 0.93f);
        private static readonly Color Star = new Color(1f, 0.86f, 0.2f);
        private static readonly Color Leather = new Color(0.48f, 0.29f, 0.16f);

        private readonly List<GameObject> props = new List<GameObject>();
        private readonly List<Renderer> hidden = new List<Renderer>();
        private Transform model;
        private Vector3 restPos, restScale;
        private Quaternion restRot;

        public bool Playing { get; private set; }

        public void Play(Transform visual, string hero, DeathCause cause, Action done)
        {
            if (Playing) return;
            StartCoroutine(Run(visual, hero, cause, done));
        }

        private IEnumerator Run(Transform visual, string hero, DeathCause cause, Action done)
        {
            Playing = true;
            model = visual != null ? visual : transform;
            restPos = model.localPosition; restRot = model.localRotation; restScale = model.localScale;
            var beats = DeathActs.Beats(hero, cause);
            float total = DeathActs.Seconds(hero, cause);
            float step = total / beats.Length;
            foreach (var beat in beats)
            {
                yield return Beat(beat, step);
            }
            waving = false;
            Restore();
            Playing = false;
            done?.Invoke();
        }

        private IEnumerator Beat(string beat, float seconds)
        {
            Vector3 head = Head();
            switch (beat)
            {
                case DeathActs.Spin:
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        float k = t / seconds;
                        model.localRotation = restRot * Quaternion.Euler(0f, 900f * k, 0f);
                        model.localPosition = restPos + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                        yield return null;
                    }
                    break;
                case DeathActs.Topple:
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                        model.localRotation = restRot * Quaternion.Euler(-85f * k, 0f, 0f);
                        model.localPosition = restPos + Vector3.up * 0.15f * k;
                        yield return null;
                    }
                    break;
                case DeathActs.Stars:
                    // Stars going round over the head stay up for the rest of the act.
                    StartCoroutine(Dizzy());
                    yield return new WaitForSeconds(seconds);
                    break;
                case DeathActs.AgalRolls:
                {
                    Hide("agal");
                    var agal = Shape(PrimitiveType.Cylinder, "Agal", head, new Vector3(0.36f, 0.025f, 0.36f), Black);
                    agal.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    Vector3 from = new Vector3(head.x, transform.position.y + 0.18f, head.z), dir = transform.right;
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        float k = t / seconds;
                        agal.transform.position = from + dir * k * 2.2f + Vector3.up * Mathf.Abs(Mathf.Sin(k * 9f)) * 0.15f * (1f - k);
                        agal.transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.Self);
                        yield return null;
                    }
                    break;
                }
                case DeathActs.ShemaghUp:
                {
                    Hide("shemagh"); Hide("agal");
                    var cloth = Cloth(head);
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        cloth.transform.position += (Vector3.up * 3.5f + transform.right * 0.6f) * Time.deltaTime;
                        cloth.transform.rotation = Quaternion.Euler(Mathf.Sin(t * 14f) * 25f, t * 200f, Mathf.Cos(t * 11f) * 25f);
                        yield return null;
                    }
                    break;
                }
                case DeathActs.Wave:
                {
                    // Both arms up, flapping, as he goes down (posed in LateUpdate, after the animator).
                    waving = true;
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        model.localPosition = restPos + Vector3.down * 0.6f * (t / seconds);
                        yield return null;
                    }
                    waving = false;
                    break;
                }
                case DeathActs.Drop:
                case DeathActs.Sink:
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        model.localPosition = restPos + Vector3.down * 3f * (t / seconds) * (t / seconds);
                        yield return null;
                    }
                    SetVisible(false);
                    break;
                case DeathActs.Bubbles:
                    for (float t = 0f, next = 0f; t < seconds; t += Time.deltaTime)
                    {
                        if (t >= next)
                        {
                            next = t + 0.12f;
                            var b = Shape(PrimitiveType.Sphere, "Bubble", transform.position + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0.8f, UnityEngine.Random.Range(-0.4f, 0.4f)), Vector3.one * UnityEngine.Random.Range(0.1f, 0.22f), White);
                            StartCoroutine(Rise(b.transform));
                        }
                        model.localPosition = restPos + Vector3.down * 1.2f * (t / seconds);
                        yield return null;
                    }
                    break;
                case DeathActs.ShemaghFloats:
                {
                    SetVisible(false);
                    Hide("shemagh"); Hide("agal");
                    var cloth = Cloth(new Vector3(transform.position.x, transform.position.y + 0.9f, transform.position.z));
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        cloth.transform.rotation = Quaternion.Euler(Mathf.Sin(t * 3f) * 6f, t * 20f, Mathf.Cos(t * 2.5f) * 6f);
                        yield return null;
                    }
                    break;
                }
                case DeathActs.AbayaSwirl:
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        float k = t / seconds;
                        float flare = 1f + Mathf.Sin(k * Mathf.PI) * 0.35f;
                        model.localRotation = restRot * Quaternion.Euler(0f, 720f * k, 0f);
                        model.localScale = new Vector3(restScale.x * flare, restScale.y, restScale.z * flare);
                        yield return null;
                    }
                    model.localScale = restScale;
                    break;
                case DeathActs.SitDown:
                    for (float t = 0f; t < seconds; t += Time.deltaTime)
                    {
                        float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                        model.localPosition = restPos + Vector3.down * 0.55f * k;
                        model.localScale = new Vector3(restScale.x, restScale.y * (1f - 0.12f * k), restScale.z);
                        yield return null;
                    }
                    break;
                case DeathActs.BagBeside:
                {
                    Vector3 at = transform.position + transform.right * 0.6f;
                    var bag = Shape(PrimitiveType.Cube, "Handbag", at + Vector3.up * 0.16f, new Vector3(0.36f, 0.28f, 0.14f), Leather);
                    var strap = Shape(PrimitiveType.Cube, "Strap", at + Vector3.up * 0.36f, new Vector3(0.26f, 0.04f, 0.04f), Leather * 0.8f);
                    strap.transform.SetParent(bag.transform, true);
                    yield return new WaitForSeconds(seconds);
                    break;
                }
                case DeathActs.Uff:
                {
                    var speaker = GetComponentInChildren<DialogueSpeaker>();
                    if (speaker != null) speaker.Say(DeathActs.UffAr, seconds + 0.6f);
                    yield return new WaitForSeconds(seconds);
                    break;
                }
                case DeathActs.OryxNudge:
                {
                    var oryx = FindFirstObjectByType<Animals.OryxCompanion>();
                    if (oryx != null) oryx.Nudge(seconds);
                    yield return new WaitForSeconds(seconds);
                    break;
                }
                default:
                    yield return new WaitForSeconds(seconds);
                    break;
            }
        }

        private bool waving;
        private Transform waveR, waveL;

        private void LateUpdate()
        {
            if (!waving || model == null) return;
            if (waveR == null) waveR = Find(model, "UpperArm.R");
            if (waveL == null) waveL = Find(model, "UpperArm.L");
            float flap = Mathf.Sin(Time.time * 18f) * 20f;
            Vector3 axis = transform.forward;
            if (waveR != null) waveR.rotation = Quaternion.AngleAxis(130f + flap, axis) * waveR.rotation;
            if (waveL != null) waveL.rotation = Quaternion.AngleAxis(-130f - flap, axis) * waveL.rotation;
        }

        private IEnumerator Dizzy()
        {
            var ring = Prop("Stars", Head() + Vector3.up * 0.25f);
            for (int i = 0; i < 3; i++) StarShape(ring.transform, i);
            while (ring != null && Playing)
            {
                ring.transform.position = Head() + Vector3.up * 0.25f;
                ring.transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
                yield return null;
            }
        }

        private void Restore()
        {
            foreach (var p in props) if (p != null) Destroy(p);
            props.Clear();
            foreach (var r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            SetVisible(true);
            if (model != null) { model.localPosition = restPos; model.localRotation = restRot; model.localScale = restScale; }
        }

        private void SetVisible(bool on)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                if (!hidden.Contains(r)) r.enabled = on;
        }

        private void Hide(string part)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                if (r.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0 && r.enabled) { r.enabled = false; hidden.Add(r); }
        }

        private Vector3 Head()
        {
            var h = Find(model, "Head");
            return h != null ? h.position : transform.position + Vector3.up * 1.75f;
        }

        private IEnumerator Rise(Transform b)
        {
            for (float t = 0f; t < 0.8f && b != null; t += Time.deltaTime)
            {
                b.position += Vector3.up * 1.6f * Time.deltaTime;
                yield return null;
            }
            if (b != null) b.gameObject.SetActive(false);
        }

        /// <summary>Red and white check shemagh, a folded square.</summary>
        private GameObject Cloth(Vector3 at)
        {
            var root = Prop("Shemagh", at);
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    var sq = Shape(PrimitiveType.Cube, "Check", Vector3.zero, new Vector3(0.16f, 0.015f, 0.16f), (i + j) % 2 == 0 ? Red : White);
                    sq.transform.SetParent(root.transform, false);
                    sq.transform.localPosition = new Vector3(-0.24f + i * 0.16f, 0f, -0.24f + j * 0.16f);
                }
            // The agal stays on it, as it came off his head.
            var ring = Shape(PrimitiveType.Cylinder, "Agal", Vector3.zero, Vector3.one, Black);
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.transform.localScale = new Vector3(0.34f, 0.03f, 0.34f);
            var hole = Shape(PrimitiveType.Cylinder, "AgalHole", Vector3.zero, Vector3.one, Red);
            hole.transform.SetParent(root.transform, false);
            hole.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            hole.transform.localScale = new Vector3(0.24f, 0.031f, 0.24f);
            root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            return root;
        }

        private void StarShape(Transform ring, int i)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            var s = Prop("Star", Vector3.zero);
            s.transform.SetParent(ring, false);
            s.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.35f, 0f, Mathf.Sin(a) * 0.35f);
            for (int k = 0; k < 2; k++)
            {
                var c = Shape(PrimitiveType.Cube, "Point", Vector3.zero, new Vector3(0.14f, 0.14f, 0.03f), Star);
                c.transform.SetParent(s.transform, false);
                c.transform.localRotation = Quaternion.Euler(0f, 0f, k * 45f);
            }
        }

        private GameObject Prop(string name, Vector3 at)
        {
            var go = new GameObject(name);
            go.transform.position = at;
            props.Add(go);
            return go;
        }

        private GameObject Shape(PrimitiveType type, string name, Vector3 at, Vector3 size, Color c)
        {
            var go = PrimitiveFactory.Create(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.position = at;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.material.color = c;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            props.Add(go);
            return go;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return root;
            foreach (Transform c in root) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }
    }
}
