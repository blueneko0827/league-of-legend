using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Presentation only. The open polyline shared with BladeWall is the damage boundary.
    public sealed class IreliaRVisuals : MonoBehaviour
    {
        const string Root = IreliaGlbAssets.Root + "R/";
        const float BladeSpacing = .55f;
        readonly List<Transform> blades = new List<Transform>();
        readonly List<Vector3> plantedPositions = new List<Vector3>();
        readonly List<float> arrival = new List<float>();
        readonly List<Renderer> renderers = new List<Renderer>();
        MaterialPropertyBlock properties;
        static readonly int Opacity = Shader.PropertyToID("_Opacity"), Tint = Shader.PropertyToID("_Tint");
        float createdAt, lifetime;
        public int BladeCount { get { return blades.Count; } }
        void Awake() { properties = new MaterialPropertyBlock(); }

        public static IreliaRVisuals CreateWall(Transform parent, IReadOnlyList<Vector3> points, Vector3 impact, float duration, bool hostile)
        {
            var root = new GameObject("R official planted blades"); root.transform.SetParent(parent, false);
            var effect = root.AddComponent<IreliaRVisuals>(); effect.createdAt = Time.time; effect.lifetime = duration;
            Mesh mesh = Resources.Load<Mesh>(Root + "Blade"), floor = Resources.Load<Mesh>(Root + "FloorStrip");
            Material bladeMaterial = Resources.Load<Material>(Root + "Blade"), energy = Resources.Load<Material>(Root + "Energy");
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i], b = points[i + 1], direction = (b - a).normalized;
                var edge = new GameObject("R edge " + i); edge.transform.SetParent(root.transform, false);
                Renderer strip = MeshObject("Ground energy", edge.transform, floor, energy);
                strip.transform.position = (a + b) * .5f + Vector3.up * .035f;
                strip.transform.rotation = Quaternion.LookRotation(direction);
                strip.transform.localScale = new Vector3(PracticeAbilityGeometry.IreliaRWallHalfWidth * 2, 1, Vector3.Distance(a, b));
                effect.renderers.Add(strip);
                int count = Mathf.CeilToInt(Vector3.Distance(a, b) / BladeSpacing);
                int bladeCount = count + (i == points.Count - 2 ? 1 : 0);
                for (int j = 0; j < bladeCount; j++)
                {
                    // Share joints without duplicates, but include the final open endpoint.
                    Vector3 position = Vector3.Lerp(a, b, (float)j / count);
                    Renderer renderer = MeshObject("Planted R blade", edge.transform, mesh, bladeMaterial);
                    renderer.transform.position = position - Vector3.up * 1.6f;
                    renderer.transform.rotation = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up)) * Quaternion.Euler(0, 0, (j % 2 == 0 ? -1 : 1) * 9);
                    renderer.transform.localScale = new Vector3(2.5f, 1.45f + .12f * (j % 3), 1.45f);
                    effect.blades.Add(renderer.transform); effect.plantedPositions.Add(position - Vector3.up * .12f);
                    effect.arrival.Add(Mathf.Clamp01(Vector3.Distance(position, impact) / 6) * .16f);
                    effect.renderers.Add(renderer);
                }
            }
            Color tint = hostile ? new Color(1, .38f, .48f) : new Color(.35f, .72f, 1);
            effect.properties.SetColor(Tint, tint); effect.Present(); return effect;
        }

        public static GameObject CreateProjectile(Transform parent)
        {
            var root = new GameObject("R official flying blades"); root.transform.SetParent(parent, false);
            Mesh mesh = Resources.Load<Mesh>(Root + "Blade"); Material material = Resources.Load<Material>(Root + "Blade");
            for (int i = 0; i < 6; i++)
            {
                Transform blade = MeshObject("R missile blade", root.transform, mesh, material).transform;
                float x = (i - 2.5f) * .48f;
                blade.localPosition = new Vector3(x, .02f * (i % 2), -.65f - Mathf.Abs(x) * .4f);
                blade.localRotation = Quaternion.Euler(90, x * 22, 0);
                blade.localScale = new Vector3(1.8f, 1.25f, 1.25f);
            }
            return root;
        }

        static Renderer MeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
        }
        void Update() { Present(); }
        void Present()
        {
            float age = Time.time - createdAt;
            float fade = Mathf.Clamp01((lifetime - age) / .28f);
            for (int i = 0; i < blades.Count; i++)
            {
                float rise = Mathf.SmoothStep(0, 1, (age - arrival[i]) / .18f);
                blades[i].position = plantedPositions[i] - Vector3.up * ((1 - rise) * 1.6f + (1 - fade) * .35f);
            }
            properties.SetFloat(Opacity, Mathf.Clamp01(age / .12f) * fade);
            foreach (Renderer renderer in renderers) renderer.SetPropertyBlock(properties);
        }
    }
}
