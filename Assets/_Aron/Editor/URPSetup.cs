// (c) Ahmed Alamrani. All rights reserved.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aron.EditorTools
{
    /// <summary>Creates the URP renderer and the Low / Medium / High pipeline assets, and assigns them.</summary>
    public static class URPSetup
    {
        public const string Dir = "Assets/_Aron/Settings";
        private const string RendererPath = Dir + "/Aron_URP_Renderer.asset";

        [MenuItem("Aron/1. Setup URP Pipeline", priority = 1)]
        public static void Setup() => QualityTiers.Setup();

        public static UniversalRendererData Renderer()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Aron", "Settings");
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data, RendererPath);
            return data;
        }
    }
}
