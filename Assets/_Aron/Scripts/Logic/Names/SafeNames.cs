// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Names
{
    /// <summary>
    /// Names a child can pick without typing: an adjective and an animal ("Swift Falcon", "صقر سريع"). Every combination passes the same checker as a typed name.
    /// Also the "Racer" name given when a name is taken away.
    /// </summary>
    public static class SafeNames
    {
        public const string RacerBase = "Racer";

        public static readonly string[] AdjectivesEn = { "Brave", "Swift", "Happy", "Bright", "Calm", "Clever", "Kind", "Lucky", "Sunny", "Cool" };
        public static readonly string[] AnimalsEn = { "Falcon", "Oryx", "Camel", "Horse", "Gazelle", "Fox", "Wolf", "Hawk", "Ibex", "Lion" };
        public static readonly string[] AdjectivesAr = { "شجاع", "سريع", "مرح", "لامع", "هادئ", "ذكي", "طيب", "محظوظ", "مشرق", "رائع" };
        public static readonly string[] AnimalsAr = { "صقر", "مها", "جمل", "حصان", "غزال", "ثعلب", "ذيب", "باز", "وعل", "اسد" };

        public static bool UsesArabic(string lang) => lang == "ar" || lang == "ur";

        /// <summary>The nth safe name for a seed. Different n give different names.</summary>
        public static string Make(int seed, int n, string lang)
        {
            bool ar = UsesArabic(lang);
            var adj = ar ? AdjectivesAr : AdjectivesEn;
            var ani = ar ? AnimalsAr : AnimalsEn;
            int total = adj.Length * ani.Length;
            int idx = (int)(((long)(seed & 0x7FFFFFFF) + (long)n * 37L) % total); // 37 is coprime with 100, so five picks never repeat
            string a = adj[idx % adj.Length], b = ani[idx / adj.Length];
            return ar ? b + " " + a : a + " " + b;
        }

        public static List<string> Options(int seed, string lang, int count = 5)
        {
            var list = new List<string>();
            for (int i = 0; i < count; i++) list.Add(Make(seed, i, lang));
            return list;
        }

        public static string RacerName(string playerId) => RacerBase;

        /// <summary>The same number for the same text on every machine (string.GetHashCode is not).</summary>
        public static int StableHash(string s)
        {
            unchecked
            {
                int h = 17;
                if (s != null) foreach (char c in s) h = h * 31 + c;
                return h & 0x7FFFFFFF;
            }
        }
    }
}
