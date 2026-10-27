// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Voice;
using UnityEngine;

namespace Aron.Input
{
    /// <summary>Merges touch, keyboard/gamepad and (optional) voice into one <see cref="PlayerIntent"/> per frame.</summary>
    [DefaultExecutionOrder(-50)]
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }
        public PlayerIntent Current { get; private set; }

        private readonly List<IIntentSource> sources = new List<IIntentSource>();
        private int lastFrame = -1;

        private void Awake()
        {
            Instance = this;
            sources.Add(new TouchGestureInput());
            sources.Add(new KeyboardInput());
            var voice = GetComponent<VoiceCommandRouter>();
            if (voice != null) sources.Add(voice);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void AddSource(IIntentSource s) { if (s != null && !sources.Contains(s)) sources.Add(s); }

        private void Update() => Refresh();

        public PlayerIntent Refresh()
        {
            if (lastFrame == Time.frameCount) return Current;
            lastFrame = Time.frameCount;
            var merged = new PlayerIntent();
            foreach (var s in sources) merged.Merge(s.Poll());
            Current = merged;
            if (merged.pausePressed && Aron.Core.GameManager.Instance != null) Aron.Core.GameManager.Instance.TogglePause();
            return merged;
        }
    }
}
