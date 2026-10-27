// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic;
using UnityEngine;

namespace Aron.Dialogue
{
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Matches DialogueSpeaker.SpeakerId (e.g. 'player', 'shopkeeper', 'boss').")]
        public string speakerId = "player";
        [TextArea(2, 4)] public string textAr = "";
        public float duration = 2.5f;
        public float delayBefore = 0.2f;
        [Tooltip("Face and body reaction while the line is on screen.")]
        public Emotion emotion = Emotion.Neutral;
    }

    [CreateAssetMenu(menuName = "Aron/Dialogue Sequence", fileName = "Dialogue")]
    public class DialogueSequence : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}
