// (c) Ahmed Alamrani. All rights reserved.
using System;
using UnityEngine;

namespace Aron.Fx
{
    public class FxPlayer : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public ParticleSystem prefab;
        }

        [SerializeField] private Entry[] effects = Array.Empty<Entry>();

        private static FxPlayer instance;

        private void Awake() => instance = this;
        private void OnDestroy() { if (instance == this) instance = null; }

        public static void Play(string id, Vector3 position) => Play(id, position, Quaternion.identity);

        public static void Play(string id, Vector3 position, Quaternion rotation)
        {
            if (instance == null) return;
            var prefab = instance.Find(id);
            if (prefab == null) return;
            var go = Pools.Spawn(prefab.gameObject, position, rotation, 0f, 12);
            if (go == null) return;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            // Under heat or battery saving fewer particles are drawn (the pooled copy remembers the prefab's own limit).
            var fx = go.GetComponent<PooledFx>();
            if (fx != null) main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(fx.baseMax * Core.QualityBus.Current.particles));
            ps.Clear(true);
            ps.Play(true);
            Pools.Despawn(go, main.duration + main.startLifetime.constantMax + 0.5f);
        }

        private ParticleSystem Find(string id)
        {
            foreach (var e in effects) if (e.id == id) return e.prefab;
            return null;
        }
    }
}
