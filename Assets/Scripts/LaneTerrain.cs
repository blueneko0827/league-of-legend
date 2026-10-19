using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Presentation only: the playable plane, towers and unit navigation remain in LaneWorld.
    public sealed class LaneTerrain : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        Material surfaceMaterial;

        public void Build(LaneWorld world)
        {
            float length = TowerToTowerRules.HalfLength * 2;
            float width = world.Settings.halfWidth * 2;
            var surface = new GameObject("Painted stone and grass");
            surface.transform.SetParent(transform, false);
            Mesh mesh = new Mesh { name = "Mid lane ground" };
            // World-space texel density: changing lane length never stretches the flagstones.
            float repeats = length / (width * 1.5f);
            // Extend only the grassy bands beyond the road. Mirroring their UVs keeps the
            // apron connected to the same painted edge instead of exposing a rectangular mat.
            float[] rows = { -2.5f, -2.25f, -2, -1.75f, -1.5f, -1.25f, -1, -0.75f, -0.5f,
                0.5f, 0.75f, 1, 1.25f, 1.5f, 1.75f, 2, 2.25f, 2.5f };
            float[] grassV = { 0, 0.2f, 0, 0.2f, 0, 0.2f, 0, 0.2f, 0,
                1, 0.8f, 1, 0.8f, 1, 0.8f, 1, 0.8f, 1 };
            var vertices = new Vector3[rows.Length * 2]; var uv = new Vector2[vertices.Length];
            var triangles = new List<int>();
            for (int row = 0; row < rows.Length; row++)
            {
                int index = row * 2;
                vertices[index] = new Vector3(-length / 2, -0.025f, rows[row] * width);
                vertices[index + 1] = new Vector3(length / 2, -0.025f, rows[row] * width);
                uv[index] = new Vector2(0, grassV[row]); uv[index + 1] = new Vector2(repeats, grassV[row]);
                if (row + 1 < rows.Length) triangles.AddRange(new[] { index, index + 2, index + 3, index, index + 3, index + 1 });
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshes.Add(mesh); surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surfaceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            surfaceMaterial.name = "Hand painted mid lane";
            surfaceMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>("Terrain/MidLaneAlbedo"));
            surfaceMaterial.SetColor("_BaseColor", new Color(0.8f, 0.82f, 0.78f));
            surfaceMaterial.SetFloat("_Smoothness", 0); surfaceMaterial.SetFloat("_Metallic", 0);
            surface.AddComponent<MeshRenderer>().sharedMaterial = surfaceMaterial;

            // The road ends at the turret centers. Only grassy scenery continues outside
            // the playable rectangle, so there is no implied route behind either tower.
            for (int side = -1; side <= 1; side += 2)
            {
                var apron = new Mesh { name = "Grass beyond lane end" };
                float edge = side * length / 2, outer = edge + side * 30;
                float left = Mathf.Min(edge, outer), right = Mathf.Max(edge, outer);
                const int bands = 24;
                var grassVertices = new Vector3[(bands + 1) * 2]; var grassUv = new Vector2[grassVertices.Length];
                var grassTriangles = new List<int>();
                for (int row = 0; row <= bands; row++)
                {
                    int index = row * 2; float z = Mathf.Lerp(-width * 2.5f, width * 2.5f, row / (float)bands);
                    grassVertices[index] = new Vector3(left, -0.025f, z); grassVertices[index + 1] = new Vector3(right, -0.025f, z);
                    float v = row % 2 == 0 ? 0.02f : 0.16f;
                    grassUv[index] = new Vector2((left + length / 2) / (width * 1.5f), v);
                    grassUv[index + 1] = new Vector2((right + length / 2) / (width * 1.5f), v);
                    if (row < bands) grassTriangles.AddRange(new[] { index, index + 2, index + 3, index, index + 3, index + 1 });
                }
                apron.vertices = grassVertices; apron.uv = grassUv; apron.triangles = grassTriangles.ToArray();
                apron.RecalculateNormals(); apron.RecalculateBounds(); meshes.Add(apron);
                var grass = new GameObject("Grass beyond " + (side < 0 ? "Blue" : "Red") + " turret"); grass.transform.SetParent(transform, false);
                grass.AddComponent<MeshFilter>().sharedMesh = apron; grass.AddComponent<MeshRenderer>().sharedMaterial = surfaceMaterial;
            }

            // Deterministic scenery uses its own generator, without changing the match's RNG.
            var random = new System.Random(72014);
            Mesh rockMesh = CreateRock(); meshes.Add(rockMesh);
            for (int side = -1; side <= 1; side += 2)
                for (float x = -world.Settings.halfLength - 6; x <= world.Settings.halfLength + 6; x += 4.7f)
                {
                    if (random.NextDouble() < 0.2) continue;
                    float edge = world.Settings.halfWidth + 1.8f;
                    Vector3 position = new Vector3(x + Range(random, -0.8f, 0.8f), 0, side * (edge + Range(random, 0, 1.8f)));
                    var rock = new GameObject("Weathered verge rock"); rock.transform.SetParent(transform, false);
                    rock.transform.localPosition = position;
                    rock.transform.localRotation = Quaternion.Euler(0, Range(random, 0, 360), 0);
                    rock.transform.localScale = new Vector3(Range(random, 0.8f, 1.8f), Range(random, 0.25f, 0.55f), Range(random, 0.5f, 1.1f));
                    rock.AddComponent<MeshFilter>().sharedMesh = rockMesh;
                    rock.AddComponent<MeshRenderer>().sharedMaterial = world.Material(new Color(0.22f, 0.28f, 0.25f));
                }
        }

        static float Range(System.Random random, float min, float max) { return Mathf.Lerp(min, max, (float)random.NextDouble()); }

        static Mesh CreateRock()
        {
            var vertices = new Vector3[15]; var triangles = new List<int>();
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2 / 7;
                vertices[i] = new Vector3(Mathf.Cos(angle), -0.05f, Mathf.Sin(angle));
                vertices[i + 7] = new Vector3(Mathf.Cos(angle) * 0.63f + 0.12f, 0.55f + 0.12f * Mathf.Sin(i * 2.1f), Mathf.Sin(angle) * 0.63f);
            }
            vertices[14] = new Vector3(0.08f, 0.68f, 0);
            for (int i = 0; i < 7; i++)
            {
                int next = (i + 1) % 7;
                triangles.AddRange(new[] { i, i + 7, next + 7, i, next + 7, next, i + 7, 14, next + 7 });
            }
            // Separate the faces for weathered facets instead of a smooth plastic silhouette.
            var faces = new Vector3[triangles.Count]; var indices = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++) { faces[i] = vertices[triangles[i]]; indices[i] = i; }
            var mesh = new Mesh { name = "Verge stone facets", vertices = faces, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        void OnDestroy()
        {
            foreach (Mesh mesh in meshes) Destroy(mesh);
            if (surfaceMaterial != null) Destroy(surfaceMaterial);
        }
    }
}
