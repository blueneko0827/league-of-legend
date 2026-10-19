using UnityEngine;
using System.Collections.Generic;
namespace MidLane
{
    public sealed class CrimsonModelAssets : MonoBehaviour
    {
        public const string Root = "Models/IreliaCrimson/Meshy_AI_Crimson_Valkyrie_1014200520_texture";
        Material material;
        RenderTexture metallicSmoothness;
        static Mesh bodyMesh;
        static Mesh[] bladeMeshes;
        static Mesh skinnedBodyMesh;
        static List<List<int>> ConnectedParts(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices; int[] triangles = mesh.triangles, parents = new int[vertices.Length];
            var welded = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                parents[i] = i;
                Vector3 v = vertices[i]; var key = new Vector3Int(Mathf.RoundToInt(v.x * 100000), Mathf.RoundToInt(v.y * 100000), Mathf.RoundToInt(v.z * 100000));
                int previous; if (welded.TryGetValue(key, out previous)) Union(parents, i, previous); else welded[key] = i;
            }
            for (int i = 0; i < triangles.Length; i += 3)
            { Union(parents, triangles[i], triangles[i + 1]); Union(parents, triangles[i], triangles[i + 2]); }
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int root = Find(parents, triangles[i]); List<int> group;
                if (!groups.TryGetValue(root, out group)) { group = new List<int>(); groups[root] = group; }
                group.Add(triangles[i]); group.Add(triangles[i + 1]); group.Add(triangles[i + 2]);
            }
            var parts = new List<List<int>>(groups.Values); parts.Sort((a, b) => b.Count.CompareTo(a.Count)); return parts;
        }
        static int Find(int[] parents, int index)
        {
            while (parents[index] != index) { parents[index] = parents[parents[index]]; index = parents[index]; }
            return index;
        }
        static void Union(int[] parents, int a, int b) { parents[Find(parents, a)] = Find(parents, b); }
        static void PrepareMeshes()
        {
            if (bodyMesh != null) return;
            GameObject source = Resources.Load<GameObject>(Root);
            MeshFilter filter = source.GetComponentInChildren<MeshFilter>(); Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices, normals = mesh.normals; Vector2[] uv = mesh.uv; Vector4[] tangents = mesh.tangents;
            List<List<int>> parts = ConnectedParts(mesh);
            if (parts.Count != 5) throw new System.InvalidOperationException("Crimson Valkyrie debe contener el cuerpo y cuatro hojas separadas.");
            // Meshy's root itself carries the -90 degree axis conversion; include that transform.
            Matrix4x4 matrix = filter.transform.localToWorldMatrix;
            bodyMesh = Slice(parts[0], matrix, vertices, normals, uv, tangents, "Crimson body");
            bladeMeshes = new Mesh[4];
            for (int i = 0; i < 4; i++) bladeMeshes[i] = Slice(parts[i + 1], matrix, vertices, normals, uv, tangents, "Crimson blade " + (i + 1));
        }
        static Mesh Slice(List<int> sourceTriangles, Matrix4x4 matrix, Vector3[] sourceVertices, Vector3[] sourceNormals,
            Vector2[] sourceUV, Vector4[] sourceTangents, string name, BoneWeight[] sourceWeights = null)
        {
            var remap = new Dictionary<int, int>(); var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uv = new List<Vector2>(); var tangents = new List<Vector4>(); var triangles = new List<int>();
            var weights = new List<BoneWeight>();
            Matrix4x4 normalMatrix = matrix.inverse.transpose;
            foreach (int sourceIndex in sourceTriangles)
            {
                int index;
                if (!remap.TryGetValue(sourceIndex, out index))
                {
                    index = vertices.Count; remap[sourceIndex] = index; vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[sourceIndex]));
                    if (sourceNormals.Length > sourceIndex) normals.Add(normalMatrix.MultiplyVector(sourceNormals[sourceIndex]).normalized);
                    if (sourceUV.Length > sourceIndex) uv.Add(sourceUV[sourceIndex]);
                    if (sourceWeights != null) weights.Add(sourceWeights[sourceIndex]);
                    if (sourceTangents.Length > sourceIndex)
                    {
                        Vector4 t = sourceTangents[sourceIndex]; Vector3 direction = matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                        tangents.Add(new Vector4(direction.x, direction.y, direction.z, t.w * (matrix.determinant < 0 ? -1 : 1)));
                    }
                }
                triangles.Add(index);
            }
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            if (normals.Count == vertices.Count) mesh.SetNormals(normals); else mesh.RecalculateNormals();
            if (uv.Count == vertices.Count) mesh.SetUVs(0, uv);
            if (tangents.Count == vertices.Count) mesh.SetTangents(tangents); else mesh.RecalculateTangents();
            if (sourceWeights != null) mesh.boneWeights = weights.ToArray();
            mesh.RecalculateBounds(); return mesh;
        }
        GameObject Visual(Mesh mesh, Transform parent, string name)
        {
            var visual = new GameObject(name); visual.transform.SetParent(parent, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = BodyMaterial; return visual;
        }
        public GameObject CreateBody(Transform parent)
        {
            GameObject source = Resources.Load<GameObject>("Models/IreliaCrimsonBiped/Walking");
            GameObject rig = Instantiate(source, parent, false); rig.name = "CrimsonValkyrie";
            foreach (Animation animation in rig.GetComponentsInChildren<Animation>()) { animation.playAutomatically = false; animation.Stop(); animation.enabled = false; }
            foreach (Animator animator in rig.GetComponentsInChildren<Animator>()) { animator.applyRootMotion = false; animator.enabled = false; }
            SkinnedMeshRenderer skin = rig.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedBodyMesh == null)
            {
                Mesh mesh = skin.sharedMesh; List<List<int>> parts = ConnectedParts(mesh);
                if (parts.Count != 5) throw new System.InvalidOperationException("El modelo biped debe conservar el cuerpo y cuatro espadas separables.");
                skinnedBodyMesh = Slice(parts[0], Matrix4x4.identity, mesh.vertices, mesh.normals, mesh.uv, mesh.tangents, "Crimson skinned body", mesh.boneWeights);
                skinnedBodyMesh.bindposes = mesh.bindposes;
            }
            skin.sharedMesh = skinnedBodyMesh; skin.sharedMaterial = BodyMaterial; skin.updateWhenOffscreen = true;
            foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>()) if (renderer != skin) renderer.enabled = false;
            AnimationClip walking = null;
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("Models/IreliaCrimsonBiped/Walking"))
                if (!clip.name.StartsWith("__preview__")) { walking = clip; break; }
            if (walking == null) throw new System.InvalidOperationException("Falta el clip Walking de Irelia.");
            rig.AddComponent<ChampionWalkAnimation>().Configure(rig.transform, skin.rootBone, walking);
            var baked = new Mesh(); skin.BakeMesh(baked); skin.localBounds = baked.bounds; Destroy(baked);
            return rig;
        }
        public Transform[] CreateFloatingBlades(Transform parent, Transform normalizedGeometry)
        {
            PrepareMeshes(); var blades = new Transform[4];
            // The walking rig has different source units. Keep blades in their established world-space pose.
            float scale = 2 / bodyMesh.bounds.size.y;
            Vector3 offset = new Vector3(-bodyMesh.bounds.center.x, -bodyMesh.bounds.min.y, -bodyMesh.bounds.center.z) * scale;
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("FloatingBlade_" + (i + 1)).transform; root.SetParent(parent, false);
                root.localPosition = offset + bladeMeshes[i].bounds.center * scale;
                GameObject visual = Visual(bladeMeshes[i], root, "CrimsonBlade");
                visual.transform.localScale = Vector3.one * scale;
                visual.transform.localPosition = -bladeMeshes[i].bounds.center * scale;
                blades[i] = root;
            }
            return blades;
        }
        public GameObject CreateSpellBlade(Transform parent, string name, Vector3 position)
        {
            PrepareMeshes(); var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            int index = name.Contains("segunda") ? 1 : 0;
            Mesh mesh = bladeMeshes[index]; GameObject visual = Visual(mesh, root.transform, "CrimsonBlade");
            float scale = 0.9f / mesh.bounds.size.y; visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = -mesh.bounds.center * scale;
            return root;
        }
        public Material BodyMaterial
        {
            get
            {
                if (material != null) return material;
                Texture2D metallic = Resources.Load<Texture2D>(Root + "_metallic");
                Texture2D roughness = Resources.Load<Texture2D>(Root + "_roughness");
                var pack = new Material(Resources.Load<Shader>("Models/IreliaCrimson/PackMetallicSmoothness"));
                pack.SetTexture("_Roughness", roughness);
                metallicSmoothness = new RenderTexture(metallic.width, metallic.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                metallicSmoothness.name = "Crimson metallic and smoothness";
                Graphics.Blit(metallic, metallicSmoothness, pack); Destroy(pack);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.name = "Crimson Valkyrie URP";
                material.SetTexture("_BaseMap", Resources.Load<Texture2D>(Root)); material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BumpMap", Resources.Load<Texture2D>(Root + "_normal")); material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_MetallicGlossMap", metallicSmoothness); material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", 1); material.SetFloat("_Smoothness", 1);
                return material;
            }
        }
        void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (metallicSmoothness != null) { metallicSmoothness.Release(); Destroy(metallicSmoothness); }
        }
    }
}
