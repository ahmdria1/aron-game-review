// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Text;

namespace Aron.Logic
{
    /// <summary>
    /// In-house Arabic shaper (RTLTMPro-style, no dependency). Converts logical Arabic text into
    /// contextual presentation forms (isolated/initial/medial/final + lam-alef ligatures).
    /// Output is still in LOGICAL order; pair it with TMP's isRightToLeftText (see Aron.Localization.ArabicTMP),
    /// which reverses glyphs per line. LTR runs (digits/Latin) are pre-reversed so they read correctly.
    /// </summary>
    public static class ArabicText
    {
        // iso, final, initial, medial (0 = form not available => right-joining only)
        private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
        {
            {'\u0621', new[]{'\uFE80','\0','\0','\0'}},
            {'\u0622', new[]{'\uFE81','\uFE82','\0','\0'}},
            {'\u0623', new[]{'\uFE83','\uFE84','\0','\0'}},
            {'\u0624', new[]{'\uFE85','\uFE86','\0','\0'}},
            {'\u0625', new[]{'\uFE87','\uFE88','\0','\0'}},
            {'\u0626', new[]{'\uFE89','\uFE8A','\uFE8B','\uFE8C'}},
            {'\u0627', new[]{'\uFE8D','\uFE8E','\0','\0'}},
            {'\u0628', new[]{'\uFE8F','\uFE90','\uFE91','\uFE92'}},
            {'\u0629', new[]{'\uFE93','\uFE94','\0','\0'}},
            {'\u062A', new[]{'\uFE95','\uFE96','\uFE97','\uFE98'}},
            {'\u062B', new[]{'\uFE99','\uFE9A','\uFE9B','\uFE9C'}},
            {'\u062C', new[]{'\uFE9D','\uFE9E','\uFE9F','\uFEA0'}},
            {'\u062D', new[]{'\uFEA1','\uFEA2','\uFEA3','\uFEA4'}},
            {'\u062E', new[]{'\uFEA5','\uFEA6','\uFEA7','\uFEA8'}},
            {'\u062F', new[]{'\uFEA9','\uFEAA','\0','\0'}},
            {'\u0630', new[]{'\uFEAB','\uFEAC','\0','\0'}},
            {'\u0631', new[]{'\uFEAD','\uFEAE','\0','\0'}},
            {'\u0632', new[]{'\uFEAF','\uFEB0','\0','\0'}},
            {'\u0633', new[]{'\uFEB1','\uFEB2','\uFEB3','\uFEB4'}},
            {'\u0634', new[]{'\uFEB5','\uFEB6','\uFEB7','\uFEB8'}},
            {'\u0635', new[]{'\uFEB9','\uFEBA','\uFEBB','\uFEBC'}},
            {'\u0636', new[]{'\uFEBD','\uFEBE','\uFEBF','\uFEC0'}},
            {'\u0637', new[]{'\uFEC1','\uFEC2','\uFEC3','\uFEC4'}},
            {'\u0638', new[]{'\uFEC5','\uFEC6','\uFEC7','\uFEC8'}},
            {'\u0639', new[]{'\uFEC9','\uFECA','\uFECB','\uFECC'}},
            {'\u063A', new[]{'\uFECD','\uFECE','\uFECF','\uFED0'}},
            {'\u0640', new[]{'\u0640','\u0640','\u0640','\u0640'}},
            {'\u0641', new[]{'\uFED1','\uFED2','\uFED3','\uFED4'}},
            {'\u0642', new[]{'\uFED5','\uFED6','\uFED7','\uFED8'}},
            {'\u0643', new[]{'\uFED9','\uFEDA','\uFEDB','\uFEDC'}},
            {'\u0644', new[]{'\uFEDD','\uFEDE','\uFEDF','\uFEE0'}},
            {'\u0645', new[]{'\uFEE1','\uFEE2','\uFEE3','\uFEE4'}},
            {'\u0646', new[]{'\uFEE5','\uFEE6','\uFEE7','\uFEE8'}},
            {'\u0647', new[]{'\uFEE9','\uFEEA','\uFEEB','\uFEEC'}},
            {'\u0648', new[]{'\uFEED','\uFEEE','\0','\0'}},
            {'\u0649', new[]{'\uFEEF','\uFEF0','\0','\0'}},
            {'\u064A', new[]{'\uFEF1','\uFEF2','\uFEF3','\uFEF4'}},
            {'\u067E', new[]{'\uFB56','\uFB57','\uFB58','\uFB59'}}, // پ
            {'\u0686', new[]{'\uFB7A','\uFB7B','\uFB7C','\uFB7D'}}, // چ
            {'\u06A4', new[]{'\uFB6A','\uFB6B','\uFB6C','\uFB6D'}}, // ڤ
            {'\u06AF', new[]{'\uFB92','\uFB93','\uFB94','\uFB95'}}, // گ
            // Urdu letters
            {'\u0679', new[]{'\uFB66','\uFB67','\uFB68','\uFB69'}}, // ٹ
            {'\u0688', new[]{'\uFB88','\uFB89','\0','\0'}}, // ڈ
            {'\u0691', new[]{'\uFB8C','\uFB8D','\0','\0'}}, // ڑ
            {'\u0698', new[]{'\uFB8A','\uFB8B','\0','\0'}}, // ژ
            {'\u06A9', new[]{'\uFB8E','\uFB8F','\uFB90','\uFB91'}}, // ک
            {'\u06BA', new[]{'\uFB9E','\uFB9F','\0','\0'}}, // ں
            {'\u06BE', new[]{'\uFBAA','\uFBAB','\uFBAC','\uFBAD'}}, // ھ
            {'\u06C1', new[]{'\uFBA6','\uFBA7','\uFBA8','\uFBA9'}}, // ہ
            {'\u06C2', new[]{'\uFBA4','\uFBA5','\0','\0'}}, // ۂ
            {'\u06CC', new[]{'\uFBFC','\uFBFD','\uFBFE','\uFBFF'}}, // ی
            {'\u06D2', new[]{'\uFBAE','\uFBAF','\0','\0'}}, // ے
            {'\u06D3', new[]{'\uFBB0','\uFBB1','\0','\0'}}, // ۓ
        };

        // lam + alef variant -> (isolated, final)
        private static readonly Dictionary<char, char[]> LamAlef = new Dictionary<char, char[]>
        {
            {'\u0622', new[]{'\uFEF5','\uFEF6'}},
            {'\u0623', new[]{'\uFEF7','\uFEF8'}},
            {'\u0625', new[]{'\uFEF9','\uFEFA'}},
            {'\u0627', new[]{'\uFEFB','\uFEFC'}},
        };

        public static bool IsDiacritic(char c) => (c >= '\u064B' && c <= '\u065F') || c == '\u0670';
        public static bool IsArabicLetter(char c) => Forms.ContainsKey(c);
        public static bool ContainsArabic(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFEFF')) return true;
            return false;
        }

        private static bool JoinsForward(char c) => Forms.TryGetValue(c, out var f) && f[2] != '\0';

        /// <summary>Shape for use with TMP isRightToLeftText = true.</summary>
        public static string Fix(string input)
        {
            if (!ContainsArabic(input)) return input;
            return ReverseLtrRuns(MirrorBrackets(Shape(input)));
        }

        /// <summary>Contextual shaping only (logical order kept).</summary>
        public static string Shape(string s)
        {
            var sb = new StringBuilder(s.Length);
            int n = s.Length;
            for (int i = 0; i < n; i++)
            {
                char c = s[i];
                if (!Forms.TryGetValue(c, out var forms)) { sb.Append(c); continue; }

                int prev = PrevBase(s, i);
                int next = NextBase(s, i);
                bool prevJoins = prev >= 0 && JoinsForward(s[prev]);

                // Lam-alef ligature
                if (c == '\u0644' && next >= 0 && LamAlef.TryGetValue(s[next], out var lig))
                {
                    sb.Append(prevJoins ? lig[1] : lig[0]);
                    for (int k = i + 1; k < next; k++) sb.Append(s[k]); // keep diacritics on lam
                    i = next;
                    continue;
                }

                bool nextJoins = next >= 0 && IsArabicLetter(s[next]) && s[next] != '\u0621';
                bool dual = forms[2] != '\0';
                char outc;
                if (prevJoins && nextJoins && dual) outc = forms[3];
                else if (prevJoins && forms[1] != '\0') outc = forms[1];
                else if (nextJoins && dual) outc = forms[2];
                else outc = forms[0];
                sb.Append(outc);
            }
            return sb.ToString();
        }

        private static int PrevBase(string s, int i)
        {
            for (int k = i - 1; k >= 0; k--) { if (IsDiacritic(s[k])) continue; return IsArabicLetter(s[k]) ? k : -1; }
            return -1;
        }

        private static int NextBase(string s, int i)
        {
            for (int k = i + 1; k < s.Length; k++) { if (IsDiacritic(s[k])) continue; return IsArabicLetter(s[k]) ? k : -1; }
            return -1;
        }

        private static bool IsLtr(char c) =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '\u0660' && c <= '\u0669');

        private static bool IsLtrJoiner(char c) => c == '.' || c == ',' || c == ':' || c == '%' || c == '/' || c == '-' || c == '+';

        /// <summary>TMP's RTL mode reverses everything; pre-reverse LTR runs (e.g. "12:30", "Aron 2") so they survive.</summary>
        public static string ReverseLtrRuns(string s)
        {
            var chars = s.ToCharArray();
            int i = 0;
            while (i < chars.Length)
            {
                if (!IsLtr(chars[i]) || InsideTag(chars, i)) { i++; continue; }
                int start = i, end = i;
                int j = i;
                while (j < chars.Length && (IsLtr(chars[j]) || chars[j] == ' ' || IsLtrJoiner(chars[j])))
                {
                    if (IsLtr(chars[j])) end = j;
                    j++;
                }
                System.Array.Reverse(chars, start, end - start + 1);
                i = end + 1;
            }
            return new string(chars);
        }

        private static bool InsideTag(char[] c, int i)
        {
            for (int k = i; k >= 0; k--) { if (c[k] == '>') return false; if (c[k] == '<') return true; }
            return false;
        }

        private static string MirrorBrackets(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '(': sb.Append(')'); break;
                    case ')': sb.Append('('); break;
                    case '[': sb.Append(']'); break;
                    case ']': sb.Append('['); break;
                    case '«': sb.Append('»'); break;
                    case '»': sb.Append('«'); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
