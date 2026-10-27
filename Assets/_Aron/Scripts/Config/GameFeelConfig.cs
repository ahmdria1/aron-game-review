// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using Aron.Logic.GameFeel;
using UnityEngine;

namespace Aron.Config
{
    /// <summary>
    /// Every game-feel number in one asset (run, air, jump, corners, landing, spin, melee, bounce, camera, input, procedural animation).
    /// The values live in <see cref="FeelValues"/>, which is plain C# so the same code is unit-tested outside Unity. Create one with
    /// Assets > Create > Aron > Game Feel and put it at Resources/Config/GameFeelConfig to override the shipped JSON; without it the game
    /// reads Resources/Config/game_feel.json.
    /// </summary>
    [CreateAssetMenu(menuName = "Aron/Game Feel", fileName = "GameFeelConfig")]
    public class GameFeelConfig : ScriptableObject
    {
        public FeelValues values = new FeelValues();

        public string ToJson() => JsonUtility.ToJson(values, true);

        public bool FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try { JsonUtility.FromJsonOverwrite(json, values); values.Clamp(); return true; }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("game feel json: " + e.Message); return false; }
        }
    }

    /// <summary>
    /// The live values. Load order: the Resources asset when there is one, else Resources/Config/game_feel.json, else the code defaults;
    /// then the tuning override written by the hidden tuning panel (persistentDataPath), if any. Every system reads <see cref="V"/> each frame,
    /// so a slider in the panel takes effect at once.
    /// </summary>
    public static class FeelSettings
    {
        public const string AssetPath = "Config/GameFeelConfig";
        public const string JsonPath = "Config/game_feel";
        public const string OverrideFile = "game_feel_tuning.json";
        public const string ExportFile = "game_feel_export.json";

        private static GameFeelConfig live, shipped;

        /// <summary>Raised after any value changed (slider, load, reset).</summary>
        public static event Action Changed;
        /// <summary>Bumps on every change, for caches (the solved jump take-off speed).</summary>
        public static int Version { get; private set; }

        public static FeelValues V { get { EnsureLoaded(); return live.values; } }
        public static FeelValues Shipped { get { EnsureLoaded(); return shipped.values; } }
        public static string OverridePath => Path.Combine(Application.persistentDataPath, OverrideFile);
        public static string ExportPath => Path.Combine(Application.persistentDataPath, ExportFile);
        public static bool HasSavedOverride => File.Exists(OverridePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { live = shipped = null; Changed = null; Version = 0; }

        private static void EnsureLoaded()
        {
            if (live != null) return;
            shipped = ScriptableObject.CreateInstance<GameFeelConfig>();
            var asset = Resources.Load<GameFeelConfig>(AssetPath);
            if (asset != null) shipped.values.CopyFrom(asset.values);
            else
            {
                var t = Resources.Load<TextAsset>(JsonPath);
                if (t != null) shipped.FromJson(t.text);
            }
            LogProblems(shipped.values, "shipped");
            live = ScriptableObject.CreateInstance<GameFeelConfig>();
            live.values.CopyFrom(shipped.values);
            if (!Application.isEditor || Application.isPlaying) TryLoadOverride();
        }

        private static void TryLoadOverride()
        {
            try
            {
                if (!File.Exists(OverridePath)) return;
                if (live.FromJson(File.ReadAllText(OverridePath))) LogProblems(live.values, "saved tuning");
            }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("game feel tuning: " + e.Message); }
        }

        private static void LogProblems(FeelValues v, string what)
        {
            var problems = v.Validate();
            if (problems.Count > 0) global::Aron.Logic.Diagnostics.Log.Warn("game feel (" + what + "): " + string.Join("; ", problems));
        }

        public static void Touch() { Version++; Changed?.Invoke(); }

        public static void Set(string field, float value)
        {
            foreach (var f in FeelValues.Fields)
                if (f.Name == field) { V.Set(f, value); Touch(); return; }
        }

        /// <summary>Writes the current values as the saved tuning, loaded again on the next start.</summary>
        public static bool Save()
        {
            try { File.WriteAllText(OverridePath, live.ToJson()); return true; }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("game feel save: " + e.Message); return false; }
        }

        /// <summary>Back to the shipped values; the saved tuning file is deleted.</summary>
        public static void ResetToShipped()
        {
            EnsureLoaded();
            live.values.CopyFrom(shipped.values);
            try { if (File.Exists(OverridePath)) File.Delete(OverridePath); } catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("game feel reset: " + e.Message); }
            Touch();
        }

        /// <summary>
        /// Writes the JSON to <see cref="ExportPath"/> and copies it to the clipboard, ready to paste over Resources/Config/game_feel.json in the repo.
        /// Returns the path, or null when the file could not be written (the clipboard copy still happens).
        /// </summary>
        public static string Export()
        {
            EnsureLoaded();
            string json = live.ToJson();
            GUIUtility.systemCopyBuffer = json;
            try { File.WriteAllText(ExportPath, json); return ExportPath; }
            catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("game feel export: " + e.Message); return null; }
        }
    }
}
