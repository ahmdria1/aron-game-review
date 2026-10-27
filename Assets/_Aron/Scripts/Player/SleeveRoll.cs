// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Enemies;
using Aron.Feedback;
using Aron.Logic.Heroes;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Aron rolls his thobe sleeves to the forearm for a fight (about 0.6 s, cut short by an attack) and lowers them after 6 s of calm,
    /// at stage clear and after a boss win. The timing and order live in <see cref="SleeveDirector"/> (tested); this component only
    /// feeds it the game's events and shows the result: the "sleeve_roll" blendshape, the "_SleeveRoll" material float, the
    /// animator triggers, and one light haptic tick. The greybox has none of these yet, so it shows nothing until the rigged model arrives.
    /// Noura never gets this component: her abaya stays fully covering.
    /// </summary>
    public class SleeveRoll : MonoBehaviour
    {
        public SleeveDirector Director { get; private set; }

        /// <summary>The face wrap (talathum) may start now: true once the sleeves are up (always true without sleeves).</summary>
        public static bool WrapMayStart => Current == null || Current.Director.WrapMayStart;
        public static SleeveRoll Current { get; private set; }

        private SkinnedMeshRenderer[] skins = new SkinnedMeshRenderer[0];
        private int[] shapeIndex = new int[0];
        private Renderer[] renderers = new Renderer[0];
        private MaterialPropertyBlock block;
        private PlayerAnimationHooks anim;
        private float shown = -1f;

        /// <summary>Call after the model is built (CharacterSetup.Apply). A hero that may not roll up gets no director.</summary>
        public void Bind(string heroId, GameObject model)
        {
            Director = new SleeveDirector(heroId);
            anim = GetComponent<PlayerAnimationHooks>();
            block = new MaterialPropertyBlock();
            if (model == null) { skins = new SkinnedMeshRenderer[0]; shapeIndex = new int[0]; renderers = new Renderer[0]; return; }
            skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            shapeIndex = new int[skins.Length];
            for (int i = 0; i < skins.Length; i++)
                shapeIndex[i] = skins[i].sharedMesh != null ? skins[i].sharedMesh.GetBlendShapeIndex(SleeveRules.Hook) : -1;
            renderers = model.GetComponentsInChildren<Renderer>(true);
            shown = -1f;
            Apply(0f);
        }

        private void OnEnable()
        {
            Current = this;
            GameEvents.PlayerAttacked += OnAttacked;
            GameEvents.BossEncounterStarted += OnBossStart;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.LevelCompleted += OnStageClear;
            GameEvents.PlayerDied += OnDied;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            GameEvents.PlayerAttacked -= OnAttacked;
            GameEvents.BossEncounterStarted -= OnBossStart;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.LevelCompleted -= OnStageClear;
            GameEvents.PlayerDied -= OnDied;
        }

        private void OnAttacked() => Director?.Attacked();
        private void OnBossStart() => Director?.BossStart();
        private void OnBossDefeated() => Director?.BossEnd(true);
        private void OnStageClear() => Director?.StageClear();
        private void OnDied() { Director?.Reset(); Apply(0f); }

        private void Update()
        {
            if (Director == null || !Director.Allowed) return;
            // Nearest living enemy decides "an enemy within engage range".
            float best = float.MaxValue;
            var here = transform.position;
            var all = Enemy.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || !e.isActiveAndEnabled) continue;
                float d = Vector3.Distance(here, e.transform.position);
                if (d < best) best = d;
            }
            if (best < float.MaxValue) Director.EnemyNear(best);

            var cue = Director.Tick(Time.deltaTime);
            if (cue == SleeveCue.RollUp) { anim?.Trigger(SleeveRules.AnimRollUp); Haptics.Play(SleeveRules.Haptic); }
            else if (cue == SleeveCue.RollDown) { anim?.Trigger(SleeveRules.AnimRollDown); Haptics.Play(SleeveRules.Haptic); }
            Apply(Director.Progress);
        }

        private void Apply(float progress)
        {
            if (Mathf.Approximately(progress, shown)) return;
            shown = progress;
            for (int i = 0; i < skins.Length; i++)
                if (skins[i] != null && shapeIndex[i] >= 0) skins[i].SetBlendShapeWeight(shapeIndex[i], progress * 100f);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(SleeveRules.MaterialProperty)) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(SleeveRules.MaterialProperty, progress);
                r.SetPropertyBlock(block);
            }
        }
    }
}
