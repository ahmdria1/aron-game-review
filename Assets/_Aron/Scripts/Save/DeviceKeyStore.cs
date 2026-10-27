// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic.Security;
using UnityEngine;

namespace Aron.Save
{
    /// <summary>
    /// Platform wrapper for the per-install save secret: Android Keystore (KeyVault.java), iOS Keychain (AronKeychain.mm).
    /// In the editor, or if the native call fails, it falls back to PlayerPrefs plus the device id (weaker; see docs/security.md).
    /// </summary>
    public sealed class DeviceKeyStore : IKeyStore
    {
        private const string PrefKey = "aron.k";

        public string DeviceId => SystemInfo.deviceUniqueIdentifier;

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern int AronKeychainLoad(byte[] buffer, int capacity);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern int AronKeychainSave(byte[] data, int length);
#endif

        public byte[] Load()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vault = new AndroidJavaClass("com.ahmedalamrani.aron.KeyVault"))
                {
                    var s = vault.CallStatic<byte[]>("load", activity);
                    if (s != null && s.Length > 0) return s;
                }
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Keystore unavailable: " + e.Message); }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                var buf = new byte[64];
                int n = AronKeychainLoad(buf, buf.Length);
                if (n > 0) { var s = new byte[n]; Buffer.BlockCopy(buf, 0, s, 0, n); return s; }
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Keychain unavailable: " + e.Message); }
#endif
            var v = PlayerPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(v)) return null;
            try { return Convert.FromBase64String(v); } catch (FormatException) { return null; }
        }

        public void Save(byte[] secret)
        {
            bool stored = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vault = new AndroidJavaClass("com.ahmedalamrani.aron.KeyVault"))
                    stored = vault.CallStatic<bool>("save", activity, secret);
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Keystore unavailable: " + e.Message); }
#elif UNITY_IOS && !UNITY_EDITOR
            try { stored = AronKeychainSave(secret, secret.Length) == 1; }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Keychain unavailable: " + e.Message); }
#endif
            if (!stored) { PlayerPrefs.SetString(PrefKey, Convert.ToBase64String(secret)); PlayerPrefs.Save(); }
        }
    }
}
