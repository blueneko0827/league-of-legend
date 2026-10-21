using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Presentation only: the playable plane, towers and unit navigation remain in LaneWorld.
    public sealed class LaneTerrain : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        Material surfaceMaterial, rockMaterial;
        Texture2D rockTexture;

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
            rockTexture = CreateRockTexture();
            rockMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Painted verge rock" };
            rockMaterial.SetTexture("_BaseMap", rockTexture);
            rockMaterial.SetColor("_BaseColor", Color.white);
            rockMaterial.SetFloat("_Smoothness", 0.08f); rockMaterial.SetFloat("_Metallic", 0);
            var rockMeshes = new Mesh[4];
            for (int i = 0; i < rockMeshes.Length; i++) { rockMeshes[i] = CreateRock(random, i); meshes.Add(rockMeshes[i]); }
            for (int side = -1; side <= 1; side += 2)
                for (float x = -world.Settings.halfLength - 6; x <= world.Settings.halfLength + 6; x += 4.7f)
                {
                    if (random.NextDouble() < 0.2) continue;
                    float edge = world.Settings.halfWidth + 1.8f;
                    Vector3 position = new Vector3(x + Range(random, -0.8f, 0.8f), 0, side * (edge + Range(random, 0, 1.8f)));
                    // A main stone, sometimes a taller boulder, with a few smaller stones beside it.
                    bool boulder = random.NextDouble() < 0.25;
                    float size = boulder ? Range(random, 1.1f, 1.6f) : Range(random, 0.7f, 1.2f);
                    float height = boulder ? Range(random, 0.75f, 1.1f) : Range(random, 0.35f, 0.65f);
                    PlaceRock(rockMeshes[random.Next(rockMeshes.Length)], position, size, height, random);
                    int pebbles = random.Next(0, 3);
                    for (int p = 0; p < pebbles; p++)
                    {
                        float angle = Range(random, 0, Mathf.PI * 2), distance = size * Range(random, 0.9f, 1.4f);
                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                        // Keep loose stones off the playable lane.
                        if (Mathf.Abs(position.z + offset.z) < world.Settings.halfWidth + 0.6f) offset.z = -offset.z;
                        PlaceRock(rockMeshes[random.Next(rockMeshes.Length)], position + offset,
                            size * Range(random, 0.3f, 0.5f), Range(random, 0.3f, 0.5f), random);
                    }
                }
        }

        void PlaceRock(Mesh mesh, Vector3 position, float size, float height, System.Random random)
        {
            var rock = new GameObject("Weathered verge rock"); rock.transform.SetParent(transform, false);
            // Sink the base slightly so no stone appears to float on the painted grass.
            rock.transform.localPosition = position - Vector3.up * 0.04f;
            rock.transform.localRotation = Quaternion.Euler(Range(random, -6, 6), Range(random, 0, 360), Range(random, -6, 6));
            rock.transform.localScale = new Vector3(size * Range(random, 0.85f, 1.15f), height, size * Range(random, 0.6f, 0.95f));
            rock.AddComponent<MeshFilter>().sharedMesh = mesh;
            rock.AddComponent<MeshRenderer>().sharedMaterial = rockMaterial;
        }

        static float Range(System.Random random, float min, float max) { return Mathf.Lerp(min, max, (float)random.NextDouble()); }

        // Faceted stone: an irregular base ring, a shoulder ring and a slanted top cap.
        // UVs: side faces use the left half of the texture (stone, darker at the base),
        // upward faces use the right half (lighter stone with moss).
        static Mesh CreateRock(System.Random random, int variant)
        {
            int segments = 7 + variant % 3;
            var rings = new Vector3[3, segments];
            float lean = Range(random, -0.12f, 0.12f);
            for (int i = 0; i < segments; i++)
            {
                float angle = (i + Range(random, -0.25f, 0.25f)) * Mathf.PI * 2 / segments;
                float radius = Range(random, 0.82f, 1.05f);
                var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                rings[0, i] = dir * radius + Vector3.down * 0.05f;
                rings[1, i] = dir * radius * Range(random, 0.78f, 0.92f) + Vector3.up * Range(random, 0.38f, 0.52f) + Vector3.right * lean * 0.5f;
                rings[2, i] = dir * radius * Range(random, 0.42f, 0.6f) + Vector3.up * Range(random, 0.78f, 0.92f) + Vector3.right * lean;
            }
            Vector3 apex = new Vector3(lean, Range(random, 0.92f, 1.02f), Range(random, -0.08f, 0.08f));
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>();
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                for (int ring = 0; ring < 2; ring++)
                {
                    AddFace(vertices, uvs, rings[ring, i], rings[ring + 1, i], rings[ring + 1, next]);
                    AddFace(vertices, uvs, rings[ring, i], rings[ring + 1, next], rings[ring, next]);
                }
                AddFace(vertices, uvs, rings[2, i], apex, rings[2, next]);
            }
            var triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
            var mesh = new Mesh { name = "Verge stone facets " + variant };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static void AddFace(List<Vector3> vertices, List<Vector2> uvs, Vector3 a, Vector3 b, Vector3 c)
        {
            // Separate vertices per face keep the hard, hand-cut facets.
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            bool top = normal.y > 0.55f;
            foreach (Vector3 vertex in new[] { a, b, c })
            {
                vertices.Add(vertex);
                if (top) uvs.Add(new Vector2(0.55f + 0.4f * Mathf.Clamp01(vertex.x * 0.5f + 0.5f), 0.1f + 0.8f * Mathf.Clamp01(vertex.z * 0.5f + 0.5f)));
                else
                {
                    float around = Mathf.Atan2(vertex.z, vertex.x) / (Mathf.PI * 2) + 0.5f;
                    uvs.Add(new Vector2(0.03f + 0.42f * around, Mathf.Clamp01(vertex.y)));
                }
            }
        }

        static Texture2D CreateRockTexture()
        {
            const int width = 128, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true) { name = "Painted verge rock", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var pixels = new Color32[width * height];
            Color stone = new Color(0.36f, 0.4f, 0.45f), stoneLight = new Color(0.5f, 0.54f, 0.57f), stoneDark = new Color(0.17f, 0.2f, 0.24f);
            Color moss = new Color(0.27f, 0.39f, 0.2f), mossLight = new Color(0.4f, 0.52f, 0.25f);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1), v = y / (float)(height - 1);
                    float broad = Mathf.PerlinNoise(x * 0.07f + 3.1f, y * 0.11f + 7.3f);
                    float fine = Mathf.PerlinNoise(x * 0.35f + 11.7f, y * 0.35f + 1.9f);
                    // Painted strokes: a few dark cracks where the fine noise is near its middle.
                    float crack = Mathf.Clamp01(1 - Mathf.Abs(fine - 0.5f) * 14);
                    Color color;
                    if (u < 0.5f)
                    {
                        color = Color.Lerp(stone, stoneLight, broad * 0.7f);
                        // Darker toward the ground, lighter on the shoulder: fake ambient occlusion.
                        color *= Mathf.Lerp(0.55f, 1.08f, Mathf.SmoothStep(0, 1, v));
                        color = Color.Lerp(color, stoneDark, crack * 0.55f);
                        // Moss creeping over the upper edge.
                        float creep = Mathf.Clamp01((v - 0.72f) * 4) * Mathf.Clamp01(broad * 1.6f - 0.35f);
                        color = Color.Lerp(color, moss, creep);
                    }
                    else
                    {
                        float mossy = Mathf.SmoothStep(0, 1, Mathf.Clamp01(broad * 1.5f - 0.2f));
                        color = Color.Lerp(stoneLight, Color.Lerp(moss, mossLight, fine), mossy);
                        color = Color.Lerp(color, stoneDark, crack * 0.3f * (1 - mossy));
                    }
                    color.a = 1;
                    pixels[y * width + x] = color;
                }
            texture.SetPixels32(pixels); texture.Apply(true);
            return texture;
        }

        void OnDestroy()
        {
            foreach (Mesh mesh in meshes) Destroy(mesh);
            if (surfaceMaterial != null) Destroy(surfaceMaterial);
            if (rockMaterial != null) Destroy(rockMaterial);
            if (rockTexture != null) Destroy(rockTexture);
        }
    }
}
