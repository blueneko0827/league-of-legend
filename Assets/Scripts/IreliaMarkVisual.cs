using UnityEngine;
namespace MidLane
{
    // Reads the combat mark directly, including E/R hits and Q consumption.
    public sealed class IreliaMarkVisual : MonoBehaviour
    {
        LaneWorld world;
        Combatant actor;
        Transform glyph;
        LineRenderer outline;
        Material material;
        public bool Visible { get { return outline != null && outline.enabled; } }
        public void Initialize(LaneWorld lane, Combatant target)
        {
            world = lane; actor = target;
            glyph = new GameObject("Irelia active mark").transform;
            glyph.SetParent(transform, false);
            outline = glyph.gameObject.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader); material.color = new Color(0.15f, 1, 0.95f);
            outline.sharedMaterial = material;
            outline.useWorldSpace = false; outline.loop = true;
            outline.widthMultiplier = 0.075f; outline.positionCount = 4;
            outline.SetPositions(new[] { new Vector3(0, .48f, 0), new Vector3(.28f, 0, 0),
                new Vector3(0, -.48f, 0), new Vector3(-.28f, 0, 0) });
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false; outline.enabled = false;
        }
        void LateUpdate()
        {
            if (actor == null || outline == null) return;
            outline.enabled = actor.Alive && actor.Marked && actor.Team != world.Player.Team;
            if (!outline.enabled) return;
            glyph.position = actor.VisualTop + Vector3.up * 1.05f;
            if (world.View != null) glyph.rotation = world.View.transform.rotation;
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
