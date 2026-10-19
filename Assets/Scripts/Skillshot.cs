using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class Skillshot : MonoBehaviour
    {
        public Combatant Owner { get; private set; }
        int castId;
        public Vector3 Origin { get; private set; }
        public Vector3 Direction { get; private set; }
        public float Range { get; private set; }
        public float Width { get; private set; }
        public float CreatedAt { get; private set; }
        public Vector3 Head { get { return Origin + Direction * traveled; } }
        public bool Launched { get { return Time.time >= launchAt; } }
        public Vector3 End { get { return Origin + Direction * Range; } }
        LaneWorld world;
        float damage, launchAt, stun, traveled;
        bool mark, pierce;
        bool reflected;
        GameObject warning, projectile;
        readonly HashSet<Combatant> hit = new HashSet<Combatant>();

        public void Initialize(LaneWorld lane, Combatant owner, Vector3 direction, float range, float width,
            float rawDamage, float delay, float stunDuration, bool appliesMark, bool piercing)
        {
            world = lane; Owner = owner; Origin = owner.Position; Direction = direction;
            castId = owner.SpellCastId;
            Range = range; Width = width; damage = rawDamage; stun = stunDuration; mark = appliesMark; pierce = piercing;
            CreatedAt = Time.time; launchAt = Time.time + delay;
            warning = world.Shape("Visible cast warning", PrimitiveType.Cube, transform,
                Origin + Direction * (range * 0.5f) + Vector3.up * 0.06f,
                new Vector3(width * 2, 0.025f, range), owner.Color * 0.65f);
            warning.transform.rotation = Quaternion.LookRotation(direction);
            projectile = world.Shape("Projectile", PrimitiveType.Sphere, transform, Origin + Vector3.up * 0.7f,
                new Vector3(width * 2, 0.25f, 0.6f), owner.Color);
            projectile.SetActive(false);
        }
        void Update()
        {
            if (world.RoundOver || Owner == null || !Owner.gameObject.activeInHierarchy || !Owner.Alive) { Destroy(gameObject); return; }
            if (world.Paused) return;
            if (!Launched) return;
            if (warning.activeSelf) { warning.SetActive(false); projectile.SetActive(true); }
            Vector3 from = Head;
            traveled = Mathf.Min(Range, traveled + Time.deltaTime * 18);
            Vector3 to = Head;
            projectile.transform.position = to + Vector3.up * 0.7f;
            // Order collisions along this frame's swept segment, so a non-piercing shot hits the nearest target.
            var victims = new List<Combatant>();
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !unit.Targetable || unit.Team == Owner.Team || hit.Contains(unit)) continue;
                float radius = Width + (unit.Mel != null && unit.Mel.CanIntercept(Owner) ? MelRules.WRadius : unit.Radius);
                if (CombatRules.SegmentDistanceSquared(unit.Position.x, unit.Position.z, from.x, from.z, to.x, to.z) <= radius * radius)
                    victims.Add(unit);
            }
            victims.Sort((a, b) => (a.Position - from).sqrMagnitude.CompareTo((b.Position - from).sqrMagnitude));
            foreach (Combatant unit in victims)
            {
                if (unit.Mel != null && unit.Mel.CanIntercept(Owner))
                {
                    Vector3 delta = Owner.Position - unit.Position; delta.y = 0;
                    damage *= unit.Mel.ReflectionFactor(!reflected && Owner.Archetype == Archetype.Brawler);
                    Owner = unit; castId = Owner.SpellCastId; reflected = true;
                    Direction = delta.sqrMagnitude > .0001f ? delta.normalized : -Direction;
                    Origin = unit.Position; traveled = 0; hit.Clear(); return;
                }
                hit.Add(unit); Owner.DealAbilityDamage(unit, damage, !reflected && Owner.Archetype == Archetype.Brawler ? DamageType.Physical : DamageType.Magic, castId, reflected ? 1 : -1);
                if (unit.Alive) { if (stun > 0) unit.Stun(stun); if (mark) unit.Mark(5); unit.Slow(0.7f, 1); }
                if (!pierce) { Destroy(gameObject); return; }
            }
            if (traveled >= Range) Destroy(gameObject);
        }
        public bool Threatens(Combatant unit)
        {
            float radius = Width + unit.Radius + 0.35f;
            Vector3 start = Launched ? Head : Origin;
            return Owner != null && Owner.Team != unit.Team &&
                CombatRules.SegmentDistanceSquared(unit.Position.x, unit.Position.z, start.x, start.z, End.x, End.z) < radius * radius;
        }
        void OnDestroy() { if (world != null) world.Shots.Remove(this); }
    }
}
