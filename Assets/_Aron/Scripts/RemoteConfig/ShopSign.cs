// (c) Ahmed Alamrani. All rights reserved.
using Aron.Localization;
using Aron.Logic;
using TMPro;
using UnityEngine;

namespace Aron.RemoteConfig
{
    /// <summary>Shop sign in the level whose text/colour/image come from <see cref="RemoteConfigService"/>.</summary>
    public class ShopSign : MonoBehaviour
    {
        [SerializeField] private string signId = "shop_1";
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer boardRenderer;

        public void Setup(string id, TMP_Text text, Renderer board) { signId = id; label = text; boardRenderer = board; }

        private void OnEnable()
        {
            RemoteConfigService.ConfigChanged += Apply;
            if (RemoteConfigService.Instance != null && RemoteConfigService.Instance.Current != null) Apply(RemoteConfigService.Instance.Current);
        }

        private void OnDisable() => RemoteConfigService.ConfigChanged -= Apply;

        private void Apply(ShopSignsConfig cfg)
        {
            var e = cfg?.Find(signId);
            if (e == null) return;
            if (label != null) ArabicTMP.SetText(label, e.textAr);
            if (boardRenderer != null)
            {
                if (!string.IsNullOrEmpty(e.colorHex) && ColorUtility.TryParseHtmlString(e.colorHex, out var c)) boardRenderer.material.color = c;
                if (!string.IsNullOrEmpty(e.imageUrl) && RemoteConfigService.Instance != null)
                    RemoteConfigService.Instance.GetTexture(e.imageUrl, tex =>
                    {
                        if (tex != null && boardRenderer != null) { boardRenderer.material.mainTexture = tex; boardRenderer.material.color = Color.white; }
                    });
            }
        }
    }
}
