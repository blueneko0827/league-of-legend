using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public sealed class TowerNavigation
    {
        sealed class Detour { public Combatant Tower; public Vector3 Entry, Exit, Goal; public bool PassedEntry; }
        readonly Dictionary<Combatant, Detour> routes = new Dictionary<Combatant, Detour>();
        public void Clear() { routes.Clear(); }
        public void Forget(Combatant actor) { routes.Remove(actor); }
        public Vector3 Step(LaneWorld world, Combatant actor, Vector3 goal, float distance)
        {
            Detour route;
            if (routes.TryGetValue(actor, out route) && (route.Tower == null || !route.Tower.Alive || Vector3.Distance(goal, route.Goal) > 2))
            { routes.Remove(actor); route = null; }
            if (route == null)
            {
                Combatant obstacle = null; float nearest = float.PositiveInfinity;
                foreach (Combatant tower in world.Units)
                {
                    if (tower == null || !tower.Alive || tower.Kind != UnitKind.Tower || actor.AttackTarget == tower) continue;
                    float clearance = actor.Radius + tower.Radius + 0.15f;
                    float pathGap = CombatRules.SegmentDistanceSquared(tower.Position.x, tower.Position.z, actor.Position.x, actor.Position.z, goal.x, goal.z);
                    float away = (tower.Position - actor.Position).sqrMagnitude;
                    if (pathGap >= clearance * clearance || away >= nearest) continue;
                    obstacle = tower; nearest = away;
                }
                if (obstacle != null)
                {
                    float radius = actor.Radius + obstacle.Radius + 0.3f;
                    float direction = goal.x >= actor.Position.x ? 1 : -1;
                    float side = Mathf.Abs(actor.Position.z - obstacle.Position.z) > 0.05f ? Mathf.Sign(actor.Position.z - obstacle.Position.z) : actor.Team == Team.Blue ? -1 : 1;
                    route = new Detour { Tower = obstacle, Goal = goal,
                        Entry = obstacle.Position + new Vector3(-direction * radius, 0, side * radius),
                        Exit = obstacle.Position + new Vector3(direction * radius, 0, side * radius) };
                    if (direction * (actor.Position.x - obstacle.Position.x) > -radius) route.PassedEntry = true;
                    routes[actor] = route;
                }
            }
            Vector3 waypoint = goal;
            if (route != null)
            {
                if (!route.PassedEntry && Vector3.Distance(actor.Position, route.Entry) <= distance + 0.05f) route.PassedEntry = true;
                waypoint = route.PassedEntry ? route.Exit : route.Entry;
                if (route.PassedEntry && Vector3.Distance(actor.Position, route.Exit) <= distance + 0.05f)
                { routes.Remove(actor); waypoint = goal; }
            }
            return world.Spacing.Step(world, actor, world.Clamp(waypoint, actor.Radius), distance);
        }
    }
}
