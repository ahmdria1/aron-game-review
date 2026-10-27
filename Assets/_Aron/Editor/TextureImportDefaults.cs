// (c) Ahmed Alamrani. All rights reserved.
using UnityEditor;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>
    /// New and re-imported textures in the game's own folders get ASTC on Android and iOS unless someone already chose a format
    /// for that platform: 6x6 for colour maps (about 3.6 bits per pixel, a quarter of the memory of uncompressed), 5x5 for normal
    /// maps and 4x4 for UI sprites where edges matter. Readability and everything else are left as they are.
    /// "Aron > Performance > Apply ASTC to existing textures" runs the same rule over the whole project once.
    /// </summary>
    public class TextureImportDefaults : AssetPostprocessor
    {
        private const string Root = "Assets/_Aron";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            Apply((TextureImporter)assetImporter);
        }

        public static bool Apply(TextureImporter importer)
        {
            if (importer == null || importer.textureType == TextureImporterType.Lightmap) return false;
            var format = importer.textureType == TextureImporterType.NormalMap ? TextureImporterFormat.ASTC_5x5
                       : importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.GUI ? TextureImporterFormat.ASTC_4x4
                       : TextureImporterFormat.ASTC_6x6;
            bool changed = false;
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var s = importer.GetPlatformTextureSettings(platform);
                if (s.overridden) continue;
                s.overridden = true;
                s.format = format;
                s.maxTextureSize = Mathf.Min(importer.maxTextureSize, 2048);
                importer.SetPlatformTextureSettings(s);
                changed = true;
            }
            return changed;
        }

        [MenuItem("Aron/Performance/Apply ASTC to existing textures")]
        public static void ApplyToAll()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti && Apply(ti)) { ti.SaveAndReimport(); n++; }
            }
            Debug.Log("[Aron] ASTC set on " + n + " textures.");
        }
    }
}
