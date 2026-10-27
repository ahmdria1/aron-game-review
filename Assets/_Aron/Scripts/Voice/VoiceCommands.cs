// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Input;
using UnityEngine;

namespace Aron.Voice
{
    public enum VoiceCommand { None, Jump, Attack, Slide, Pause }

    /// <summary>
    /// Plug a speech recognizer in here (Android SpeechRecognizer plugin, Whisper, cloud STT...).
    /// Implementations raise <see cref="CommandRecognized"/>; nothing ships enabled.
    /// </summary>
    public interface IVoiceCommandProvider
    {
        event Action<VoiceCommand> CommandRecognized;
        bool IsAvailable { get; }
        void StartListening();
        void StopListening();
    }

    /// <summary>Stub provider: does nothing. Call <see cref="Simulate"/> from tests/debug UI.</summary>
    public class NullVoiceCommandProvider : IVoiceCommandProvider
    {
        public event Action<VoiceCommand> CommandRecognized;
        public bool IsAvailable => false;
        public void StartListening() { }
        public void StopListening() { }
        public void Simulate(VoiceCommand c) => CommandRecognized?.Invoke(c);
    }

    /// <summary>Maps Arabic keywords to commands, for providers that return raw transcripts.</summary>
    public static class ArabicVoiceKeywords
    {
        public static readonly Dictionary<string, VoiceCommand> Map = new Dictionary<string, VoiceCommand>
        {
            { "اقفز", VoiceCommand.Jump }, { "نط", VoiceCommand.Jump },
            { "اضرب", VoiceCommand.Attack }, { "هجوم", VoiceCommand.Attack },
            { "انزلق", VoiceCommand.Slide }, { "وقف", VoiceCommand.Pause },
        };

        public static VoiceCommand Parse(string transcript)
        {
            if (string.IsNullOrEmpty(transcript)) return VoiceCommand.None;
            foreach (var kv in Map) if (transcript.Contains(kv.Key)) return kv.Value;
            return VoiceCommand.None;
        }
    }

    /// <summary>Feeds recognized voice commands into the input pipeline as one-frame intents.</summary>
    public class VoiceCommandRouter : MonoBehaviour, IIntentSource
    {
        private IVoiceCommandProvider provider;
        private VoiceCommand pending = VoiceCommand.None;

        public void SetProvider(IVoiceCommandProvider p)
        {
            if (provider != null) { provider.CommandRecognized -= OnCommand; provider.StopListening(); }
            provider = p;
            if (provider != null) { provider.CommandRecognized += OnCommand; if (provider.IsAvailable) provider.StartListening(); }
        }

        private void Awake() { if (provider == null) SetProvider(new NullVoiceCommandProvider()); }
        private void OnDestroy() => SetProvider(null);
        private void OnCommand(VoiceCommand c) => pending = c;

        public PlayerIntent Poll()
        {
            var i = new PlayerIntent();
            switch (pending)
            {
                case VoiceCommand.Jump: i.jumpPressed = true; break;
                case VoiceCommand.Attack: i.attackPressed = true; break;
                case VoiceCommand.Slide: i.slidePressed = true; break;
                case VoiceCommand.Pause: i.pausePressed = true; break;
            }
            pending = VoiceCommand.None;
            return i;
        }
    }
}
