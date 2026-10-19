using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MidLane.Editor
{
    // Offline conversion for the three supplied, uncompressed glTF 2.0 files.
    // Produces native Unity assets; the player needs no GLB loader or network access.
    public static class IreliaGlbBake
    {
        const string Source = "Assets/SourceModels/IreliaGLB/irelia";
        const string Output = "Assets/Resources/Models/IreliaGLB";
        [Serializable] sealed class Document { public Node[] nodes; public Skin[] skins; public MeshData[] meshes; public Accessor[] accessors; public View[] bufferViews; public Image[] images; public MaterialData[] materials; public TextureData[] textures; public Motion[] animations; }
        [Serializable] sealed class Node { public string name; public int[] children; public float[] translation, rotation, scale, matrix; }
        [Serializable] sealed class Skin { public int[] joints; public int inverseBindMatrices; }
        [Serializable] sealed class MeshData { public Primitive[] primitives; }
        [Serializable] sealed class Primitive { public Attributes attributes; public int indices, material; public int mode = 4; }
        [Serializable] sealed class Attributes { public int POSITION, TEXCOORD_0, JOINTS_0, WEIGHTS_0; }
        [Serializable] sealed class Accessor { public int bufferView, byteOffset, componentType, count; public string type; public bool normalized; }
        [Serializable] sealed class View { public int byteOffset, byteLength, byteStride; }
        [Serializable] sealed class Image { public int bufferView; public string mimeType; }
        [Serializable] sealed class TextureData { public int source; }
        [Serializable] sealed class MaterialData { public string name; public Pbr pbrMetallicRoughness; }
        [Serializable] sealed class Pbr { public TextureRef baseColorTexture; }
        [Serializable] sealed class TextureRef { public int index; }
        [Serializable] sealed class Motion { public string name; public Channel[] channels; public Sampler[] samplers; }
        [Serializable] sealed class Channel { public int sampler; public Target target; }
        [Serializable] sealed class Target { public int node; public string path; }
        [Serializable] sealed class Sampler { public int input, output; public string interpolation = "LINEAR"; }
        sealed class Glb
        {
            public Document Doc;
            public byte[] Bytes;
            int bin;
            public Glb(string path)
            {
                Bytes = File.ReadAllBytes(path);
                if (BitConverter.ToUInt32(Bytes, 0) != 0x46546C67 || BitConverter.ToUInt32(Bytes, 4) != 2 || BitConverter.ToInt32(Bytes, 8) != Bytes.Length)
                    throw new InvalidDataException("Expected a complete GLB 2.0 file: " + path);
                int jsonLength = BitConverter.ToInt32(Bytes, 12);
                if (BitConverter.ToUInt32(Bytes, 16) != 0x4E4F534A) throw new InvalidDataException("Missing GLB JSON chunk.");
                Doc = JsonUtility.FromJson<Document>(Encoding.UTF8.GetString(Bytes, 20, jsonLength));
                bin = 20 + jsonLength + 8;
                if (BitConverter.ToUInt32(Bytes, bin - 4) != 0x004E4942) throw new InvalidDataException("Missing GLB BIN chunk.");
            }
            public float[][] Read(int index)
            {
                Accessor a = Doc.accessors[index]; View view = Doc.bufferViews[a.bufferView];
                int components = a.type == "SCALAR" ? 1 : a.type == "VEC2" ? 2 : a.type == "VEC3" ? 3 : a.type == "VEC4" ? 4 : a.type == "MAT4" ? 16 : throw new InvalidDataException("Unsupported accessor shape.");
                int bytes = a.componentType == 5121 ? 1 : a.componentType == 5123 ? 2 : a.componentType == 5125 || a.componentType == 5126 ? 4 : throw new InvalidDataException("Unsupported component type.");
                int step = view.byteStride > 0 ? view.byteStride : components * bytes;
                var result = new float[a.count][];
                for (int i = 0; i < a.count; i++)
                {
                    result[i] = new float[components];
                    for (int j = 0; j < components; j++)
                    {
                        int offset = bin + view.byteOffset + a.byteOffset + i * step + j * bytes;
                        float value = a.componentType == 5126 ? BitConverter.ToSingle(Bytes, offset) : a.componentType == 5125 ? BitConverter.ToUInt32(Bytes, offset) : a.componentType == 5123 ? BitConverter.ToUInt16(Bytes, offset) : Bytes[offset];
                        if (a.normalized) value /= a.componentType == 5121 ? 255f : 65535f;
                        result[i][j] = value;
                    }
                }
                return result;
            }
            public byte[] ImageBytes(int index)
            {
                View view = Doc.bufferViews[Doc.images[index].bufferView]; var data = new byte[view.byteLength];
                Buffer.BlockCopy(Bytes, bin + view.byteOffset, data, 0, data.Length); return data;
            }
        }
        static T Save<T>(T asset, string name, string folder = Output) where T : Object
        {
            string path = folder + "/" + name;
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
            EditorUtility.CopySerialized(asset, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(asset); return existing;
        }
        static Vector3 Position(float[] v) { return new Vector3(-v[0], v[1], v[2]); }
        static Quaternion Rotation(float[] v) { return new Quaternion(v[0], -v[1], -v[2], v[3]); }
        [MenuItem("MidLane/Reimportar modelos GLB de dummies")]
        public static void BakeTrainingDummies()
        {
            const string folder = "Assets/Resources/Models/TrainingDummy";
            const string sourceFolder = "Assets/SourceModels/TrainingDummy/target_dummy";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var source = new Glb(sourceFolder + "Idle.glb"); var impact = new Glb(sourceFolder + "Hit.glb");
            Document doc = source.Doc;
            if (impact.Doc.nodes.Length != doc.nodes.Length) throw new InvalidDataException("Dummy skeleton sizes differ.");
            var root = new GameObject("TrainingDummy");
            try
            {
                var nodes = new Transform[doc.nodes.Length];
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (impact.Doc.nodes[i].name != doc.nodes[i].name) throw new InvalidDataException("Dummy bone names differ.");
                    nodes[i] = new GameObject(doc.nodes[i].name).transform; nodes[i].SetParent(root.transform, false);
                }
                for (int i = 0; i < nodes.Length; i++) if (doc.nodes[i].children != null)
                    foreach (int child in doc.nodes[i].children) nodes[child].SetParent(nodes[i], false);
                for (int i = 0; i < nodes.Length; i++)
                {
                    Node n = doc.nodes[i];
                    if (n.matrix != null) throw new InvalidDataException("Dummy source must use TRS transforms.");
                    if (n.translation != null) nodes[i].localPosition = Position(n.translation);
                    if (n.rotation != null) nodes[i].localRotation = Rotation(n.rotation);
                    if (n.scale != null) nodes[i].localScale = new Vector3(n.scale[0], n.scale[1], n.scale[2]);
                }
                var materials = new Material[doc.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    MaterialData description = doc.materials[i]; int image = doc.textures[description.pbrMetallicRoughness.baseColorTexture.index].source;
                    string texturePath = folder + "/Texture" + image + ".png";
                    File.WriteAllBytes(texturePath, source.ImageBytes(image)); AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.maxTextureSize = 2048; importer.anisoLevel = 4;
                    importer.alphaSource = TextureImporterAlphaSource.None; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
                    var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Dummy original material" };
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetColor("_BaseColor", Color.white); material.SetFloat("_Cull", 0);
                    materials[i] = Save(material, "Material" + i + ".mat", folder);
                }
                Skin skin = doc.skins[0]; var bones = new Transform[skin.joints.Length];
                for (int i = 0; i < bones.Length; i++) bones[i] = nodes[skin.joints[i]];
                var poses = new Matrix4x4[bones.Length]; float[][] rawPoses = source.Read(skin.inverseBindMatrices);
                Matrix4x4 reflection = Matrix4x4.Scale(new Vector3(-1, 1, 1));
                for (int i = 0; i < poses.Length; i++)
                {
                    var matrix = new Matrix4x4(); for (int c = 0; c < 4; c++) for (int r = 0; r < 4; r++) matrix[r, c] = rawPoses[i][c * 4 + r];
                    poses[i] = reflection * matrix * reflection;
                }
                for (int p = 0; p < doc.meshes[0].primitives.Length; p++)
                {
                    Primitive primitive = doc.meshes[0].primitives[p];
                    if (primitive.mode != 4) throw new InvalidDataException("Expected dummy triangle geometry.");
                    Mesh mesh = MakeMesh(source, primitive, poses); mesh.name = "Training dummy mesh";
                    mesh = Save(mesh, "Mesh" + p + ".asset", folder);
                    var visual = new GameObject("DummyMesh" + p); visual.transform.SetParent(nodes[0], false);
                    var renderer = visual.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = mesh; renderer.bones = bones; renderer.rootBone = bones[0];
                    renderer.sharedMaterial = materials[primitive.material]; renderer.updateWhenOffscreen = true; renderer.localBounds = new Bounds(Vector3.up, Vector3.one * 4);
                }
                // Idle.glb contains a rest pose, but no animation track. Preserve that pose exactly.
                var idle = new AnimationClip { name = "Idle", legacy = true, frameRate = 30, wrapMode = WrapMode.Loop };
                foreach (Transform node in nodes)
                {
                    string path = AnimationUtility.CalculateTransformPath(node, root.transform);
                    Vector3 position = node.localPosition, scale = node.localScale; Quaternion rotation = node.localRotation;
                    for (int c = 0; c < 3; c++)
                    {
                        idle.SetCurve(path, typeof(Transform), "m_LocalPosition." + "xyz"[c], AnimationCurve.Constant(0, 1, position[c]));
                        idle.SetCurve(path, typeof(Transform), "m_LocalScale." + "xyz"[c], AnimationCurve.Constant(0, 1, scale[c]));
                    }
                    for (int c = 0; c < 4; c++) idle.SetCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[c], AnimationCurve.Constant(0, 1, rotation[c]));
                }
                Save(idle, "Idle.anim", folder);
                AnimationClip hit = MakeClip(impact, root.transform, nodes, "Hit"); hit.wrapMode = WrapMode.ClampForever; Save(hit, "Hit.anim", folder);
                PrefabUtility.SaveAsPrefabAsset(root, folder + "/Dummy.prefab"); AssetDatabase.SaveAssets();
                Debug.Log("Training dummy baked: original skinned mesh/texture, static idle pose and supplied Hit animation.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [MenuItem("MidLane/Reimportar modelo GLB de Irelia")]
        public static void Bake()
        { BakeIrelia(Source + "Idle.glb", Output, false); }
        [MenuItem("MidLane/Reimportar Irelia oficial")]
        public static void BakeOfficial()
        { BakeIrelia("Assets/SourceModels/IreliaOfficial/IreliaOfficial.glb", "Assets/Resources/Models/IreliaOfficial", true); }
        [MenuItem("MidLane/Reimportar Mel oficial")]
        public static void BakeOfficialMel()
        { BakeIrelia("Assets/SourceModels/MelOfficial/MelOfficial.glb", "Assets/Resources/Models/MelOfficial", true, true); }
        [MenuItem("MidLane/Reimportar minions y torres oficiales")]
        public static void BakeOfficialLaneUnits()
        {
            string[] names = { "BlueMelee", "BlueCaster", "BlueCannon", "RedMelee", "RedCaster", "RedCannon", "BlueTower", "RedTower" };
            string[][] clips = {
                new[] { "minion_melee_idle", "minion_melee_run", "minion_melee_attack" },
                new[] { "minion_caster_idle", "minion_caster_run", "minion_caster_attack1" },
                new[] { "cannon_order_idle1", "cannon_order_run1", "cannon_order_attack1" },
                new[] { "minion_melee_idle", "minion_melee_run", "minion_melee_attack" },
                new[] { "minion_caster_idle", "minion_caster_run", "minion_caster_attack" },
                new[] { "cannon_chaos_idle1", "cannon_chaos_run1", "cannon_chaos_attack1" },
                new[] { "idle1" }, new[] { "idle1" } };
            for (int i = 0; i < names.Length; i++)
                BakeIrelia("Assets/SourceModels/LaneUnitsOfficial/" + names[i] + "/Model.glb",
                    "Assets/Resources/Models/LaneUnitsOfficial/" + names[i], true, false, names[i], clips[i]);
        }
        [MenuItem("MidLane/Reimportar Sett oficial")]
        public static void BakeOfficialSett()
        {
            BakeIrelia("Assets/SourceModels/SettOfficial/SettOfficial.glb", "Assets/Resources/Models/SettOfficial", true, false,
                "SettOfficial", new[] { "sett_idle", "sett_run", "sett_attack1_start", "sett_attack1" });
        }
        static void BakeIrelia(string sourcePath, string output, bool official, bool mel = false, string laneUnit = null, string[] laneClips = null)
        {
            Directory.CreateDirectory(output); AssetDatabase.Refresh();
            var source = new Glb(sourcePath); Document doc = source.Doc;
            var root = new GameObject(laneUnit != null ? laneUnit : mel ? "MelOfficial" : official ? "IreliaOfficial" : "IreliaGLB");
            try
            {
                Transform[] nodes = new Transform[doc.nodes.Length];
                for (int i = 0; i < nodes.Length; i++) { nodes[i] = new GameObject(doc.nodes[i].name).transform; nodes[i].SetParent(root.transform, false); }
                for (int i = 0; i < nodes.Length; i++) if (doc.nodes[i].children != null)
                    foreach (int child in doc.nodes[i].children) nodes[child].SetParent(nodes[i], false);
                for (int i = 0; i < nodes.Length; i++)
                {
                    Node n = doc.nodes[i];
                    if (n.matrix != null) throw new InvalidDataException("This source must use node TRS transforms.");
                    if (n.translation != null) nodes[i].localPosition = Position(n.translation);
                    if (n.rotation != null) nodes[i].localRotation = Rotation(n.rotation);
                    if (n.scale != null) nodes[i].localScale = new Vector3(n.scale[0], n.scale[1], n.scale[2]);
                }
                var materials = new Material[doc.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    MaterialData description = doc.materials[i];
                    string texturePath = output + "/Texture" + doc.textures[description.pbrMetallicRoughness.baseColorTexture.index].source + ".png";
                    File.WriteAllBytes(texturePath, source.ImageBytes(doc.textures[description.pbrMetallicRoughness.baseColorTexture.index].source));
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.maxTextureSize = 2048; importer.anisoLevel = 4;
                    importer.alphaSource = TextureImporterAlphaSource.None; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
                    var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = description.name };
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_Cull", 0); materials[i] = Save(material, "Material" + i + ".mat", output);
                }
                Skin skin = doc.skins[0]; var bones = new Transform[skin.joints.Length];
                for (int i = 0; i < bones.Length; i++) bones[i] = nodes[skin.joints[i]];
                var poses = new Matrix4x4[bones.Length]; float[][] rawPoses = source.Read(skin.inverseBindMatrices);
                Matrix4x4 reflection = Matrix4x4.Scale(new Vector3(-1, 1, 1));
                for (int i = 0; i < poses.Length; i++)
                {
                    var matrix = new Matrix4x4(); for (int c = 0; c < 4; c++) for (int r = 0; r < 4; r++) matrix[r, c] = rawPoses[i][c * 4 + r];
                    poses[i] = reflection * matrix * reflection;
                }
                // Compact each primitive: bounds must exclude unused vertices from the blades/body.
                for (int p = 0; p < doc.meshes[0].primitives.Length; p++)
                {
                    Primitive primitive = doc.meshes[0].primitives[p];
                    // Sett's photograph/emote props and empowered fist mesh are hidden in his normal form.
                    if (laneUnit == "SettOfficial" && p > 0) continue;
                    // Indestructible towers use only their intact Base, never overlapping destruction stages.
                    if (laneUnit != null && laneUnit.EndsWith("Tower") && p > 0 && !(laneUnit == "RedTower" && (p == 4 || p == 5))) continue;
                    if (primitive.mode != 4) throw new InvalidDataException("Expected triangle geometry.");
                    Mesh mesh = MakeMesh(source, primitive, poses);
                    mesh = Save(mesh, "Mesh" + p + ".asset", output);
                    var visual = new GameObject(p == 2 && !mel && laneUnit == null ? "AnimatedBlades" : "Body" + p); visual.transform.SetParent(nodes[0], false);
                    var renderer = visual.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = mesh; renderer.bones = bones;
                    renderer.rootBone = official ? nodes[0] : nodes[1]; renderer.sharedMaterial = materials[primitive.material]; renderer.updateWhenOffscreen = true;
                    renderer.localBounds = new Bounds(Vector3.up, Vector3.one * 6);
                    if (p == 2 && !mel && laneUnit == null)
                    {
                        int sword = Array.FindIndex(bones, bone => bone.name == "Sword1_Skn");
                        Mesh blade = MakeMesh(source, primitive, null, sword); blade.name = "Irelia spell blade"; Save(blade, "SpellBlade.asset", output);
                    }
                }
                string[] states = official && !mel ? new[] { "Idle", "Run", "Attack", "WCharge", "WRelease", "WRecovery", "Attack2", "QDash", "QToIdle", "QToRun", "QChain",
                    "EFirst", "EFirstRun", "EFirstToIdle", "ESecond", "ESecondRun", "ESecondToIdle", "RCast", "RHold", "RToIdle", "RToRun" } : new[] { "Idle", "Run", "Attack" };
                string[] officialClips = { "irelia_idle_01", "irelia_run_base", "irelia_attack_01", "irelia_spell2", "irelia_spell2_2", "irelia_spell2_3", "irelia_attack_02", "irelia_spell1", "irelia_spell1_to_idle", "irelia_spell1_into_runbase", "irelia_spell1_into_spell1",
                    "irelia_spell3", "irelia_spell3_run", "irelia_spell3_to_idle", "irelia_spell3_b", "irelia_spell3_b_run", "irelia_spell3_b_to_idle", "irelia_spell4", "irelia_spell4_idle", "irelia_spell4_to_idle", "irelia_spell4_to_run" };
                if (mel) officialClips = new[] { "idle1", "run", "attack1" };
                if (laneUnit != null)
                { states = laneUnit == "SettOfficial" ? new[] { "Idle", "Run", "AttackStart", "Attack" } : laneUnit.EndsWith("Tower") ? new[] { "Idle" } : new[] { "Idle", "Run", "Attack" }; officialClips = laneClips; }
                for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
                {
                    string state = states[stateIndex];
                    var motion = official ? source : new Glb(Source + state + ".glb");
                    if (motion.Doc.nodes.Length != nodes.Length) throw new InvalidDataException("Animation skeletons differ.");
                    for (int i = 0; i < nodes.Length; i++) if (motion.Doc.nodes[i].name != doc.nodes[i].name) throw new InvalidDataException("Animation bone names differ.");
                    int motionIndex = official ? Array.FindIndex(doc.animations, clip => clip.name == officialClips[stateIndex]) : 0;
                    if (motionIndex < 0) throw new InvalidDataException("Missing champion animation: " + state);
                    Save(MakeClip(motion, root.transform, nodes, state, motionIndex), state + ".anim", output);
                }
                PrefabUtility.SaveAsPrefabAsset(root, output + (laneUnit != null ? "/Model.prefab" : mel ? "/Mel.prefab" : "/Irelia.prefab")); AssetDatabase.SaveAssets();
                Debug.Log((laneUnit ?? (mel ? "Mel" : "Irelia")) + " baked at " + output + ": " + states.Length + " native clips, " + bones.Length + " bones and original textures.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        static Mesh MakeMesh(Glb source, Primitive primitive, Matrix4x4[] bindposes, int onlyBone = -1)
        {
            float[][] positions = source.Read(primitive.attributes.POSITION), uvs = source.Read(primitive.attributes.TEXCOORD_0);
            float[][] joints = source.Read(primitive.attributes.JOINTS_0), weights = source.Read(primitive.attributes.WEIGHTS_0), rawIndices = source.Read(primitive.indices);
            var remap = new Dictionary<int, int>(); var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var boneWeights = new List<BoneWeight>(); var indices = new List<int>();
            for (int i = 0; i < rawIndices.Length; i += 3)
            {
                bool keep = true;
                if (onlyBone >= 0) for (int c = 0; c < 3; c++)
                {
                    int index = (int)rawIndices[i + c][0], dominant = 0;
                    for (int n = 1; n < 4; n++) if (weights[index][n] > weights[index][dominant]) dominant = n;
                    if ((int)joints[index][dominant] != onlyBone) keep = false;
                }
                if (!keep) continue;
                for (int c = 0; c < 3; c++)
                {
                    int original = (int)rawIndices[i + (c == 0 ? 0 : 3 - c)][0]; int mapped;
                    if (!remap.TryGetValue(original, out mapped))
                    {
                        mapped = vertices.Count; remap[original] = mapped; vertices.Add(Position(positions[original])); uv.Add(new Vector2(uvs[original][0], 1 - uvs[original][1]));
                        float[] j = joints[original], w = weights[original];
                        boneWeights.Add(new BoneWeight { boneIndex0 = (int)j[0], boneIndex1 = (int)j[1], boneIndex2 = (int)j[2], boneIndex3 = (int)j[3], weight0 = w[0], weight1 = w[1], weight2 = w[2], weight3 = w[3] });
                    }
                    indices.Add(mapped);
                }
            }
            if (vertices.Count == 0) throw new InvalidDataException("Empty mesh selection.");
            var mesh = new Mesh { name = onlyBone < 0 ? "Irelia primitive " + primitive.material : "Irelia blade", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            if (bindposes != null) { mesh.boneWeights = boneWeights.ToArray(); mesh.bindposes = bindposes; }
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
        static AnimationClip MakeClip(Glb source, Transform root, Transform[] nodes, string name, int motionIndex = 0)
        {
            Motion motion = source.Doc.animations[motionIndex];
            var clip = new AnimationClip { name = name, legacy = true, frameRate = 30,
                wrapMode = name == "Idle" || name == "Run" ? WrapMode.Loop : WrapMode.ClampForever };
            foreach (Channel channel in motion.channels)
            {
                Sampler sampler = motion.samplers[channel.sampler];
                if (sampler.interpolation != "LINEAR" && sampler.interpolation != "STEP") throw new InvalidDataException("Expected sampled linear animation.");
                float[][] times = source.Read(sampler.input), values = source.Read(sampler.output);
                string path = AnimationUtility.CalculateTransformPath(nodes[channel.target.node], root);
                string property = channel.target.path == "translation" ? "m_LocalPosition" : channel.target.path == "rotation" ? "m_LocalRotation" : channel.target.path == "scale" ? "m_LocalScale" : throw new InvalidDataException("Unsupported animation channel.");
                for (int c = 0; c < values[0].Length; c++)
                {
                    var keys = new Keyframe[times.Length];
                    for (int i = 0; i < times.Length; i++)
                    {
                        float value = values[i][c];
                        if ((channel.target.path == "translation" && c == 0) || (channel.target.path == "rotation" && (c == 1 || c == 2))) value = -value;
                        keys[i] = new Keyframe(times[i][0], value);
                    }
                    var curve = new AnimationCurve(keys);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        var mode = sampler.interpolation == "STEP" ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.Linear;
                        AnimationUtility.SetKeyLeftTangentMode(curve, i, mode); AnimationUtility.SetKeyRightTangentMode(curve, i, mode);
                    }
                    clip.SetCurve(path, typeof(Transform), property + "." + "xyzw"[c], curve);
                }
            }
            clip.EnsureQuaternionContinuity(); return clip;
        }
    }
}
