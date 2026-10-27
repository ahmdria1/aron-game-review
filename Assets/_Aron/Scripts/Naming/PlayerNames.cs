// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Threading.Tasks;
using Aron.Logic;
using Aron.Logic.Names;
using Aron.Save;
using UnityEngine;

namespace Aron.Naming
{
    /// <summary>
    /// The game's single door to player names: loads the word lists from Resources/Names once, builds the checker, and keeps the name in the save.
    /// The server side (Cloud Code, cloud/AronNames) is plugged in with <see cref="UseRemote"/> once Unity Gaming Services is set up; until then
    /// names are kept on the device and marked "not confirmed".
    /// </summary>
    public static class PlayerNames
    {
        private static NameChecker checker;
        private static PlayerNameService service;
        private static IPlayerNameService remote;

        public static NameChecker Checker
        {
            get
            {
                if (checker != null) return checker;
                var lists = new List<BlocklistFile>();
                foreach (var lang in new[] { "ar", "en", "tr", "de", "es", "pt", "id", "ms", "fr", "ur", "hi", "ja" })
                {
                    var t = Resources.Load<TextAsset>("Names/blocklist_" + lang);
                    if (t != null) lists.Add(JsonUtility.FromJson<BlocklistFile>(t.text));
                }
                var rt = Resources.Load<TextAsset>("Names/reserved");
                var reserved = rt != null ? JsonUtility.FromJson<ReservedFile>(rt.text) : new ReservedFile();
                checker = new NameChecker(lists, reserved);
                Logic.Online.NameFilter.Checker = checker;
                return checker;
            }
        }

        public static IPlayerNameService Service => service ?? (service = new PlayerNameService(Checker, remote));

        /// <summary>Plug in the Cloud Code client when the backend exists.</summary>
        public static void UseRemote(IPlayerNameService r) { remote = r; service = null; }

        public static bool HasName => !string.IsNullOrEmpty(SaveSystem.Data.playerName);

        public static string Display => NameRegistry.Display(SaveSystem.Data.playerName, SaveSystem.Data.playerNameTag);

        public static bool CanRaceOnline => HasName && NameFlow.CanRaceOnline(SaveSystem.Data.nameReserved, SaveSystem.Data.nameMustChange);

        /// <summary>Saves the result of a successful reserve/change (also the offline case).</summary>
        public static void Apply(NameResult r)
        {
            var d = SaveSystem.Data;
            d.playerName = r.Name;
            d.playerNameTag = r.Status == NameStatus.Ok ? r.Tag : 0;
            d.nameReserved = r.Status == NameStatus.Ok;
            d.nameMustChange = false;
            d.nameChangedTicks = System.DateTime.UtcNow.Ticks;
            SaveSystem.Save();
        }

        /// <summary>When the game comes back online: tries once to confirm a name that was only kept on the device.</summary>
        public static async Task SyncAsync()
        {
            var d = SaveSystem.Data;
            if (!HasName || d.nameReserved || remote == null) return;
            var r = await Service.ReserveAsync(d.playerName, true);
            if (r.Status == NameStatus.Ok) Apply(r);
        }
    }
}
