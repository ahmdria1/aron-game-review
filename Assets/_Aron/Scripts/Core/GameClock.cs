// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic.Integrity;
using Aron.Save;
using UnityEngine;

namespace Aron.Core
{
    /// <summary>The one clock for rentals, daily caps and gifts: server time when online, a checked device clock otherwise.</summary>
    public static class GameClock
    {
        private static TrustedClock clock;
        private static object memoryOwner;

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern long AronMonotonicMs();
#endif

        /// <summary>Milliseconds from a clock the player can't set, that keeps counting while the phone sleeps.</summary>
        private static long Monotonic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var c = new AndroidJavaClass("android.os.SystemClock")) return c.CallStatic<long>("elapsedRealtime");
#elif UNITY_IOS && !UNITY_EDITOR
            return AronMonotonicMs();
#else
            return (long)(System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
#endif
        }

        private static TrustedClock Get()
        {
            var data = SaveSystem.Data;   // the memory lives in the save, so it survives restarts
            if (clock == null || !ReferenceEquals(memoryOwner, data))
            {
                clock = new TrustedClock(() => DateTime.UtcNow, Monotonic, data.clock);
                memoryOwner = data;
            }
            return clock;
        }

        public static DateTime Now()
        {
            var c = Get();
            var t = c.Now();
            if (c.Trust == ClockTrust.Tampered) SoftFlags.Raise(SoftFlag.ClockTampered);
            return t;
        }

        public static bool CanEarn { get { Now(); return Get().CanEarn; } }

        /// <summary>Called with the time a Cloud Code answer carries.</summary>
        public static void SyncServer(DateTime serverUtc) { Get().SyncServer(serverUtc); }
    }
}
