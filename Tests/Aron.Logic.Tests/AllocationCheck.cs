// (c) Ahmed Alamrani. All rights reserved.
using System;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>
    /// Proves that a piece of per-frame logic does not allocate: runs it a few times to warm up, then counts the
    /// bytes this thread allocates over many calls. It covers only the pure logic; Unity-side code needs the profiler.
    /// </summary>
    public static class AllocationCheck
    {
        public static long Measure(Action body, int iterations = 2000)
        {
            for (int i = 0; i < 50; i++) body();
            GC.Collect(); GC.WaitForPendingFinalizers();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) body();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        public static void AssertNoAllocation(Action body, string what, int iterations = 2000)
        {
            long bytes = Measure(body, iterations);
            Assert.AreEqual(0, bytes, what + " allocated " + bytes + " bytes over " + iterations + " calls");
        }
    }
}
