using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Presentation only. The official champion, dummy and tower materials use URP/Unlit,
    // which keeps Riot's painted colors but has no shadow-caster pass, so those units cast
    // no shadow on the lane. Each visible renderer gets an invisible "shadows only" twin that
    // shares its mesh and bones: the original material and colors stay untouched.
    public sealed class UnitShadows : MonoBehaviour
    {
        static readonly Color MaterialKey = new Color(0.0123f, 0.0123f, 0.0123f, 1);
        readonly List<Renderer> sources = new List<Renderer>();
        readonly List<Renderer> proxies = new List<Renderer>();

        // Call after the visual has been fitted and tilted, so the twins are not measured.
        public static void Attach(LaneWorld world, GameObject visual)
        {
            if (world == null || visual == null || world.Presentation == null || !world.Presentation.unit_shadows) return;
            Material material = world.Material(MaterialKey);
            // Two-sided casting: several official meshes (cape, hair) are authored with Cull Off.
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
            var set = visual.GetComponent<UnitShadows>();
            if (set == null) set = visual.AddComponent<UnitShadows>();
            foreach (Renderer source in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (source == null || set.sources.Contains(source) || set.proxies.Contains(source) || source.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;
                Renderer proxy = CreateProxy(source, material);
                if (proxy == null) continue;
                set.sources.Add(source); set.proxies.Add(proxy);
            }
            set.LateUpdate();
        }

        static Renderer CreateProxy(Renderer source, Material material)
        {
            var skinned = source as SkinnedMeshRenderer;
            var meshRenderer = source as MeshRenderer;
            if (skinned == null && meshRenderer == null) return null;
            Mesh mesh = skinned != null ? skinned.sharedMesh : SharedMesh(meshRenderer);
            if (mesh == null) return null;
            var twin = new GameObject(source.name + " (sombra)");
            twin.transform.SetParent(source.transform, false);
            twin.layer = source.gameObject.layer;
            Renderer proxy;
            if (skinned != null)
            {
                var copy = twin.AddComponent<SkinnedMeshRenderer>();
                copy.sharedMesh = mesh; copy.rootBone = skinned.rootBone; copy.bones = skinned.bones;
                copy.localBounds = skinned.localBounds; copy.updateWhenOffscreen = skinned.updateWhenOffscreen;
                copy.quality = skinned.quality;
                proxy = copy;
            }
            else
            {
                twin.AddComponent<MeshFilter>().sharedMesh = mesh;
                proxy = twin.AddComponent<MeshRenderer>();
            }
            var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            proxy.sharedMaterials = materials;
            proxy.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            proxy.receiveShadows = false;
            proxy.lightProbeUsage = LightProbeUsage.Off;
            proxy.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return proxy;
        }

        static Mesh SharedMesh(MeshRenderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        // Hidden parts (for example blades disabled by an animation state) stop casting too.
        void LateUpdate()
        {
            for (int i = 0; i < proxies.Count; i++)
            {
                Renderer proxy = proxies[i];
                if (proxy == null) continue;
                Renderer source = sources[i];
                bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy;
                if (proxy.enabled != visible) proxy.enabled = visible;
            }
        }
    }
}
