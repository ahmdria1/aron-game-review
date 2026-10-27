// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Integrity
{
    public enum ClockTrust
    {
        /// <summary>Anchored to a server time seen this session.</summary>
        Server,
        /// <summary>Offline, but the device clock agrees with the monotonic clock since the last check.</summary>
        Verified,
        /// <summary>Offline after a reboot or a first run: no way to compare. Time is kept from going backwards only.</summary>
        Unverified,
        /// <summary>The device clock jumped away from the monotonic clock: it was set forward or back.</summary>
        Tampered,
    }

    /// <summary>What is kept between sessions so an offline check can compare clocks (store it in the save).</summary>
    [Serializable]
    public class ClockMemory
    {
        public long deviceTicks;
        public long monotonicMs = -1;
        public long serverTicks;
        public int tamperCount;
    }

    /// <summary>
    /// The time used for 48-hour rentals and daily caps. Online: the server's time plus the monotonic clock since that answer.
    /// Offline: the device clock is accepted only while it moves in step with the monotonic clock (Android elapsedRealtime or
    /// iOS system uptime, which a player can't set). A jump larger than the tolerance is reported and the expected time is used instead.
    /// </summary>
    public class TrustedClock
    {
        private readonly Func<DateTime> device;
        private readonly Func<long> monotonicMs;
        private readonly ClockMemory mem;
        private readonly long toleranceMs;
        private DateTime anchorServer; private long anchorMono; private bool hasServer;

        public ClockTrust Trust { get; private set; } = ClockTrust.Unverified;
        public ClockMemory Memory => mem;

        public TrustedClock(Func<DateTime> deviceUtc, Func<long> monotonicMs, ClockMemory memory = null, long toleranceSeconds = 120)
        {
            device = deviceUtc; this.monotonicMs = monotonicMs; mem = memory ?? new ClockMemory(); toleranceMs = toleranceSeconds * 1000;
        }

        public void SyncServer(DateTime serverUtc)
        {
            anchorServer = serverUtc; anchorMono = monotonicMs(); hasServer = true;
            mem.serverTicks = Math.Max(mem.serverTicks, serverUtc.Ticks);
            Remember(serverUtc);
            Trust = ClockTrust.Server;
        }

        private void Remember(DateTime t) { mem.deviceTicks = Math.Max(mem.deviceTicks, t.Ticks); mem.monotonicMs = monotonicMs(); }

        public DateTime Now()
        {
            long mono = monotonicMs();
            if (hasServer)
            {
                var t = anchorServer.AddMilliseconds(mono - anchorMono);
                Remember(t);
                return t;
            }
            var dev = device();
            // The best proof we have that time passed: the last known time, plus the monotonic clock since then.
            if (mem.monotonicMs >= 0 && mono >= mem.monotonicMs)
            {
                var expected = new DateTime(mem.deviceTicks, DateTimeKind.Utc).AddMilliseconds(mono - mem.monotonicMs);
                double offMs = Math.Abs((dev - expected).TotalMilliseconds);
                if (offMs <= toleranceMs) { Trust = ClockTrust.Verified; Remember(dev); return dev; }
                Trust = ClockTrust.Tampered; mem.tamperCount++;
                Remember(expected);
                return expected;
            }
            // First run, or the phone rebooted (the monotonic clock restarted): never let time go back.
            Trust = ClockTrust.Unverified;
            var floor = new DateTime(Math.Max(mem.deviceTicks, mem.serverTicks), DateTimeKind.Utc);
            if (dev < floor) dev = floor;
            Remember(dev);
            return dev;
        }

        /// <summary>Points and rentals are only granted or extended when the clock is trusted. Spending and using what you own never needs it.</summary>
        public bool CanEarn => Trust == ClockTrust.Server || Trust == ClockTrust.Verified;
    }
}
