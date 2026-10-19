using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MidLane.Editor
{
    public static class IreliaEVisualBake
    {
        const string Source = "Assets/SourceModels/IreliaOfficial/E/";
        const string Output = "Assets/Resources/Models/IreliaOfficial/E/";
        [MenuItem("MidLane/Reimportar convergencia E de Irelia")]
        public static void Bake()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            Save(ReadBeam(), "Beam.asset");
            foreach (string name in new[] { "BeamMask", "Missile", "Trail", "StunBeam" })
            {
                string path = Output + name + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
                importer.mipmapEnabled = true; importer.wrapMode = name == "BeamMask" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.SaveAndReimport();
            }
            foreach (string name in new[] { "Beam", "Missile", "Trail", "Impact" })
            {
                var material = new Material(Shader.Find("MidLane/IreliaEConvergence")) { name = "Official E " + name };
                material.SetTexture("_BaseMap", Texture(name == "Beam" || name == "Impact" ? "StunBeam" : name));
                material.SetTexture("_MaskMap", Texture("BeamMask"));
                material.SetFloat("_UseMask", name == "Beam" ? 1 : 0); Save(material, name + ".mat");
            }
            AssetDatabase.SaveAssets(); Debug.Log("Baked official Irelia E beam geometry and convergence materials.");
        }
        static Texture2D Texture(string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Output + name + ".png");
            if (texture == null) throw new InvalidDataException("E texture did not import: " + name);
            return texture;
        }
        static Mesh ReadBeam()
        {
            using (var reader = new BinaryReader(File.OpenRead(Source + "irelia_base_e_beam.scb")))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != "r3d2Mesh" || reader.ReadUInt16() != 3 || reader.ReadUInt16() != 2)
                    throw new InvalidDataException("Expected SCB 3.2 E beam.");
                reader.ReadBytes(128); int count = reader.ReadInt32(), faces = reader.ReadInt32();
                uint flags = reader.ReadUInt32(); reader.ReadBytes(24); bool colors = reader.ReadUInt32() != 0;
                if (count != 35 || faces != 48 || (flags & 1) != 0) throw new InvalidDataException("Unexpected E beam topology.");
                var raw = new Vector3[count]; Bounds bounds = new Bounds();
                for (int i = 0; i < count; i++)
                {
                    raw[i] = new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    if (i == 0) bounds = new Bounds(raw[i], Vector3.zero); else bounds.Encapsulate(raw[i]);
                }
                // The source vertex alpha is zero; LoL's missing emitters control opacity instead.
                if (colors) reader.ReadBytes(count * 4);
                reader.ReadBytes(12);
                var vertices = new Vector3[faces * 3]; var uv = new Vector2[vertices.Length]; var indices = new int[vertices.Length];
                for (int face = 0; face < faces; face++)
                {
                    var ids = new[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() }; reader.ReadBytes(64);
                    var u = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                    var v = new[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                    for (int j = 0; j < 3; j++)
                    {
                        if (ids[j] >= count) throw new InvalidDataException("Bad E beam index.");
                        int k = face * 3 + j; Vector3 p = raw[ids[j]] - bounds.center;
                        vertices[k] = new Vector3(p.x / bounds.size.x, p.y / 100, p.z / bounds.size.z);
                        uv[k] = new Vector2(u[j], 1 - v[j]); indices[k] = face * 3 + (j == 0 ? 0 : 3 - j);
                    }
                }
                if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Unread E beam data.");
                var mesh = new Mesh { name = "irelia_base_e_beam" };
                mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateBounds(); mesh.RecalculateNormals(); return mesh;
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
