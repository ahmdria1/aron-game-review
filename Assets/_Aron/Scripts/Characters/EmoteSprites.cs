// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using UnityEngine;

namespace Aron.Characters
{
    public static class EmoteSprites
    {
        private const int Size = 128;
        private static readonly Dictionary<Emotion, Sprite> Cache = new Dictionary<Emotion, Sprite>();

        public static Sprite Get(Emotion e)
        {
            if (e == Emotion.Neutral) return null;
            if (Cache.TryGetValue(e, out var s) && s != null) return s;
            float[] px = EmoteArt.Render(e, Size);
            var colors = new Color[Size * Size];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Color(px[i * 4], px[i * 4 + 1], px[i * 4 + 2], px[i * 4 + 3]);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Emote_" + e, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(colors);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
            s.name = tex.name;
            Cache[e] = s;
            return s;
        }
    }
}
