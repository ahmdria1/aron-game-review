// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic.Feedback;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Aron.Feedback
{
    /// <summary>
    /// The phone side of haptics. Android: a VibrationEffect waveform with amplitudes (API 26+ and a motor that can vary strength), a
    /// timing-only waveform on motors that cannot, the legacy vibrate(long[]) below API 26, and Handheld.Vibrate() as the last resort.
    /// iOS: UIImpactFeedbackGenerator / UINotificationFeedbackGenerator through Plugins/iOS/AronHaptics.mm. Editor and other platforms do nothing.
    /// Needs the VIBRATE permission on Android (Unity adds it when Handheld.Vibrate is referenced, which this file does).
    /// </summary>
    public class HapticService : IHapticService
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AronHapticPrepare();
        [DllImport("__Internal")] private static extern void AronHapticImpact(int style, float intensity);
        [DllImport("__Internal")] private static extern void AronHapticNotification(int type);
        [DllImport("__Internal")] private static extern int AronHapticLowPower();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator, powerManager;
        private AndroidJavaClass effectClass;
        private int sdk;
        private bool amplitudeControl, failed;
#endif

        public HapticService()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
                }
                using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
                if (sdk >= 26) { effectClass = new AndroidJavaClass("android.os.VibrationEffect"); amplitudeControl = vibrator.Call<bool>("hasAmplitudeControl"); }
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Haptics] " + e.Message); failed = true; }
#elif UNITY_IOS && !UNITY_EDITOR
            try { AronHapticPrepare(); } catch (Exception) { }
#endif
        }

        /// <summary>The phone's own battery saver (Android) or Low Power Mode (iOS).</summary>
        public bool BatterySaver
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try { return powerManager != null && powerManager.Call<bool>("isPowerSaveMode"); } catch (Exception) { return false; }
#elif UNITY_IOS && !UNITY_EDITOR
                try { return AronHapticLowPower() == 1; } catch (Exception) { return false; }
#else
                return false;
#endif
            }
        }

        public void Play(HapticRequest request)
        {
            var p = request.pattern;
            if (p == null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (failed || vibrator == null) { Handheld.Vibrate(); return; }
            try
            {
                var timings = new long[p.timings.Length];
                for (int i = 0; i < timings.Length; i++) timings[i] = p.timings[i];
                if (sdk >= 26)
                {
                    AndroidJavaObject effect = amplitudeControl
                        ? effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, request.amplitudes, -1)
                        : effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1);
                    vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", timings, -1);
            }
            catch (Exception) { failed = true; Handheld.Vibrate(); }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                float strength = 0.4f;
                foreach (var a in request.amplitudes) if (a / 255f > strength) strength = a / 255f;
                switch (p.ios)
                {
                    case "success": AronHapticNotification(0); break;
                    case "warning": AronHapticNotification(1); break;
                    case "error": AronHapticNotification(2); break;
                    case "heavy": AronHapticImpact(2, strength); break;
                    case "medium": AronHapticImpact(1, strength); break;
                    default: AronHapticImpact(0, strength); break;
                }
            }
            catch (Exception) { }
#endif
        }

        public void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { vibrator?.Call("cancel"); } catch (Exception) { }
#endif
        }
    }
}
