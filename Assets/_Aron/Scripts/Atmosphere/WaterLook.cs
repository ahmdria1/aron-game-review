// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Sky;
using UnityEngine;

namespace Aron.Atmosphere
{
    /// <summary>Makes Aron/Water materials from the profiles in weather.json and keeps them in step with the weather.</summary>
    public static class WaterLook
    {
        private static readonly int WindId = Shader.PropertyToID("_Wind"), RippleId = Shader.PropertyToID("_RainRipples");

        /// <summary>Null when the shader is not in the build; stages then keep their flat coloured water.</summary>
        public static Material Make(WaterProfile p, QualityTierSettings q, bool still)
        {
            var shader = Shader.Find("Aron/Water");
            if (shader == null) return null;
            if (p == null) p = new WaterProfile { id = "default" };
            var m = new Material(shader) { name = "Water_" + p.id + (still ? "_still" : "") };
            m.SetColor("_ShallowColor", Hex(p.shallow, new Color(0.25f, 0.65f, 0.7f)));
            m.SetColor("_DeepColor", Hex(p.deep, new Color(0.04f, 0.24f, 0.36f)));
            m.SetColor("_FoamColor", Hex(p.foam, Color.white));
            float height = still ? p.waveHeight * 0.12f : p.waveHeight;
            m.SetFloat("_WaveHeight", height);
            m.SetFloat("_WaveLength", still ? Mathf.Max(2f, p.waveLength * 0.3f) : p.waveLength);
            m.SetFloat("_Choppy", p.kind == "sea" && !still ? 0.45f : 0.1f);
            // Rivers run along the stage (+z); seas just swell.
            m.SetVector("_Flow", new Vector4(0f, 0f, still ? 0f : -p.flow, 0f));
            m.SetFloat("_Murk", 1f - Mathf.Clamp01(p.clarity));
            m.SetFloat("_Foam", still ? 0.15f : p.kind == "sea" ? 0.55f : 0.3f);
            m.SetFloat("_EdgeFoam", p.kind == "marsh" ? 0.02f : 0.08f);
            m.SetFloat("_Fade", q.Reflections ? 0.7f : 0.35f);
            if (q.VertexWaves && !still) m.EnableKeyword("_ARON_WAVES"); else m.DisableKeyword("_ARON_WAVES");
            m.DisableKeyword("_ARON_PLANAR");
            return m;
        }

        public static void Step(Material m, float wind, float rain, bool ripples)
        {
            if (m == null) return;
            m.SetFloat(WindId, wind);
            m.SetFloat(RippleId, ripples ? rain : 0f);
        }

        public static Color Hex(string hex, Color fallback)
            => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
    }

    /// <summary>The bits of <see cref="WeatherQuality"/> the water needs.</summary>
    public struct QualityTierSettings
    {
        public bool VertexWaves, Reflections;
    }

    /// <summary>
    /// High tier with the reflections toggle on: a second camera renders the scene mirrored in the water plane into a
    /// half-size texture that Aron/Water samples. The camera sits at the mirrored position looking along the mirrored
    /// view, which is an ordinary rotation, so culling stays as it is and only the image's x is flipped (the shader
    /// samples 1 - u). An oblique near plane keeps anything under the water out of the reflection.
    /// </summary>
    public class PlanarReflection : MonoBehaviour
    {
        private static readonly int TexId = Shader.PropertyToID("_PlanarTex");
        private Camera reflCam;
        private RenderTexture rt;
        private float planeY;
        private Material[] targets;

        public static PlanarReflection Create(Transform parent, float waterY, params Material[] mats)
        {
            var go = new GameObject("PlanarReflection");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<PlanarReflection>();
            p.planeY = waterY;
            p.targets = mats;
            return p;
        }

        private void OnEnable()
        {
            if (reflCam != null) reflCam.enabled = true;
            if (rt != null && targets != null) foreach (var m in targets) if (m != null) m.EnableKeyword("_ARON_PLANAR");
        }

        private void OnDisable()
        {
            if (reflCam != null) reflCam.enabled = false;
            if (targets != null) foreach (var m in targets) if (m != null) m.DisableKeyword("_ARON_PLANAR");
        }

        private void LateUpdate()
        {
            var main = Camera.main;
            if (main == null) return;
            if (reflCam == null) Build(main);
            // Mirror the main camera in the plane y = planeY.
            var pos = main.transform.position;
            var fwd = main.transform.forward;
            var up = main.transform.up;
            reflCam.transform.position = new Vector3(pos.x, 2f * planeY - pos.y, pos.z);
            reflCam.transform.rotation = Quaternion.LookRotation(new Vector3(fwd.x, -fwd.y, fwd.z), new Vector3(up.x, -up.y, up.z));
            reflCam.fieldOfView = main.fieldOfView;
            reflCam.aspect = main.aspect;
            reflCam.ResetProjectionMatrix();
            // Oblique near plane on the water so nothing below it reaches the texture.
            var w2c = reflCam.worldToCameraMatrix;
            var pn = w2c.MultiplyVector(Vector3.up).normalized;
            var pp = w2c.MultiplyPoint(new Vector3(0f, planeY + 0.05f, 0f));
            reflCam.projectionMatrix = reflCam.CalculateObliqueMatrix(new Vector4(pn.x, pn.y, pn.z, -Vector3.Dot(pn, pp)));
        }

        private void Build(Camera main)
        {
            int w = Mathf.Max(256, Screen.width / 2), h = Mathf.Max(144, Screen.height / 2);
            rt = new RenderTexture(w, h, 16, RenderTextureFormat.Default) { name = "PlanarReflection" };
            var go = new GameObject("ReflectionCamera");
            go.transform.SetParent(transform, false);
            reflCam = go.AddComponent<Camera>();
            reflCam.CopyFrom(main);
            reflCam.targetTexture = rt;
            reflCam.depth = main.depth - 1;
            reflCam.allowMSAA = false;
            reflCam.allowHDR = false;
            reflCam.farClipPlane = Mathf.Min(main.farClipPlane, 250f);
            foreach (var m in targets) if (m != null) { m.SetTexture(TexId, rt); m.EnableKeyword("_ARON_PLANAR"); }
        }

        private void OnDestroy()
        {
            if (rt != null) { rt.Release(); Destroy(rt); }
        }
    }
}
