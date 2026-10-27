// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Atmosphere
{
    /// <summary>Owns the Aron/Sky skybox material. Falls back to a plain camera colour when the shader is missing.</summary>
    public class SkyDome
    {
        private static readonly int Zenith = Shader.PropertyToID("_ZenithColor"), Horizon = Shader.PropertyToID("_HorizonColor"),
            Ground = Shader.PropertyToID("_GroundColor"), SunDir = Shader.PropertyToID("_SunDir"), SunColor = Shader.PropertyToID("_SunColor"),
            MoonDir = Shader.PropertyToID("_MoonDir"), MoonPhase = Shader.PropertyToID("_MoonPhase"), Stars = Shader.PropertyToID("_Stars"),
            MilkyWay = Shader.PropertyToID("_MilkyWay"), StarSpin = Shader.PropertyToID("_StarSpin"), Cloud = Shader.PropertyToID("_CloudCover"),
            CloudColor = Shader.PropertyToID("_CloudColor"), CloudShade = Shader.PropertyToID("_CloudShade"), CloudSpeed = Shader.PropertyToID("_CloudSpeed"),
            Haze = Shader.PropertyToID("_Haze"), HazeColor = Shader.PropertyToID("_HazeColor"), Flash = Shader.PropertyToID("_Flash"),
            CloudLayer = Shader.PropertyToID("_CloudLayer");

        public Material Material { get; private set; }
        public bool Live => Material != null;

        public static SkyDome Create()
        {
            var d = new SkyDome();
            var shader = Shader.Find("Aron/Sky");
            if (shader != null)
            {
                d.Material = new Material(shader) { name = "AronSky" };
                RenderSettings.skybox = d.Material;
            }
            return d;
        }

        public struct Frame
        {
            public Color Zenith, Horizon, Ground, Sun, CloudLit, CloudShade, HazeColor;
            public Vector3 SunDir, MoonDir;
            public float MoonLit, MoonLimb, MoonVisible, Stars, MilkyWay, StarSpin, Cloud, Haze, Flash;
            public Vector2 CloudDrift;
            public bool CloudLayer;
        }

        public void Apply(in Frame f, Camera cam)
        {
            if (Material == null)
            {
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.Lerp(f.Horizon, f.CloudShade, f.Cloud * 0.6f); }
                return;
            }
            if (cam != null && cam.clearFlags != CameraClearFlags.Skybox) cam.clearFlags = CameraClearFlags.Skybox;
            var m = Material;
            m.SetColor(Zenith, f.Zenith);
            m.SetColor(Horizon, f.Horizon);
            m.SetColor(Ground, f.Ground);
            m.SetVector(SunDir, f.SunDir);
            m.SetColor(SunColor, f.Sun);
            m.SetVector(MoonDir, f.MoonDir);
            m.SetVector(MoonPhase, new Vector4(f.MoonLit, f.MoonLimb, 0.035f, f.MoonVisible));
            m.SetFloat(Stars, f.Stars);
            m.SetFloat(MilkyWay, f.MilkyWay);
            m.SetFloat(StarSpin, f.StarSpin);
            m.SetFloat(Cloud, f.Cloud);
            m.SetColor(CloudColor, f.CloudLit);
            m.SetColor(CloudShade, f.CloudShade);
            m.SetVector(CloudSpeed, new Vector4(f.CloudDrift.x, f.CloudDrift.y, 0f, 0f));
            m.SetFloat(CloudLayer, f.CloudLayer ? 1f : 0f);
            m.SetFloat(Haze, f.Haze);
            m.SetColor(HazeColor, f.HazeColor);
            m.SetFloat(Flash, f.Flash);
        }

        /// <summary>Only the flash changes every frame; everything else goes through <see cref="Apply"/>.</summary>
        public void SetFlash(float flash) { if (Material != null) Material.SetFloat(Flash, flash); }

        public void Dispose()
        {
            if (Material == null) return;
            if (RenderSettings.skybox == Material) RenderSettings.skybox = null;
            Object.Destroy(Material);
            Material = null;
        }
    }
}
