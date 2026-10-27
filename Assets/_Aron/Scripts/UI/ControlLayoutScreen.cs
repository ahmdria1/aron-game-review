// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Localization;
using Aron.Logic.Access;
using Aron.Logic.Safety;
using Aron.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Move the on-screen buttons by dragging them. They are drawn at the size and opacity chosen in Accessibility, inside the same safe
    /// area the game uses, so what is seen here is where they will be. A drop that would cover the other button slides clear of it.
    /// </summary>
    public class ControlLayoutScreen : MonoBehaviour
    {
        public static ControlLayoutScreen Open(Transform parent)
        {
            var go = new GameObject("ControlLayout");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<ControlLayoutScreen>();
            s.Build();
            return s;
        }

        private RectTransform area;
        private readonly Dictionary<string, RectTransform> pieces = new Dictionary<string, RectTransform>();

        private static AccessSettings A
        {
            get
            {
                var d = SaveSystem.Data;
                if (d.access == null) d.access = new AccessSettings();
                return d.access;
            }
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("ControlLayoutCanvas", 30);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.97f));
            var safe = UIFactory.SafeRoot(canvas);
            area = safe;
            var title = UIFactory.Label(safe, "Title", Strings.Get("move_buttons"), 56f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1400f, 90f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var hint = UIFactory.Label(safe, "Hint", Strings.Get("move_buttons_hint"), 36f, new Color(1f, 1f, 1f, 0.8f));
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1500f, 70f));
            hint.rectTransform.pivot = new Vector2(0.5f, 1f);

            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => { SaveSystem.Save(); Destroy(gameObject); });
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));
            var reset = UIFactory.CreateButton(safe, "Reset", Strings.Get("reset_buttons"), () => { ControlLayout.Reset(A); SaveSystem.Save(); Layout(); });
            UIFactory.Anchor((RectTransform)reset.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(520f, 100f));

            foreach (var id in ControlLayout.Ids)
            {
                var rt = UIFactory.Panel(safe, "Piece_" + id, UIFactory.Green, false);
                var label = UIFactory.Label(rt, "Label", Strings.Get(id == ControlLayout.Pause ? "pause_button" : "recenter"), 46f, Color.white);
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.raycastTarget = false;
                var drag = rt.gameObject.AddComponent<DragPiece>();
                drag.Init(this, id);
                pieces[id] = rt;
            }
            Layout();
        }

        private void Layout()
        {
            foreach (var r in ControlLayout.ResolveAll(A)) ButtonLayoutUi.Apply(pieces[r.Id], r);
        }

        private void Move(string id, Vector2 screenPoint, Camera cam)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, cam, out var local)) return;
            var n = Rect.PointToNormalized(area.rect, local);
            var rect = pieces[id];
            rect.anchorMin = rect.anchorMax = new Vector2(ControlLayout.ClampX(n.x), ControlLayout.ClampY(n.y));
        }

        private void Drop(string id, Vector2 screenPoint, Camera cam)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, cam, out var local))
            {
                var n = Rect.PointToNormalized(area.rect, local);
                ControlLayout.Place(A, id, n.x, n.y);
                SaveSystem.Save();
            }
            Layout();
        }

        private class DragPiece : MonoBehaviour, IDragHandler, IEndDragHandler
        {
            private ControlLayoutScreen owner;
            private string id;
            public void Init(ControlLayoutScreen o, string buttonId) { owner = o; id = buttonId; }
            public void OnDrag(PointerEventData e) => owner.Move(id, e.position, e.pressEventCamera);
            public void OnEndDrag(PointerEventData e) => owner.Drop(id, e.position, e.pressEventCamera);
        }
    }
}
