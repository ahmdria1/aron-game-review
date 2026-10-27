// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Access;
using UnityEngine;

namespace Aron.UI
{
    /// <summary>Puts an on-screen button where <see cref="ControlLayout"/> says: position, size and opacity.</summary>
    public static class ButtonLayoutUi
    {
        public static void Apply(RectTransform rt, ButtonRect r)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(r.X, r.Y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(r.Width, r.Height);
            var g = rt.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = r.Alpha;
        }
    }
}
