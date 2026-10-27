// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Level
{
    /// <summary>Ping-pong platform. Carries the CharacterController by delta-moving it.</summary>
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, 6f);
        [SerializeField] private float period = 4f;
        private Vector3 start, lastPos;
        private CharacterController rider;

        public void Setup(Vector3 travel, float seconds) { offset = travel; period = seconds; }

        private void Start() { start = lastPos = transform.position; }

        private void FixedUpdate()
        {
            float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / period) + 1f) * 0.5f;
            transform.position = Vector3.Lerp(start, start + offset, t);
            Vector3 delta = transform.position - lastPos;
            lastPos = transform.position;
            if (rider != null && rider.enabled) rider.Move(delta);
        }

        private void OnTriggerEnter(Collider other) { var cc = other.GetComponent<CharacterController>(); if (cc != null) rider = cc; }
        private void OnTriggerExit(Collider other) { if (rider != null && other.gameObject == rider.gameObject) rider = null; }
    }
}
