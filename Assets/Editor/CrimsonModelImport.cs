using UnityEditor;
namespace MidLane.Editor
{
    public sealed class CrimsonModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            bool biped = assetPath.Contains("/Models/IreliaCrimsonBiped/") || assetPath.Contains("/Models/Sett/");
            if (!biped && !assetPath.Contains("/Models/IreliaCrimson/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.isReadable = true;
            importer.importCameras = false; importer.importLights = false;
            if (biped)
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                if (assetPath.EndsWith("Walking.fbx"))
                {
                    ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                    foreach (ModelImporterClipAnimation clip in clips)
                    { clip.name = "Walking"; clip.loopTime = true; clip.wrapMode = UnityEngine.WrapMode.Loop; }
                    importer.clipAnimations = clips;
                }
            }
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Models/IreliaCrimson/") && !assetPath.Contains("/Models/Sett/")) return;
            var importer = (TextureImporter)assetImporter;
            if (assetPath.EndsWith("_normal.png")) importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = !assetPath.EndsWith("_normal.png") && !assetPath.EndsWith("_metallic.png") && !assetPath.EndsWith("_roughness.png");
        }
    }
}
