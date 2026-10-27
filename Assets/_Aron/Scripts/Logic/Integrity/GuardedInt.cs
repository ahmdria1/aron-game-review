// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Integrity
{
    /// <summary>
    /// A whole number kept masked in memory so a memory scanner can't find "100 coins" and overwrite it. Every write picks a new
    /// mask; every read checks a second copy. If the stored bytes were edited from outside, <see cref="Tampered"/> turns on
    /// and the last good value is returned. This only slows memory editing down; the server is the real check.
    /// </summary>
    public sealed class GuardedInt
    {
        private static readonly Random Rng = new Random(Guid.NewGuid().GetHashCode());
        private int maskA, keyA, sumA, maskB, keyB;

        public bool Tampered { get; private set; }
        /// <summary>Raised once per tamper event (static: one hook for all guarded numbers).</summary>
        public static event Action<GuardedInt> TamperDetected;

        public GuardedInt(int value = 0) { Value = value; }

        private static int Sum(int v, int k) => unchecked((v * 73856093) ^ (k * 19349663) ^ 0x5bd1e995);

        public int Value
        {
            get
            {
                int v = maskA ^ keyA;
                if (Sum(v, keyA) == sumA) return v;
                Raise();
                return maskB ^ keyB;
            }
            set
            {
                lock (Rng) { keyA = Rng.Next(); keyB = Rng.Next(); }
                maskA = value ^ keyA; sumA = Sum(value, keyA);
                maskB = value ^ keyB;
            }
        }

        private void Raise() { if (!Tampered) { Tampered = true; TamperDetected?.Invoke(this); } }

        public void Add(int n) { Value = checked(Value + n); }

        /// <summary>Takes <paramref name="n"/> away; false (and nothing changes) when there isn't enough.</summary>
        public bool TrySpend(int n)
        {
            int v = Value;
            if (n < 0 || v < n) return false;
            Value = v - n;
            return true;
        }

        /// <summary>Test hook: simulates an outside memory edit of the masked field.</summary>
        internal void CorruptForTest(int newMask) { maskA = newMask; }
    }
}
