// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Logic.Performance;
using UnityEngine;

namespace Aron.Fx
{
    /// <summary>Remembers a particle prefab's own particle limit, so thinning effects under heat never compounds.</summary>
    public class PooledFx : MonoBehaviour
    {
        public ParticleSystem system;
        public int baseMax;
    }

    /// <summary>
    /// Re-uses short-lived objects (hit puffs, shockwaves, projectiles, crate bits) instead of instantiating and
    /// destroying them during play. One component updates, and only while something is waiting to be handed back.
    /// Pools register with the low-memory trim, which frees spare objects.
    /// </summary>
    public class Pools : MonoBehaviour
    {
        private static Pools instance;
        private readonly Dictionary<GameObject, Pool<GameObject>> pools = new Dictionary<GameObject, Pool<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> owner = new Dictionary<GameObject, GameObject>();
        private readonly ReleaseScheduler<GameObject> scheduler = new ReleaseScheduler<GameObject>(32);
        private System.Action<GameObject> releaseAction;

        private static Pools Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("Pools");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Pools>();
                instance.releaseAction = instance.Release;
                instance.enabled = false;
                Caches.Registry.Register("pools", 1, () => instance != null ? instance.Trim() : 0);
                return instance;
            }
        }

        /// <summary>Takes (or creates) one object from the prefab's pool, places it and, if <paramref name="lifetime"/> is above zero, hands it back after that long.</summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 0f, int max = 16)
        {
            if (prefab == null) return null;
            return Instance.Take(prefab, position, rotation, lifetime, max);
        }

        public static void Despawn(GameObject go)
        {
            if (go == null) return;
            if (instance == null || !instance.owner.ContainsKey(go)) { Destroy(go); return; }
            instance.Release(go);
        }

        /// <summary>Hands the object back after <paramref name="delay"/> seconds.</summary>
        public static void Despawn(GameObject go, float delay)
        {
            if (go == null) return;
            if (instance == null || !instance.owner.ContainsKey(go)) { Destroy(go, delay); return; }
            instance.scheduler.Schedule(go, Time.time + delay);
            instance.enabled = true;
        }

        private GameObject Take(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime, int max)
        {
            if (!pools.TryGetValue(prefab, out var pool))
            {
                pool = new Pool<GameObject>(() => Make(prefab), max, null, Park, Kill);
                pools[prefab] = pool;
            }
            var go = pool.Get();
            if (go == null) go = pool.Get(); // a pooled object was destroyed with its scene: take a fresh one
            if (go == null) return null;
            owner[go] = prefab;
            go.transform.SetParent(null, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            if (lifetime > 0f)
            {
                scheduler.Schedule(go, Time.time + lifetime);
                enabled = true;
            }
            return go;
        }

        private GameObject Make(GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var fx = go.AddComponent<PooledFx>();
                fx.system = ps; fx.baseMax = ps.main.maxParticles;
            }
            return go;
        }

        private void Park(GameObject go)
        {
            if (go == null) return;
            var fx = go.GetComponent<PooledFx>();
            if (fx != null) fx.system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
        }

        private static void Kill(GameObject go) { if (go != null) Destroy(go); }

        private void Release(GameObject go)
        {
            if (go == null) return;
            if (owner.TryGetValue(go, out var prefab) && pools.TryGetValue(prefab, out var pool)) pool.Release(go);
            else Destroy(go);
        }

        private int Trim()
        {
            int n = 0;
            foreach (var p in pools.Values) n += p.Trim(2);
            return n;
        }

        private void Update()
        {
            scheduler.Collect(Time.time, releaseAction);
            if (scheduler.Count == 0) enabled = false;
        }
    }
}
