// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>A famous building the hero should stop and admire the first time it comes into view.</summary>
    public class Landmark : MonoBehaviour
    {
        public static readonly List<Landmark> All = new List<Landmark>();

        [SerializeField] private float viewDistance = 45f;
        [SerializeField] private float focusHeight = 8f;

        public void Setup(float distance, float height) { viewDistance = distance; focusHeight = height; }

        public bool Seen { get; set; }
        public float ViewDistance => viewDistance;
        public Vector3 Focus => transform.position + Vector3.up * focusHeight;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);
    }
}
