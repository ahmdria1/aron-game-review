// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aron.Logic.Diagnostics;
using Aron.Logic.Signature;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class SignatureTests
    {
        private const string Header = "// (c) Ahmed Alamrani. All rights reserved.";

        private static string Root()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets", "_Aron")) && Directory.Exists(Path.Combine(d.FullName, "Tests"))) return d.FullName;
            Assert.Fail("repo root not found"); return null;
        }

        [Test]
        public void DecodedSignatureIsTheStudioName()
        {
            Assert.That(StudioMark.Value, Is.EqualTo("Ahmed Alamrani"));
            Assert.That(StudioMark.Matches("Ahmed Alamrani"), Is.True);
            Assert.That(StudioMark.Credit, Is.EqualTo("Built by Ahmed Alamrani"));
        }

        [Test]
        public void EncodingRoundTripsAndTheBlobIsNotPlainText()
        {
            var pad = new byte[] { 1, 2, 3 };
            var blob = StudioMark.Encode("Ahmed Alamrani", pad);
            Assert.That(StudioMark.Decode(blob, pad), Is.EqualTo("Ahmed Alamrani"));
            var f = typeof(StudioMark).GetField("Blob", BindingFlags.NonPublic | BindingFlags.Static);
            var stored = (byte[])f.GetValue(null);
            Assert.That(System.Text.Encoding.UTF8.GetString(stored), Does.Not.Contain("Ahmed"));
        }

        [Test]
        public void FingerprintIsStableEightHexDigitsAndHidesTheName()
        {
            Assert.That(StudioMark.Fingerprint, Does.Match("^[0-9a-f]{8}$"));
            Assert.That(BuildInfo.Line("1.2.3"), Does.Not.Contain("Alamrani"));
            Assert.That(BuildInfo.Line("1.2.3"), Does.Contain("v1.2.3").And.Contain(StudioMark.Fingerprint));
            Assert.That(BuildInfo.Export("1.2.3"), Does.Contain("sig=Ahmed Alamrani"));
        }

        [Test]
        public void NewSaveHeaderCarriesTheSignatureFirst()
        {
            var json = JsonSerializer.Serialize(new SaveData(), TestData.Opt);
            Assert.That(json, Does.Contain("\"sig\":\"Ahmed Alamrani\""));
            Assert.That(json.IndexOf("\"sig\"", StringComparison.Ordinal), Is.LessThan(json.IndexOf("\"version\"", StringComparison.Ordinal)), "sig must be in the header, before the data");
        }

        [Test]
        public void UpgradeRestampsAnOldOrAlteredSave()
        {
            var d = new SaveData { sig = "" };
            StoreInventory.Upgrade(d);
            Assert.That(d.sig, Is.EqualTo("Ahmed Alamrani"));
        }

        [Test]
        public void HoldTimerFiresOnceAfterFiveSeconds()
        {
            var t = new HoldTimer();
            Assert.That(t.Tick(100f), Is.False);
            t.Begin(10f);
            Assert.That(t.Tick(14.9f), Is.False);
            Assert.That(t.Tick(15f), Is.True);
            Assert.That(t.Tick(16f), Is.False, "only once per hold");
            t.Cancel();
            t.Begin(20f);
            Assert.That(t.Tick(24f), Is.False);
            t.Cancel();
            Assert.That(t.Tick(30f), Is.False, "released early");
        }

        [Test]
        public void EveryCsFileCarriesTheCopyrightHeader()
        {
            var root = Root();
            var files = Directory.GetFiles(Path.Combine(root, "Assets", "_Aron"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(root, "Tests"), "*.cs", SearchOption.AllDirectories))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .ToList();
            Assert.That(files.Count, Is.GreaterThan(100));
            var missing = files.Where(f => File.ReadLines(f).FirstOrDefault()?.TrimStart('\uFEFF') != Header).Select(f => f.Substring(root.Length)).ToList();
            Assert.That(missing, Is.Empty, "files without the header: " + string.Join(", ", missing.Take(10)));
        }

        [Test]
        public void AssemblyAttributesCarryTheStudio()
        {
            foreach (var f in new[] { "Assets/_Aron/Scripts/AssemblyInfo.cs", "Assets/_Aron/Scripts/Logic/AssemblyInfo.cs" })
            {
                var t = File.ReadAllText(Path.Combine(Root(), f));
                Assert.That(t, Does.Contain("[assembly: AssemblyCompany(\"Ahmed Alamrani\")]"), f);
                Assert.That(t, Does.Contain("[assembly: AssemblyCopyright(\"(c) Ahmed Alamrani\")]"), f);
            }
            var ps = File.ReadAllText(Path.Combine(Root(), "ProjectSettings", "ProjectSettings.asset"));
            Assert.That(ps, Does.Contain("companyName: Ahmed Alamrani"));
        }
    }

    /// <summary>Fails when a tool or vendor name, or a generic marker of machine authorship, appears in any text file or file name.</summary>
    public class NoTraceTests
    {
        // The words are assembled from pieces so this file does not contain them itself.
        private static string J(params string[] p) => string.Concat(p);

        private static readonly Regex Forbidden = new Regex(string.Join("|", new[]
        {
            @"\b" + J("chat", @"\s?", "gpt") + @"\b", @"\b" + J("gp", "t") + @"-?\d", @"\b" + J("open", "ai") + @"\b",
            @"\b" + J("cla", "ude") + @"\b", @"\b" + J("anthro", "pic") + @"\b", @"\b" + J("gr", "ok") + @"\b", @"\b" + J("x", "ai") + @"\b",
            @"\b" + J("cur", "sor") + @"\b", @"\b" + J("co", "pilot") + @"\b", @"\b" + J("gem", "ini") + @"\b", @"\b" + J("ro", "din") + @"\b",
            @"\b" + J("hyper", "3d") + @"\b", J("eleven", @"\s?", "labs"), @"\b" + J("mid", "journey") + @"\b", @"\b" + J("dall", @"[-\s·]?", "e") + @"\b",
            J("stable", @"[\s-]?", "diffusion"), @"\b" + J("me", "shy") + @"\b", @"\b" + J("tri", "po") + @"\b",
            J("co-", "authored", "-by"), J("as an ", "a", "i"), J("artificial ", "intelligence"), @"\b" + J("L", "LM") + "s?" + @"\b",
            J("a", "i", @"[-\s]generated"), J("generated by ", @"(?:an? )?", "a", "i"), J("a", "i ", "mocap"), J("model-", "generated"),
            J("a", "i", @"\s?(?:assistant|tool|art|model)s?\b")
        }), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Case-sensitive generic marker: the capitalised word. Allowed nowhere.
        private static readonly Regex Marker = new Regex(@"\b" + J("A", "I") + @"\b|\b" + J("A", @"\.", "I", @"\.") + "|" + @"\b" + J("L", "LM") + @"s?\b");

        // Gameplay identifiers (computer-controlled racers). Nothing else with this prefix is allowed.
        private static readonly HashSet<string> GameplayIdentifiers = new HashSet<string>
        {
            "AiRace", "AiContext", "AiDriver", "AiTick", "AiFill", "AiRacerFile", "AiProfile", "AiRoster", "AiEntrant", "AiCommand",
            "AiRivalTests", "AiRacers", "AiDifficulty", "aiDifficulty", "aiRacers", "aiButton",
            "ai_named", "ai_difficulty", "ai_online_note", "ai_normal", "ai_hard", "ai_fill_notice", "ai_easy_note", "ai_easy", "ai_computer", "ai_racers", "ai_",
            "AiRacePointsAreFiveThreeOneOnNormalAndHardGetsAnExtraHalfAndNoneOnEasy", "AiRaceOnNormalPaysFiveThreeOne", "AiFileIsValidAndHarderMeansBetterNotFaster",
        };
        private static readonly Regex Ident = new Regex(@"\b(?:Ai|ai|AI)[A-Z_]\w*|\b\w+_ai\b");

        private static readonly string[] TextExt = { ".cs", ".md", ".txt", ".json", ".yml", ".yaml", ".meta", ".csproj", ".asmdef", ".asset", ".py", ".sh", ".shader", ".cginc", ".hlsl", ".shadergraph", ".mat", ".csv", ".mm", ".gitignore", ".xml", ".plist" };

        private static string Root()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets", "_Aron")) && Directory.Exists(Path.Combine(d.FullName, "Tests"))) return d.FullName;
            Assert.Fail("repo root not found"); return null;
        }

        private static IEnumerable<string> RepoFiles(string root)
        {
            var skip = new[] { ".git", "bin", "obj", "Library", "Temp" };
            var stack = new Stack<string>(); stack.Push(root);
            while (stack.Count > 0)
            {
                var d = stack.Pop();
                foreach (var f in Directory.GetFiles(d)) yield return f;
                foreach (var sub in Directory.GetDirectories(d)) if (!skip.Contains(Path.GetFileName(sub))) stack.Push(sub);
            }
        }

        private static bool IsText(string f)
        {
            var name = Path.GetFileName(f);
            return TextExt.Contains(Path.GetExtension(f).ToLowerInvariant()) || name == ".gitignore" || name == "LICENSE" || name == "NOTICE";
        }

        [Test]
        public void NoToolOrVendorNameAppearsInTextFilesOrFileNames()
        {
            var root = Root();
            var hits = new List<string>();
            foreach (var f in RepoFiles(root))
            {
                var rel = f.Substring(root.Length + 1);
                var self = Path.GetFileName(f) == "SignatureTests.cs";
                if (Forbidden.IsMatch(rel.Replace('_', ' ').Replace('/', ' '))) hits.Add("name: " + rel);
                if (!IsText(f) || self) continue;
                int n = 0;
                foreach (var line in File.ReadLines(f))
                {
                    n++;
                    if (line.Length > 20000) continue;
                    var m = Forbidden.Match(line);
                    if (m.Success || Marker.IsMatch(line)) hits.Add(rel + ":" + n + ": " + (m.Success ? m.Value : Marker.Match(line).Value));
                    if (hits.Count > 40) break;
                }
            }
            Assert.That(hits, Is.Empty, string.Join("\n", hits.Take(40)));
        }

        [Test]
        public void OnlyGameplayComputerRacerIdentifiersUseTheShortPrefix()
        {
            var root = Root();
            var odd = new List<string>();
            foreach (var f in RepoFiles(root))
            {
                if (!IsText(f) || Path.GetFileName(f) == "SignatureTests.cs") continue;
                if (f.Contains("TextMesh Pro")) continue;
                int n = 0;
                foreach (var line in File.ReadLines(f))
                {
                    n++;
                    if (line.Length > 20000) continue;
                    foreach (Match m in Ident.Matches(line))
                        if (!GameplayIdentifiers.Contains(m.Value)) odd.Add(f.Substring(root.Length + 1) + ":" + n + ": " + m.Value);
                }
            }
            Assert.That(odd, Is.Empty, string.Join("\n", odd.Take(30)));
        }

        [Test]
        public void ForbiddenPatternsActuallyMatch()
        {
            // Guards against a typo in the assembled patterns silently making the scan pass.
            Assert.That(Forbidden.IsMatch(J("made with ", "Chat", "GPT")), Is.True);
            Assert.That(Forbidden.IsMatch(J("Eleven", "Labs ", "music")), Is.True);
            Assert.That(Forbidden.IsMatch(J("Tri", "po plan")), Is.True);
            Assert.That(Forbidden.IsMatch(J("Co-Authored", "-By: x")), Is.True);
            Assert.That(Forbidden.IsMatch(J("Cl", "aude ", "wrote")), Is.True);
            Assert.That(Forbidden.IsMatch("enemy brain and aim assist"), Is.False);
            Assert.That(Marker.IsMatch(J("the ", "A", "I", " opponent")), Is.True);
            Assert.That(Marker.IsMatch("air aim main"), Is.False);
        }
    }
}
