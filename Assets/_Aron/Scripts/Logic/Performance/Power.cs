// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Performance
{
    public enum ThermalLevel { Nominal = 0, Fair = 1, Serious = 2, Critical = 3 }

    public enum ScreenMode { Gameplay, Menu, Paused, Loading, Background }

    public enum ShadowMode { Soft, Hard, Off }

    /// <summary>What the phone reports about heat, mapped onto four levels.</summary>
    public static class ThermalMap
    {
        /// <summary>iOS ProcessInfo.thermalState: 0 nominal, 1 fair, 2 serious, 3 critical.</summary>
        public static ThermalLevel FromIos(int state) => state <= 0 ? ThermalLevel.Nominal : state == 1 ? ThermalLevel.Fair : state == 2 ? ThermalLevel.Serious : ThermalLevel.Critical;

        /// <summary>
        /// Android PowerManager thermal status: 0 none, 1 light, 2 moderate, 3 severe, 4 critical, 5 emergency, 6 shutdown.
        /// Light throttling does not touch the user experience, so it still counts as nominal.
        /// </summary>
        public static ThermalLevel FromAndroid(int status) => status <= 1 ? ThermalLevel.Nominal : status == 2 ? ThermalLevel.Fair : status == 3 ? ThermalLevel.Serious : ThermalLevel.Critical;
    }

    /// <summary>Settings the player controls (saved in SaveData.power).</summary>
    [Serializable]
    public class PowerSettings
    {
        /// <summary>30 fps and lighter effects everywhere.</summary>
        public bool batterySaver;
        /// <summary>High-tier phones may run gameplay at 60 fps. Off by default: 30 fps is cooler and lasts longer.</summary>
        public bool prefer60;
        /// <summary>Waits for Wi-Fi for anything that is not the race itself, and sends less.</summary>
        public bool dataSaver;
        /// <summary>Allows large downloads on mobile data. Off by default.</summary>
        public bool mobileData;
        /// <summary>Day number (days since 2020-01-01) when the player said no to the battery-saver hint.</summary>
        public int saverHintDeclinedDay = -1;
    }

    public struct PowerInputs
    {
        public QualityTier tier;
        public ScreenMode mode;
        public bool batterySaver;
        public bool prefer60;
        public bool osPowerSave;
        public ThermalLevel thermal;
        /// <summary>0..100, or -1 when the phone does not say.</summary>
        public int batteryPercent;
        public bool charging;
        /// <summary>The frame-rate governor already found that this phone cannot hold 60 here.</summary>
        public bool heldTo30;
    }

    public readonly struct FrameCap
    {
        /// <summary>Application.targetFrameRate.</summary>
        public readonly int target;
        /// <summary>OnDemandRendering.renderFrameInterval: the screen is redrawn every Nth frame.</summary>
        public readonly int renderEvery;
        public FrameCap(int target, int renderEvery) { this.target = target; this.renderEvery = renderEvery < 1 ? 1 : renderEvery; }
        public int RenderedFps => target / renderEvery;
        public override string ToString() => target + "/" + renderEvery;
    }

    /// <summary>
    /// One place that decides how often the phone draws. Default is 30 fps on every tier; 60 is an
    /// opt-in for High phones, and it is withdrawn when the battery is low, the OS is saving power,
    /// the phone is warm, or the governor already failed to hold it. Menus, pause and loading draw 15 times a second.
    /// </summary>
    public static class FrameCapPolicy
    {
        public const int MenuFps = 15;
        public const int LowBatteryPercent = 20;

        public static bool LowBattery(in PowerInputs p) => p.batteryPercent >= 0 && p.batteryPercent < LowBatteryPercent && !p.charging;

        public static FrameCap Decide(in PowerInputs p)
        {
            switch (p.mode)
            {
                case ScreenMode.Background: return new FrameCap(1, 1);
                case ScreenMode.Menu:
                case ScreenMode.Paused:
                case ScreenMode.Loading:
                    // Keep the loop at 30 so taps feel instant; only every second frame is drawn.
                    return new FrameCap(30, 30 / MenuFps);
            }
            if (p.thermal >= ThermalLevel.Critical) return new FrameCap(20, 1);
            bool sixty = p.tier == QualityTier.High && p.prefer60 && !p.batterySaver && !p.osPowerSave && !p.heldTo30
                         && p.thermal == ThermalLevel.Nominal && !LowBattery(p);
            return new FrameCap(sixty ? 60 : 30, 1);
        }
    }

    public readonly struct QualityPlan
    {
        public readonly int step;
        /// <summary>Multiplies the tier's own render scale.</summary>
        public readonly float resolution;
        public readonly ShadowMode shadows;
        /// <summary>Share of grass and foliage instances kept (0..1).</summary>
        public readonly float foliage;
        /// <summary>Share of particle emission kept (0..1).</summary>
        public readonly float particles;
        public QualityPlan(int step, float resolution, ShadowMode shadows, float foliage, float particles)
        { this.step = step; this.resolution = resolution; this.shadows = shadows; this.foliage = foliage; this.particles = particles; }
    }

    /// <summary>The order in which things get cheaper: resolution first, then shadows, foliage, particles.</summary>
    public static class QualityLadder
    {
        public const int MaxStep = 3;
        private static readonly QualityPlan[] Plans =
        {
            new QualityPlan(0, 1.00f, ShadowMode.Soft, 1.00f, 1.00f),
            new QualityPlan(1, 0.85f, ShadowMode.Hard, 0.85f, 0.80f),
            new QualityPlan(2, 0.75f, ShadowMode.Off,  0.70f, 0.60f),
            new QualityPlan(3, 0.65f, ShadowMode.Off,  0.40f, 0.35f),
        };

        public static QualityPlan Plan(int step) => Plans[step < 0 ? 0 : step > MaxStep ? MaxStep : step];

        /// <summary>The step wanted by heat, power saving and the player's choice; the biggest wins.</summary>
        public static int StepFor(ThermalLevel applied, bool batterySaver, bool osPowerSave, bool lowBattery)
        {
            int s = (int)applied;
            if (batterySaver || osPowerSave || lowBattery) s = Math.Max(s, 1);
            return Math.Min(s, MaxStep);
        }
    }

    /// <summary>
    /// Heat reports jump around. Getting hotter is obeyed at once; getting cooler is believed only after
    /// the cooler level has lasted <see cref="CooldownSeconds"/>, one level at a time, so quality does not flicker.
    /// </summary>
    public class ThermalGovernor
    {
        public float CooldownSeconds = 60f;
        public ThermalLevel Applied { get; private set; }
        private float coolerSince = -1f;

        /// <summary>Returns true when <see cref="Applied"/> changed.</summary>
        public bool Report(ThermalLevel reported, float now)
        {
            if (reported >= Applied)
            {
                coolerSince = -1f;
                if (reported == Applied) return false;
                Applied = reported;
                return true;
            }
            if (coolerSince < 0f) coolerSince = now;
            if (now - coolerSince < CooldownSeconds) return false;
            Applied = (ThermalLevel)((int)Applied - 1);
            coolerSince = Applied > reported ? now : -1f;
            return true;
        }

        public void Reset() { Applied = ThermalLevel.Nominal; coolerSince = -1f; }
    }

    public static class BatterySaverPolicy
    {
        public static bool ShouldSuggest(in PowerInputs p, PowerSettings s, int today)
        {
            if (s.batterySaver) return false;
            if (s.saverHintDeclinedDay == today) return false;
            return p.osPowerSave || FrameCapPolicy.LowBattery(p);
        }
    }
}
