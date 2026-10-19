using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Presentation only: BladeLink remains the sole authority for impact and collision.
    public sealed class IreliaEConvergenceVisual : MonoBehaviour
    {
        const string Root = IreliaGlbAssets.Root + "E/";
        public const float FadeDuration = .18f;
        public bool Finishing { get; private set; }
        public Vector3 First { get; private set; }
        public Vector3 Second { get; private set; }
        Renderer beam, impact;
        readonly Renderer[] missiles = new Renderer[2], trails = new Renderer[2];
        MaterialPropertyBlock properties;
        float convergeAt, impactAt, width;
        Color tint;

        public static IreliaEConvergenceVisual Create(Transform parent, Vector3 first, Vector3 second,
            float convergence, float collision, float fullWidth, bool hostile)
        {
            var obj = new GameObject("E official convergence"); obj.transform.SetParent(parent, false);
            var effect = obj.AddComponent<IreliaEConvergenceVisual>();
            effect.First = first; effect.Second = second; effect.convergeAt = convergence;
            effect.impactAt = collision; effect.width = fullWidth;
            effect.properties = new MaterialPropertyBlock();
            effect.tint = hostile ? new Color(1, .4f, .5f) : new Color(.5f, .8f, 1);
            Mesh mesh = Resources.Load<Mesh>(Root + "Beam");
            Mesh strip = Resources.Load<Mesh>(IreliaGlbAssets.Root + "R/FloorStrip");
            effect.beam = Make("Official E beam", obj.transform, mesh, "Beam");
            effect.impact = Make("Official E impact", obj.transform, strip, "Impact");
            for (int i = 0; i < 2; i++)
            {
                effect.missiles[i] = Make("E converging missile " + i, obj.transform, strip, "Missile");
                effect.trails[i] = Make("E convergence trail " + i, obj.transform, strip, "Trail");
            }
            effect.Present(); return effect;
        }
        static Renderer Make(string name, Transform parent, Mesh mesh, string material)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = Resources.Load<Material>(Root + material);
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
        }
        public void Finish()
        {
            // Preserve the short afterglow while the gameplay object is removed immediately.
            Finishing = true; transform.SetParent(transform.parent.parent, true); Present();
        }
        void Update()
        {
            Present();
            if (Finishing && Time.time >= impactAt + FadeDuration) Destroy(gameObject);
        }
        void Present()
        {
            Vector3 center = (First + Second) * .5f;
            Vector3 delta = Second - First; float length = delta.magnitude;
            Vector3 direction = length > .001f ? delta / length : Vector3.right;
            float progress = Mathf.Clamp01((Time.time - convergeAt) / Mathf.Max(.001f, impactAt - convergeAt));
            bool afterImpact = Time.time >= impactAt;
            float fade = Mathf.Clamp01(1 - (Time.time - impactAt) / FadeDuration);
            Place(beam, center, direction, width, length, afterImpact ? fade * .7f : .12f + progress * .3f, .075f);
            Place(impact, center, direction, width, length, afterImpact ? fade : 0, .10f);
            for (int i = 0; i < 2; i++)
            {
                Vector3 start = i == 0 ? First : Second;
                Vector3 position = Vector3.Lerp(start, center, progress);
                Vector3 travel = (center - start).normalized;
                float opacity = Time.time >= convergeAt && !afterImpact ? 1 : 0;
                float missileLength = Mathf.Min(.85f, length * .5f);
                Place(missiles[i], position, travel, width * .8f, missileLength, opacity, .12f);
                Vector3 tail = Vector3.MoveTowards(position, start, Mathf.Min(1.8f, length * progress * .5f));
                Place(trails[i], (tail + position) * .5f, travel, width, Vector3.Distance(tail, position), opacity * .75f, .095f);
            }
        }
        void Place(Renderer renderer, Vector3 point, Vector3 direction, float span, float length, float opacity, float height)
        {
            renderer.enabled = opacity > .001f && length > .001f;
            renderer.transform.position = point + Vector3.up * height;
            renderer.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > .001f ? direction : Vector3.right);
            renderer.transform.localScale = new Vector3(span, 1, Mathf.Max(.001f, length));
            properties.SetColor("_Tint", tint); properties.SetFloat("_Opacity", opacity); renderer.SetPropertyBlock(properties);
        }
    }
}
