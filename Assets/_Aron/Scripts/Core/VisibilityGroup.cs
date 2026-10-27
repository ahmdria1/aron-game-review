// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Core
{
    /// <summary>
    /// Switches a decorative behaviour (a spin, a bob, a flicker) off while none of its renderers is on screen,
    /// so idle objects cost no Update time. Use <see cref="Attach"/> for behaviours that have no OnEnable/OnDisable
    /// side effects. An object that has never been seen keeps running until it first leaves the screen.
    /// </summary>
    public class VisibilityGroup : MonoBehaviour
    {
        private Behaviour target;
        private int seen;
        private bool wasSeen;

        public static void Attach(Behaviour target)
        {
            if (target == null) return;
            var group = target.gameObject.AddComponent<VisibilityGroup>();
            group.target = target;
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
                r.gameObject.AddComponent<VisibilityRelay>().group = group;
        }

        internal void Changed(bool visible)
        {
            seen = Mathf.Max(0, seen + (visible ? 1 : -1));
            if (visible) wasSeen = true;
            if (target == null || !wasSeen) return;
            bool on = seen > 0;
            if (target.enabled != on) target.enabled = on;
        }
    }

    internal class VisibilityRelay : MonoBehaviour
    {
        public VisibilityGroup group;
        private void OnBecameVisible() { if (group != null) group.Changed(true); }
        private void OnBecameInvisible() { if (group != null) group.Changed(false); }
    }
}
