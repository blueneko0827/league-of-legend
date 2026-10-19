using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MidLane.Editor
{
    public static class IreliaRVisualBake
    {
        const string Source = "Assets/SourceModels/IreliaOfficial/R/";
        const string Output = "Assets/Resources/Models/IreliaOfficial/R/";

        [MenuItem("MidLane/Reimportar efectos R de Irelia")]
        public static void Bake()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            Save(ReadBlade(Source + "irelia_base_r_mis_blades.scb"), "Blade.asset");
            var floor = new Mesh { name = "R wall energy strip" };
            floor.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f) };
            floor.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };
            floor.triangles = new[] { 0, 2, 1, 1, 2, 3 }; floor.RecalculateBounds(); floor.RecalculateNormals();
            Save(floor, "FloorStrip.asset");
            string texturePath = Output + "WallEnergy.png";
            File.Copy(Source + "WallEnergy.png", texturePath, true);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var blades = new Material(Shader.Find("MidLane/IreliaRBlade")) { name = "Official R blades" };
            blades.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Models/IreliaOfficial/Material2.mat").GetTexture("_BaseMap"));
            Save(blades, "Blade.mat");
            var energy = new Material(Shader.Find("MidLane/IreliaREnergy")) { name = "Official R wall energy" };
            energy.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); Save(energy, "Energy.mat");
            AssetDatabase.SaveAssets();
            Debug.Log("Irelia R baked: original SCB blade, blade atlas, TEX energy and native Unity materials.");
        }

        static Mesh ReadBlade(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != "r3d2Mesh" || reader.ReadUInt16() != 3 || reader.ReadUInt16() != 2)
                    throw new InvalidDataException("Expected SCB 3.2.");
                reader.ReadBytes(128); int vertexCount = reader.ReadInt32(), faceCount = reader.ReadInt32();
                uint flags = reader.ReadUInt32(); reader.ReadBytes(24); bool colors = reader.ReadUInt32() != 0;
                if (vertexCount <= 0 || vertexCount > 65535 || faceCount <= 0 || (flags & 1) != 0)
                    throw new InvalidDataException("Unexpected R blade topology.");
                var raw = new Vector3[vertexCount];
                Bounds bounds = new Bounds();
                for (int i = 0; i < vertexCount; i++)
                {
                    raw[i] = new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    if (i == 0) bounds = new Bounds(raw[i], Vector3.zero); else bounds.Encapsulate(raw[i]);
                }
                if (colors) reader.ReadBytes(vertexCount * 4);
                reader.ReadBytes(12); // SCB central point. Face material names occupy 64 bytes in this version.
                var vertices = new Vector3[faceCount * 3]; var uv = new Vector2[vertices.Length]; var indices = new int[vertices.Length];
                Vector3 origin = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                for (int face = 0; face < faceCount; face++)
                {
                    var sourceIndices = new[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };
                    reader.ReadBytes(64);
                    var u = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                    var v = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                    for (int j = 0; j < 3; j++)
                    {
                        if (sourceIndices[j] >= vertexCount) throw new InvalidDataException("Invalid SCB index.");
                        int k = face * 3 + j;
                        vertices[k] = (raw[sourceIndices[j]] - origin) / bounds.size.y;
                        uv[k] = new Vector2(u[j], 1 - v[j]); indices[k] = face * 3 + (j == 0 ? 0 : 3 - j);
                    }
                }
                if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Unread SCB data.");
                var mesh = new Mesh { name = "irelia_base_r_mis_blades" };
                mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateBounds(); mesh.RecalculateNormals();
                return mesh;
            }
        }
        static void Save<T>(T asset, string name) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(Output + name);
            if (existing == null) AssetDatabase.CreateAsset(asset, Output + name);
            else { EditorUtility.CopySerialized(asset, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(asset); }
        }
    }
}
