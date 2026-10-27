// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Characters;
using Aron.Logic;
using UnityEngine;

namespace Aron.Dialogue
{
    /// <summary>Plays sequences as timed bubbles on the registered speakers. Gameplay keeps running.</summary>
    public class DialogueDirector : MonoBehaviour
    {
        public static DialogueDirector Instance { get; private set; }
        private Coroutine running;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Play(DialogueSequence seq, bool interrupt = true)
        {
            if (seq == null || seq.lines == null) return;
            if (running != null) { if (!interrupt) return; StopCoroutine(running); }
            running = StartCoroutine(Run(seq));
        }

        private IEnumerator Run(DialogueSequence seq)
        {
            foreach (var line in seq.lines)
            {
                if (line.delayBefore > 0f) yield return new WaitForSeconds(line.delayBefore);
                var sp = DialogueSpeaker.Find(line.speakerId);
                if (sp != null)
                {
                    sp.Say(line.textAr, line.duration);
                    if (line.emotion != Emotion.Neutral)
                    {
                        var feel = sp.GetComponentInChildren<CharacterEmotion>();
                        if (feel != null) feel.Feel(line.emotion, line.duration + 0.3f);
                    }
                }
                yield return new WaitForSeconds(line.duration);
            }
            running = null;
        }
    }
}
