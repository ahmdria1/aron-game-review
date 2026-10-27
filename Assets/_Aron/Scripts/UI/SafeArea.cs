// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using UnityEngine;

namespace Aron.UI
{
    /// <summary>Shrinks this RectTransform to the screen's safe area (notches, punch-holes, rounded corners, home bar).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private Rect applied;
        private Vector2Int size;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != applied || Screen.width != size.x || Screen.height != size.y) Apply();
        }

        private void Apply()
        {
            applied = Screen.safeArea;
            size = new Vector2Int(Screen.width, Screen.height);
            var a = ScreenFit.SafeAnchors(applied.x, applied.y, applied.width, applied.height, size.x, size.y);
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(a.minX, a.minY);
            rt.anchorMax = new Vector2(a.maxX, a.maxY);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
