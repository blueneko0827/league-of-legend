using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Loads the imported official particle data once per LaneWorld and owns the meshes and
    // materials built from it. Textures stay Resources assets.
    public sealed class RiotVfxLibrary : MonoBehaviour
    {
        public const string Root = "Vfx/Irelia/";
        public RiotVfxLibraryData Data { get; private set; }
        public Mesh Quad { get; private set; }
        public bool Loaded { get { return Data != null && Shader != null; } }
        public Shader Shader { get; private set; }
        readonly Dictionary<string, RiotVfxSystemData> systems = new Dictionary<string, RiotVfxSystemData>();
        readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        // Per-emitter cache: avoids building the string key for every particle every frame.
        readonly Dictionary<RiotVfxEmitterData, Material> emitterMaterials = new Dictionary<RiotVfxEmitterData, Material>();
        readonly Dictionary<RiotVfxEmitterData, Material> attachedMaterials = new Dictionary<RiotVfxEmitterData, Material>();
        readonly List<Object> owned = new List<Object>();
        static readonly int MainTex = Shader.PropertyToID("_MainTex"), ErosionTex = Shader.PropertyToID("_ErosionTex");
        static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend"), DstBlend = Shader.PropertyToID("_DstBlend");
        static readonly int Cull = Shader.PropertyToID("_Cull"), Premultiply = Shader.PropertyToID("_Premultiply");
        static readonly int UseErosion = Shader.PropertyToID("_UseErosion"), AlphaClip = Shader.PropertyToID("_AlphaClip");
        static readonly int Inflate = Shader.PropertyToID("_Inflate");
        static readonly int MultTex = Shader.PropertyToID("_MultTex"), UseMult = Shader.PropertyToID("_UseMult");
        static readonly int VertexColor = Shader.PropertyToID("_VertexColor"), Linearize = Shader.PropertyToID("_Linearize");
        static readonly int EdgeFade = Shader.PropertyToID("_EdgeFade");
        static readonly int ErosionMainUv = Shader.PropertyToID("_ErosionMainUV"), ErosionSlice = Shader.PropertyToID("_ErosionSlice");
        // erosionSliceWidth below this draws only a thin band of the erosion map above the
        // threshold (R_Hit_Aoe's lightning, 0.1); the usual width (1 and above) is plain erosion.
        const float NarrowSlice = .25f;
        static readonly int PaletteTex = Shader.PropertyToID("_PaletteTex"), UsePalette = Shader.PropertyToID("_UsePalette"), PaletteV = Shader.PropertyToID("_PaletteV");

        public static RiotVfxLibrary For(LaneWorld world)
        {
            var library = world.GetComponent<RiotVfxLibrary>();
            if (library == null) { library = world.gameObject.AddComponent<RiotVfxLibrary>(); library.Load(); }
            return library;
        }

        void Load()
        {
            Shader = Resources.Load<Shader>("Shaders/RiotParticle");
            TextAsset asset = Resources.Load<TextAsset>(Root + "IreliaVfx");
            if (asset == null || Shader == null)
            {
                Debug.LogWarning("Faltan los efectos oficiales importados (" + Root + "IreliaVfx / Shaders/RiotParticle).");
                return;
            }
            Data = JsonUtility.FromJson<RiotVfxLibraryData>(asset.text);
            if (Data.systems != null) foreach (RiotVfxSystemData system in Data.systems) systems[system.name] = system;
            Quad = CreateQuad();
            // Build every material and mesh up front so the first attack does not hitch.
            foreach (RiotVfxSystemData system in systems.Values)
                if (system.emitters != null)
                    foreach (RiotVfxEmitterData emitter in system.emitters)
                    {
                        if (emitter == null || emitter.Skipped) continue;
                        GetMaterial(emitter, emitter.primitive == "attachedMesh");
                        if (emitter.primitive == "mesh" || emitter.IsBeam) GetMesh(emitter.mesh);
                    }
        }

        public RiotVfxSystemData FindSystem(string name)
        {
            RiotVfxSystemData system;
            return name != null && systems.TryGetValue(name, out system) ? system : null;
        }

        // Riot quads span -1..1 (birthScale is a half size); LoL texture space has v down.
        // u runs toward local -X because the whole effect is mirrored on X like the models.
        Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "Riot VFX quad" };
            mesh.vertices = new[] { new Vector3(-1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0), new Vector3(-1, -1, 0) };
            mesh.uv = new[] { new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2);
            owned.Add(mesh);
            return mesh;
        }

        public Mesh GetMesh(string name)
        {
            if (string.IsNullOrEmpty(name) || Data == null || Data.meshes == null) return null;
            Mesh mesh;
            if (meshes.TryGetValue(name, out mesh)) return mesh;
            RiotVfxMeshData source = System.Array.Find(Data.meshes, m => m.name == name);
            if (source != null && source.positions != null && source.indices != null)
            {
                int count = source.positions.Length / 3;
                var vertices = new Vector3[count]; var uvs = new Vector2[count]; var colors = new Color[count];
                for (int i = 0; i < count; i++)
                {
                    vertices[i] = new Vector3(source.positions[i * 3], source.positions[i * 3 + 1], source.positions[i * 3 + 2]);
                    uvs[i] = source.uvs != null && source.uvs.Length >= count * 2 ? new Vector2(source.uvs[i * 2], source.uvs[i * 2 + 1]) : Vector2.zero;
                    colors[i] = source.colors != null && source.colors.Length >= count * 4 ?
                        new Color(source.colors[i * 4], source.colors[i * 4 + 1], source.colors[i * 4 + 2], source.colors[i * 4 + 3]) : Color.white;
                }
                mesh = new Mesh { name = "Riot VFX " + name };
                mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colors; mesh.triangles = source.indices;
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                owned.Add(mesh);
            }
            meshes[name] = mesh;
            return mesh;
        }

        public Texture2D GetTexture(string name)
        {
            if (string.IsNullOrEmpty(name)) return Texture2D.whiteTexture;
            Texture2D texture;
            if (!textures.TryGetValue(name, out texture))
            {
                texture = Resources.Load<Texture2D>(Root + "Textures/" + name);
                textures[name] = texture;
            }
            return texture != null ? texture : Texture2D.whiteTexture;
        }

        // Blend modes of VfxEmitterDefinitionData.blendMode.
        public Material GetMaterial(RiotVfxEmitterData emitter, bool attached = false)
        {
            Dictionary<RiotVfxEmitterData, Material> cache = attached ? attachedMaterials : emitterMaterials;
            Material cached;
            if (cache.TryGetValue(emitter, out cached) && cached != null) return cached;
            cached = BuildMaterial(emitter, attached);
            cache[emitter] = cached;
            return cached;
        }

        Material BuildMaterial(RiotVfxEmitterData emitter, bool attached)
        {
            string key = emitter.texture + "|" + emitter.blend + "|" + emitter.twoSided + "|" + emitter.pass + "|" + emitter.groundLayer + "|" + attached +
                "|" + (emitter.erosion != null ? emitter.erosion.map + "#" + SliceWidth(emitter) : "") + "|" + emitter.alphaRef +
                "|" + (emitter.textureMult != null ? emitter.textureMult.texture : "") +
                "|" + (emitter.palette != null && emitter.palette.Present ? emitter.palette.texture + "#" + emitter.palette.index + "/" + emitter.palette.count : "");
            Material material;
            if (materials.TryGetValue(key, out material) && material != null) return material;
            material = new Material(Shader) { name = "Riot VFX " + emitter.name };
            material.SetTexture(MainTex, GetTexture(emitter.texture));
            BlendMode src = BlendMode.SrcAlpha, dst = BlendMode.OneMinusSrcAlpha; bool premultiply = false;
            switch (emitter.blend)
            {
                case 0: src = BlendMode.One; dst = BlendMode.One; premultiply = true; break;             // additive (premultiplied)
                case 2: src = BlendMode.Zero; dst = BlendMode.OneMinusSrcColor; premultiply = true; break; // darken
                case 3: src = BlendMode.One; dst = BlendMode.Zero; break;                                 // opaque
                case 4: case 6: case 7: src = BlendMode.SrcAlpha; dst = BlendMode.One; break;              // additive
                case 5: src = BlendMode.One; dst = BlendMode.OneMinusSrcAlpha; break;                     // premultiplied
            }
            material.SetFloat(SrcBlend, (float)src); material.SetFloat(DstBlend, (float)dst);
            material.SetFloat(Premultiply, premultiply ? 1 : 0);
            // LoL blends in gamma space. Additive glows keep their raw values (on mid-tone ground this
            // matches LoL's brightness); alpha-type blends are converted to linear exactly.
            bool additive = emitter.blend == 0 || emitter.blend == 4 || emitter.blend == 6 || emitter.blend == 7;
            material.SetFloat(Linearize, additive ? 0 : 1);
            material.SetFloat(EdgeFade, EdgeFadeMode(emitter));
            // Riot meshes and quads are authored for both sides unless culling is explicit;
            // the X mirror also flips their winding, so draw both faces.
            material.SetFloat(Cull, (float)CullMode.Off);
            material.SetFloat(AlphaClip, emitter.alphaRef > 0 ? emitter.alphaRef / 255f : 0);
            material.SetFloat(Inflate, attached ? .012f : 0);
            material.SetFloat(VertexColor, attached ? 0 : 1);
            bool erosion = emitter.erosion != null && !string.IsNullOrEmpty(emitter.erosion.map);
            material.SetFloat(UseErosion, erosion ? 1 : 0);
            if (erosion)
            {
                material.SetTexture(ErosionTex, GetTexture(emitter.erosion.map));
                // An emitter eroded by its own texture erodes what it draws: the map scrolls and
                // tiles with the texture (R_Hit_Aoe's pulse sweeps its lightning out to the wall).
                material.SetFloat(ErosionMainUv, emitter.erosion.map == emitter.texture ? 1 : 0);
                material.SetFloat(ErosionSlice, SliceWidth(emitter));
            }
            bool mult = emitter.textureMult != null && emitter.textureMult.Present;
            material.SetFloat(UseMult, mult ? 1 : 0);
            if (mult) material.SetTexture(MultTex, GetTexture(emitter.textureMult.texture));
            bool palette = emitter.palette != null && emitter.palette.Present;
            material.SetFloat(UsePalette, palette ? 1 : 0);
            if (palette)
            {
                // Rows run top to bottom in the source image; Unity's v runs bottom to top.
                material.SetTexture(PaletteTex, GetTexture(emitter.palette.texture));
                material.SetFloat(PaletteV, 1 - (Mathf.Clamp(emitter.palette.index, 0, emitter.palette.count - 1) + .5f) / emitter.palette.count);
            }
            // LoL draws ground-layer particles (rings, dark discs, beams on the floor) before the
            // others, so a floor decal never covers a shield or a blade standing above it.
            material.renderQueue = emitter.groundLayer ? 2600 + Mathf.Clamp(emitter.pass, -90, 90)
                : 3000 + Mathf.Clamp(emitter.pass, -400, 400);
            materials[key] = material; owned.Add(material);
            return material;
        }

        static float SliceWidth(RiotVfxEmitterData e)
        {
            float width = e.erosion != null ? e.erosion.sliceWidth : 0;
            return width > 0 && width < NarrowSlice ? width : 0;
        }

        // Billboards and quads get a soft border so a texture that is not fully transparent at
        // its edge (DXT/mip bleeding, wrap) never shows a box. Scrolled, offset or multiplied
        // textures tile across the quad, so they get an elliptical fade (the passive aura).
        static float EdgeFadeMode(RiotVfxEmitterData e)
        {
            if (e.IsBeam) return 3;
            if (e.primitive != "billboard" && e.primitive != "quad" && e.primitive != "ray") return 0;
            bool tiled = e.uvMode == 2 || (e.textureMult != null && e.textureMult.Present) ||
                (e.uvScroll != null && e.uvScroll.Present) || (e.birthUvScroll != null && e.birthUvScroll.Present) ||
                (e.birthUvOffset != null && e.birthUvOffset.Present);
            return tiled ? 2 : 1;
        }

        void OnDestroy()
        {
            foreach (Object item in owned) if (item != null) Destroy(item);
            owned.Clear(); materials.Clear(); meshes.Clear(); emitterMaterials.Clear(); attachedMaterials.Clear();
        }
    }
}
