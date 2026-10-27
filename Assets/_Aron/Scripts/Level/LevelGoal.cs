// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Player;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>End-of-level portal. Optionally locked until the boss is defeated.</summary>
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] private bool requiresBossDefeat = true;
        [SerializeField] private GameObject lockedVisual;
        private bool unlocked;

        public void SetRequiresBoss(bool required) => requiresBossDefeat = required;

        private void OnEnable() { GameEvents.BossDefeated += Unlock; }
        private void OnDisable() { GameEvents.BossDefeated -= Unlock; }
        private void Start() { unlocked = !requiresBossDefeat; if (lockedVisual != null) lockedVisual.SetActive(!unlocked); }

        private void Unlock() { unlocked = true; if (lockedVisual != null) lockedVisual.SetActive(false); }

        private void OnTriggerEnter(Collider other)
        {
            if (!unlocked || other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null) GameManager.Instance.CompleteLevel();
        }
    }
}
