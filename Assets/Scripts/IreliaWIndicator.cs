using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // W (Defiant Dance) aiming indicator, as in LoL: a circle around Irelia (teal glow inside a thin
    // bright rim), the dashed side lines of the hit area, the arrow body, the target reticle (it
    // faces the camera) and the blade-shaped arrowhead. Presentation only; never part of combat.
    // Proportions are measured on a top-down view of in-game footage, in units of the circle's
    // radius R (the arrowhead's point is at 2.867 R). The side lines follow the W's hit width.
    // Textures: Tools/Make-IreliaWIndicator.py and Tools/Trace-IreliaWArrowhead.py.
    public sealed class IreliaWIndicator : MonoBehaviour
    {
        const string Root = IreliaGlbAssets.Root + "WAim/";
        const float Tip = 2.867f;                                  // range = Tip * R
        const float HeadStart = 1.55f, HeadEnd = 2.95f, HeadHalf = .42f, HeadEdge = .3515f;
        const float BodyStart = .12f, BodyWidth = .062f;
        const float EdgeStart = .35f, EdgeWidth = .012f, DashPeriod = .087f;
        const float ReticleAt = 2.217f, ReticleSize = .21f;
        Transform circle, leftEdge, rightEdge, line, reticle, head;
        Material leftEdgeMaterial, rightEdgeMaterial;
        Mesh ground, centered;
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
            float r = range / Tip, half = width * .5f;
            // The arrowhead is drawn with its side lines HeadEdge R off the axis; it is widened
            // with them to the W's hit width.
            float across = half / (HeadEdge * r);
            Place(circle, 0, -r, r * 2, r * 2, 0);
            float edgeLength = (HeadStart - EdgeStart) * r, edge = Mathf.Max(.025f, EdgeWidth * r);
            Place(leftEdge, -half, EdgeStart * r, edge, edgeLength, .001f);
            Place(rightEdge, half, EdgeStart * r, edge, edgeLength, .001f);
            Vector2 dashes = new Vector2(1, edgeLength / (DashPeriod * r));
            leftEdgeMaterial.SetTextureScale("_BaseMap", dashes); rightEdgeMaterial.SetTextureScale("_BaseMap", dashes);
            Place(line, 0, BodyStart * r, BodyWidth * r * across, (HeadStart - BodyStart) * r, .002f);
            Place(head, 0, HeadStart * r, HeadHalf * 2 * r * across, (HeadEnd - HeadStart) * r, .003f);
            reticle.localPosition = new Vector3(0, .02f, ReticleAt * r);
            reticle.localScale = Vector3.one * (ReticleSize * r);
            gameObject.SetActive(true);
            FaceCamera();
        }

        public void Hide() { gameObject.SetActive(false); }

        void LateUpdate() { FaceCamera(); }

        // LoL draws the reticle on the screen plane, round whatever the aim.
        void FaceCamera()
        {
            Camera view = Camera.main;
            if (view == null || reticle == null) return;
            reticle.rotation = Quaternion.LookRotation(view.transform.up, -view.transform.forward);
        }

        static void Place(Transform part, float x, float z, float width, float length, float lift)
        {
            part.localPosition = new Vector3(x, lift, z);
            part.localScale = new Vector3(width, 1, length);
        }

        void Build()
        {
            ground = Quad("W aiming ground quad", 0, 1);
            centered = Quad("W reticle quad", -.5f, .5f);
            Material unused;
            circle = Surface("W range circle", "Circle", 0, ground, out unused);
            leftEdge = Surface("W hit area edge (left)", "Edge", 1, ground, out leftEdgeMaterial);
            rightEdge = Surface("W hit area edge (right)", "Edge", 1, ground, out rightEdgeMaterial);
            line = Surface("W arrow", "Line", 2, ground, out unused);
            head = Surface("W arrowhead", "Head", 3, ground, out unused);
            reticle = Surface("W target reticle", "Reticle", 4, centered, out unused);
        }

        static Mesh Quad(string name, float near, float far)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(-.5f, 0, near), new Vector3(.5f, 0, near), new Vector3(-.5f, 0, far), new Vector3(.5f, 0, far) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; mesh.RecalculateBounds();
            return mesh;
        }

        Transform Surface(string label, string texture, int order, Mesh mesh, out Material material)
        {
            material = new Material(Shader.Find("MidLane/IreliaWAim"));
            material.SetTexture("_BaseMap", Resources.Load<Texture2D>(Root + texture));
            material.renderQueue = 2950 + order;
            materials.Add(material);
            var obj = new GameObject(label); obj.transform.SetParent(transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return obj.transform;
        }

        void OnDestroy()
        {
            if (ground != null) Destroy(ground);
            if (centered != null) Destroy(centered);
            foreach (Material material in materials) if (material != null) Destroy(material);
        }
    }
}
