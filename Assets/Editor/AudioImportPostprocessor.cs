using UnityEditor;
using UnityEngine;

/// <summary>
/// Performance-oriented audio import rules, applied automatically on import.
/// BGM  -> Vorbis + CompressedInMemory (Streaming is unsupported on WebGL / Douyin mini game).
/// SFX  -> PCM + DecompressOnLoad (short clips, zero runtime decode cost).
/// </summary>
public class AudioImportPostprocessor : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;

        if (assetPath.Contains("/Resources/Audio/BGM/"))
        {
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.quality = 0.7f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.preloadAudioData = true;
        }
        else if (assetPath.Contains("/Resources/Audio/SFX/"))
        {
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
        }
    }
}
