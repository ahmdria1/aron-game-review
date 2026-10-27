// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Runtime.InteropServices;
using Aron.Logic.Performance;
using UnityEngine;

namespace Aron.Core
{
    /// <summary>
    /// Reads heat and the system's power-saving flag. It is called every few seconds, never per frame.
    /// Android asks PowerManager (thermal status needs Android 10); iOS asks ProcessInfo through AronPower.mm.
    /// Anything that fails reads as "normal", so a quirky phone is never throttled by mistake.
    /// </summary>
    public static class PowerProbe
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int AronThermalState();
        [DllImport("__Internal")] private static extern int AronLowPowerMode();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject powerManager;
        private static int sdk = -1;

        private static AndroidJavaObject Manager()
        {
            if (powerManager != null) return powerManager;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
            using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
            return powerManager;
        }
#endif

        public static ThermalLevel Thermal()
        {
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                return ThermalMap.FromIos(AronThermalState());
#elif UNITY_ANDROID && !UNITY_EDITOR
                var pm = Manager();
                if (pm == null || sdk < 29) return ThermalLevel.Nominal;
                return ThermalMap.FromAndroid(pm.Call<int>("getCurrentThermalStatus"));
#endif
            }
            catch (Exception) { /* read as normal */ }
            return ThermalLevel.Nominal;
        }

        public static bool OsPowerSave()
        {
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                return AronLowPowerMode() != 0;
#elif UNITY_ANDROID && !UNITY_EDITOR
                var pm = Manager();
                return pm != null && pm.Call<bool>("isPowerSaveMode");
#endif
            }
            catch (Exception) { /* read as off */ }
            return false;
        }

        /// <summary>0..100, or -1 when the phone does not say.</summary>
        public static int BatteryPercent()
        {
            float l = SystemInfo.batteryLevel;
            return l < 0f ? -1 : Mathf.Clamp(Mathf.RoundToInt(l * 100f), 0, 100);
        }

        public static bool Charging() => SystemInfo.batteryStatus == BatteryStatus.Charging || SystemInfo.batteryStatus == BatteryStatus.Full;
    }
}
