// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Aron.Logic.Names
{
    /// <summary>The reason codes a name check can give. Each one has a line of text in every language ("name_err_" + code).</summary>
    public static class NameCode
    {
        public const string Empty = "empty", TooShort = "too_short", TooLong = "too_long", EdgeSpace = "edge_space", Separators = "separators",
            Hidden = "hidden_chars", Emoji = "emoji", BadChar = "bad_char", MixedScript = "mixed_script", NotEnoughLetters = "not_enough_letters",
            TooManyDigits = "too_many_digits", Blocked = "blocked", Reserved = "reserved", Taken = "taken", Cooldown = "cooldown",
            RateLimited = "rate_limited", Error = "error";

        public static readonly string[] All =
        {
            Empty, TooShort, TooLong, EdgeSpace, Separators, Hidden, Emoji, BadChar, MixedScript, NotEnoughLetters, TooManyDigits, Blocked, Reserved, Taken, Cooldown, RateLimited, Error,
        };
    }

    /// <summary>The shape of a player name, as pure rules. The same class runs in the game and in the Cloud Code module (see docs/player_names.md).</summary>
    public static class PlayerNameRules
    {
        public const int Min = 3, Max = 16, MaxDigits = 4, MinLetters = 2;

        // ---- characters ---------------------------------------------------------------------------------------

        public static bool IsLatinLetter(char c) =>
            (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '\u00C0' && c <= '\u024F' && c != '\u00D7' && c != '\u00F7');

        public static bool IsArabicLetter(char c) =>
            (c >= '\u0621' && c <= '\u063A') || (c >= '\u0641' && c <= '\u064A') || (c >= '\u066E' && c <= '\u066F') ||
            (c >= '\u0671' && c <= '\u06D3') || c == '\u06D5' || (c >= '\u06FA' && c <= '\u06FC') || c == '\u06FF';

        public static bool IsDigit(char c) => (c >= '0' && c <= '9') || (c >= '\u0660' && c <= '\u0669') || (c >= '\u06F0' && c <= '\u06F9');

        /// <summary>Zero-width, bidi-control, invisible and formatting characters: they make two names look the same, or hide text.</summary>
        public static bool IsHidden(char c)
        {
            if (c == '\u00AD' || c == '\u034F' || c == '\u061C' || c == '\u115F' || c == '\u1160' || c == '\u17B4' || c == '\u17B5' || c == '\u3164' || c == '\uFEFF' || c == '\uFFA0') return true;
            if (c >= '\u180B' && c <= '\u180E') return true;
            if (c >= '\u200B' && c <= '\u200F') return true;
            if (c >= '\u202A' && c <= '\u202E') return true;
            if (c >= '\u2060' && c <= '\u206F') return true;
            if (c >= '\uFE00' && c <= '\uFE0F') return true;
            if (c >= '\uFFF9' && c <= '\uFFFB') return true;
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            return cat == UnicodeCategory.Control || cat == UnicodeCategory.Format || cat == UnicodeCategory.LineSeparator ||
                   cat == UnicodeCategory.ParagraphSeparator || cat == UnicodeCategory.PrivateUse;
        }

        public static bool IsEmoji(char c)
        {
            if (char.IsSurrogate(c)) return true; // everything outside the basic plane: emoji and rare symbols
            if (c >= '\u2190' && c <= '\u2BFF') return true; // arrows, symbols, dingbats, shapes
            if (c == '\u00A9' || c == '\u00AE' || c == '\u203C' || c == '\u2049' || c == '\u2122' || c == '\u3030' || c == '\u303D') return true;
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            return cat == UnicodeCategory.OtherSymbol || cat == UnicodeCategory.ModifierSymbol;
        }

        public static bool IsSeparator(char c) => c == ' ' || c == '_';

        /// <summary>The name without tatweel (the stretching mark that makes the same Arabic word look different).</summary>
        public static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name) if (c != '\u0640') sb.Append(c);
            return sb.ToString();
        }

        // ---- the shape check ----------------------------------------------------------------------------------

        /// <summary>Null when the shape is fine; otherwise a <see cref="NameCode"/>. <paramref name="clean"/> is the name that will be kept.</summary>
        public static string CheckShape(string name, out string clean)
        {
            clean = "";
            if (string.IsNullOrEmpty(name)) return NameCode.Empty;
            foreach (char c in name) if (IsHidden(c)) return NameCode.Hidden;
            foreach (char c in name) if (IsEmoji(c)) return NameCode.Emoji;
            if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[name.Length - 1])) return NameCode.EdgeSpace;
            clean = Clean(name);
            bool latin = false, arabic = false;
            int letters = 0, digits = 0, seps = 0;
            for (int i = 0; i < clean.Length; i++)
            {
                char c = clean[i];
                if (IsLatinLetter(c)) { latin = true; letters++; }
                else if (IsArabicLetter(c)) { arabic = true; letters++; }
                else if (IsDigit(c)) digits++;
                else if (IsSeparator(c)) seps++;
                else return NameCode.BadChar; // other whitespace, punctuation, other scripts, combining marks, diacritics
            }
            if (clean.Length == 0) return NameCode.Empty;
            if (clean.Length < Min) return NameCode.TooShort;
            if (clean.Length > Max) return NameCode.TooLong;
            if (IsSeparator(clean[0]) || IsSeparator(clean[clean.Length - 1])) return NameCode.Separators;
            if (seps > 1) return NameCode.Separators;
            if (latin && arabic) return NameCode.MixedScript;
            if (letters < MinLetters) return NameCode.NotEnoughLetters;
            if (digits > MaxDigits) return NameCode.TooManyDigits;
            return null;
        }

        // ---- the comparison form ------------------------------------------------------------------------------

        /// <summary>Arabic letters folded to one form: alef forms, taa marbuta, alef maqsura, hamza carriers, diacritics, tatweel, and the Persian and Urdu look-alikes.</summary>
        public static char NormalizeArabic(char c)
        {
            switch (c)
            {
                case '\u0623': case '\u0625': case '\u0622': case '\u0671': case '\u0672': case '\u0673': return '\u0627'; // أ إ آ ٱ -> ا
                case '\u0629': case '\u06C1': case '\u06BE': case '\u06D5': case '\u06C3': return '\u0647';              // ة ہ ھ ە -> ه
                case '\u0649': case '\u06CC': case '\u06D2': case '\u0626': case '\u06D0': return '\u064A';              // ى ی ے ئ -> ي
                case '\u0624': return '\u0648';                                                                           // ؤ -> و
                case '\u06A9': case '\u06AF': case '\u06AA': return '\u0643';                                           // ک گ -> ك
                case '\u067E': return '\u0628'; case '\u0686': return '\u062C'; case '\u0698': return '\u0632';           // پ چ ژ
                case '\u0679': return '\u062A'; case '\u0688': return '\u062F'; case '\u0691': return '\u0631'; case '\u06BA': return '\u0646'; // ٹ ڈ ڑ ں
                default: return c;
            }
        }

        private static char Leet(char c)
        {
            switch (c)
            {
                case '0': return 'o'; case '1': case '!': case '|': case 'l': return 'i'; case '3': return 'e'; case '4': case '@': return 'a';
                case '5': case '$': return 's'; case '7': case '+': return 't'; case '8': return 'b'; case '9': return 'g';
                default: return c;
            }
        }

        /// <summary>Lower case, accents removed, Arabic folded, Arabic-Indic digits to 0-9, underscore to space. Digits and separators are kept.</summary>
        public static string Base(string name)
        {
            var sb = new StringBuilder();
            foreach (char c0 in Clean(name))
            {
                char c = c0;
                if (c >= '\u0660' && c <= '\u0669') c = (char)('0' + (c - '\u0660'));
                else if (c >= '\u06F0' && c <= '\u06F9') c = (char)('0' + (c - '\u06F0'));
                if (c == '_') c = ' ';
                if (IsArabicLetter(c))
                {
                    if (c == '\u0640' || ArabicDiacritic(c)) continue;
                    sb.Append(NormalizeArabic(c));
                    continue;
                }
                if (IsLatinLetter(c))
                {
                    string d = c.ToString().Normalize(NormalizationForm.FormD);
                    foreach (char x in d)
                    {
                        if (CharUnicodeInfo.GetUnicodeCategory(x) == UnicodeCategory.NonSpacingMark) continue;
                        char l = char.ToLowerInvariant(x);
                        switch (l)
                        {
                            case '\u0131': sb.Append('i'); break;                 // ı
                            case '\u00DF': sb.Append("ss"); break;                // ß
                            case '\u00F8': sb.Append('o'); break;                 // ø
                            case '\u0111': sb.Append('d'); break;                 // đ
                            case '\u0142': sb.Append('l'); break;                 // ł
                            case '\u00E6': sb.Append("ae"); break;                // æ
                            case '\u0153': sb.Append("oe"); break;                // œ
                            default: sb.Append(l); break;
                        }
                    }
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool ArabicDiacritic(char c) => (c >= '\u064B' && c <= '\u065F') || c == '\u0670';

        private static string StripTrailingDigits(string s)
        {
            int e = s.Length;
            while (e > 0 && s[e - 1] >= '0' && s[e - 1] <= '9') e--;
            return e >= 2 ? s.Substring(0, e) : s;
        }

        /// <summary>
        /// A token of the name (a part between spaces or underscores) as the lists see it: trailing digits dropped when letters remain,
        /// look-alike digits and symbols turned into letters, l and 1 and I the same letter, "rn" the same as "m", "vv" as "w".
        /// </summary>
        public static string TokenKey(string baseToken, bool stripTrailing = true)
        {
            string t = stripTrailing ? StripTrailingDigits(baseToken) : baseToken;
            var sb = new StringBuilder();
            foreach (char c in t)
            {
                char x = Leet(c);
                if (IsLatinLetter(x) || IsArabicLetter(x)) sb.Append(x);
            }
            return sb.ToString().Replace("rn", "m").Replace("vv", "w");
        }

        /// <summary>The tokens of a name in comparison form, one key per space-separated part.</summary>
        public static List<string> Tokens(string name, bool stripTrailing = true)
        {
            var list = new List<string>();
            foreach (var t in Base(name).Split(' ')) { var k = TokenKey(t, stripTrailing); if (k.Length > 0) list.Add(k); }
            return list;
        }

        /// <summary>The whole name as one key: no spaces, no digits that are not look-alike letters. "S p 3 x" and "sp3x" and "SPEX" are the same key.</summary>
        public static string Key(string name, bool stripTrailing = true)
        {
            string b = Base(name).Replace(" ", "");
            return TokenKey(b, stripTrailing);
        }

        /// <summary>The key with every run of the same letter shortened to one. Catches "fuuuck" and "shiiit".</summary>
        public static string Collapse(string key)
        {
            var sb = new StringBuilder();
            foreach (char c in key) if (sb.Length == 0 || sb[sb.Length - 1] != c) sb.Append(c);
            return sb.ToString();
        }

        /// <summary>The key used for "is this name taken": the same for names that look alike. Trailing digits are part of the name there, so this one keeps them.</summary>
        public static string UniqueKey(string name)
        {
            string b = Base(name).Replace(" ", "");
            var sb = new StringBuilder();
            foreach (char c in b)
            {
                if (c >= '0' && c <= '9') { char x = Leet(c); sb.Append(x == c ? c : x); continue; }
                char y = Leet(c);
                if (IsLatinLetter(y) || IsArabicLetter(y)) sb.Append(y);
            }
            return sb.ToString().Replace("rn", "m").Replace("vv", "w");
        }
    }
}
