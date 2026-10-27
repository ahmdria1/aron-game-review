// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Audio
{
    public enum Weather { Clear, Rain, HeavyRain, Sandstorm }

    /// <summary>Ambience layers that the weather loops blend between.</summary>
    public enum WeatherLayer { RainLight, RainHeavy, RainRoof, Sandstorm, SandHiss }

    /// <summary>Gain for each ambience layer, eased toward the current weather over a fixed fade time.</summary>
    public class WeatherMix
    {
        public static readonly int LayerCount = Enum.GetValues(typeof(WeatherLayer)).Length;

        private readonly float[] gains = new float[LayerCount];
        private readonly float[] targets = new float[LayerCount];
        private float fadeSeconds;

        public Weather Current { get; private set; } = Weather.Clear;
        public bool UnderCover { get; private set; }

        public WeatherMix(float fadeSeconds) { this.fadeSeconds = Math.Max(0.01f, fadeSeconds); }

        public float Gain(WeatherLayer layer) => gains[(int)layer];
        public bool Silent => Array.TrueForAll(gains, g => g <= 0f);

        public void Set(Weather weather, bool underCover, float fade)
        {
            Current = weather;
            UnderCover = underCover;
            fadeSeconds = Math.Max(0.01f, fade);
            Targets(weather, underCover, targets);
        }

        public void Snap()
        {
            Array.Copy(targets, gains, LayerCount);
        }

        public void Step(float dt)
        {
            float step = dt / fadeSeconds;
            for (int i = 0; i < LayerCount; i++)
            {
                float d = targets[i] - gains[i];
                gains[i] = Math.Abs(d) <= step ? targets[i] : gains[i] + Math.Sign(d) * step;
            }
        }

        public static void Targets(Weather weather, bool underCover, float[] into)
        {
            Array.Clear(into, 0, into.Length);
            switch (weather)
            {
                case Weather.Rain:
                    into[(int)WeatherLayer.RainLight] = underCover ? 0.4f : 1f;
                    break;
                case Weather.HeavyRain:
                    into[(int)WeatherLayer.RainLight] = 0.35f;
                    into[(int)WeatherLayer.RainHeavy] = underCover ? 0.45f : 1f;
                    break;
                case Weather.Sandstorm:
                    into[(int)WeatherLayer.Sandstorm] = 1f;
                    into[(int)WeatherLayer.SandHiss] = underCover ? 0.2f : 0.7f;
                    break;
            }
            if (underCover && (weather == Weather.Rain || weather == Weather.HeavyRain))
                into[(int)WeatherLayer.RainRoof] = weather == Weather.HeavyRain ? 1f : 0.75f;
        }
    }
}
