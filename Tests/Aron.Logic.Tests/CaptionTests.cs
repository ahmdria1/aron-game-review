// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using Aron.Logic.Access;
using Aron.Logic.Audio;
using Aron.Logic.Localization;
using Aron.Logic.Safety;
using NUnit.Framework;
using static Aron.Logic.Tests.TestData;

namespace Aron.Logic.Tests
{
    /// <summary>Captions for what is said and what is heard, and the text size that goes with them.</summary>
    public class CaptionTests
    {
        private static CaptionFile File_() => Read<CaptionFile>("Config", "captions.json");

        // sounds that carry no information: a button tap and the jump the player just made
        private static readonly string[] NoCaption = { "click", "jump" };

        [Test]
        public void TheCaptionFileIsValidInEveryLanguage()
        {
            var f = File_();
            Assert.That(f.Validate(), Is.Empty);
            Assert.That(f.entries.Length, Is.GreaterThan(100));
        }

        [Test]
        public void EveryVoiceLineHasACaptionInItsOwnLanguage()
        {
            var f = File_();
            var voices = Read<VoiceLineFile>("Audio", "VoiceLines.json");
            foreach (var l in voices.lines)
            {
                var e = f.Find(l.id);
                Assert.That(e, Is.Not.Null, l.id);
                Assert.That(e.text.ar.Trim('(', ')', ' '), Is.EqualTo(l.textAr.Trim('(', ')', ' ')), l.id + ": the Arabic caption is the spoken line");
                string who = l.character.StartsWith("boss_") ? "boss" : l.character == "any" ? "" : l.character;
                Assert.That(e.speaker, Is.EqualTo(who), l.id);
            }
        }

        [Test]
        public void EverySoundTheGamePlaysHasACaption()
        {
            var f = File_();
            foreach (var id in AudioCues.Sfx.Concat(AudioCues.Voice)) Assert.That(f.Find(id), Is.Not.Null, id);
            foreach (var path in Directory.GetFiles(Path.Combine(Resources(), "Audio", "Sfx"), "*.wav"))
            {
                string id = Path.GetFileNameWithoutExtension(path);
                if (NoCaption.Contains(id)) continue;
                Assert.That(f.Find(id), Is.Not.Null, id);
            }
            foreach (var id in new[] { "crate_break", "jar_break", "glass_break", "metal_break", "wall_crumble", "explosion", "boss_hit", "thunder_near", "thunder_far", "wind_gust" })
                Assert.That(f.Find(id), Is.Not.Null, id);
        }

        [Test]
        public void SoundsAreSoundKindAndSpeechIsNot()
        {
            var f = File_();
            foreach (var id in AudioCues.Sfx) Assert.That(f.Find(id).GetKind(), Is.EqualTo(CaptionKind.Sound), id);
            foreach (var l in Read<VoiceLineFile>("Audio", "VoiceLines.json").lines)
                if (l.kind == "taunt" || l.kind == "hurt" || l.kind == "defeat") continue;
            // a boss grunt with no words is captioned as a sound, so it is bracketed and can be switched off
            Assert.That(f.Find("vo_boss_shark_taunt").GetKind(), Is.EqualTo(CaptionKind.Sound));
            Assert.That(f.Find("vo_aron_help").GetKind(), Is.EqualTo(CaptionKind.Speech));
            Assert.That(f.entries.Where(e => e.GetKind() == CaptionKind.Sound).All(e => !e.text.ar.StartsWith("(")));
        }

        [Test]
        public void WarningsAreImportantAndDecorationIsNot()
        {
            var f = File_();
            foreach (var id in new[] { AudioCues.BishtLost, AudioCues.PuzzleWrong, AudioCues.TimerTick, AudioCues.ChaseStart, "hit", "enemy", "gameover" })
                Assert.That(f.Find(id).important, Is.True, id);
            foreach (var id in new[] { AudioCues.Lever, AudioCues.CratePush, "bounce", "coin", "thunder_far" })
                Assert.That(f.Find(id).important, Is.False, id);
        }

        [Test]
        public void TheLanguageFallsBackToEnglishThenArabic()
        {
            var f = File_();
            Assert.That(f.Text("vo_aron_help", "fr"), Is.EqualTo("À l'aide ! À l'aide !"));
            Assert.That(f.Text("vo_aron_help", "xx"), Is.EqualTo(f.Text("vo_aron_help", "en")));
            Assert.That(f.Text("vo_aron_help", "ar"), Is.EqualTo("الحقوني! الحقوني!"));
            Assert.That(f.Text("nothing", "en"), Is.Null);
            Assert.That(f.Text(null, "en"), Is.Null);
        }

        [Test]
        public void ValidateCatchesAShortOrMissingTranslation()
        {
            var f = new CaptionFile { entries = new[] { new CaptionEntry { id = "a" } } };
            Assert.That(f.Validate(), Is.Not.Empty);
            var e = new CaptionEntry { id = "b" };
            foreach (var l in UiLanguages.Codes) e.text = Set(e.text, l, new string('x', 61));
            Assert.That(new CaptionFile { entries = new[] { e } }.Validate().Any(x => x.Contains("longer")));
        }

        private static Aron.Logic.Culture.LocalText Set(Aron.Logic.Culture.LocalText t, string lang, string v)
        {
            typeof(Aron.Logic.Culture.LocalText).GetField(lang).SetValue(t, v);
            return t;
        }

        // ---- when a caption is shown

        [Test]
        public void SpeechFollowsTheSwitchAndSoundsFollowTheirMode()
        {
            var speech = new CaptionEntry { id = "s", kind = "speech" };
            var warn = new CaptionEntry { id = "w", kind = "sound", important = true };
            var soft = new CaptionEntry { id = "d", kind = "sound" };
            var a = new AccessSettings();
            Assert.That(CaptionPolicy.Show(a, speech), Is.True, "on by default");
            Assert.That(CaptionPolicy.Show(a, warn), Is.True);
            Assert.That(CaptionPolicy.Show(a, soft), Is.False, "important only by default");
            a.soundCaptions = SoundCaptionMode.All;
            Assert.That(CaptionPolicy.Show(a, soft), Is.True);
            a.soundCaptions = SoundCaptionMode.Off;
            Assert.That(CaptionPolicy.Show(a, speech), Is.True);
            Assert.That(CaptionPolicy.Show(a, warn), Is.False);
            a.captions = false;
            Assert.That(CaptionPolicy.Show(a, speech), Is.False);
            Assert.That(CaptionPolicy.Show(null, speech), Is.False);
            Assert.That(CaptionPolicy.Show(a, null), Is.False);
        }

        [Test]
        public void SizesStepThroughAndWrap()
        {
            float s = 1f;
            var seen = new System.Collections.Generic.List<float>();
            for (int i = 0; i < CaptionRules.Sizes.Length; i++) { s = CaptionRules.NextSize(s); seen.Add(s); }
            Assert.That(seen.Distinct().Count(), Is.EqualTo(CaptionRules.Sizes.Length));
            Assert.That(seen.Last(), Is.EqualTo(1f));
            Assert.That(CaptionRules.NextSize(2f), Is.EqualTo(CaptionRules.Sizes[0]));
            Assert.That(CaptionRules.NextSize(1.2f), Is.EqualTo(1.5f), "a value between steps snaps to the nearest first");
            Assert.That(CaptionRules.FontSize(1f), Is.EqualTo(40f));
            Assert.That(CaptionRules.FontSize(99f), Is.EqualTo(80f));
            Assert.That(CaptionRules.FontSize(0f), Is.EqualTo(32f));
            Assert.That(new AccessSettings().Validate(), Is.Empty);
            foreach (var x in CaptionRules.Sizes) Assert.That(new AccessSettings { subtitleSize = x }.Validate(), Is.Empty);
        }

        [Test]
        public void ReadingTimeGrowsWithTheLineAndStaysInBounds()
        {
            Assert.That(CaptionRules.Duration(""), Is.EqualTo(CaptionRules.MinSeconds));
            Assert.That(CaptionRules.Duration(new string('x', 12)), Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(CaptionRules.Duration(new string('x', 600)), Is.EqualTo(CaptionRules.MaxSeconds));
            foreach (var e in File_().entries)
                foreach (var t in e.text.All()) Assert.That(CaptionRules.Duration(t), Is.InRange(CaptionRules.MinSeconds, CaptionRules.MaxSeconds));
        }

        [Test]
        public void ASoundIsBracketedAndSpeechIsNot()
        {
            Assert.That(CaptionRules.Format(CaptionKind.Sound, "Stomp"), Is.EqualTo("[ Stomp ]"));
            Assert.That(CaptionRules.Format(CaptionKind.Speech, "Help!"), Is.EqualTo("Help!"));
        }

        // ---- the queue

        [Test]
        public void TheQueueShowsAtMostTwoLinesAndSpeechStaysOnTop()
        {
            var q = new CaptionQueue();
            Assert.That(q.Request("a", "ding", CaptionKind.Sound, "", 0), Is.True);
            Assert.That(q.Request("b", "Help!", CaptionKind.Speech, "aron", 0.1), Is.True);
            Assert.That(q.Lines.Select(l => l.Id), Is.EqualTo(new[] { "b", "a" }));
            // a third line: the sound makes room for another sound, never for speech
            Assert.That(q.Request("c", "boom", CaptionKind.Sound, "", 0.2), Is.True);
            Assert.That(q.Lines.Select(l => l.Id), Is.EqualTo(new[] { "b", "c" }));
            Assert.That(q.Lines.Count, Is.EqualTo(2));
        }

        [Test]
        public void ASoundNeverPushesSpeechOut()
        {
            var q = new CaptionQueue();
            q.Request("s1", "One", CaptionKind.Speech, "aron", 0);
            q.Request("s2", "Two", CaptionKind.Speech, "noura", 0.1);
            Assert.That(q.Request("x", "boom", CaptionKind.Sound, "", 0.2), Is.False);
            Assert.That(q.Lines.Select(l => l.Id), Is.EqualTo(new[] { "s1", "s2" }));
            // newer speech does replace the oldest speech
            Assert.That(q.Request("s3", "Three", CaptionKind.Speech, "boss", 0.3), Is.True);
            Assert.That(q.Lines.Select(l => l.Id), Is.EqualTo(new[] { "s2", "s3" }));
        }

        [Test]
        public void TheSameCueIsNotRepeatedWhileItIsFresh()
        {
            var q = new CaptionQueue();
            Assert.That(q.Request("step", "step", CaptionKind.Sound, "", 0), Is.True);
            Assert.That(q.Request("step", "step", CaptionKind.Sound, "", 1.0), Is.False);
            Assert.That(q.Request("step", "step", CaptionKind.Sound, "", CaptionQueue.RepeatSeconds + 0.01), Is.True);
        }

        [Test]
        public void LinesExpireAndEmptyTextIsIgnored()
        {
            var q = new CaptionQueue();
            q.Request("a", "ding", CaptionKind.Sound, "", 0);
            q.Tick(1.0);
            Assert.That(q.Lines.Count, Is.EqualTo(1));
            q.Tick(CaptionRules.MaxSeconds + 1);
            Assert.That(q.Lines, Is.Empty);
            Assert.That(q.Request("e", "", CaptionKind.Speech, "", 10), Is.False);
            Assert.That(q.Request("e", null, CaptionKind.Speech, "", 10), Is.False);
            q.Request("a", "ding", CaptionKind.Sound, "", 20);
            q.Clear();
            Assert.That(q.Lines, Is.Empty);
            Assert.That(q.Request("a", "ding", CaptionKind.Sound, "", 20.1), Is.True, "clear forgets the repeat guard");
        }

        // ---- text size

        [Test]
        public void TextSizeStepsWrapAndStaysInBounds()
        {
            float s = 1f;
            var seen = new System.Collections.Generic.List<float>();
            for (int i = 0; i < TextSizeRules.Steps.Length; i++) { s = TextSizeRules.Next(s); seen.Add(s); }
            Assert.That(seen.Distinct().Count(), Is.EqualTo(TextSizeRules.Steps.Length));
            Assert.That(seen.Last(), Is.EqualTo(1f));
            Assert.That(TextSizeRules.Next(1.6f), Is.EqualTo(TextSizeRules.Steps[0]));
            foreach (var x in TextSizeRules.Steps) Assert.That(new AccessSettings { textSize = x }.Validate(), Is.Empty, x.ToString());
            Assert.That(TextSizeRules.Clamp(5f), Is.EqualTo(1.6f));
            Assert.That(TextSizeRules.Clamp(0.1f), Is.EqualTo(0.8f));
            Assert.That(TextSizeRules.Font(30, 1.15f), Is.EqualTo(35));
            Assert.That(TextSizeRules.Font(1, 0.8f), Is.EqualTo(1));
            Assert.That(TextSizeRules.Percent(1.15f), Is.EqualTo(115));
        }

        // ---- the settings text

        [Test]
        public void EveryCaptionSettingStringExistsInTwelveLanguages()
        {
            foreach (var key in new[] { "access_open", "access_title", "captions_on", "caption_sounds", "cap_mode_off", "cap_mode_important", "cap_mode_all", "caption_size", "caption_backdrop", "caption_sample", "text_size" })
                foreach (var l in UiLanguages.Codes)
                    Assert.That(UiStringTables.All[l].TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v), Is.True, l + " " + key);
            foreach (var key in new[] { "captions_on", "caption_sounds", "caption_size", "caption_backdrop", "text_size" })
                foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l][key], Does.Contain("{0}"), l + " " + key);
        }

        [Test]
        public void CaptionSettingsSurviveARoundTripAndOldSavesKeepTheDefaults()
        {
            var a = new AccessSettings { captions = false, soundCaptions = SoundCaptionMode.All, captionBackdrop = 0.5f };
            var json = System.Text.Json.JsonSerializer.Serialize(a, Opt);
            var back = System.Text.Json.JsonSerializer.Deserialize<AccessSettings>(json, Opt);
            Assert.That(back.captions, Is.False);
            Assert.That(back.soundCaptions, Is.EqualTo(SoundCaptionMode.All));
            var old = System.Text.Json.JsonSerializer.Deserialize<AccessSettings>("{\"buttonSize\":1.2}", Opt);
            Assert.That(old.captions, Is.True);
            Assert.That(old.soundCaptions, Is.EqualTo(SoundCaptionMode.Important));
            Assert.That(old.captionBackdrop, Is.EqualTo(0.75f));
            Assert.That(new AccessSettings { captionBackdrop = 0.1f }.Validate(), Is.Not.Empty);
        }
    }
}
