// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using UnityEngine;

namespace Aron.Atmosphere
{
    public enum SwayKind { PalmLeaf, Sail, Flag }

    /// <summary>
    /// Moves palm fronds, sails and flags with the wind. Builders register the transforms as they make them; one
    /// component on the stage animates them all, nearest first, up to a cap that depends on the phone.
    /// </summary>
    public class WindSway : MonoBehaviour
    {
        private struct Item { public Transform T; public Quaternion Rest; public Vector3 Scale; public SwayKind Kind; public float Phase; }

        private static readonly List<Item> Items = new List<Item>();
        private static readonly int WindId = Shader.PropertyToID("_AronWind");

        /// <summary>Most transforms moved per frame, by tier.</summary>
        public static int Cap(QualityTier tier) => tier == QualityTier.Low ? 40 : tier == QualityTier.Medium ? 140 : 360;

        public static void Add(Transform t, SwayKind kind)
        {
            if (t == null) return;
            Items.Add(new Item { T = t, Rest = t.localRotation, Scale = t.localScale, Kind = kind, Phase = Random.value * 6.283f });
        }

        public static void Clear() => Items.Clear();
        public static int Count => Items.Count;

        public QualityTier Tier = QualityTier.Medium;
        private int scanIndex;

        // No clearing on destroy: entries from an unloaded stage turn null and drop out on the next pass.

        private void LateUpdate()
        {
            if (Items.Count == 0) return;
            float wind = Mathf.Clamp01(Shader.GetGlobalFloat(WindId));
            float t = Time.time;
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            int budget = Mathf.Min(Cap(Tier), Items.Count);
            // Walk the list round-robin so a long stage still gets every frond moving, just not all every frame.
            for (int n = 0; n < budget; n++)
            {
                if (Items.Count == 0) return;
                scanIndex = (scanIndex + 1) % Items.Count;
                var it = Items[scanIndex];
                if (it.T == null) { Items.RemoveAt(scanIndex); scanIndex = Mathf.Max(0, scanIndex - 1); continue; }
                if (!it.T.gameObject.activeInHierarchy) continue;
                if ((it.T.position - eye).sqrMagnitude > 90f * 90f) continue;
                float gust = 0.7f + 0.3f * Mathf.Sin(t * 0.37f + it.Phase * 0.5f);
                switch (it.Kind)
                {
                    case SwayKind.PalmLeaf:
                    {
                        // Fronds bob and droop more in a strong wind.
                        float a = Mathf.Sin(t * (1.6f + wind * 2.4f) + it.Phase) * (2f + 9f * wind) * gust;
                        it.T.localRotation = it.Rest * Quaternion.Euler(a + 6f * wind, 0f, a * 0.4f);
                        break;
                    }
                    case SwayKind.Sail:
                    {
                        // A sail leans off the mast and fills out a little.
                        float lean = Mathf.Sin(t * 0.9f + it.Phase) * (1.5f + 5f * wind);
                        it.T.localRotation = it.Rest * Quaternion.Euler(0f, lean, 0f);
                        it.T.localScale = new Vector3(it.Scale.x * (1f + 3f * wind * gust), it.Scale.y, it.Scale.z);
                        break;
                    }
                    default:
                    {
                        float flap = Mathf.Sin(t * (3f + 6f * wind) + it.Phase) * (4f + 18f * wind);
                        it.T.localRotation = it.Rest * Quaternion.Euler(0f, flap, 0f);
                        break;
                    }
                }
            }
        }
    }
}
