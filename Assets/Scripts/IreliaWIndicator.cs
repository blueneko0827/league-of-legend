using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // W (Defiant Dance) aiming indicator, drawn after LoL's: a circle around Irelia (teal glow
    // inside a bright rim), the two thin side lines of the hit area, a bright arrow ending in a
    // target reticle and a blade-shaped arrowhead. Presentation only; never part of the combat.
    // Textures: Tools/Make-IreliaWIndicator.py. Proportions measured on in-game footage.
    public sealed class IreliaWIndicator : MonoBehaviour
    {
        const string Root = IreliaGlbAssets.Root + "WAim/";
        const float CircleRadius = .355f, HeadLength = .26f, ReticleSize = .075f; // of the range
        const float LineWidth = .15f, EdgeWidth = .014f;                           // of the width
        Transform circle, leftEdge, rightEdge, line, reticle, head;
        Mesh ground;
        readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();
        public float Range { get; private set; }
        public float Width { get; private set; }
        public Vector3 Direction { get; private set; }

        public static IreliaWIndicator Create(Transform parent, string label)
        {
            var root = new GameObject(label); root.transform.SetParent(parent, false);
            var view = root.AddComponent<IreliaWIndicator>(); view.Build(); return view;
        }

        public void Present(Vector3 origin, Vector3 direction, float range, float width)
        {
            Range = range; Width = width; direction.y = 0;
            Direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.forward;
            transform.SetPositionAndRotation(origin + Vector3.up * .055f, Quaternion.LookRotation(Direction));
            float radius = CircleRadius * range, headLength = HeadLength * range, half = width * .5f;
            float edge = Mathf.Max(.03f, EdgeWidth * width), body = range - headLength, mark = ReticleSize * range;
            Place(circle, 0, -radius, radius * 2, radius * 2, 0);
            Place(leftEdge, -half, 0, edge, body, .001f);
            Place(rightEdge, half, 0, edge, body, .001f);
            Place(line, 0, 0, LineWidth * width, body, .002f);
            // The arch of the arrowhead spans 96% of its texture (Tools/Make-IreliaWIndicator.py).
            Place(head, 0, body, width / .96f, headLength, .003f);
            Place(reticle, 0, body - mark * .5f, mark, mark, .004f);
            gameObject.SetActive(true);
        }

        public void Hide() { gameObject.SetActive(false); }

        static void Place(Transform part, float x, float z, float width, float length, float lift)
        {
            part.localPosition = new Vector3(x, lift, z);
            part.localScale = new Vector3(width, 1, length);
        }

        void Build()
        {
            ground = new Mesh { name = "W aiming ground quad" };
            ground.vertices = new[] { new Vector3(-.5f, 0, 0), new Vector3(.5f, 0, 0), new Vector3(-.5f, 0, 1), new Vector3(.5f, 0, 1) };
            ground.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            ground.triangles = new[] { 0, 2, 1, 1, 2, 3 }; ground.RecalculateBounds();
            circle = Surface("W range circle", "Circle", 0);
            leftEdge = Surface("W hit area edge (left)", "Edge", 1);
            rightEdge = Surface("W hit area edge (right)", "Edge", 1);
            line = Surface("W arrow", "Line", 2);
            head = Surface("W arrowhead", "Head", 3);
            reticle = Surface("W target reticle", "Reticle", 4);
        }

        Transform Surface(string label, string texture, int order)
        {
            var material = new Material(Shader.Find("MidLane/IreliaWAim"));
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>(Root + texture));
            material.renderQueue = 2950 + order;
            materials.Add(material);
            var obj = new GameObject(label); obj.transform.SetParent(transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = ground;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return obj.transform;
        }

        void OnDestroy()
        {
            if (ground != null) Destroy(ground);
            foreach (Material material in materials) if (material != null) Destroy(material);
        }
    }
}
