using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    // Ground-plane circle contacts. Ability-driven movement remains free to cross units.
    public sealed class MinionSpacing
    {
        readonly List<Combatant> minions = new List<Combatant>();
        struct Neighbor { public Combatant Unit; public Vector3 Position; public float Radius; }
        readonly List<Neighbor> neighbors = new List<Neighbor>(24);
        public const float Gap = 0; // Exact gameplay circles; cursor padding belongs only to selection.
        const float PersonalSpace = .3f;
        sealed class Steering { public float Side; public Vector3 Direction; }
        sealed class Yield { public Combatant Champion; public Vector3 Goal, Direction, Origin; }
        readonly Dictionary<Combatant, Steering> steering = new Dictionary<Combatant, Steering>();
        readonly Dictionary<Combatant, Yield> yields = new Dictionary<Combatant, Yield>();
        readonly CrowdPathfinding paths=new CrowdPathfinding();
        public void Clear() { paths.Clear(); steering.Clear(); yields.Clear(); }
        public void Forget(Combatant actor) { paths.Forget(actor); steering.Remove(actor); yields.Remove(actor); }
        public Vector3 Step(LaneWorld world, Combatant actor, Vector3 goal, float distance)
        {
            // Courtesy sidesteps stay local; reserve complete grid searches for champions.
            if(actor.Kind==UnitKind.Champion && actor.Dummy==null && distance>0)
                goal=paths.Waypoint(world,actor,goal);
            Vector3 start = actor.Position, forward = goal - start; forward.y = 0;
            if (distance <= 0 || forward.sqrMagnitude < .000001f || actor.Kind == UnitKind.Minion && forward.sqrMagnitude < .04f * .04f) return start;
            float step = Mathf.Min(distance, forward.magnitude); forward.Normalize();
            neighbors.Clear();
            // Read transforms and eligibility once, then test all steering directions
            // against the small local set. Include tower corrections conservatively.
            float towerPush = Vector3.Distance(start, world.AvoidTower(actor, start));
            foreach (var other in world.Units)
            {
                if (!Blocks(actor, other)) continue;
                Vector3 position = other.Position;
                float reach = step * 2 + towerPush + actor.Radius + other.Radius + PersonalSpace;
                if ((position - start).sqrMagnitude > reach * reach) continue;
                neighbors.Add(new Neighbor { Unit = other, Position = position, Radius = other.Radius });
            }
            Vector3 desired = forward;
            if (actor.Kind == UnitKind.Minion)
            {
                Vector3 separation = Vector3.zero;
                foreach (var neighbor in neighbors)
                {
                    if (neighbor.Unit.Kind != UnitKind.Minion) continue;
                    Vector3 away = start - neighbor.Position; away.y = 0;
                    float comfort = actor.Radius + neighbor.Radius + PersonalSpace;
                    float length = away.magnitude;
                    if (length > .0001f && length < comfort)
                        separation += away / length * ((comfort - length) / PersonalSpace);
                }
                // Separation is lateral: it must not reverse an order in a packed wave.
                separation -= desired * Vector3.Dot(separation, desired);
                forward = (forward + Vector3.ClampMagnitude(separation, 1) * .7f).normalized;
            }
            Vector3 best = start; float bestScore = float.NegativeInfinity;
            float side = (actor.GetHashCode() & 1) == 0 ? 1 : -1;
            Steering state = null;
            if (actor.Kind == UnitKind.Minion)
            {
                if (!steering.TryGetValue(actor, out state))
                { state = new Steering { Side = side, Direction = forward }; steering.Add(actor, state); }
                side = state.Side;
            }
            Vector3 straight = world.AvoidTower(actor, start + forward * step);
            if ((state == null || Vector3.Dot(state.Direction, forward) > .7f) && Clear(world, actor, start, straight))
            {
                if (state != null) state.Direction = forward;
                return straight;
            }
            for (int i = 0; i < 11; i++)
            {
                float angle = i == 0 ? 0 : ((i + 1) / 2) * 18 * (i % 2 == 1 ? side : -side);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 candidate = world.AvoidTower(actor, start + direction * step);
                if (!Clear(world, actor, start, candidate)) continue;
                Vector3 displacement = candidate - start;
                // A circle's tangent can initially point slightly away from the destination.
                // Rejecting that motion pins the minion to the front of a larger blocker.
                if (state != null && (displacement.sqrMagnitude < .000001f || Vector3.Dot(desired, displacement) < -step * .65f)) continue;
                float score = Vector3.Dot(forward, displacement) - Mathf.Abs(angle) * .00001f;
                if (state != null)
                {
                    // Retain a chosen bypass direction instead of flipping at near-equal contacts.
                    score += Vector3.Dot(state.Direction, displacement) * .35f;
                    if (angle * side > 0) score += step * .08f;
                }
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            if (state != null && (best - start).sqrMagnitude > .000001f)
            {
                state.Direction = (best - start).normalized;
                float turn = Vector3.SignedAngle(desired, state.Direction, Vector3.up);
                if (Mathf.Abs(turn) > 10) state.Side = Mathf.Sign(turn);
            }
            return best;
        }
        public bool TryYieldGoal(LaneWorld world,Combatant minion,out Vector3 goal)
        {
            goal=minion.Position;
            Yield current;
            if (yields.TryGetValue(minion, out current))
            {
                Combatant owner = current.Champion;
                float clearance = owner != null ? owner.Radius + minion.Radius + .3f : 0;
                if (owner != null && owner.Alive && owner.gameObject.activeInHierarchy && owner.HasWalkingOrder &&
                    Vector3.Dot(owner.WalkingDirection, current.Direction) > .7f &&
                    Vector3.Dot(current.Origin - owner.Position, current.Direction) > -clearance &&
                    (owner.Position - current.Origin).sqrMagnitude < 12)
                { goal = current.Goal; return true; }
                yields.Remove(minion);
            }
            foreach(var champion in world.Units)
            {
                if(champion==null || champion.Kind!=UnitKind.Champion || champion.Dummy!=null || champion.Team!=minion.Team ||
                    !champion.Alive || !champion.HasWalkingOrder || !champion.gameObject.activeInHierarchy) continue;
                Vector3 forward=champion.WalkingDirection,offset=minion.Position-champion.Position;
                if(forward.sqrMagnitude<.001f) continue;
                float ahead=Vector3.Dot(offset,forward),clearance=champion.Radius+minion.Radius+.3f;
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
                if(cost<float.PositiveInfinity)
                {
                    yields[minion] = new Yield { Champion = champion, Goal = best, Direction = forward, Origin = minion.Position };
                    goal=best; return true;
                }
            }
            return false;
        }
        static bool Blocks(Combatant actor, Combatant other)
        {
            return other != null && other != actor && other.Alive && other.gameObject.activeInHierarchy && other.Kind != UnitKind.Tower &&
                !(other.Blade != null && other.Blade.IsDashing) && !(other.Sett != null && other.Sett.Casting);
        }
        bool Clear(LaneWorld world, Combatant actor, Vector3 start, Vector3 end)
        {
            foreach (var other in neighbors)
            {
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
            // Swept movement prevents new overlaps. This bounded repair handles spawns/dashes;
            // a contact tolerance stops endless sub-pixel projections against towers and walls.
            for (int pass = 0; pass < 12; pass++)
            {
                bool moved = false;
                for (int i = 0; i < minions.Count; i++) for (int j = i + 1; j < minions.Count; j++)
                {
                    Combatant a = minions[i], b = minions[j];
                    Vector3 delta = b.Position - a.Position; delta.y = 0;
                    float required = a.Radius + b.Radius + (a.Kind == UnitKind.Minion && b.Kind == UnitKind.Minion ? Gap : 0);
                    if (delta.sqrMagnitude >= required * required) continue;
                    float distance = delta.magnitude;
                    if (required - distance <= .0002f) continue;
                    Vector3 normal = distance > 0.0001f ? delta / distance :
                        a.Team == b.Team ? Vector3.forward : (a.Team == Team.Blue ? Vector3.right : Vector3.left);
                    // Training targets are kinematic: contact must not push the fixed target
                    // away from center or the moving target beyond its practice area.
                    bool anchorA = a.Dummy != null || (a.Kind == UnitKind.Minion && b.Kind == UnitKind.Champion && b.Dummy == null);
                    bool anchorB = b.Dummy != null || (b.Kind == UnitKind.Minion && a.Kind == UnitKind.Champion && a.Dummy == null);
                    if (anchorA && anchorB) continue;
                    Vector3 correction = normal * ((required - distance) * (anchorA || anchorB ? 1 : .5f));
                    if (!anchorA) a.transform.position = world.AvoidTower(a, a.Position - correction);
                    if (!anchorB) b.transform.position = world.AvoidTower(b, b.Position + correction);
                    moved = true;
                }
                if (!moved) break;
            }
        }
    }
}
