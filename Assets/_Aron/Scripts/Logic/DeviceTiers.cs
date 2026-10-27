// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    public enum QualityTier { Low = 0, Medium = 1, High = 2 }

    /// <summary>What we can learn about the phone at startup (all from SystemInfo/Screen).</summary>
    public struct DeviceSpecs
    {
        public int memoryMB;
        public int cpuCores;
        public string gpuName;
        public bool isIOS;
        public int screenWidth, screenHeight;
    }

    /// <summary>
    /// Picks a graphics tier from memory and GPU, and the render scale that keeps the 3D view near a
    /// tier-specific height. Phones with 1440p panels otherwise burn battery drawing pixels nobody sees.
    /// </summary>
    public static class DeviceTiers
    {
        // SystemInfo reports a bit under the marketing number: a "4 GB" phone shows ~3.6 GB.
        public const int LowMemoryMB = 3200;
        public const int HighMemoryMB = 5200;
        public const int IosLowMemoryMB = 2500;
        public const int IosHighMemoryMB = 3500;

        // Older GPU families that struggle with URP lighting at 60 fps. A trailing * matches the whole
        // family (Mali-T*: T720, T830...); without it the model must match exactly, so "Mali-G71" does not
        // catch the much newer Mali-G710.
        private static readonly string[] LowGpus =
        {
            "Mali-4*", "Mali-T*", "Mali-G31", "Mali-G51", "Mali-G52",
            "Adreno (TM) 3*", "Adreno (TM) 4*", "Adreno (TM) 50*", "Adreno (TM) 51*",
            "PowerVR Rogue GE*", "PowerVR SGX*", "PowerVR GE*",
        };

        private static readonly string[] MidGpus =
        {
            "Mali-G57", "Mali-G68", "Mali-G71", "Mali-G72", "Mali-G76",
            "Adreno (TM) 5*", "Adreno (TM) 61*", "Adreno (TM) 62*",
        };

        public static QualityTier Pick(DeviceSpecs d)
        {
            QualityTier byMemory;
            if (d.isIOS)
            {
                byMemory = d.memoryMB <= 0 ? QualityTier.Medium
                         : d.memoryMB < IosLowMemoryMB ? QualityTier.Low
                         : d.memoryMB < IosHighMemoryMB ? QualityTier.Medium
                         : QualityTier.High;
                return byMemory;
            }

            byMemory = d.memoryMB <= 0 ? QualityTier.Medium
                     : d.memoryMB < LowMemoryMB ? QualityTier.Low
                     : d.memoryMB < HighMemoryMB ? QualityTier.Medium
                     : QualityTier.High;
            if (d.cpuCores > 0 && d.cpuCores < 6 && byMemory > QualityTier.Low) byMemory--;

            var byGpu = QualityTier.High;
            if (Matches(d.gpuName, LowGpus)) byGpu = QualityTier.Low;
            else if (Matches(d.gpuName, MidGpus)) byGpu = QualityTier.Medium;

            return byMemory < byGpu ? byMemory : byGpu;
        }

        public static int TargetHeight(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Low: return 720;
                case QualityTier.Medium: return 900;
                default: return 1080;
            }
        }

        /// <summary>URP render scale for this screen: 1 on small panels, less on very sharp ones.</summary>
        public static float RenderScale(QualityTier tier, int screenWidth, int screenHeight)
        {
            int shortSide = Math.Min(screenWidth, screenHeight);
            if (shortSide <= 0) return 1f;
            float scale = (float)TargetHeight(tier) / shortSide;
            return scale >= 1f ? 1f : Math.Max(0.5f, scale);
        }

        public static int FrameRate(QualityTier tier) => tier == QualityTier.Low ? 30 : 60;

        private static bool Matches(string gpu, string[] prefixes)
        {
            if (string.IsNullOrEmpty(gpu)) return false;
            foreach (var p in prefixes)
            {
                bool family = p.EndsWith("*");
                string name = family ? p.Substring(0, p.Length - 1) : p;
                int at = gpu.IndexOf(name, StringComparison.OrdinalIgnoreCase);
                if (at < 0) continue;
                int next = at + name.Length;
                if (family || next >= gpu.Length || !char.IsDigit(gpu[next])) return true;
            }
            return false;
        }
    }
}
