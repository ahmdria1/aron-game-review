// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Audio;
using Aron.Characters;
using Aron.Config;
using Aron.Core;
using Aron.Localization;
using Aron.Logic;
using Aron.Naming;
using Aron.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>Arabic main menu: continue, region map, character choice (Aron / heroine), music toggle, quit.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CharacterRoster roster;
        [SerializeField] private string firstLevelScene = SceneNames.Riyadh;
        private Button characterButton, musicButton, weatherButton, flashButton, effectsButton, rivalButton, aiButton;

        private void Start()
        {
            Time.timeScale = 1f;
            if (roster == null) roster = CharacterRoster.Load();
            Strings.Language = SaveSystem.Data.language;
            AudioManager.PlayMusic(MusicCues.Menu);

            var root = UIFactory.CreateCanvas("MenuCanvas", 0);
            root.transform.SetParent(transform, false);
            var side = UIFactory.FadePanel(root.transform, "Bg", new Color(0.18f, 0.1f, 0.05f, 0.5f));
            UIFactory.Anchor(side, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(1250f, 4000f));
            var g = UIFactory.VerticalGroup(UIFactory.SafeRoot(root), "Group", 26f);
            UIFactory.Anchor(g, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(900f, 1000f));
            UIFactory.Label(g, "Title", Strings.Get("title"), 170f, Color.white);
            UIFactory.Label(g, "Subtitle", Strings.Get("subtitle"), 52f, UIFactory.Sand);
            var next = Logic.Regions.StageProgress.Frontier(Stages.StageSession.Catalog, SaveSystem.Data);
            if (next != null)
                UIFactory.CreateButton(g, "Play", Strings.Format("continue_stage", next.nameAr), () => Stages.StageSession.Play(next.id));
            else
                UIFactory.CreateButton(g, "Play", Strings.Get("play"), () => Aron.Core.SceneLoader.Load(firstLevelScene));
            UIFactory.CreateButton(g, "Map", Strings.Get("map"), () => WorldMapScreen.Open(transform));
            characterButton = UIFactory.CreateButton(g, "Character", "", CycleCharacter);
            musicButton = UIFactory.CreateButton(g, "Music", "", ToggleMusic);
            // Weather and flashing options sit in their own column, bottom left, so the main list keeps its height.
            var opts = UIFactory.VerticalGroup(UIFactory.SafeRoot(root), "Options", 14f);
            UIFactory.Anchor(opts, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(600f, 790f));
            weatherButton = UIFactory.CreateButton(opts, "Weather", "", () => Toggle(d => d.weather.dynamicWeather = !d.weather.dynamicWeather));
            effectsButton = UIFactory.CreateButton(opts, "WeatherEffects", "", () => Toggle(d => d.weather.freePlayEffects = !d.weather.freePlayEffects));
            flashButton = UIFactory.CreateButton(opts, "ReduceFlash", "", () => Toggle(d => d.access.reduceFlash = !d.access.reduceFlash));
            rivalButton = UIFactory.CreateButton(opts, "RivalChatter", "", () => Toggle(d => d.rivalChatter = d.rivalChatter == 0 ? 1 : 0));
            aiButton = UIFactory.CreateButton(opts, "AiDifficulty", "", () => Toggle(d => d.aiDifficulty = d.aiDifficulty == "easy" ? "normal" : d.aiDifficulty == "normal" ? "hard" : "easy"));
            UIFactory.CreateButton(opts, "Museum", Strings.Get("museum"), () => MuseumScreen.Open(transform));
            UIFactory.CreateButton(opts, "Settings", Strings.Get("settings"), () => SettingsScreen.Open(transform));
            // Apple rejects apps with their own quit button; Android players expect one.
            if (Application.platform == RuntimePlatform.Android)
                UIFactory.CreateButton(g, "Quit", Strings.Get("quit"), Application.Quit);
            // Five quick taps on the version label open the hidden performance overlay (Diagnostics/PerfOverlay).
            var ver = UIFactory.SafeRoot(root);
            Diagnostics.PerfOverlay.AttachVersionLabel(ver);
            UIFactory.Anchor((RectTransform)ver.Find("Version"), new Vector2(1f, 0f), new Vector2(-20f, 12f), new Vector2(260f, 70f));
            // First launch (or a name taken away after reports): choose a name before anything else.
            if (!PlayerNames.HasName || SaveSystem.Data.nameMustChange) NameScreen.Open(transform, Refresh);
            Refresh();
            StartCoroutine(IdleMoods());
            if (Stages.StageSession.OpenMap) WorldMapScreen.Open(transform);
            Stages.StageSession.OpenMap = false;
            Stages.StageSession.Clear();
        }

        private IEnumerator IdleMoods()
        {
            var moods = new[] { Emotion.Joy, Emotion.Admiration, Emotion.Joy, Emotion.Worry };
            int i = 0;
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(4f, 7f));
                if (roster == null || roster.characters == null || roster.characters.Length == 0) continue;
                var who = CharacterEmotion.Find(roster.characters[i % roster.characters.Length].characterId);
                if (who != null) who.Feel(moods[i % moods.Length], 2f);
                i++;
            }
        }

        private void CycleCharacter()
        {
            if (roster == null || roster.characters == null || roster.characters.Length == 0) return;
            var d = SaveSystem.Data;
            int idx = 0;
            for (int i = 0; i < roster.characters.Length; i++) if (roster.characters[i].characterId == d.selectedCharacterId) idx = i;
            d.selectedCharacterId = roster.characters[(idx + 1) % roster.characters.Length].characterId;
            SaveSystem.Save();
            Refresh();
            var chosen = CharacterEmotion.Find(d.selectedCharacterId);
            if (chosen != null) chosen.Feel(Emotion.Joy, 2.5f, true);
        }

        private void ToggleMusic()
        {
            var d = SaveSystem.Data;
            d.musicEnabled = !d.musicEnabled;
            SaveSystem.Save();
            AudioManager.Instance.ApplyVolumes();
            Refresh();
        }

        private void Toggle(System.Action<Logic.SaveData> change)
        {
            var d = SaveSystem.Data;
            if (d.weather == null) d.weather = new Logic.Sky.WeatherSettings();
            if (d.access == null) d.access = new Logic.Safety.AccessSettings();
            change(d);
            SaveSystem.Save();
            Refresh();
        }

        private static string OnOff(bool on) => Strings.Get(on ? "on" : "off");

        private void Refresh()
        {
            var c = roster != null ? roster.Get(SaveSystem.Data.selectedCharacterId) : null;
            UIFactory.SetButtonText(characterButton, Strings.Format("character", c != null ? c.displayNameAr : "أرون"));
            var data = SaveSystem.Data;
            UIFactory.SetButtonText(musicButton, Strings.Format("music", OnOff(data.musicEnabled)));
            UIFactory.SetButtonText(weatherButton, Strings.Format("weather_live", OnOff(data.weather == null || data.weather.dynamicWeather)));
            UIFactory.SetButtonText(effectsButton, Strings.Format("weather_effects", OnOff(data.weather != null && data.weather.freePlayEffects)));
            UIFactory.SetButtonText(rivalButton, Strings.Format("rival_chatter", OnOff(data.rivalChatter != 0)));
            string diff = data.aiDifficulty == "easy" || data.aiDifficulty == "hard" ? data.aiDifficulty : "normal";
            UIFactory.SetButtonText(aiButton, Strings.Format("ai_difficulty", Strings.Get("ai_" + diff)));
            UIFactory.SetButtonText(flashButton, Strings.Format("reduce_flash", OnOff(data.access != null && data.access.reduceFlash)));
        }
    }
}
