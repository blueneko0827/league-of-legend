using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Ionian Fervor at four stacks, on Irelia's model: her blades glow orange-gold and her body
    // takes a gold rim, as in LoL (whose blade material blooms to Bloom_Color 1, 0.62, 0 at four
    // IreliaPassiveStacks; the project has no bloom pass, so an additive glow pass is drawn on top
    // of each skinned mesh). Fades in and out instead of popping. Presentation only.
    public sealed class IreliaPassiveGlow
    {
        const float FadeTime = .2f;
        static readonly int Intensity = Shader.PropertyToID("_Intensity");
        readonly List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
        readonly Transform model;
        readonly Material blades, body;
        readonly float bladesIntensity, bodyIntensity;
        float level, target;
        bool attached;

        public IreliaPassiveGlow(Transform model)
        {
            this.model = model;
            Shader shader = Shader.Find("MidLane/IreliaPassiveGlow");
            if (model == null || shader == null) return;
            // Blades: LoL's bloom color as a fill plus a brighter rim, slightly larger than the blade.
            blades = new Material(shader) { name = "Irelia passive glow (blades)" };
            blades.SetColor("_GlowColor", new Color(1, .62f, 0, 1));
            blades.SetFloat("_Fill", .45f); blades.SetFloat("_RimPower", 1.6f); blades.SetFloat("_Inflate", .012f);
            bladesIntensity = 1.1f;
            // Body: a warm gold rim around her silhouette.
            body = new Material(shader) { name = "Irelia passive glow (body)" };
            body.SetColor("_GlowColor", new Color(1, .7f, .25f, 1));
            body.SetFloat("_Fill", .06f); body.SetFloat("_RimPower", 2.4f); body.SetFloat("_Inflate", .004f);
            bodyIntensity = .85f;
        }

        public void Set(bool on) { target = on ? 1 : 0; }

        public void Tick(float deltaTime)
        {
            if (blades == null) return;
            level = Mathf.MoveTowards(level, target, deltaTime / FadeTime);
            if (level > 0 && !attached) Attach(true);
            if (level <= 0 && attached) Attach(false);
            float ease = level * level * (3 - 2 * level);
            blades.SetFloat(Intensity, bladesIntensity * ease);
            body.SetFloat(Intensity, bodyIntensity * ease);
        }

        public void Clear() { target = level = 0; if (attached) Attach(false); }

        void Attach(bool on)
        {
            attached = on;
            // Visible meshes only (the shadow proxies of UnitShadows are skipped), found once the
            // model is complete.
            if (renderers.Count == 0 && model != null)
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) renderers.Add(renderer);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                Material glow = renderer.name == "AnimatedBlades" ? blades : body;
                var materials = new List<Material>(renderer.sharedMaterials);
                materials.Remove(blades); materials.Remove(body);
                if (on) materials.Add(glow);
                renderer.sharedMaterials = materials.ToArray();
            }
        }

        public void Destroy()
        {
            Clear();
            if (blades != null) Object.Destroy(blades);
            if (body != null) Object.Destroy(body);
        }
    }
}
