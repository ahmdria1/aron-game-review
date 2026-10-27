// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Fx;
using Aron.Player;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>Collectible coin. Spins/bobs; 100 = extra life.</summary>
    public class DateCollectible : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private float spinSpeed = 120f;
        private Vector3 basePos;
        private float phase;
        private bool taken;

        /// <summary>Coins still lying around, so the falcon doesn't have to search the scene.</summary>
        public static readonly List<DateCollectible> All = new List<DateCollectible>();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            basePos = transform.position;
            phase = Random.value * 6.28f;
            // The spin and bob only run while the coin is on screen.
            var bob = gameObject.AddComponent<DateBob>();
            bob.Setup(basePos, spinSpeed, phase);
            VisibilityGroup.Attach(bob);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Collect();
        }

        public void Collect()
        {
            if (taken) return;
            taken = true;
            if (GameManager.Instance != null) GameManager.Instance.AddDates(value);
            FxPlayer.Play("coin", transform.position);
            Destroy(gameObject);
        }
    }

    /// <summary>The coin's spin and bob, kept apart so it can be switched off when the coin is off screen.</summary>
    public class DateBob : MonoBehaviour
    {
        private Vector3 basePos;
        private float spinSpeed, phase;

        public void Setup(Vector3 basePos, float spinSpeed, float phase) { this.basePos = basePos; this.spinSpeed = spinSpeed; this.phase = phase; }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3f + phase) * 0.12f;
        }
    }
}
