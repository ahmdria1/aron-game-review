// (c) Ahmed Alamrani. All rights reserved.
using Aron.Player;
using UnityEngine;

namespace Aron.Dialogue
{
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueSequence sequence;
        [SerializeField] private bool once = true;
        private bool played;

        public void SetSequence(DialogueSequence s) => sequence = s;

        private void OnTriggerEnter(Collider other)
        {
            if ((once && played) || other.GetComponentInParent<PlayerController>() == null) return;
            played = true;
            if (DialogueDirector.Instance != null) DialogueDirector.Instance.Play(sequence);
        }
    }
}
