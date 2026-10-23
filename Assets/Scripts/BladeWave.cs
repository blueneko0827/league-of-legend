using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class BladeWave : MonoBehaviour
    {
        public Vector3 Head { get { return origin + direction * traveled; } }
        public BladeDuelist Blade { get { return blade; } }
        public bool IsUltimate { get { return ultimate; } }
        public float CreatedAt { get; private set; }
        public float Range { get { return range; } }
        // Presentation: the R's blades have left Irelia (end of the cast).
        public bool Launched { get { return launched; } }
        LaneWorld world;
        BladeDuelist blade;
        Combatant reflectedOwner;
        float reflectionFactor = 1;
        public Combatant DamageOwner { get { return reflectedOwner != null ? reflectedOwner : blade.Actor; } }
        Vector3 origin, direction;
        float damage, delayUntil, traveled, range, width;
        bool ultimate;
        bool launched;
        bool impactSoundPlayed;
        long launchSound;
        int castId;
        GameObject visual;
        readonly HashSet<Combatant> hit = new HashSet<Combatant>();

        public void Initialize(LaneWorld lane, BladeDuelist owner, Vector3 heading, bool isUltimate, float rawDamage)
        {
            world = lane; blade = owner; direction = heading; ultimate = isUltimate; damage = rawDamage;
            castId = owner.Actor.SpellCastId;
            origin = blade.Actor.Position; CreatedAt = Time.time;
            range = ultimate ? blade.Tuning.rRange : blade.Tuning.wRange;
            width = ultimate ? blade.Tuning.rHalfWidth : blade.Tuning.wHalfWidth;
            delayUntil = Time.time + (ultimate ? blade.Tuning.rCastTime : blade.Tuning.wReleaseTime);
            visual = ultimate ? IreliaRVisuals.CreateProjectile(transform) : world.Shape("W Danza de Insurrección", PrimitiveType.Cube, transform,
                origin + Vector3.up * 0.65f, new Vector3(width * 2, 0.2f, 0.5f), blade.Actor.Color);
            visual.transform.rotation = Quaternion.LookRotation(direction); visual.SetActive(false);
            // The official W slash and R missile particles (IreliaVfx) replace the placeholder
            // bar and the hand-made R blades.
            if (world.Presentation.official_vfx)
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        }
        void Update()
        {
            if (world.RoundOver || blade == null) { Destroy(gameObject); return; }
            if (world.Paused) return;
            if (!launched)
            {
                // R + Flash launches from Irelia's position at the end of the cast.
                if (ultimate) origin = blade.Actor.Position;
                if (Time.time < delayUntil) return;
                launched = true;
                if (ultimate && blade.Actor.Audio != null) launchSound = blade.Actor.Audio.RLaunched();
                // The proximity check is not a projectile. Resolve it at the end of
                // the cast from the updated origin, preserving the R + Flash combo.
                if (ultimate && CheckOrigin()) return;
            }
            visual.SetActive(true);
            Vector3 from = Head;
            traveled = Mathf.Min(range, traveled + (ultimate ? blade.Tuning.rSpeed * Time.deltaTime : range));
            Vector3 to = Head;
            visual.transform.position = to + Vector3.up * 0.65f;
            var victims = new List<Combatant>();
            Vector3 side = Vector3.Cross(direction, Vector3.up);
            float start = Vector3.Dot(from - origin, direction);
            foreach (Combatant target in world.Units)
            {
                if (!DamageOwner.ValidEnemy(target) || target.Kind == UnitKind.Tower || hit.Contains(target)) continue;
                Vector3 offset = target.Position - origin;
                float body = target.Mel != null && target.Mel.CanIntercept(DamageOwner) ? MelRules.WRadius : target.Radius;
                if (CombatRules.CircleIntersectsRectangle(Vector3.Dot(offset, direction), Vector3.Dot(offset, side), body,
                    start, traveled, width))
                    victims.Add(target);
            }
            victims.Sort((a, b) => (a.Position - from).sqrMagnitude.CompareTo((b.Position - from).sqrMagnitude));
            foreach (Combatant target in victims)
            {
                if (target.Mel != null && target.Mel.CanIntercept(DamageOwner))
                {
                    Combatant previousOwner = DamageOwner;
                    reflectionFactor *= target.Mel.ReflectionFactor(!ultimate && reflectedOwner == null);
                    reflectedOwner = target; castId = target.SpellCastId;
                    Vector3 delta = previousOwner.Position - target.Position; delta.y = 0;
                    direction = delta.sqrMagnitude > .00001f ? delta.normalized : -direction;
                    origin = target.Position; traveled = 0; hit.Clear();
                    visual.transform.rotation = Quaternion.LookRotation(direction);
                    return;
                }
                Apply(target);
                if (!ultimate || target.Kind != UnitKind.Champion) continue;
                Detonate(target.Position); return;
            }
            if (traveled >= range)
            {
                if (reflectedOwner == null) blade.AwardSpellHits(hit);
                Destroy(gameObject);
            }
        }
        bool CheckOrigin()
        {
            var victims = new List<Combatant>();
            foreach (Combatant target in world.Units)
            {
                if (!blade.Actor.ValidEnemy(target) || target.Kind == UnitKind.Tower) continue;
                float radius = PracticeAbilityGeometry.IreliaROriginRadius + target.Radius;
                if ((target.Position - origin).sqrMagnitude <= radius * radius) victims.Add(target);
            }
            victims.Sort((a, b) => (a.Position - origin).sqrMagnitude.CompareTo((b.Position - origin).sqrMagnitude));
            foreach (Combatant target in victims)
            {
                Apply(target);
                if (target.Kind != UnitKind.Champion) continue;
                Detonate(target.Position); return true;
            }
            return false;
        }
        void Detonate(Vector3 center)
        {
            if (blade.Actor.Audio != null) blade.Actor.Audio.RImpact(true);
            foreach (Combatant nearby in world.Units)
            {
                if (!DamageOwner.ValidEnemy(nearby) || nearby.Kind == UnitKind.Tower || hit.Contains(nearby)) continue;
                float radius = PracticeAbilityGeometry.IreliaRExplosionRadius + nearby.Radius;
                if ((nearby.Position - center).sqrMagnitude <= radius * radius) Apply(nearby);
            }
            if (reflectedOwner == null) blade.AwardSpellHits(hit);
            // Structural deployment is independent of damage absorbed by a shield.
            world.CreateBladeWall(blade, center, direction, castId, reflectedOwner, reflectionFactor);
            Destroy(gameObject);
        }
        void Apply(Combatant target)
        {
            if (!impactSoundPlayed && (!ultimate || target.Kind != UnitKind.Champion) && blade.Actor.Audio != null)
            {
                impactSoundPlayed = true;
                if (ultimate) blade.Actor.Audio.RImpact(false); else blade.Actor.Audio.WImpact();
            }
            if (!ultimate && reflectedOwner == null) blade.NotifyWHit(target);
            hit.Add(target); DamageOwner.DealAbilityDamage(target, damage * reflectionFactor,
                ultimate || reflectedOwner != null ? DamageType.Magic : DamageType.Physical, castId, reflectedOwner != null ? 1 : ultimate ? 3 : 1);
            if (ultimate && target.Alive && target.Kind == UnitKind.Champion)
            { target.Mark(blade.Tuning.markDuration); target.Slow(0.1f, 0.5f); }
        }
        public bool Threatens(Combatant target)
        {
            if (blade == null || target.Team == DamageOwner.Team) return false;
            Vector3 end = origin + direction * range;
            float radius = width + target.Radius + 0.25f;
            return CombatRules.SegmentDistanceSquared(target.Position.x, target.Position.z, Head.x, Head.z, end.x, end.z) < radius * radius;
        }
        public Vector3 Direction { get { return direction; } }
        void OnDestroy()
        {
            if (world != null) world.BladeWaves.Remove(this);
            // Stop_sfx_Irelia_IreliaR_onmissilelaunch: the launch sound fades when the missile ends.
            if (ultimate && launched && blade != null && blade.Actor != null && blade.Actor.Audio != null) blade.Actor.Audio.RMissileEnded(launchSound);
        }
    }
}
