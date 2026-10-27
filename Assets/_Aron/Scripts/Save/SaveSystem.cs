// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic;
using Aron.Logic.Integrity;
using Aron.Logic.Security;
using Aron.Logic.Signature;
using UnityEngine;

namespace Aron.Save
{
    /// <summary>JSON save file in persistentDataPath.</summary>
    public static class SaveSystem
    {
        private static SaveData data;
        private static string overridePath;
        private static SecureSaveStore store;
        private static readonly WalletWatch wallet = new WalletWatch();
        private static IKeyStore keys;

        /// <summary>Tests can swap the key store (for example an in-memory one).</summary>
        public static void UseKeyStore(IKeyStore k) { keys = k; store = null; }

        private static SecureSaveStore Store => store ?? (store = new SecureSaveStore(FilePath, keys ?? (keys = new DeviceKeyStore())));

        public static event Action<SaveData> Saved;

        public static string FilePath => overridePath ?? System.IO.Path.Combine(Application.persistentDataPath, "aron_save.json");

        public static SaveData Data
        {
            get { if (data == null) Load(); return data; }
        }

        /// <summary>Redirects storage (tests). Pass null to restore the default location.</summary>
        public static void UsePath(string path) { overridePath = path; data = null; store = null; }

        public static void Load()
        {
            data = null;
            bool rewrite = false;
            try
            {
                var outcome = Store.Read();
                if (outcome.Json != null)
                {
                    try { data = JsonUtility.FromJson<SaveData>(outcome.Json); rewrite = outcome.NeedsRewrite; }
                    catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Unreadable save, starting fresh: " + e.Message); }
                }
                if (outcome.Recovered) global::Aron.Logic.Diagnostics.Log.Warn("[Save] Save file was damaged or altered: " + outcome.Note);
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Load failed, starting fresh: " + e.Message); }
            if (data == null) data = new SaveData();
            StoreInventory.Upgrade(data);
            if (rewrite) Save();   // old plaintext save or restored backup: write it back sealed
        }

        public static void Save()
        {
            if (data == null) return;
            data.sig = StudioMark.Value;
            wallet.Audit(data);   // soft flag only, never blocks a save
            try
            {
                Store.Write(JsonUtility.ToJson(data, false));
                Saved?.Invoke(data);
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Save] Failed: " + e.Message); }
        }

        public static void ResetAll() { data = new SaveData(); Save(); }
    }
}
