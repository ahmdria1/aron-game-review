// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Localization;
using Aron.Logic;
using Aron.UI;
using Aron.Logic.Sky;
using Aron.Player;
using Aron.Save;
using UnityEngine;

namespace Aron.Atmosphere
{
    /// <summary>
    /// The gameplay half of weather: a sandstorm's gusts lean on the hero, and sun on a hot day wears him down a little until he finds
    /// shade (anything solid overhead). Numbers come from Resources/Config/weather_gameplay.json. Races, time trials and bosses are untouched.
    /// Rain's grip is in <see cref="WeatherDirector"/> and the authored rain stage; its puddles are in the stage's RainWeather.
    /// </summary>
    public class WeatherGameplayRuntime : MonoBehaviour
    {
        private static WeatherGameplayFile file;
        public static WeatherGameplayFile File
        {
            get
            {
                if (file != null) return file;
                var t = Resources.Load<TextAsset>("Config/weather_gameplay");
                try { file = t != null ? JsonUtility.FromJson<WeatherGameplayFile>(t.text) : null; } catch (System.Exception) { file = null; }
                if (file == null || file.Validate().Count > 0) file = new WeatherGameplayFile();
                return file;
            }
        }

        /// <summary>Speed share from puddles, set by the stage's rain component each frame; 1 outside puddles.</summary>
        public static float PuddleSpeed = 1f;

        private WeatherDirector director;
        private HeatStress heat;
        private string climate = "";
        private bool hinted;

        public void Init(WeatherDirector d, string climateId) { director = d; climate = climateId ?? ""; }

        private void Awake() { heat = new HeatStress(File); PuddleSpeed = 1f; }

        private void Update()
        {
            var p = PlayerController.Active;
            if (p == null || director == null || Time.timeScale <= 0f) return;
            if (!WeatherGameplay.Applies(director.Mode)) { Clear(p); return; }
            // Free play only feels weather when the player turned "weather effects" on (the menu option); stages always do.
            if (director.Mode == Logic.Sky.PlayMode.FreePlay)
            {
                var w = Save.SaveSystem.Data.weather;
                if (w == null || !w.freePlayEffects) { Clear(p); return; }
            }
            var f = File;
            var look = director.Look;
            bool reduce = SaveSystem.Data != null && Aron.Logic.Access.FlashSafety.MotionReduced(SaveSystem.Data.access);
            float push = WeatherGameplay.WindPush(f, look, Time.time, p.IsRiding, reduce);
            p.WindPush = new Vector3(push, 0f, push * 0.25f);

            float strength = WeatherGameplay.HeatStrength(f, climate, look, WeatherGameplay.Daylight(director.Phase));
            bool shade = Physics.Raycast(p.transform.position + Vector3.up * 1.2f, Vector3.up, 8f, ~0, QueryTriggerInteraction.Ignore);
            heat.Tick(Time.deltaTime, strength, shade, false);
            p.WeatherSpeed = heat.SpeedScale * PuddleSpeed;
            if (heat.ShowHint && !hinted) { hinted = true; GameUI.Banner(Strings.Get("heat_hint"), 2.5f); }
            if (!heat.ShowHint && heat.Level < 0.05f) hinted = false;
        }

        private void Clear(PlayerController p) { p.WindPush = Vector3.zero; p.WeatherSpeed = 1f; }

        private void OnDestroy()
        {
            var p = PlayerController.Active;
            if (p != null) Clear(p);
            PuddleSpeed = 1f;
        }
    }
}
