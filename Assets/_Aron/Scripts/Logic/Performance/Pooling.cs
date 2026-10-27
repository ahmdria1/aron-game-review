// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Performance
{
    /// <summary>
    /// Re-uses short-lived things (crates breaking, particles, projectiles, one-shot sounds) instead of
    /// creating and destroying them mid-play. After <see cref="Prewarm"/>, Get and Release do not allocate.
    /// </summary>
    public class Pool<T> where T : class
    {
        private readonly Stack<T> free;
        private readonly Func<T> create;
        private readonly Action<T> onGet, onRelease, onDrop;

        public int MaxSize { get; }
        public int Created { get; private set; }
        public int Reused { get; private set; }
        public int Dropped { get; private set; }
        public int InUse { get; private set; }
        public int Free => free.Count;

        public Pool(Func<T> create, int maxSize = 32, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDrop = null)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            MaxSize = maxSize < 1 ? 1 : maxSize;
            free = new Stack<T>(MaxSize);
            this.onGet = onGet; this.onRelease = onRelease; this.onDrop = onDrop;
        }

        public void Prewarm(int count)
        {
            for (int i = free.Count; i < count && i < MaxSize; i++)
            {
                var item = create();
                Created++;
                onRelease?.Invoke(item);
                free.Push(item);
            }
        }

        public T Get()
        {
            T item;
            if (free.Count > 0) { item = free.Pop(); Reused++; }
            else { item = create(); Created++; }
            InUse++;
            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            if (InUse > 0) InUse--;
            if (free.Count >= MaxSize) { Dropped++; onDrop?.Invoke(item); return; }
            onRelease?.Invoke(item);
            free.Push(item);
        }

        /// <summary>Frees spare items down to <paramref name="keep"/> (used when memory is short).</summary>
        public int Trim(int keep = 0)
        {
            int n = 0;
            while (free.Count > keep) { var item = free.Pop(); onDrop?.Invoke(item); n++; }
            return n;
        }
    }

    /// <summary>
    /// Hands things back to their pool after a delay without one timer or coroutine per thing.
    /// The owner runs one update only while <see cref="Count"/> is above zero.
    /// </summary>
    public class ReleaseScheduler<T> where T : class
    {
        private T[] items;
        private float[] due;
        public int Count { get; private set; }

        public ReleaseScheduler(int capacity = 32)
        {
            items = new T[capacity < 4 ? 4 : capacity];
            due = new float[items.Length];
        }

        public void Schedule(T item, float at)
        {
            if (Count == items.Length) { Array.Resize(ref items, Count * 2); Array.Resize(ref due, Count * 2); }
            items[Count] = item; due[Count] = at; Count++;
        }

        /// <summary>Calls <paramref name="release"/> for everything that is due. Allocation-free.</summary>
        public int Collect(float now, Action<T> release)
        {
            int n = 0;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (due[i] > now) continue;
                var item = items[i];
                Count--;
                items[i] = items[Count]; due[i] = due[Count]; items[Count] = null;
                release(item);
                n++;
            }
            return n;
        }

        public void Clear() { Array.Clear(items, 0, Count); Count = 0; }
    }

    public enum TrimLevel { Moderate = 0, Critical = 1 }

    /// <summary>
    /// Everything that can give memory back (pools, decoded sound caches, downloaded sign pictures)
    /// registers here; a low-memory warning trims them cheapest-first.
    /// </summary>
    public class CacheRegistry
    {
        private struct Entry { public string name; public int priority; public bool criticalOnly; public Func<int> trim; }
        private readonly List<Entry> entries = new List<Entry>();
        public int Count => entries.Count;
        public int Trims { get; private set; }

        /// <summary>Lower priority numbers are given up first. The function returns how many items it freed.</summary>
        public void Register(string name, int priority, Func<int> trim, bool criticalOnly = false)
        {
            entries.RemoveAll(e => e.name == name);
            entries.Add(new Entry { name = name, priority = priority, trim = trim, criticalOnly = criticalOnly });
            entries.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public void Unregister(string name) => entries.RemoveAll(e => e.name == name);

        public int TrimAll(TrimLevel level)
        {
            Trims++;
            int freed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].criticalOnly && level != TrimLevel.Critical) continue;
                try { freed += entries[i].trim(); } catch (Exception) { /* one broken cache must not stop the rest */ }
            }
            return freed;
        }
    }
}
