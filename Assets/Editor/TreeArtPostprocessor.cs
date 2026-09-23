using UnityEditor;
using UnityEngine;

public class TreeArtPostprocessor : AssetPostprocessor
{
    private const string TargetFolder = "Assets/Resources/Art/Trees/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TargetFolder))
        {
            return;
        }

        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower();

        if (!fileName.Contains("tree") && !fileName.Contains("stump") && !fileName.Contains("bobber"))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;

        if (fileName.Contains("bobber"))
        {
            return;
        }
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;

        int width;
        int height;

        importer.GetSourceTextureWidthAndHeight(out width, out height);

        if (height > 0)
        {
            importer.spritePixelsPerUnit = Mathf.Max(1f, height / 3f);
        }
    }
}