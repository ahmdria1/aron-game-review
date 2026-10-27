// (c) Ahmed Alamrani. All rights reserved.
using UnityEditor;
using UnityEngine;

namespace Aron.EditorTools
{
    // Import rules for the weather and breakable sounds in Audio/SFX.
    // Ambience loops stay stereo Vorbis, compressed in memory so five layers can crossfade without disk reads.
    // Long one-shots (thunder, gusts) are mono Vorbis compressed in memory; short hits are mono and decoded at load.
    public class SfxImport : AssetPostprocessor
    {
        private const string Folder = "/_Aron/Audio/SFX/";
        private static readonly string[] Loops = { "/rain_light.", "/rain_heavy.", "/rain_roof.", "/sandstorm.", "/sand_hiss.", "/wind_loop.", "/waves_loop.", "/palm_rustle_light.", "/palm_rustle_moderate.", "/palm_rustle_strong.", "/leaves_rustle_light.", "/leaves_rustle_medium.", "/leaves_rustle_strong." };
        private static readonly string[] Long = { "/thunder_", "/wind_gust_", "/foliage_gust_", "/explosion_" };

        public override uint GetVersion() => 4;

        private void OnPreprocessAudio()
        {
            if (!assetPath.Contains(Folder)) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;

            if (Matches(Loops))
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.quality = 0.45f;
                importer.forceToMono = false;
                importer.loadInBackground = true;
            }
            else if (Matches(Long))
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.quality = 0.5f;
                importer.forceToMono = true;
                importer.loadInBackground = true;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.6f;
                importer.forceToMono = true;
                importer.loadInBackground = false;
            }
            importer.defaultSampleSettings = settings;
        }

        private bool Matches(string[] names)
        {
            foreach (var n in names) if (assetPath.Contains(n)) return true;
            return false;
        }
    }
}
