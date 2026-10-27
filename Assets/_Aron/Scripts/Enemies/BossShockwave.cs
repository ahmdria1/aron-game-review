// (c) Ahmed Alamrani. All rights reserved.
using Aron.Player;
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>Expanding ground ring. Jump over it (only hurts grounded players near the ring edge).</summary>
    public class BossShockwave : MonoBehaviour
    {
        public float maxRadius = 9f;
        public float speed = 8f;
        public float thickness = 0.8f;
        private float radius = 0.5f;
        private PlayerController player;

        private void Start() { player = FindFirstObjectByType<PlayerController>(); }

        private void Update()
        {
            radius += speed * Time.deltaTime;
            transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            if (player != null)
            {
                Vector3 d = player.transform.position - transform.position;
                float flat = new Vector2(d.x, d.z).magnitude;
                if (Mathf.Abs(flat - radius) < thickness && d.y < 0.6f && d.y > -1.5f) player.Hurt(true);
            }
            if (radius >= maxRadius) Destroy(gameObject);
        }
    }
}
