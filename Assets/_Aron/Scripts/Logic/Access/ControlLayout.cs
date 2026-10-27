// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Safety;

namespace Aron.Logic.Access
{
    /// <summary>Where the player dragged one on-screen button: the centre as a fraction of the screen (0..1, y up from the bottom).</summary>
    [Serializable]
    public class ButtonPlacement
    {
        public string id;
        public float x, y;
    }

    /// <summary>One button, resolved: centre, size in reference pixels (1920x1080) and opacity.</summary>
    public struct ButtonRect
    {
        public string Id;
        public float X, Y, Width, Height, Alpha;

        public bool Overlaps(ButtonRect o, float screenW = 1920f, float screenH = 1080f)
        {
            float dx = Math.Abs(X - o.X) * screenW, dy = Math.Abs(Y - o.Y) * screenH;
            return dx < (Width + o.Width) * 0.5f && dy < (Height + o.Height) * 0.5f;
        }
    }

    /// <summary>
    /// The on-screen buttons and where they go. Defaults come from the one-hand and left-handed settings; a position the player dragged to
    /// wins over both. Size and opacity are the player's too. The game plays with gestures, so these are the only two buttons: recentre the
    /// camera and pause.
    /// </summary>
    public static class ControlLayout
    {
        public const string Recenter = "recenter", Pause = "pause";
        public static readonly string[] Ids = { Recenter, Pause };

        public static readonly float[] Sizes = { 0.8f, 1f, 1.2f, 1.4f, 1.6f };
        public static readonly float[] Opacities = { 0.4f, 0.6f, 0.8f, 1f };

        /// <summary>Base size of a button on the 1920x1080 reference screen.</summary>
        public const float BaseWidth = 300f, BaseHeight = 90f;
        /// <summary>No button is drawn shorter than this, whatever the size setting (about 6.5 mm on a phone held sideways).</summary>
        public const float MinHeight = 96f;
        /// <summary>A button's centre stays this far from the screen edge (a fraction), so it is never half off the screen or under a notch.</summary>
        public const float EdgeMargin = 0.07f;
        public const float ReferenceWidth = 1920f, ReferenceHeight = 1080f;

        public static float NextSize(float current) => Next(Sizes, current);
        public static float NextOpacity(float current) => Next(Opacities, current);

        private static float Next(float[] steps, float current)
        {
            int best = 0;
            for (int i = 0; i < steps.Length; i++) if (Math.Abs(steps[i] - current) < Math.Abs(steps[best] - current)) best = i;
            return steps[(best + 1) % steps.Length];
        }

        /// <summary>Where a button sits with nothing customised: the right-hand side, mirrored when left-handed, pulled toward the thumb in one-hand mode.</summary>
        public static (float x, float y) DefaultCentre(AccessSettings a, string id)
        {
            float y = id == Pause ? 0.5f : 0.3f;
            return (OneHandMode.ButtonX(a, 0.9f), y);
        }

        public static float ClampX(float x) => x < EdgeMargin ? EdgeMargin : x > 1f - EdgeMargin ? 1f - EdgeMargin : x;
        public static float ClampY(float y) => y < EdgeMargin ? EdgeMargin : y > 1f - EdgeMargin ? 1f - EdgeMargin : y;

        public static ButtonPlacement Find(AccessSettings a, string id)
        {
            if (a == null || a.placements == null) return null;
            foreach (var p in a.placements) if (p != null && p.id == id) return p;
            return null;
        }

        /// <summary>The button as it is drawn.</summary>
        public static ButtonRect Resolve(AccessSettings a, string id)
        {
            a = a ?? new AccessSettings();
            var (x, y) = DefaultCentre(a, id);
            var custom = Find(a, id);
            if (custom != null) { x = custom.x; y = custom.y; }
            float size = a.buttonSize < 0.7f ? 0.7f : a.buttonSize > 1.6f ? 1.6f : a.buttonSize;
            float h = Math.Max(MinHeight, BaseHeight * size);
            // the button keeps its shape when the height was raised to the minimum
            float w = BaseWidth * (h / BaseHeight);
            if (w < BaseWidth * size) w = BaseWidth * size;
            float alpha = a.buttonOpacity < 0.3f ? 0.3f : a.buttonOpacity > 1f ? 1f : a.buttonOpacity;
            return new ButtonRect { Id = id, X = ClampX(x), Y = ClampY(y), Width = w, Height = h, Alpha = alpha };
        }

        /// <summary>Every button as drawn, in <see cref="Ids"/> order.</summary>
        public static List<ButtonRect> ResolveAll(AccessSettings a)
        {
            var list = new List<ButtonRect>();
            foreach (var id in Ids) list.Add(Resolve(a, id));
            return list;
        }

        /// <summary>Does any pair of buttons cover each other?</summary>
        public static bool AnyOverlap(AccessSettings a)
        {
            var all = ResolveAll(a);
            for (int i = 0; i < all.Count; i++) for (int j = i + 1; j < all.Count; j++) if (all[i].Overlaps(all[j])) return true;
            return false;
        }

        /// <summary>
        /// Saves a dragged position: the centre is clamped to the screen and, if it would cover another button, moved up or down (whichever is
        /// nearer, then further) until it is clear. Returns false only when there is no room, and then nothing is saved.
        /// </summary>
        public static bool Place(AccessSettings a, string id, float x, float y)
        {
            if (a == null || Array.IndexOf(Ids, id) < 0) return false;
            x = ClampX(x); y = ClampY(y);
            var me = Resolve(a, id);
            var others = new List<ButtonRect>();
            foreach (var o in ResolveAll(a)) if (o.Id != id) others.Add(o);
            bool Clear(float cy)
            {
                var r = me; r.X = x; r.Y = cy;
                foreach (var o in others) if (r.Overlaps(o)) return false;
                return true;
            }
            float step = 0.02f;
            float found = float.NaN;
            for (int i = 0; i <= 50 && float.IsNaN(found); i++)
            {
                float up = ClampY(y + i * step), down = ClampY(y - i * step);
                // prefer the side the drag came from: the smaller move wins, ties go up
                if (Clear(up)) found = up;
                else if (Clear(down)) found = down;
            }
            if (float.IsNaN(found)) return false;
            if (a.placements == null) a.placements = new List<ButtonPlacement>();
            var p = Find(a, id);
            if (p == null) { p = new ButtonPlacement { id = id }; a.placements.Add(p); }
            p.x = x; p.y = found;
            return true;
        }

        /// <summary>Back to the default places.</summary>
        public static void Reset(AccessSettings a) { if (a != null) a.placements = new List<ButtonPlacement>(); }

        public static bool HasCustom(AccessSettings a) => a != null && a.placements != null && a.placements.Count > 0;

        /// <summary>Checks the saved placements: known ids, once each, on the screen.</summary>
        public static List<string> Validate(AccessSettings a)
        {
            var e = new List<string>();
            if (a == null || a.placements == null) return e;
            var seen = new HashSet<string>();
            foreach (var p in a.placements)
            {
                if (p == null || Array.IndexOf(Ids, p.id) < 0) { e.Add("unknown button"); continue; }
                if (!seen.Add(p.id)) e.Add("button " + p.id + " placed twice");
                if (p.x < 0f || p.x > 1f || p.y < 0f || p.y > 1f) e.Add("button " + p.id + " off the screen");
            }
            return e;
        }
    }
}
