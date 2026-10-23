using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Vanguard's Edge (R): the wall of blades as LoL draws it, i.e. the IreliaBlades unit of the
    // game files (Tools/Import-IreliaRBlades.py -> Vfx/Irelia/IreliaRBlades.json): 26 blades on
    // their bones playing the unit's clips. Cast: the blades burst out of the impact and land on
    // the wall (1.1 s); cycle: they stand; death: they sink into the ground when the wall ends.
    // Material: Irelia's blades texture with the rim light of Irelia_bladeglow_inst
    // (IreliaRWallBlade.shader). Presentation only: BladeWall keeps the damage boundary and the
    // blades stand on it (their ground positions in gameplay units, their size and height at
    // the champions' scale, like the other effects).
    public sealed class IreliaRBlades : MonoBehaviour
    {
        [Serializable] sealed class Library { public int fps; public string[] bones; public BladeData[] blades; public ClipData[] clips; }
        [Serializable] sealed class BladeData { public string bone; public float[] positions, normals, uvs; public int[] indices; }
        [Serializable] sealed class ClipData { public string name; public int frames; public float[] poses; }

        public const string ResourcePath = "Vfx/Irelia/IreliaRBlades";
        const int Stride = 10;
        // Animation graph of IreliaBlades: R_Wall_Bladeglow starts on every blade at frame 9 of the cast.
        public const float BladeGlowAt = 9f / 30;
        static Library library;
        static Mesh[] meshes;
        static Material material;

        LaneWorld world;
        Transform[] bones;
        ClipData cast, cycle, death;
        float age, deathAge = -1, groundScale, heightScale;

        public float Age { get { return age; } }
        public bool Dying { get { return deathAge >= 0; } }
        public int BladeCount { get { return bones != null ? bones.Length - 1 : 0; } }
        // Bone 0 is Sword_Root (R_Hit_Aoe plays there); bones 1.. carry one blade each.
        public Transform Bone(int index) { return bones[index]; }

        public static bool Available { get { return Load(); } }

        static bool Load()
        {
            // Meshes and material are built once; rebuilt if Unity unloaded them (play mode without domain reload).
            if (library != null && (library.blades == null || library.blades.Length == 0)) return false;
            if (library != null && material != null && meshes != null && Array.TrueForAll(meshes, m => m != null)) return true;
            if (library == null)
            {
                TextAsset source = Resources.Load<TextAsset>(ResourcePath);
                library = source != null ? JsonUtility.FromJson<Library>(source.text) : new Library();
            }
            if (library.blades == null || library.blades.Length == 0) return false;
            meshes = new Mesh[library.blades.Length];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = BuildMesh(library.blades[i]);
            Shader shader = Shader.Find("MidLane/IreliaRWallBlade");
            material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit")) { name = "Irelia R wall blades" };
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>(IreliaGlbAssets.Root + "Texture1"));
            return true;
        }

        static Mesh BuildMesh(BladeData blade)
        {
            int count = blade.positions.Length / 3;
            var vertices = new Vector3[count]; var normals = new Vector3[count]; var uvs = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(blade.positions[i * 3], blade.positions[i * 3 + 1], blade.positions[i * 3 + 2]);
                normals[i] = new Vector3(blade.normals[i * 3], blade.normals[i * 3 + 1], blade.normals[i * 3 + 2]);
                uvs[i] = new Vector2(blade.uvs[i * 2], blade.uvs[i * 2 + 1]);
            }
            var mesh = new Mesh { name = "Irelia R wall " + blade.bone };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uvs; mesh.triangles = blade.indices;
            mesh.RecalculateBounds();
            return mesh;
        }

        // impact and direction: the wall's center and the R's flight (BladeWall); unitScale: the
        // effects' Unity units per LoL unit (RiotVfxPlayer.UnitScale).
        public static IreliaRBlades Create(LaneWorld world, Vector3 impact, Vector3 direction, float unitScale)
        {
            if (world == null || !Load()) return null;
            var root = new GameObject("R official blade wall");
            root.transform.SetParent(world.transform, false);
            direction.y = 0;
            root.transform.SetPositionAndRotation(impact, Quaternion.LookRotation(direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward));
            var wall = root.AddComponent<IreliaRBlades>();
            wall.world = world;
            wall.groundScale = LeagueChampionStats.WorldUnitsPerLeagueUnit;
            wall.heightScale = unitScale;
            wall.cast = Clip("cast"); wall.cycle = Clip("cycle"); wall.death = Clip("death");
            wall.bones = new Transform[library.bones.Length];
            wall.bones[0] = new GameObject(library.bones[0]).transform;
            wall.bones[0].SetParent(root.transform, false);
            for (int i = 1; i < wall.bones.Length; i++)
            {
                var blade = new GameObject(library.bones[i]);
                blade.transform.SetParent(root.transform, false);
                blade.AddComponent<MeshFilter>().sharedMesh = meshes[i - 1];
                var renderer = blade.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                wall.bones[i] = blade.transform;
            }
            wall.Present();
            return wall;
        }

        static ClipData Clip(string name) { return Array.Find(library.clips, c => c.name == name); }

        // The wall ended: the blades sink (death clip) and the object goes.
        public void Die() { if (deathAge < 0) deathAge = age; }

        void Update()
        {
            if (world == null) { Destroy(gameObject); return; }
            if (world.Paused) return;
            age += Time.deltaTime;
            Present();
        }

        void Present()
        {
            float fps = library.fps > 0 ? library.fps : 30;
            ClipData clip; float frame;
            if (deathAge >= 0)
            {
                clip = death; frame = (age - deathAge) * fps;
                if (frame >= death.frames - 1) { Destroy(gameObject); return; }
            }
            else
            {
                frame = age * fps; clip = cast;
                // The cycle starts on the cast's last pose and loops.
                if (frame >= cast.frames - 1) { clip = cycle; frame = Mathf.Repeat(frame - (cast.frames - 1), cycle.frames - 1); }
            }
            int first = Mathf.Clamp(Mathf.FloorToInt(frame), 0, clip.frames - 1), next = Mathf.Min(first + 1, clip.frames - 1);
            float blend = Mathf.Clamp01(frame - first);
            for (int bone = 0; bone < bones.Length; bone++) Pose(bones[bone], clip, first, next, blend, bone);
        }

        void Pose(Transform bone, ClipData clip, int first, int next, float blend, int index)
        {
            int a = (first * bones.Length + index) * Stride, b = (next * bones.Length + index) * Stride;
            float[] p = clip.poses;
            Vector3 position = Vector3.Lerp(new Vector3(p[a], p[a + 1], p[a + 2]), new Vector3(p[b], p[b + 1], p[b + 2]), blend);
            Quaternion rotation = Quaternion.Slerp(new Quaternion(p[a + 3], p[a + 4], p[a + 5], p[a + 6]),
                new Quaternion(p[b + 3], p[b + 4], p[b + 5], p[b + 6]), blend);
            Vector3 scale = Vector3.Lerp(new Vector3(p[a + 7], p[a + 8], p[a + 9]), new Vector3(p[b + 7], p[b + 8], p[b + 9]), blend);
            bone.localPosition = new Vector3(position.x * groundScale, position.y * heightScale, position.z * groundScale);
            bone.localRotation = rotation;
            bone.localScale = scale * heightScale;
        }
    }
}
