// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using UnityEngine;

namespace Aron.Characters
{
    /// <summary>
    /// What a character is feeling right now: drives the face, a short body reaction and the emote icon above the head.
    /// A mood is the resting emotion (e.g. worried on the last life) that comes back once a reaction runs out.
    /// </summary>
    public class CharacterEmotion : MonoBehaviour
    {
        private static readonly Dictionary<string, CharacterEmotion> Registry = new Dictionary<string, CharacterEmotion>();
        private static readonly int CheerParam = Animator.StringToHash("Cheer");
        private static readonly int SadParam = Animator.StringToHash("Sad");

        [SerializeField] private string characterId;
        [SerializeField] private CharacterFace face;
        [SerializeField] private bool showIcons = true;
        [SerializeField] private float iconSize = 0.45f;

        private Emotion mood = Emotion.Neutral;
        private Emotion active = Emotion.Neutral;
        private float activeUntil, reactionStart = -10f, iconStart = -10f, iconUntil;
        private Animator animator;
        private readonly HashSet<int> animParams = new HashSet<int>();
        private Vector3 restPos, restScale;
        private Quaternion restRot;
        private bool hasRest;
        private float bodyHeight = 1.8f;
        private SpriteRenderer icon;
        private Camera cam;

        public string CharacterId => characterId;
        public Emotion Current => Time.time < activeUntil ? active : mood;
        public Emotion Mood => mood;

        public static CharacterEmotion Find(string id) => id != null && Registry.TryGetValue(id, out var e) && e != null ? e : null;

        public void SetId(string id) { Unregister(); characterId = id; Register(); }

        private void OnEnable() => Register();
        private void OnDisable() => Unregister();
        private void Register() { if (!string.IsNullOrEmpty(characterId)) Registry[characterId] = this; }
        private void Unregister() { if (characterId != null && Registry.TryGetValue(characterId, out var e) && e == this) Registry.Remove(characterId); }

        private void Awake()
        {
            if (face == null) face = GetComponentInChildren<CharacterFace>(true);
            animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var p in animator.parameters) animParams.Add(p.nameHash);
        }

        private void Start()
        {
            restPos = transform.localPosition;
            restRot = transform.localRotation;
            restScale = transform.localScale;
            hasRest = true;
            var bounds = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            if (any) bodyHeight = Mathf.Max(0.5f, bounds.max.y - transform.position.y);
            if (face != null) face.Show(Current, true);
        }

        /// <summary>React with an emotion for a while. <paramref name="big"/> also plays the full-body clip when there is one.</summary>
        public bool Feel(Emotion e, float duration = 2f, bool big = false)
        {
            if (!isActiveAndEnabled) return false;
            if (Time.time < activeUntil && !EmotionRules.CanInterrupt(active, e)) return false;
            active = e;
            activeUntil = Time.time + Mathf.Max(0.1f, duration);
            reactionStart = Time.time;
            if (face != null) face.Show(e);
            if (big) PlayBodyClip(e);
            if (showIcons && e != Emotion.Neutral) ShowIcon(e, Mathf.Min(duration, 2.2f));
            return true;
        }

        public void SetMood(Emotion e)
        {
            mood = e;
            if (Time.time >= activeUntil && face != null) face.Show(e);
        }

        public void Clear()
        {
            activeUntil = 0f;
            mood = Emotion.Neutral;
            if (face != null) face.Show(Emotion.Neutral);
        }

        private void PlayBodyClip(Emotion e)
        {
            int hash = e == Emotion.Joy ? CheerParam : e == Emotion.Sadness ? SadParam : 0;
            if (hash != 0 && animator != null && animParams.Contains(hash)) animator.SetTrigger(hash);
        }

        private void ShowIcon(Emotion e, float duration)
        {
            if (icon == null)
            {
                var go = new GameObject("Emote");
                go.transform.SetParent(transform, false);
                icon = go.AddComponent<SpriteRenderer>();
                icon.sortingOrder = 50;
            }
            icon.sprite = EmoteSprites.Get(e);
            icon.enabled = icon.sprite != null;
            iconStart = Time.time;
            iconUntil = Time.time + duration;
        }

        private void LateUpdate()
        {
            Emotion now = Current;
            if (face != null && face.Showing != now && Time.time >= activeUntil) face.Show(now);
            Body(now);
            UpdateIcon();
        }

        private void Body(Emotion now)
        {
            if (!hasRest) return;
            float t = Time.time - reactionStart;
            float fresh = Mathf.Clamp01(1f - t / 0.9f);
            float h = bodyHeight;
            Vector3 pos = Vector3.zero;
            Vector3 euler = Vector3.zero;
            Vector3 scale = Vector3.one;
            switch (now)
            {
                case Emotion.Joy:
                    pos.y = Mathf.Abs(Mathf.Sin(t * 11f)) * 0.08f * h * fresh;
                    scale = new Vector3(1f + 0.06f * fresh, 1f - 0.04f * fresh * Mathf.Cos(t * 22f), 1f + 0.06f * fresh);
                    break;
                case Emotion.Sadness:
                    euler.x = 10f;
                    pos.y = -0.02f * h;
                    scale = new Vector3(1f, 0.96f, 1f);
                    break;
                case Emotion.Anger:
                    pos.x = Mathf.Sin(t * 55f) * 0.015f * h * fresh;
                    euler.x = 6f;
                    scale = Vector3.one * (1f + 0.05f * fresh);
                    break;
                case Emotion.Fear:
                    pos.x = Mathf.Sin(t * 70f) * 0.006f * h;
                    euler.x = -7f;
                    scale = new Vector3(0.97f, 0.95f, 0.97f);
                    break;
                case Emotion.Admiration:
                    euler.x = -9f * Mathf.Clamp01(t * 3f);
                    pos.y = Mathf.Sin(t * 3f) * 0.01f * h;
                    break;
                case Emotion.Worry:
                    euler.z = Mathf.Sin(t * 4f) * 4f;
                    break;
            }
            transform.localPosition = restPos + restRot * pos;
            transform.localRotation = restRot * Quaternion.Euler(euler);
            transform.localScale = Vector3.Scale(restScale, scale);
        }

        private void UpdateIcon()
        {
            if (icon == null) return;
            float now = Time.time;
            if (now >= iconUntil) { if (icon.enabled) icon.enabled = false; return; }
            if (cam == null) cam = Camera.main;
            float k = now - iconStart;
            float pop = k < 0.25f ? Mathf.Sin(k / 0.25f * Mathf.PI * 0.5f) * 1.15f : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((k - 0.25f) * 6f));
            float fade = Mathf.Clamp01((iconUntil - now) / 0.3f);
            float size = Mathf.Clamp(bodyHeight * 0.22f, 0.3f, 0.9f) * (iconSize / 0.45f);
            Vector3 side = cam != null ? cam.transform.right : transform.right;
            var t = icon.transform;
            t.position = transform.position + Vector3.up * (bodyHeight + 0.25f + Mathf.Sin(k * 5f) * 0.04f) + side * size * 0.9f;
            if (cam != null) t.rotation = cam.transform.rotation;
            float parent = Mathf.Max(0.0001f, transform.lossyScale.x);
            t.localScale = Vector3.one * (size * pop * fade / parent);
            var c = icon.color; c.a = fade; icon.color = c;
        }
    }
}
