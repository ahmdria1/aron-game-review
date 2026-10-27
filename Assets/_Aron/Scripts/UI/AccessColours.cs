// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Access;
using Aron.Save;
using UnityEngine;

namespace Aron.UI
{
    /// <summary>The player's colour-blind mode as Unity colours and sprites. With no mode set, every colour is the game's usual one.</summary>
    public static class AccessColours
    {
        public static ColourMode Mode
        {
            get
            {
                var a = SaveSystem.Data != null ? SaveSystem.Data.access : null;
                return a == null ? ColourMode.None : ColourVision.Parse(a.colourFilter);
            }
        }

        public static Color Of(ColourRole role) => Of(Mode, role);

        public static Color Of(ColourMode mode, ColourRole role)
        {
            var c = AccessPalette.Colour(mode, role);
            return new Color(c.R, c.G, c.B, 1f);
        }

        private const int Size = 32;
        private static readonly Dictionary<MarkShape, Sprite> Sprites = new Dictionary<MarkShape, Sprite>();

        /// <summary>A white shape with a dark rim, to be tinted with a role colour (the rim keeps it readable on any background).</summary>
        public static Sprite Shape(MarkShape shape)
        {
            if (Sprites.TryGetValue(shape, out var cached) && cached != null) return cached;
            var mask = ShapeMask.Pixels(shape, Size);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Mark_" + shape };
            var px = new Color32[Size * Size];
            for (int row = 0; row < Size; row++)
                for (int col = 0; col < Size; col++)
                {
                    int i = row * Size + col;
                    if (!mask[i]) px[i] = new Color32(0, 0, 0, 0);
                    else px[i] = ShapeMask.IsRim(mask, Size, col, row) ? new Color32(20, 20, 20, 255) : new Color32(255, 255, 255, 255);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            Sprites[shape] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// A small shape floating above something that hurts (an enemy, a boss, a boss's shot, the rolling boulder), drawn in the Danger colour
    /// and a triangle so it can be told apart without colour. Only added when a colour-blind mode is on.
    /// </summary>
    public class HazardMarker : MonoBehaviour
    {
        private const string ChildName = "[HazardMark]";
        private Transform mark, owner;
        private Camera cam;
        private float lift, worldScale;

        /// <summary>Adds the marker unless a colour-blind mode is off, or one is there already.</summary>
        public static void Attach(GameObject target, float scale = 0.7f)
        {
            if (target == null || AccessColours.Mode == ColourMode.None || target.transform.Find(ChildName) != null) return;
            var go = new GameObject(ChildName);
            go.transform.SetParent(target.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AccessColours.Shape(AccessPalette.Shape(ColourRole.Danger));
            sr.color = AccessColours.Of(ColourRole.Danger);
            sr.sortingOrder = 50;
            var m = go.AddComponent<HazardMarker>();
            m.mark = go.transform; m.owner = target.transform; m.worldScale = scale;
            m.Measure();
            m.Follow();
        }

        // how far above the owner's centre the highest renderer ends, so the mark sits on top of it
        private void Measure()
        {
            var b = new Bounds(owner.position, Vector3.zero);
            bool any = false;
            foreach (var r in owner.GetComponentsInChildren<Renderer>())
            {
                if (r.transform == mark) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            lift = (any ? b.max.y - owner.position.y : 1.5f) + 0.6f;
        }

        // world position and size are set every frame, so a spinning or scaled owner does not turn or shrink the mark
        private void Follow()
        {
            if (cam == null) cam = Camera.main;
            mark.position = owner.position + Vector3.up * lift;
            if (cam != null) mark.rotation = cam.transform.rotation;
            float parentScale = Mathf.Max(0.0001f, owner.lossyScale.y);
            mark.localScale = Vector3.one * (worldScale / parentScale);
        }

        private void LateUpdate() => Follow();
    }
}
