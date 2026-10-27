// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Dialogue
{
    /// <summary>Registers a character as a speaker and owns its world-space speech bubble.</summary>
    public class DialogueSpeaker : MonoBehaviour
    {
        private static readonly Dictionary<string, DialogueSpeaker> Registry = new Dictionary<string, DialogueSpeaker>();

        [SerializeField] private string speakerId = "player";
        [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 2.8f, 0f);
        private SpeechBubble bubble;

        public string SpeakerId => speakerId;
        public void SetId(string id) { Unregister(); speakerId = id; Register(); }

        public static DialogueSpeaker Find(string id) => id != null && Registry.TryGetValue(id, out var s) && s != null ? s : null;

        private void OnEnable() => Register();
        private void OnDisable() => Unregister();
        private void Register() { if (!string.IsNullOrEmpty(speakerId)) Registry[speakerId] = this; }
        private void Unregister() { if (speakerId != null && Registry.TryGetValue(speakerId, out var s) && s == this) Registry.Remove(speakerId); }

        public void Say(string text, float duration)
        {
            if (bubble == null) bubble = SpeechBubble.Create(transform, bubbleOffset);
            bubble.Show(text, duration);
        }
    }
}
