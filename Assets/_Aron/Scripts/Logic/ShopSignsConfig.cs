// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    [Serializable]
    public class ShopSignEntry
    {
        public string id;
        public string textAr;
        public string imageUrl;
        public string colorHex;
    }

    /// <summary>Server-provided shop sign content (schema: Resources/RemoteConfig/default_signs.json).</summary>
    [Serializable]
    public class ShopSignsConfig
    {
        public int version;
        public ShopSignEntry[] signs;
        /// <summary>Optional season settings from the server (see SeasonCalendar).</summary>
        public Seasons.SeasonOverride seasons = new Seasons.SeasonOverride();

        public ShopSignEntry Find(string id)
        {
            if (signs == null || id == null) return null;
            foreach (var s in signs) if (s != null && s.id == id) return s;
            return null;
        }

        /// <summary>A config is usable if it has a sign list and every sign has an id.</summary>
        public bool IsValid()
        {
            if (signs == null) return false;
            foreach (var s in signs) if (s == null || string.IsNullOrEmpty(s.id)) return false;
            return true;
        }

        /// <summary>Accept a downloaded config unless it is invalid or older than what we have.</summary>
        public static bool ShouldReplace(ShopSignsConfig current, ShopSignsConfig incoming)
        {
            if (incoming == null || !incoming.IsValid()) return false;
            return current == null || incoming.version >= current.version;
        }
    }
}
