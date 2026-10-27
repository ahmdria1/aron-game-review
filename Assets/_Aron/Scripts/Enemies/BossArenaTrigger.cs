// (c) Ahmed Alamrani. All rights reserved.
using Aron.Player;
using UnityEngine;

namespace Aron.Enemies
{
    public class BossArenaTrigger : MonoBehaviour
    {
        [SerializeField] private RiyadhBoss boss;
        [SerializeField] private GameObject[] gatesToClose;

        private void OnTriggerEnter(Collider other)
        {
            if (boss == null || other.GetComponentInParent<PlayerController>() == null) return;
            foreach (var g in gatesToClose) if (g != null) g.SetActive(true);
            boss.Activate();
            enabled = false;
        }
    }
}
