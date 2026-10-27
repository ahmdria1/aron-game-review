// (c) Ahmed Alamrani. All rights reserved.
using Aron.Audio;
using Aron.Core;
using Aron.Fx;
using Aron.Localization;
using Aron.Logic;
using Aron.UI;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// The golden bisht on whoever is being played, Aron or Noura. It comes out of the gold-trimmed boxes and goes
    /// on by itself. Each one takes a hit, up to three; the third makes the hero glow gold and smash anything he
    /// runs into for ten seconds, with the music sped up.
    /// </summary>
    public class GoldenBisht : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly BishtPower power = new BishtPower();
        private Transform glow;
        private Light halo;
        private readonly Transform[] orbs = new Transform[BishtPower.MaxLayers];
        private MaterialPropertyBlock block;
        private float spin, nextSpark;
        private bool tinted;

        public int Layers => power.Layers;
        public bool Invincible => power.Invincible;
        public float InvincibleLeft => power.InvincibleLeft;

        private void Start() => BuildGlow();

        public void Collect()
        {
            var result = power.Collect();
            Vector3 at = transform.position + Vector3.up * 1.2f;
            FxPlayer.Play("sparkle", at);
            GameEvents.PlaySfx(AudioCues.BishtPickup);
            switch (result)
            {
                case BishtPickup.Layer:
                    GameUI.Banner(Strings.Get(power.Layers == 1 ? "bisht_1" : "bisht_2"), 1.8f);
                    AudioManager.PlayVoice(AudioCues.VoBisht, Speaker());
                    AudioManager.Bark(AudioCues.VoAronBishtCheer, null);
                    break;
                case BishtPickup.Invincible:
                    GameUI.Banner(Strings.Get("bisht_3"), 2.2f);
                    AudioManager.PlayVoice(AudioCues.VoBishtFull, Speaker());
                    AudioManager.Hurry(true);
                    FollowCamera.Shake(0.15f);
                    break;
                case BishtPickup.Topped:
                    GameUI.Banner(Strings.Get("bisht_more"), 1.4f);
                    break;
            }
            Changed();
        }

        /// <summary>A hit lands on him. True when the bisht took it.</summary>
        public bool TakeHit()
        {
            if (power.Invincible) return true;
            if (!power.Absorb()) return false;
            GameEvents.PlaySfx(AudioCues.BishtLost);
            FxPlayer.Play("sparkle", transform.position + Vector3.up);
            GameUI.Banner(Strings.Get("bisht_lost"), 1.2f);
            Changed();
            return true;
        }

        /// <summary>Back from the third death at one checkpoint: one bisht to help him past.</summary>
        public void Mercy()
        {
            if (power.Layers > 0 || power.Invincible) return;
            power.Collect();
            GameUI.Banner(Strings.Get("bisht_mercy"), 2.2f);
            AudioManager.PlayVoice(AudioCues.VoMercy, Speaker());
            FxPlayer.Play("sparkle", transform.position + Vector3.up);
            Changed();
        }

        public void Clear()
        {
            bool was = power.Invincible;
            power.Clear();
            if (was) AudioManager.Hurry(false);
            Changed();
        }

        private void OnDisable()
        {
            if (power.Invincible) AudioManager.Hurry(false);
            Tint(false);
        }

        private void Update()
        {
            if (power.Tick(Time.deltaTime))
            {
                AudioManager.Hurry(false);
                GameUI.Banner(Strings.Get("bisht_end"), 1.4f);
                Changed();
            }
            if (glow == null) return;

            bool inv = power.Invincible;
            spin += Time.deltaTime * (inv ? 540f : 120f);
            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] == null) continue;
                bool on = inv || i < power.Layers;
                if (orbs[i].gameObject.activeSelf != on) orbs[i].gameObject.SetActive(on);
                if (!on) continue;
                float a = (spin + i * 120f) * Mathf.Deg2Rad;
                orbs[i].localPosition = new Vector3(Mathf.Cos(a) * 0.75f, 0.1f + Mathf.Sin(a * 2f) * 0.25f + i * 0.2f, Mathf.Sin(a) * 0.75f);
                orbs[i].localScale = Vector3.one * (inv ? 0.3f : 0.2f);
            }
            if (halo != null)
            {
                float target = inv ? 3f + Mathf.Sin(Time.time * 18f) * 1.2f : power.Layers * 0.8f;
                halo.intensity = Mathf.MoveTowards(halo.intensity, target, Time.deltaTime * 12f);
                halo.enabled = halo.intensity > 0.01f;
            }
            if (inv)
            {
                // The whole body flashes gold while he is untouchable, faster in the last two seconds.
                float rate = power.InvincibleLeft < 2f ? 16f : 8f;
                Tint(Aron.Logic.Access.FlashSafety.BlinkOn(Aron.Save.SaveSystem.Data.access, Time.time, rate, 0.5f));
                if (Time.time >= nextSpark) { nextSpark = Time.time + 0.25f; FxPlayer.Play("sparkle", transform.position + Vector3.up); }
            }
            else if (tinted) Tint(false);
        }

        private void Changed() => GameEvents.RaiseBishtChanged(power.Layers, power.Invincible);

        private string Speaker()
        {
            var team = Heroes.HeroTeam.Instance;
            return team != null ? team.Active : "aron";
        }

        private void Tint(bool on)
        {
            if (on == tinted) return;
            tinted = on;
            var pc = GetComponent<PlayerController>();
            var root = pc != null && pc.VisualRoot != null ? pc.VisualRoot : transform;
            if (block == null) block = new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(BaseColorId, Gold);
            block.SetColor(ColorId, Gold);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                r.SetPropertyBlock(on ? block : null);
            }
        }

        private void BuildGlow()
        {
            glow = new GameObject("BishtGlow").transform;
            glow.SetParent(transform, false);
            glow.localPosition = new Vector3(0f, 0.9f, 0f);
            halo = glow.gameObject.AddComponent<Light>();
            halo.type = LightType.Point;
            halo.color = Gold;
            halo.range = 4f;
            halo.intensity = 0f;
            halo.shadows = LightShadows.None;
            halo.enabled = false;
            for (int i = 0; i < orbs.Length; i++)
            {
                var o = PrimitiveFactory.Create(PrimitiveType.Sphere);
                o.name = "Gold" + (i + 1);
                Destroy(o.GetComponent<Collider>());
                o.transform.SetParent(glow, false);
                var r = o.GetComponent<Renderer>();
                var m = r.material;
                m.color = Gold;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Gold * 1.6f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                o.SetActive(false);
                orbs[i] = o.transform;
            }
        }
    }
}
