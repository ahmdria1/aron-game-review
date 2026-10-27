// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aron.EditorTools
{
    /// <summary>
    /// Three quality levels, Low / Medium / High, each with its own URP asset. DeviceSettings picks one per
    /// phone at startup. The game has no post-processing volumes, so HDR and post effects stay off everywhere;
    /// the tiers differ in shadows, MSAA, lights and texture resolution.
    /// </summary>
    public static class QualityTiers
    {
        private struct Tier
        {
            public string name;
            public int msaa, shadowResolution, cascades, extraLights, textureLimit;
            public bool shadows, softShadows;
            public float shadowDistance, lodBias;
            public AnisotropicFiltering aniso;
        }

        private static readonly Tier[] Tiers =
        {
            new Tier { name = "Low", msaa = 1, shadows = false, shadowResolution = 512, shadowDistance = 0f, cascades = 1,
                       extraLights = 2, textureLimit = 1, lodBias = 0.6f, aniso = AnisotropicFiltering.Disable },
            new Tier { name = "Medium", msaa = 2, shadows = true, shadowResolution = 1024, shadowDistance = 25f, cascades = 1,
                       extraLights = 4, textureLimit = 0, lodBias = 1f, aniso = AnisotropicFiltering.Disable },
            new Tier { name = "High", msaa = 4, shadows = true, softShadows = true, shadowResolution = 2048, shadowDistance = 40f,
                       cascades = 2, extraLights = 8, textureLimit = 0, lodBias = 1.5f, aniso = AnisotropicFiltering.Enable },
        };

        public static void Setup()
        {
            var renderer = URPSetup.Renderer();
            var assets = new UniversalRenderPipelineAsset[Tiers.Length];
            for (int i = 0; i < Tiers.Length; i++) assets[i] = PipelineFor(Tiers[i], renderer);
            GraphicsSettings.defaultRenderPipeline = assets[(int)QualityTier.High];

            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = so.FindProperty("m_QualitySettings");
            levels.arraySize = Tiers.Length;
            for (int i = 0; i < Tiers.Length; i++)
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue = Tiers[i].name;
            SetPlatformDefault(so, "Android", (int)QualityTier.Medium);
            SetPlatformDefault(so, "iPhone", (int)QualityTier.Medium);
            SetPlatformDefault(so, "Standalone", (int)QualityTier.High);
            so.ApplyModifiedPropertiesWithoutUndo();

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < Tiers.Length; i++)
            {
                var t = Tiers[i];
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = assets[i];
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
                QualitySettings.globalTextureMipmapLimit = t.textureLimit;
                QualitySettings.anisotropicFiltering = t.aniso;
                QualitySettings.lodBias = t.lodBias;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.softParticles = false;
                QualitySettings.skinWeights = i == 0 ? SkinWeights.TwoBones : SkinWeights.FourBones;
            }
            QualitySettings.SetQualityLevel(Mathf.Clamp(current, 0, Tiers.Length - 1), false);
            AssetDatabase.SaveAssets();
            Debug.Log("[Aron] Quality tiers ready: Low / Medium / High.");
        }

        private static UniversalRenderPipelineAsset PipelineFor(Tier t, ScriptableRendererData renderer)
        {
            string path = $"{URPSetup.Dir}/Aron_URP_{t.name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.renderScale = 1f;
            asset.supportsHDR = false;
            asset.msaaSampleCount = t.msaa;
            asset.shadowDistance = t.shadowDistance;
            asset.shadowCascadeCount = t.cascades;
            asset.mainLightShadowmapResolution = t.shadowResolution;
            asset.maxAdditionalLightsCount = t.extraLights;
            asset.supportsCameraDepthTexture = false;
            asset.supportsCameraOpaqueTexture = false;
            asset.useSRPBatcher = true;

            // These have no public setters.
            var so = new SerializedObject(asset);
            SetBool(so, "m_MainLightShadowsSupported", t.shadows);
            SetBool(so, "m_SoftShadowsSupported", t.softShadows);
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetBool(SerializedObject so, string field, bool value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.boolValue = value;
            else Debug.LogWarning($"[Aron] URP asset has no field {field}; set it by hand in the Inspector.");
        }

        private static void SetPlatformDefault(SerializedObject so, string platform, int level)
        {
            var map = so.FindProperty("m_PerPlatformDefaultQuality");
            if (map == null) return;
            for (int i = 0; i < map.arraySize; i++)
            {
                var e = map.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("first").stringValue != platform) continue;
                e.FindPropertyRelative("second").intValue = level;
                return;
            }
            map.arraySize++;
            var added = map.GetArrayElementAtIndex(map.arraySize - 1);
            added.FindPropertyRelative("first").stringValue = platform;
            added.FindPropertyRelative("second").intValue = level;
        }
    }
}
