using UnityEditor;
using UnityEngine;

namespace MidLane.Editor
{
    public sealed class LaneTerrainImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Terrain/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None; importer.mipmapEnabled = true;
            importer.wrapModeU = TextureWrapMode.Mirror; importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
