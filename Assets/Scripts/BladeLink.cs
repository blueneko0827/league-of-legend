using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class BladeLink : MonoBehaviour
    {
        public bool HasSecond { get; private set; }
        public float SecondPlacedAt { get; private set; }
        public Vector3 First { get; private set; }
        public Vector3 Second { get; private set; }
        // Read-only timeline for the official E particles (IreliaVfx).
        public BladeDuelist Owner { get { return blade; } }
        public Vector3 FirstOrigin { get { return firstOrigin; } }
        public Vector3 SecondOrigin { get { return secondOrigin; } }
        public float FirstLaunchedAt { get { return firstAt; } }
        public float SecondLaunchedAt { get { return secondAt; } }
        public float ConvergeAt { get { return convergeAt; } }
        public float ImpactAt { get { return impactAt; } }
        bool OfficialVfx { get { return world.Presentation.official_vfx; } }
        LaneWorld world;
        BladeDuelist blade;
        GameObject firstVisual, secondVisual;
        IreliaEConvergenceVisual convergenceVisual;
        Vector3 firstOrigin, secondOrigin;
        float firstAt, secondAt, convergeAt, impactAt;
        bool resolved;
        bool convergenceSoundPlayed, firstLandedSound, secondLandedSound;
        int castId;
        // Presentation only: keep the enlarged mesh above ground without changing the cast points.
        float VisualHeight { get { return 0.45f * blade.Tuning.eBladeVisualMultiplier; } }

        public void Initialize(LaneWorld lane, BladeDuelist owner, Vector3 first)
        {
            world = lane; blade = owner; First = first; firstOrigin = owner.Actor.Position;
            castId = owner.Actor.SpellCastId;
            firstAt = Time.time;
            // The official E particles (IreliaVfx) replace the mesh blades and the convergence beam.
            if (!OfficialVfx)
            {
                firstVisual = PracticeModels.CreateSpellBlade(world, transform, owner.Actor.Team, "E primera hoja", firstOrigin + Vector3.up);
                firstVisual.transform.localScale *= blade.Tuning.eBladeVisualMultiplier;
            }
            if (blade.Actor.Audio != null) blade.Actor.Audio.EPlace(false);
        }
        public void PlaceSecond(Vector3 second)
        {
            if (HasSecond || resolved) return;
            HasSecond = true; Second = second; SecondPlacedAt = Time.time;
            secondAt = Time.time + blade.Tuning.eSecondCastTime;
            secondOrigin = blade.Actor.Position;
            convergeAt = Mathf.Max(firstAt, secondAt) + blade.Tuning.ePlacementTime;
            impactAt = convergeAt + blade.Tuning.eConvergeTime;
            if (!OfficialVfx)
            {
                secondVisual = PracticeModels.CreateSpellBlade(world, transform, blade.Actor.Team, "E segunda hoja", secondOrigin + Vector3.up);
                secondVisual.transform.localScale *= blade.Tuning.eBladeVisualMultiplier;
                convergenceVisual = IreliaEConvergenceVisual.Create(transform, First, Second, convergeAt, impactAt,
                    blade.Tuning.eHalfWidth * 2, blade.Actor.Team != world.Player.Team);
            }
            if (blade.Actor.Audio != null) blade.Actor.Audio.EPlace(true);
        }
        void Update()
        {
            if (world.RoundOver || blade == null || !blade.Actor.Alive) { Destroy(gameObject); return; }
            if (world.Paused || resolved) return;
            // Each blade lands at its point after the flight (official missile_stop + sparkles).
            if (!firstLandedSound && Time.time >= firstAt + blade.Tuning.ePlacementTime)
            { firstLandedSound = true; if (blade.Actor.Audio != null) blade.Actor.Audio.EBladeLanded(); }
            if (HasSecond && !secondLandedSound && Time.time >= secondAt + blade.Tuning.ePlacementTime)
            { secondLandedSound = true; if (blade.Actor.Audio != null) blade.Actor.Audio.EBladeLanded(); }
            if (firstVisual != null)
                firstVisual.transform.position = Vector3.Lerp(firstOrigin + Vector3.up, First + Vector3.up * VisualHeight,
                    (Time.time - firstAt) / blade.Tuning.ePlacementTime);
            if (!HasSecond) return;
            if (secondVisual != null)
                secondVisual.transform.position = Vector3.Lerp(secondOrigin + Vector3.up, Second + Vector3.up * VisualHeight,
                    (Time.time - secondAt) / blade.Tuning.ePlacementTime);
            if (Time.time >= convergeAt)
            {
                if (!convergenceSoundPlayed)
                { convergenceSoundPlayed = true; if (blade.Actor.Audio != null) blade.Actor.Audio.EConverging(); }
                float t = (Time.time - convergeAt) / blade.Tuning.eConvergeTime;
                Vector3 center = (First + Second) * 0.5f + Vector3.up * VisualHeight;
                if (firstVisual != null) firstVisual.transform.position = Vector3.Lerp(First + Vector3.up * VisualHeight, center, t);
                if (secondVisual != null) secondVisual.transform.position = Vector3.Lerp(Second + Vector3.up * VisualHeight, center, t);
            }
            if (Time.time < impactAt) return;
            resolved = true;
            var victims = new List<Combatant>();
            foreach (Combatant target in world.Units)
            {
                if (!blade.Actor.ValidEnemy(target) || target.Kind == UnitKind.Tower) continue;
                Vector3 segment = Second - First, offset = target.Position - First;
                Vector3 direction = segment.sqrMagnitude > 0.00001f ? segment.normalized : Vector3.right;
                if (!CombatRules.CircleIntersectsRectangle(Vector3.Dot(offset, direction), Vector3.Dot(offset, Vector3.Cross(direction, Vector3.up)),
                    target.Radius, 0, segment.magnitude, blade.Tuning.eHalfWidth)) continue;
                // Rebuttal blocks this convergence for Mel, including its stun and Unsteady mark.
                if (target.Mel != null && target.Mel.BlocksBladeConvergence(blade.Actor)) continue;
                blade.Actor.DealAbilityDamage(target, 30 + blade.Rank(2) * 40 + blade.Actor.AbilityPower, DamageType.Magic, castId, 2);
                bool stunned = target.Alive;
                if (target.Alive)
                {
                    target.Stun(blade.Tuning.eStun);
                    if (target.Kind == UnitKind.Champion) target.Mark(blade.Tuning.markDuration);
                }
                victims.Add(target);
                blade.NotifyEHit(target, stunned);
            }
            blade.AwardSpellHits(victims);
            if (victims.Count > 0 && blade.Actor.Audio != null)
                blade.Actor.Audio.EImpact(victims.Exists(target => target.Kind == UnitKind.Champion));
            if (convergenceVisual != null) convergenceVisual.Finish();
            Destroy(gameObject);
        }
        public bool Threatens(Combatant target)
        {
            if (!HasSecond || resolved || blade == null || target.Team == blade.Actor.Team) return false;
            float radius = blade.Tuning.eHalfWidth + target.Radius + 0.25f;
            return CombatRules.SegmentDistanceSquared(target.Position.x, target.Position.z, First.x, First.z, Second.x, Second.z) < radius * radius;
        }
        void OnDestroy() { if (world != null) world.BladeLinks.Remove(this); }
    }
}
