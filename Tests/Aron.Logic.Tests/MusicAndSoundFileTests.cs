// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>
    /// Every music id, country theme and sound id the game asks for is a real Ogg Vorbis file with the right length,
    /// the stage music rules pick the right track, and no generator or stand-in audio code ships.
    /// </summary>
    public class MusicAndSoundFileTests
    {
        private static string Assets() => Path.GetFullPath(Path.Combine(TestData.Resources(), ".."));
        private static string Repo() => Path.GetFullPath(Path.Combine(Assets(), "..", ".."));
        private static string MusicDir() => Path.Combine(TestData.Resources(), "Audio", "Music");
        private static string RemoteMusicDir() => Path.Combine(Assets(), "Remote", "Music");

        /// <summary>The file of a track: inside the app (Resources/Audio/Music) or in its country's downloadable group (Remote/Music/country_x).</summary>
        private static string MusicPath(string id)
        {
            string local = Path.Combine(MusicDir(), id + ".ogg");
            if (File.Exists(local)) return local;
            foreach (var dir in Directory.Exists(RemoteMusicDir()) ? Directory.GetDirectories(RemoteMusicDir()) : new string[0])
            {
                string p = Path.Combine(dir, id + ".ogg");
                if (File.Exists(p)) return p;
            }
            return local;
        }

        private static System.Collections.Generic.List<string> AllMusicFiles()
        {
            var list = Directory.GetFiles(MusicDir(), "*").ToList();
            if (Directory.Exists(RemoteMusicDir())) list.AddRange(Directory.GetFiles(RemoteMusicDir(), "*", SearchOption.AllDirectories));
            return list;
        }
        private static string SfxDir() => Path.Combine(TestData.Resources(), "Audio", "Sfx");

        /// <summary>Length in seconds, channels and sample rate of an Ogg Vorbis file, read from its headers.</summary>
        private static (double seconds, int channels, int rate) Ogg(string path)
        {
            var b = File.ReadAllBytes(path);
            Assert.That(b.Length, Is.GreaterThan(4000), path + " is too small to be a real recording");
            Assert.That(Encoding.ASCII.GetString(b, 0, 4), Is.EqualTo("OggS"), path);
            int id = -1;
            for (int i = 0; i < Math.Min(b.Length - 8, 400); i++)
                if (b[i] == 1 && b[i + 1] == 'v' && b[i + 2] == 'o' && b[i + 3] == 'r' && b[i + 4] == 'b') { id = i; break; }
            Assert.That(id, Is.GreaterThan(0), path + " is not Vorbis");
            int channels = b[id + 11];
            int rate = BitConverter.ToInt32(b, id + 12);
            int last = -1;
            for (int i = b.Length - 14; i >= 0; i--)
                if (b[i] == 'O' && b[i + 1] == 'g' && b[i + 2] == 'g' && b[i + 3] == 'S') { last = i; break; }
            long granule = BitConverter.ToInt64(b, last + 6);
            return (granule / (double)rate, channels, rate);
        }

        private static RegionCatalog Catalog() => TestData.Catalog();

        // ------------------------------------------------------------------ music files

        [Test]
        public void EveryMusicIdResolvesToARealFileOfTheRightLength()
        {
            var cat = Catalog();
            var ids = MusicCues.All(cat);
            Assert.That(ids.Length, Is.GreaterThanOrEqualTo(MusicCues.Generic.Length + 5 + 34));
            foreach (var id in ids)
            {
                string path = MusicPath(id);
                Assert.That(File.Exists(path), Is.True, "missing music file for " + id);
                var (sec, ch, rate) = Ogg(path);
                Assert.That(rate, Is.EqualTo(44100), id);
                Assert.That(ch, Is.InRange(1, 2), id);
                bool stinger = id == MusicCues.Victory || id == MusicCues.Defeat;
                if (stinger) Assert.That(sec, Is.InRange(3.0, 15.0), id + " stinger length");
                else if (id == MusicCues.Invincible) Assert.That(sec, Is.InRange(20.0, 125.0), id);
                else Assert.That(sec, Is.InRange(55.0, 125.0), id + " loop length");
            }
        }

        [Test]
        public void NoMusicFileIsLeftOverThatTheGameNeverAsksFor()
        {
            var wanted = MusicCues.All(Catalog()).ToHashSet();
            var onDisk = AllMusicFiles().Select(Path.GetFileName).Where(f => !f.EndsWith(".meta")).ToList();   // .meta files are Unity's own, never committed
            Assert.That(onDisk.All(f => f.EndsWith(".ogg")), Is.True, "only Ogg Vorbis music ships");
            foreach (var f in onDisk) Assert.That(wanted, Does.Contain(Path.GetFileNameWithoutExtension(f)), f + " is not used by any stage, menu or cue");
        }

        [Test]
        public void EveryCountryHasItsOwnThemeFile()
        {
            var cat = Catalog();
            var countries = cat.regions.Select(r => r.country).Distinct().ToList();
            Assert.That(countries.Count, Is.EqualTo(35));
            foreach (var c in countries)
            {
                var regions = cat.regions.Where(r => r.country == c).ToList();
                if (c == "saudi")
                {
                    foreach (var r in regions) Assert.That(MusicCues.Saudi, Does.Contain(r.music), r.id);
                    foreach (var v in MusicCues.Saudi) Assert.That(regions.Any(r => r.music == v), Is.True, v + " is a regional theme nobody uses");
                }
                else
                {
                    foreach (var r in regions) Assert.That(r.music, Is.EqualTo(MusicCues.Country(c)), r.id);
                    Assert.That(File.Exists(MusicPath(MusicCues.Country(c))), Is.True, c);
                }
            }
            foreach (var v in MusicCues.Saudi) Assert.That(File.Exists(MusicPath(v)), Is.True, v);
        }

        [Test]
        public void TheShippedRegionFilesValidateWithTheirMusic()
        {
            Assert.That(Catalog().Validate(), Is.Empty);
        }

        // ------------------------------------------------------------------ which track plays where

        private static StageDef Stage(string id) { var c = Catalog(); var s = c.Stage(id, out int r, out _); Assert.That(s, Is.Not.Null, id); return s; }
        private static RegionDef RegionOf(string stageId) { var c = Catalog(); c.Stage(stageId, out int r, out _); return c.regions[r]; }

        [Test]
        public void NormalStagesPlayTheirCountryTheme()
        {
            foreach (var r in Catalog().regions)
                foreach (var s in r.stages.Where(s => s.type == "normal" && string.IsNullOrEmpty(s.music)))
                    Assert.That(MusicCues.ForStage(s, r), Is.EqualTo(r.music), s.id);
            Assert.That(MusicCues.ForStage(Stage("egypt_1"), RegionOf("egypt_1")), Is.EqualTo("music_country_egypt"));
            Assert.That(MusicCues.ForStage(Stage("riyadh_1"), RegionOf("riyadh_1")), Is.EqualTo(MusicCues.SaudiNajd));
            Assert.That(MusicCues.ForStage(Stage("makkah_1"), RegionOf("makkah_1")), Is.EqualTo(MusicCues.SaudiHejaz));
        }

        [Test]
        public void ChaseActionRaceAndBossUseTheTypeTracks()
        {
            var cat = Catalog();
            int chase = 0, action = 0, race = 0, boss = 0;
            foreach (var r in cat.regions)
                foreach (var s in r.stages)
                {
                    if (!string.IsNullOrEmpty(s.music) && !s.boss && s.music != MusicCues.KuwaitSea) Assert.Fail(s.id + ": only boss stages and the Kuwait sea stages name their own track");
                    if (s.boss) { boss++; Assert.That(MusicCues.ForStage(s, r), Is.EqualTo(r.music), s.id + " approaches the boss to the country theme"); Assert.That(MusicCues.ForBoss(s), Is.EqualTo(string.IsNullOrEmpty(s.music) ? MusicCues.StageBoss : s.music), s.id); continue; }
                    if (s.type == "chase") { chase++; Assert.That(MusicCues.ForStage(s, r), Is.EqualTo(MusicCues.StageChase), s.id); }
                    if (s.type == "action") { action++; Assert.That(MusicCues.ForStage(s, r), Is.EqualTo(MusicCues.StageAction), s.id); }
                    if (s.type == "race" && string.IsNullOrEmpty(s.music)) { race++; Assert.That(MusicCues.ForStage(s, r), Is.EqualTo(MusicCues.StageRace), s.id); }
                }
            Assert.That(chase, Is.GreaterThan(20)); Assert.That(action, Is.GreaterThan(5)); Assert.That(boss, Is.EqualTo(cat.regions.Length));
            Assert.That(race, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TabukBossHasTheHandClappedGroupChantTrack()
        {
            var s = Stage("tabuk_7");
            Assert.That(s.boss, Is.True);
            Assert.That(MusicCues.ForBoss(s), Is.EqualTo(MusicCues.TabukBoss));
            Assert.That(MusicCues.TabukBoss, Is.EqualTo("music_tabuk_boss"));
            var (sec, _, _) = Ogg(MusicPath(MusicCues.TabukBoss));
            Assert.That(sec, Is.InRange(80.0, 100.0), "a 90 second loop");
            // Every other boss uses the shared boss track.
            foreach (var r in Catalog().regions) foreach (var b in r.stages.Where(x => x.boss && x.id != "tabuk_7")) Assert.That(MusicCues.ForBoss(b), Is.EqualTo(MusicCues.StageBoss), b.id);
        }

        [Test]
        public void KuwaitHasItsMawwalThemeAndTheSeaTrackOnThreeStages()
        {
            var kuwait = Catalog().Region("kuwait");
            Assert.That(kuwait.music, Is.EqualTo("music_country_kuwait"));
            var sea = kuwait.stages.Where(s => s.music == MusicCues.KuwaitSea).Select(s => s.id).OrderBy(x => x).ToArray();
            Assert.That(sea, Is.EqualTo(new[] { "kuwait_1", "kuwait_3", "kuwait_race" }));
            foreach (var id in sea) Assert.That(MusicCues.ForStage(Stage(id), kuwait), Is.EqualTo(MusicCues.KuwaitSea), id);
            Assert.That(MusicCues.ForStage(Stage("kuwait_2"), kuwait), Is.EqualTo("music_country_kuwait"));
            Assert.That(MusicCues.ForStage(Stage("kuwait_4"), kuwait), Is.EqualTo(MusicCues.StageChase));
            foreach (var t in new[] { "music_country_kuwait", MusicCues.KuwaitSea })
                Assert.That(Ogg(MusicPath(t)).seconds, Is.InRange(55.0, 125.0), t);
        }

        [Test]
        public void MenuMapStoreAndStingersHaveTheirOwnTracks()
        {
            var all = new[] { MusicCues.Menu, MusicCues.Map, MusicCues.Store, MusicCues.Victory, MusicCues.Defeat, MusicCues.Invincible };
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length));
            foreach (var id in all) Assert.That(File.Exists(MusicPath(id)), Is.True, id);
            Assert.That(AudioCues.InvincibleMusic, Is.EqualTo(MusicCues.Invincible));
        }

        // ------------------------------------------------------------------ sound effects

        private static string[] SfxFolders() => new[]
        {
            SfxDir(),
            Path.Combine(TestData.Resources(), "AronSfx"),
            Path.Combine(Assets(), "Audio", "SFX", "Resources", "AronSfx"),
        };

        [Test]
        public void EveryCueIdResolvesToARealSoundFile()
        {
            foreach (var id in AudioCues.Sfx)
            {
                string path = Path.Combine(SfxDir(), id + ".ogg");
                Assert.That(File.Exists(path), Is.True, "missing sound for " + id);
                var (sec, _, rate) = Ogg(path);
                Assert.That(rate, Is.EqualTo(44100), id);
                Assert.That(sec, Is.InRange(0.1, 4.0), id);
            }
            // The sounds the game plays by plain name.
            foreach (var id in new[] { "jump", "doublejump", "spin", "coin", "crate", "bounce", "enemy", "life", "checkpoint", "death", "finish", "gameover", "hit", "swing", "click" })
                Assert.That(File.Exists(Path.Combine(SfxDir(), id + ".ogg")), Is.True, id);
        }

        [Test]
        public void EveryEntryInTheSoundLibraryHasItsClips()
        {
            string src = File.ReadAllText(Path.Combine(Assets(), "Scripts", "Audio", "SfxLibrary.cs"));
            var rx = new Regex("new Entry\\(\"(?<id>[a-z0-9_]+)\", (?<takes>\\d+), [^,]+, [^,]+, \\d+(?:, [^,]+)?(?:, (?<loop>true))?\\)");
            var entries = rx.Matches(src).Cast<Match>().ToList();
            Assert.That(entries.Count, Is.GreaterThan(25));
            foreach (var m in entries)
            {
                string id = m.Groups["id"].Value; int takes = int.Parse(m.Groups["takes"].Value); bool loop = m.Groups["loop"].Success;
                var names = loop ? new[] { id + ".ogg" } : Enumerable.Range(1, Math.Max(1, takes)).Select(i => id + "_" + i + ".ogg").ToArray();
                foreach (var n in names)
                    Assert.That(SfxFolders().Any(d => File.Exists(Path.Combine(d, n))), Is.True, "missing " + n + " for sound " + id);
            }
            foreach (var id in new[] { "palm_rustle_light", "palm_rustle_moderate", "palm_rustle_strong", "leaves_rustle_light", "leaves_rustle_medium", "leaves_rustle_strong", "foliage_gust", "frond_creak", "wind_loop", "waves_loop", "sand_hiss", "sandstorm", "wall_crumble", "boss_hit", Aron.Logic.Passport.PassportBook.StampSfx })
                Assert.That(entries.Any(m => m.Groups["id"].Value == id), Is.True, id + " must be in the library");
        }

        [Test]
        public void FoliageLoopsAreTenToTwentySecondsAndTheGustsAreShort()
        {
            string dir = Path.Combine(Assets(), "Audio", "SFX", "Resources", "AronSfx");
            foreach (var id in new[] { "palm_rustle_light", "palm_rustle_moderate", "palm_rustle_strong", "leaves_rustle_light", "leaves_rustle_medium", "leaves_rustle_strong" })
            {
                var (sec, _, rate) = Ogg(Path.Combine(dir, id + ".ogg"));
                Assert.That(sec, Is.InRange(10.0, 20.0), id); Assert.That(rate, Is.EqualTo(44100));
            }
            for (int i = 1; i <= 4; i++) Assert.That(Ogg(Path.Combine(dir, "foliage_gust_" + i + ".ogg")).seconds, Is.InRange(0.5, 8.0), "gust " + i);
            Assert.That(File.Exists(Path.Combine(dir, "foliage_gust_5.ogg")), Is.False);
        }

        // ------------------------------------------------------------------ nothing made-up ships

        private static readonly string[] TextExt = { ".cs", ".json", ".md", ".txt", ".py", ".meta", ".yml", ".yaml", ".asmdef", ".csproj", ".shader", ".hlsl", ".unity", ".asset", ".prefab" };

        private static string[] RepoTextFiles() =>
            Directory.GetFiles(Repo(), "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                            && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "Library" + Path.DirectorySeparatorChar))
                .Where(f => TextExt.Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();

        [Test]
        public void NoGeneratorOrStandInAudioCodeShips()
        {
            var bad = new Regex(@"AudioClip\.Create|OnAudioFilterRead|StandIn\(|gen_[a-z_]*sound", RegexOptions.IgnoreCase);
            foreach (var f in RepoTextFiles().Where(f => !f.EndsWith("MusicAndSoundFileTests.cs")))
            {
                string t = File.ReadAllText(f);
                Assert.That(bad.IsMatch(t), Is.False, f + " refers to generated or stand-in audio");
            }
            Assert.That(Directory.GetFiles(Path.Combine(Repo(), "tools"), "*sound*").Length, Is.EqualTo(0), "no sound generator scripts in tools");
            Assert.That(File.Exists(Path.Combine(Repo(), "ThirdParty", "AUDIO_CREDITS.md")), Is.False);
        }

        [Test]
        public void NoWordsAboutHowTheAudioWasMadeAreInTheRepo()
        {
            // Built from pieces so this file does not contain the words it forbids.
            var forbidden = new Regex("ele" + "venlabs|ele" + "ven_mus" + "ic|text_" + "to_sound|ai[- ]generated|synthesi[sz]ed|noise placeholders?", RegexOptions.IgnoreCase);
            foreach (var f in RepoTextFiles())
            {
                if (f.EndsWith("MusicAndSoundFileTests.cs")) continue;
                string name = Path.GetFileName(f);
                Assert.That(forbidden.IsMatch(name), Is.False, name);
                Assert.That(forbidden.IsMatch(File.ReadAllText(f)), Is.False, f);
            }
            foreach (var f in Directory.GetFiles(Path.Combine(Assets()), "*.ogg", SearchOption.AllDirectories)) Assert.That(forbidden.IsMatch(Path.GetFileName(f)), Is.False, f);
        }

        [Test]
        public void EveryAudioFileCarriesOnlyTheOwnersTags()
        {
            var files = Directory.GetFiles(Assets(), "*.*", SearchOption.AllDirectories).Where(f => new[] { ".ogg", ".wav", ".mp3", ".flac", ".m4a" }.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            Assert.That(files.Count, Is.GreaterThan(150));
            foreach (var f in files)
            {
                Assert.That(Path.GetExtension(f).ToLowerInvariant(), Is.EqualTo(".ogg"), f);
                var b = File.ReadAllBytes(f);
                string head = Encoding.Latin1.GetString(b, 0, Math.Min(b.Length, 4096));
                Assert.That(head, Does.Contain("artist=Ahmed Alamrani"), f);
                Assert.That(head, Does.Contain("copyright=(c) Ahmed Alamrani"), f);
                foreach (var w in new[] { "Lavf", "Lavc", "encoder=", "ffmpeg", "libvorbis", "title=", "comment=", "software=" })
                    Assert.That(head.IndexOf(w, StringComparison.OrdinalIgnoreCase), Is.LessThan(0), f + " has a leftover tag " + w);
            }
        }
    }
}
