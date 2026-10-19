using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    // Ground-plane circle contacts. Ability-driven movement remains free to cross units.
    public sealed class MinionSpacing
    {
        readonly List<Combatant> minions = new List<Combatant>();
        public const float Gap = 0; // Exact gameplay circles; cursor padding belongs only to selection.
        const float PersonalSpace = .18f;
        readonly CrowdPathfinding paths=new CrowdPathfinding();
        public void Clear() { paths.Clear(); }
        public Vector3 Step(LaneWorld world, Combatant actor, Vector3 goal, float distance, bool routeAroundCrowd = false)
        {
            if((actor.Kind==UnitKind.Champion && actor.Dummy==null || routeAroundCrowd) && distance>0)
                goal=paths.Waypoint(world,actor,goal);
            Vector3 start = actor.Position, forward = goal - start; forward.y = 0;
            if (distance <= 0 || forward.sqrMagnitude < .000001f) return start;
            float step = Mathf.Min(distance, forward.magnitude); forward.Normalize();
            if (actor.Kind == UnitKind.Minion)
            {
                Vector3 separation = Vector3.zero;
                foreach (var other in world.Units)
                {
                    if (!Blocks(actor, other) || other.Kind != UnitKind.Minion) continue;
                    Vector3 away = start - other.Position; away.y = 0;
                    float comfort = actor.Radius + other.Radius + PersonalSpace;
                    float length = away.magnitude;
                    if (length > .0001f && length < comfort)
                        separation += away / length * ((comfort - length) / PersonalSpace);
                }
                forward = (forward + Vector3.ClampMagnitude(separation, 1) * .7f).normalized;
            }
            Vector3 best = start; float bestScore = float.NegativeInfinity;
            // A stable side per actor avoids left/right oscillations in crowded waves.
            float side = (actor.GetHashCode() & 1) == 0 ? 1 : -1;
            for (int i = 0; i < 11; i++)
            {
                float angle = i == 0 ? 0 : ((i + 1) / 2) * 18 * (i % 2 == 1 ? side : -side);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 candidate = world.AvoidTower(actor, start + direction * step);
                if (!Clear(world, actor, start, candidate)) continue;
                float score = Vector3.Dot(forward, candidate - start) - Mathf.Abs(angle) * .00001f;
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }
        public bool TryYieldGoal(LaneWorld world,Combatant minion,out Vector3 goal)
        {
            goal=minion.Position;
            foreach(var champion in world.Units)
            {
                if(champion==null || champion.Kind!=UnitKind.Champion || champion.Dummy!=null || champion.Team!=minion.Team ||
                    !champion.Alive || !champion.HasWalkingOrder || !champion.gameObject.activeInHierarchy) continue;
                Vector3 forward=champion.WalkingDirection,offset=minion.Position-champion.Position;
                if(forward.sqrMagnitude<.001f) continue;
                float ahead=Vector3.Dot(offset,forward),clearance=champion.Radius+minion.Radius+.18f;
                Vector3 side=Vector3.Cross(Vector3.up,forward); float lateral=Vector3.Dot(offset,side);
                if(ahead<-.1f || ahead>clearance+1 || Mathf.Abs(lateral)>=clearance || offset.sqrMagnitude>7) continue;
                float preferred=Mathf.Abs(lateral)>.05f?Mathf.Sign(lateral):(minion.GetHashCode()&1)==0?1:-1;
                Vector3 best=minion.Position; float cost=float.PositiveInfinity;
                for(int signIndex=0;signIndex<2;signIndex++) for(int slot=0;slot<4;slot++)
                {
                    float sign=signIndex==0?preferred:-preferred;
                    float targetLateral=sign*(clearance+.12f+slot*(minion.Radius*2+.08f));
                    Vector3 candidate=world.Clamp(minion.Position+side*(targetLateral-lateral),minion.Radius);
                    float travel=(candidate-minion.Position).sqrMagnitude+signIndex*.001f;
                    if(travel>=cost || !CrowdPathfinding.Free(world,minion,candidate,candidate)) continue;
                    best=candidate; cost=travel;
                }
                if(cost<float.PositiveInfinity) { goal=best; return true; }
            }
            return false;
        }
        static bool Blocks(Combatant actor, Combatant other)
        {
            return other != null && other != actor && other.Alive && other.gameObject.activeInHierarchy && other.Kind != UnitKind.Tower &&
                !(other.Blade != null && other.Blade.IsDashing) && !(other.Sett != null && other.Sett.Casting);
        }
        static bool Clear(LaneWorld world, Combatant actor, Vector3 start, Vector3 end)
        {
            foreach (var other in world.Units)
            {
                if (!Blocks(actor, other)) continue;
                float radius = actor.Radius + other.Radius;
                float before = (start - other.Position).sqrMagnitude;
                float swept = CombatRules.SegmentDistanceSquared(other.Position.x, other.Position.z, start.x, start.z, end.x, end.z);
                // Existing overlap may only improve; otherwise a swept contact blocks the step.
                if (before < radius * radius)
                { if ((end - other.Position).sqrMagnitude <= before + .000001f) return false; }
                else if (swept < radius * radius - .00001f) return false;
            }
            return true;
        }
        public void Resolve(LaneWorld world)
        {
            minions.Clear();
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Alive && unit.Kind != UnitKind.Tower && unit.gameObject.activeInHierarchy &&
                    !(unit.Blade != null && unit.Blade.IsDashing) && !(unit.Sett != null && unit.Sett.Casting)) minions.Add(unit);
            // Packed units beside a tower need more projections to settle the coupled contacts.
            // Ordinary formations still exit early as soon as no correction is needed.
            for (int pass = 0; pass < 64; pass++)
            {
                bool moved = false;
                for (int i = 0; i < minions.Count; i++) for (int j = i + 1; j < minions.Count; j++)
                {
                    Combatant a = minions[i], b = minions[j];
                    Vector3 delta = b.Position - a.Position; delta.y = 0;
                    float required = a.Radius + b.Radius + (a.Kind == UnitKind.Minion && b.Kind == UnitKind.Minion ? Gap : 0);
                    if (delta.sqrMagnitude >= required * required) continue;
                    float distance = delta.magnitude;
                    Vector3 normal = distance > 0.0001f ? delta / distance :
                        a.Team == b.Team ? Vector3.forward : (a.Team == Team.Blue ? Vector3.right : Vector3.left);
                    // Training targets are kinematic: contact must not push the fixed target
                    // away from center or the moving target beyond its practice area.
                    bool anchorA = a.Dummy != null || (a.Kind == UnitKind.Minion && b.Kind == UnitKind.Champion && b.Dummy == null);
                    bool anchorB = b.Dummy != null || (b.Kind == UnitKind.Minion && a.Kind == UnitKind.Champion && a.Dummy == null);
                    if (anchorA && anchorB) continue;
                    Vector3 correction = normal * ((required - distance) * (anchorA || anchorB ? 1 : .5f) + .0005f);
                    if (!anchorA) a.transform.position = world.AvoidTower(a, a.Position - correction);
                    if (!anchorB) b.transform.position = world.AvoidTower(b, b.Position + correction);
                    moved = true;
                }
                if (!moved) break;
            }
        }
    }
}
