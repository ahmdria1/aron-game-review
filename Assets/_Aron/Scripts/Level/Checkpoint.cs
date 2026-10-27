// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Player;
using UnityEngine;

namespace Aron.Level
{
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Renderer flag;
        [SerializeField] private Color activeColor = new Color(0f, 0.6f, 0.25f);
        private bool active;

        private void OnTriggerEnter(Collider other)
        {
            if (active || other.GetComponentInParent<PlayerController>() == null) return;
            active = true;
            if (flag != null) flag.material.color = activeColor;
            if (GameManager.Instance != null) GameManager.Instance.SetCheckpoint(transform.position + Vector3.up * 0.5f);
        }
    }
}
