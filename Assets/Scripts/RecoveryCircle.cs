using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    public sealed class RecoveryCircle : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> materials = new List<Material>();
        public float Radius { get; private set; }
        public void Build(Team team, float radius)
        {
            Radius = radius;
            Color tint = team == Team.Blue ? new Color(0.12f, 0.65f, 1) : new Color(1, 0.22f, 0.38f);
            var floor = new Mesh { name = "Recovery aura floor" };
            float edge = radius + 0.2f;
            floor.vertices = new[] { new Vector3(-edge, 0.025f, -edge), new Vector3(-edge, 0.025f, edge),
                new Vector3(edge, 0.025f, edge), new Vector3(edge, 0.025f, -edge) };
            floor.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            floor.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            AddMesh("Recovery ring", floor, tint, 0, radius / edge);
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var random = new System.Random(8401 + (int)team);
            for (int i = 0; i < 80; i++)
            {
                float angle = i * Mathf.PI * 2 / 80;
                Vector3 center = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)) * (0.016f + (float)random.NextDouble() * 0.025f);
                float height = 0.2f + (float)random.NextDouble() * 0.8f;
                int start = vertices.Count;
                vertices.Add(center - tangent + Vector3.up * 0.03f); vertices.Add(center - tangent + Vector3.up * height);
                vertices.Add(center + tangent + Vector3.up * height); vertices.Add(center + tangent + Vector3.up * 0.03f);
                uv.Add(Vector2.zero); uv.Add(Vector2.up); uv.Add(Vector2.one); uv.Add(Vector2.right);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var beams = new Mesh { name = "Recovery light columns" };
            beams.SetVertices(vertices); beams.SetUVs(0, uv); beams.SetTriangles(triangles, 0);
            AddMesh("Recovery rising light", beams, tint, 1, 1);
        }
        void AddMesh(string label, Mesh mesh, Color color, float mode, float radius)
        {
            mesh.RecalculateBounds(); meshes.Add(mesh);
            var visual = new GameObject(label); visual.transform.SetParent(transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Resources.Load<Shader>("Shaders/RecoveryAura"));
            material.SetColor("_Tint", color); material.SetFloat("_Mode", mode); material.SetFloat("_Radius", radius); materials.Add(material);
            var renderer = visual.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
        void OnDestroy()
        {
            foreach (Mesh mesh in meshes) Destroy(mesh);
            foreach (Material material in materials) Destroy(material);
        }
    }
}
