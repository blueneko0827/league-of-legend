using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Presentation only: plays one official particle system and never touches gameplay.
    // The pose source is read every frame, so effects can follow a bone-like anchor.
    // Runs after the rig sampling and CameraFacingVisualTilt (order 200) so bones are final.
    [DefaultExecutionOrder(300)]
    public sealed class RiotVfxPlayer : MonoBehaviour
    {
        public RiotVfxInstance Instance { get; private set; }
        public float TimeScale = 1;
        // World units the draws are pulled toward the camera (hit effects inside their target).
        public float DepthPull;
        public string SystemName { get { return Instance != null ? Instance.Data.name : ""; } }
        public RiotVfxPose Pose { get { return pose; } }
        LaneWorld world;
        RiotVfxLibrary library;
        Func<RiotVfxPose> poseSource;
        RiotVfxPose pose;
        GameObject attachedVisual;
        readonly List<RiotVfxDraw> draws = new List<RiotVfxDraw>();
        readonly List<Mesh> trailMeshes = new List<Mesh>();
        readonly List<Renderer> attachedProxies = new List<Renderer>();
        MaterialPropertyBlock block;
        static readonly int ColorId = Shader.PropertyToID("_Color"), UvId = Shader.PropertyToID("_UV"), Uv2Id = Shader.PropertyToID("_UV2");
        static readonly int ErosionId = Shader.PropertyToID("_Erosion"), DepthPullId = Shader.PropertyToID("_DepthPull");
        static int seed = 7919;
        const string ProxySuffix = " (VFX)";
        // Raised for every system that starts (diagnostics and Play Mode tests).
        public static event Action<RiotVfxPlayer> Started;

        // Unity units per LoL unit for effects drawn next to the enlarged official models:
        // the champion is shown at ChampionVisualHeight instead of the mesh's 228 LoL units.
        public const float OfficialIreliaHeight = 228;
        public static float UnitScale(LaneWorld world)
        {
            return world.Presentation.ChampionVisualHeight / OfficialIreliaHeight * world.Presentation.vfx_scale;
        }

        // boundSize: FlexShape "bound object size" in LoL units (scales marks to their target).
        public static RiotVfxPlayer Play(LaneWorld world, string system, Func<RiotVfxPose> source, float timeScale = 1, GameObject attached = null, float boundSize = 100)
        {
            if (world == null || source == null || !world.Presentation.official_vfx) return null;
            RiotVfxLibrary library = RiotVfxLibrary.For(world);
            RiotVfxSystemData data = library.Loaded ? library.FindSystem(system) : null;
            if (data == null) return null;
            var root = new GameObject("VFX " + system);
            root.transform.SetParent(world.transform, false);
            var player = root.AddComponent<RiotVfxPlayer>();
            player.world = world; player.library = library; player.poseSource = source; player.TimeScale = timeScale;
            player.attachedVisual = attached;
            player.Instance = new RiotVfxInstance(data, seed = seed * 31 + 17)
            { UnitScale = UnitScale(world), BoundSize = boundSize, ResolveChild = library.FindSystem };
            player.pose = source();
            player.block = new MaterialPropertyBlock();
            if (Started != null) Started(player);
            return player;
        }

        public static RiotVfxPlayer PlayAt(LaneWorld world, string system, Vector3 position, Quaternion rotation, float timeScale = 1)
        {
            var fixedPose = new RiotVfxPose(position, rotation);
            return Play(world, system, () => fixedPose, timeScale);
        }

        public void Stop() { if (Instance != null) Instance.Stop(); }

        // Riot bone names are case-insensitive (events use C_BUFFBONE_GLB_LAYOUT_LOC).
        // Hidden children are skipped: a practice dummy shown as a minion keeps its dummy rig
        // inactive next to the minion rig, and both have the same buff bones.
        public static Transform FindBone(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                Transform found = FindBone(child, name);
                if (found != null) return found;
            }
            return null;
        }

        void LateUpdate()
        {
            if (Instance == null || world == null) { Destroy(gameObject); return; }
            if (!world.Paused)
            {
                if (poseSource != null)
                {
                    // A pose source whose owner disappeared ends the effect instead of throwing every frame.
                    try { pose = poseSource(); }
                    catch (Exception e) { poseSource = null; Instance.Stop(); Debug.LogException(e); }
                }
                Instance.Tick(Time.deltaTime * TimeScale, pose);
            }
            if (Instance.Finished) { Destroy(gameObject); return; }
            Render();
        }

        void Render()
        {
            Camera view = world.View != null ? world.View : Camera.main;
            if (view == null) return;
            draws.Clear();
            Instance.Collect(draws, new RiotVfxView(view.transform.position, view.transform.rotation));
            int trail = 0; bool attachedDrawn = false; Color attachedColor = Color.clear;
            foreach (RiotVfxDraw draw in draws)
            {
                if (draw.kind == RiotVfxDrawKind.AttachedMesh) { attachedDrawn = true; attachedColor = draw.color; continue; }
                Mesh mesh = draw.kind == RiotVfxDrawKind.Mesh ? library.GetMesh(draw.emitter.mesh) :
                    draw.kind == RiotVfxDrawKind.Trail ? TrailMesh(trail++, draw) : library.Quad;
                if (mesh == null) continue;
                block.SetVector(ColorId, (Vector4)draw.color); block.SetVector(UvId, draw.uv); block.SetVector(Uv2Id, draw.uv2);
                block.SetVector(ErosionId, Erosion(draw)); block.SetFloat(DepthPullId, DepthPull);
                Graphics.DrawMesh(mesh, draw.matrix, library.GetMaterial(draw.emitter), gameObject.layer, null, 0, block,
                    ShadowCastingMode.Off, false);
            }
            UpdateAttached(attachedDrawn, attachedColor);
        }

        static Vector4 Erosion(RiotVfxDraw draw)
        {
            RiotVfxErosion erosion = draw.emitter.erosion;
            if (erosion == null || string.IsNullOrEmpty(erosion.map)) return new Vector4(0, 0, .1f, 0);
            return new Vector4(draw.erosion, erosion.featherIn, Mathf.Max(.01f, erosion.featherOut), erosion.useAlpha ? 1 : 0);
        }

        Mesh TrailMesh(int index, RiotVfxDraw draw)
        {
            while (trailMeshes.Count <= index) trailMeshes.Add(new Mesh { name = "Riot VFX trail" });
            Mesh mesh = trailMeshes[index];
            mesh.Clear();
            int count = draw.trailCount;
            if (count < 4) return null;
            var vertices = new Vector3[count]; var uvs = new Vector2[count]; var colors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = Instance.TrailPositions[draw.trailStart + i];
                uvs[i] = Instance.TrailUvs[draw.trailStart + i];
                colors[i] = Instance.TrailColors[draw.trailStart + i];
            }
            var triangles = new int[(count / 2 - 1) * 6];
            for (int i = 0, k = 0; i + 3 < count; i += 2)
            {
                triangles[k++] = i; triangles[k++] = i + 2; triangles[k++] = i + 1;
                triangles[k++] = i + 1; triangles[k++] = i + 2; triangles[k++] = i + 3;
            }
            mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colors; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // VfxPrimitiveAttachedMesh: the owner's skinned mesh redrawn with the effect material.
        void UpdateAttached(bool visible, Color color)
        {
            if (visible && attachedProxies.Count == 0 && attachedVisual != null)
            {
                RiotVfxEmitterData emitter = Array.Find(Instance.Data.emitters, e => e.primitive == "attachedMesh");
                Material material = emitter != null ? library.GetMaterial(emitter, true) : null;
                if (material != null)
                    foreach (SkinnedMeshRenderer source in attachedVisual.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        // Skip shadow twins and glow copies made by another live effect.
                        if (source.shadowCastingMode == ShadowCastingMode.ShadowsOnly || source.sharedMesh == null ||
                            source.name.EndsWith(ProxySuffix, StringComparison.Ordinal)) continue;
                        var twin = new GameObject(source.name + ProxySuffix); twin.transform.SetParent(source.transform, false);
                        twin.layer = source.gameObject.layer;
                        var copy = twin.AddComponent<SkinnedMeshRenderer>();
                        copy.sharedMesh = source.sharedMesh; copy.rootBone = source.rootBone; copy.bones = source.bones;
                        copy.localBounds = source.localBounds; copy.updateWhenOffscreen = source.updateWhenOffscreen;
                        var shared = new Material[Mathf.Max(1, source.sharedMesh.subMeshCount)];
                        for (int i = 0; i < shared.Length; i++) shared[i] = material;
                        copy.sharedMaterials = shared; copy.shadowCastingMode = ShadowCastingMode.Off; copy.receiveShadows = false;
                        attachedProxies.Add(copy);
                    }
            }
            for (int i = attachedProxies.Count - 1; i >= 0; i--)
            {
                Renderer proxy = attachedProxies[i];
                if (proxy == null) { attachedProxies.RemoveAt(i); continue; }
                Renderer source = proxy.transform.parent != null ? proxy.transform.parent.GetComponent<Renderer>() : null;
                proxy.enabled = visible && source != null && source.enabled && source.gameObject.activeInHierarchy;
                if (!proxy.enabled) continue;
                block.Clear(); block.SetVector(ColorId, (Vector4)color); block.SetVector(UvId, new Vector4(1, 1, 0, 0)); block.SetVector(Uv2Id, new Vector4(1, 1, 0, 0));
                block.SetVector(ErosionId, new Vector4(0, 0, .1f, 0)); block.SetFloat(DepthPullId, 0);
                proxy.SetPropertyBlock(block);
            }
        }

        void OnDestroy()
        {
            foreach (Mesh mesh in trailMeshes) if (mesh != null) Destroy(mesh);
            foreach (Renderer proxy in attachedProxies) if (proxy != null) Destroy(proxy.gameObject);
            trailMeshes.Clear(); attachedProxies.Clear();
        }
    }
}
