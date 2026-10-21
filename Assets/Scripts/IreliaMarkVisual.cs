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
        RiotVfxPlayer official;
        // Either the official Q mark (Irelia_Base_Q_Mark_SelfOnly) or the fallback diamond.
        public bool Visible { get { return (outline != null && outline.enabled) || official != null; } }
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
            if (actor == null || outline == null || world.Paused) return;
            bool marked = actor.Alive && actor.Marked && world.Player != null && actor.Team != world.Player.Team;
            // The official mark loops for its authored 6 s; restart it while the mark lasts.
            if (marked && official == null && world.Presentation.official_vfx)
                official = RiotVfxPlayer.Play(world, "Irelia_Base_Q_Mark_SelfOnly",
                    IreliaVfx.TargetBonePose(actor, "C_Buffbone_Glb_Chest_Loc", Vector3.forward), 1, null, MarkBoundSize());
            if (!marked && official != null) { official.Stop(); official = null; }
            outline.enabled = marked && official == null;
            if (!outline.enabled) return;
            glyph.position = actor.VisualTop + Vector3.up * 1.05f;
            if (world.View != null) glyph.rotation = world.View.transform.rotation;
        }
        // The official mark lives under the world, so it must stop when this unit is hidden.
        // LoL draws the mark around the marked unit's upper body, about as wide as the unit is tall.
        // The mark scales with its FlexShape bound size (40 × 0.01 × size half-width).
        float MarkBoundSize() { return 1.125f * actor.VisualHeight / RiotVfxPlayer.UnitScale(world); }
        void OnDisable() { if (official != null) official.Stop(); official = null; }
        void OnDestroy()
        {
            if (official != null) official.Stop();
            if (material != null) Destroy(material);
        }
    }
}
