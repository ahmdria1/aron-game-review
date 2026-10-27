// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Enemies;
using Aron.Logic.Combat;
using Aron.Logic.Safety;
using Aron.Player;
using Aron.Save;
using UnityEngine;

namespace Aron.Combat
{
    /// <summary>
    /// Light aim assist on touch, for the slingshot tap and the melee swing. Candidates are enemies and the boss only: crates and levers keep
    /// their direct tap, and animals, birds and companions are never in the list, so they can never be picked.
    /// </summary>
    public static class AimAssistRuntime
    {
        private static readonly List<AimCandidate> list = new List<AimCandidate>(16);
        private static readonly List<Vector3> positions = new List<Vector3>(16);

        private static float Strength()
        {
            var d = SaveSystem.Data;
            if (d.access != null && !d.access.aimAssist) return 0f;
            return CombatFeelConfig.File.aim.Scale(d.difficulty) * OneHandMode.AimScale(d.access ?? new AccessSettings());
        }

        private static void Gather(Camera cam, PlayerController player)
        {
            list.Clear(); positions.Clear();
            foreach (var e in Enemy.All) Add(cam, player, e, e.CanBeDamaged, TargetKind.Enemy, e.name);
            var boss = RiyadhBoss.Current;
            if (boss != null) Add(cam, player, boss, boss.CanBeDamaged, TargetKind.Boss, "boss");
        }

        private static void Add(Camera cam, PlayerController player, Component c, bool canHit, TargetKind kind, string id)
        {
            if (c == null || !canHit) return;
            Vector3 pos = c.transform.position + Vector3.up * 0.8f;
            Vector3 sp = cam.WorldToScreenPoint(pos);
            if (sp.z <= 0f) return;
            Vector3 to = c.transform.position - player.transform.position; to.y = 0f;
            var cand = new AimCandidate { id = id, kind = kind, screenX = sp.x, screenY = sp.y, distance = to.magnitude, angle = Vector3.Angle(player.transform.forward, to) };
            list.Add(cand); positions.Add(c.transform.position);
        }

        /// <summary>A slingshot tap that missed everything by a little: the target it should count as on, if any.</summary>
        public static bool TrySling(Camera cam, Vector2 tap, out Vector3 point)
        {
            point = default;
            var player = PlayerController.Active;
            float s = Strength();
            if (cam == null || player == null || s <= 0f) return false;
            Gather(cam, player);
            int i = AimAssist.PickSling(list, tap.x, tap.y, Screen.dpi > 0f ? Screen.dpi : 160f, CombatFeelConfig.File.aim, s);
            if (i < 0) return false;
            point = positions[i];
            return true;
        }

        /// <summary>A melee swing with no tap target: turn toward the nearest enemy in front, a little.</summary>
        public static bool TryMelee(Camera cam, out Vector3 point)
        {
            point = default;
            var player = PlayerController.Active;
            float s = Strength();
            if (cam == null || player == null || s <= 0f) return false;
            Gather(cam, player);
            int i = AimAssist.PickMelee(list, CombatFeelConfig.File.aim, s);
            if (i < 0) return false;
            point = positions[i];
            return true;
        }
    }
}
