// (c) Ahmed Alamrani. All rights reserved.
using Aron.Localization;
using TMPro;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>Fixed Arabic text on a world label (street and souq signs). Shaped at runtime with the Arabic font.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ArabicLabel : MonoBehaviour
    {
        [SerializeField, TextArea] private string text;

        public string Text => text;

        public void Set(string value)
        {
            text = value;
            ArabicTMP.SetText(GetComponent<TMP_Text>(), text);
        }

        private void Start() => ArabicTMP.SetText(GetComponent<TMP_Text>(), text);
    }
}
