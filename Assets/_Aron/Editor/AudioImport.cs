// (c) Ahmed Alamrani. All rights reserved.
using UnityEditor;
using UnityEngine;

namespace Aron.EditorTools
{
    // Phone-friendly audio import.
    // Music: Vorbis, long tracks stream from storage instead of sitting decompressed in memory;
    // the short win/lose stingers stay in memory so they start instantly.
    // Sound effects: mono ADPCM, decoded once at load. Cheap to play many at once and no decode spikes.
    public class AudioImport : AssetPostprocessor
    {
        public override uint GetVersion() => 4;

        private void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            if (assetPath.Contains("/_Aron/Resources/Audio/Music/")) Music(importer, false);
            else if (assetPath.Contains("/_Aron/Remote/Music/")) Music(importer, true);
            else if (assetPath.Contains("/_Aron/Resources/Audio/Sfx/")) Sfx(importer);
        }

        private void Music(AudioImporter importer, bool remote)
        {
            bool stinger = assetPath.Contains("/music_victory.") || assetPath.Contains("/music_defeat.");
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            // Country music comes out of a downloaded bundle: keep it compressed in memory (about 2 MB a track) instead of streaming from the bundle file.
            settings.loadType = stinger ? AudioClipLoadType.DecompressOnLoad : remote ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.Streaming;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = !stinger;
        }

        private static void Sfx(AudioImporter importer)
        {
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
        }
    }
}
