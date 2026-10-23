using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class BladeWall : MonoBehaviour
    {
        LaneWorld world;
        BladeDuelist blade;
        Combatant reflectedOwner;
        float reflectionFactor = 1;
        Combatant DamageOwner { get { return reflectedOwner != null ? reflectedOwner : blade.Actor; } }
        public void SetReflection(Combatant actor, float factor) { reflectedOwner = actor; reflectionFactor = factor; }
        float expiresAt;
        int castId;
        Vector3[] points;
        public IReadOnlyList<Vector3> Points { get { return points; } }
        public int SegmentCount { get { return points.Length - 1; } }
        // Presentation: whose R it is, where it detonated, its flight and when the wall ends.
        public BladeDuelist Blade { get { return blade; } }
        public Vector3 Impact { get; private set; }
        public Vector3 Direction { get; private set; }
        public float ExpiresAt { get { return expiresAt; } }
        // The team its damage belongs to (another champion's after a reflection).
        public Team OwnerTeam { get { return DamageOwner.Team; } }
        readonly HashSet<Combatant> hit = new HashSet<Combatant>();
        readonly Dictionary<Combatant, Vector3> previous = new Dictionary<Combatant, Vector3>();
        public void Initialize(LaneWorld lane, BladeDuelist owner, Vector3 impact, Vector3 direction, int spellCastId = 0, Combatant reflectedSource = null, float reflectedMultiplier = 1)
        {
            world = lane; blade = owner; expiresAt = Time.time + blade.Tuning.rWallLifetime;
            reflectedOwner = reflectedSource; reflectionFactor = reflectedMultiplier;
            castId = spellCastId;
            Impact = impact; Direction = direction;
            points = IreliaRGeometry.WallPoints(impact, direction);
            // With official_vfx, IreliaVfx shows LoL's own wall (IreliaRBlades and its particles).
            if (!world.Presentation.official_vfx)
                IreliaRVisuals.CreateWall(transform, points, impact, blade.Tuning.rWallLifetime, DamageOwner.Team != world.PlayerTeam);
            foreach (Combatant unit in world.Units) if (unit != null) previous[unit] = unit.Position;
        }
        void Update()
        {
            if (world.RoundOver || blade == null || Time.time >= expiresAt) { Destroy(gameObject); return; }
            if (world.Paused) return;
            foreach (Combatant unit in world.Units)
            {
                if (!DamageOwner.ValidEnemy(unit) || unit.Kind != UnitKind.Champion || hit.Contains(unit)) continue;
                Vector3 last;
                if (!previous.TryGetValue(unit, out last)) last = unit.Position;
                bool crossed = false;
                for (int i = 0; i < SegmentCount; i++)
                {
                    Vector3 a = points[i], b = points[i + 1];
                    if (TouchesWall(last, unit.Position, a, b, unit.Radius)) { crossed = true; break; }
                }
                previous[unit] = unit.Position;
                if (!crossed) continue;
                hit.Add(unit); DamageOwner.DealAbilityDamage(unit, (50 + blade.Rank(3) * 75 + blade.Actor.AbilityPower) * reflectionFactor, DamageType.Magic, castId, reflectedOwner != null ? 1 : 3);
                if (blade.Actor.Audio != null) blade.Actor.Audio.RWallImpact();
                if (unit.Alive) unit.Slow(0.1f, blade.Tuning.rWallSlowDuration);
                blade.NotifyRWallHit(unit);
            }
        }
        public static bool TouchesWall(Vector3 from, Vector3 to, Vector3 a, Vector3 b, float bodyRadius)
        {
            float radius = bodyRadius + PracticeAbilityGeometry.IreliaRWallHalfWidth;
            float r2 = radius * radius;
            return Crossed(from, to, a, b) ||
                CombatRules.SegmentDistanceSquared(from.x, from.z, a.x, a.z, b.x, b.z) <= r2 ||
                CombatRules.SegmentDistanceSquared(to.x, to.z, a.x, a.z, b.x, b.z) <= r2 ||
                CombatRules.SegmentDistanceSquared(a.x, a.z, from.x, from.z, to.x, to.z) <= r2 ||
                CombatRules.SegmentDistanceSquared(b.x, b.z, from.x, from.z, to.x, to.z) <= r2;
        }
        static bool Crossed(Vector3 from, Vector3 to, Vector3 a, Vector3 b)
        {
            Vector3 move = to - from, edge = b - a;
            float denominator = move.x * edge.z - move.z * edge.x;
            if (Mathf.Abs(denominator) < 0.00001f) return false;
            Vector3 offset = a - from;
            float t = (offset.x * edge.z - offset.z * edge.x) / denominator;
            float u = (offset.x * move.z - offset.z * move.x) / denominator;
            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }
        void OnDestroy() { if (world != null) world.BladeWalls.Remove(this); }
    }
}
