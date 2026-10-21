using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        Vector3 laneRouteGoal, laneRouteWaypoint;
        float laneRouteAt;
        readonly List<Vector3> laneRoutePoints = new List<Vector3>();
        readonly List<Vector3> laneShortRoute = new List<Vector3>(2);
        readonly Dictionary<Vector3, bool> laneApproaches = new Dictionary<Vector3, bool>();
        int laneApproachFrame = -1;
        Vector3 laneApproachOrigin;
        float laneApproachHealth;
        int laneApproachUnits;
        static readonly float[] LaneRouteOffsets = { -3f, 3f, -5f, 5f };

        float MinionProjectilesDue(float seconds)
        {
            float damage = 0;
            foreach (BasicProjectile shot in world.BasicProjectiles)
                if (shot != null && shot.Target == self && shot.Source != null && shot.Source.Kind == UnitKind.Minion && shot.TimeToImpact <= seconds)
                    damage += self.PreviewIncomingDamage(shot.Source.BasicDamageAgainst(self, shot.RawDamage), shot.Source, DamageType.Physical);
            return damage;
        }

        bool TowerCoversPoint(Combatant minion, Vector3 point)
        {
            Combatant tower = minion.AttackTarget;
            return tower != null && tower.Kind == UnitKind.Tower && tower.Team == self.Team &&
                Vector3.Distance(minion.Position, tower.Position) <= minion.AttackReach(tower) &&
                Vector3.Distance(point, tower.Position) <= tower.AttackReach(self);
        }

        float LaneThreatDps(Vector3 point, float after = 0, Combatant removedMinion = null)
        {
            float damage = 0;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !self.ValidEnemy(unit)) continue;
                float distance = Vector3.Distance(unit.Position, point);
                if (unit.Kind == UnitKind.Tower)
                {
                    if (distance <= unit.AttackReach(self) + self.Radius)
                        damage += self.MaxHealth * 2 + unit.AttackDamage;
                }
                else if (unit.Kind == UnitKind.Minion && unit != removedMinion && distance <= unit.AttackReach(self) + .2f)
                {
                    bool targeting = unit.AttackTarget == self || unit.PendingAttackTarget == self;
                    if (!targeting && TowerCoversPoint(unit, point)) continue;
                    damage += self.PreviewIncomingDamage(unit.BasicDamageAgainst(self, unit.AttackDamage), unit, DamageType.Physical) *
                        unit.BaseAttackSpeed * (targeting ? 1 : .5f);
                }
            }
            Combatant player = world.Player;
            if (player.Targetable && player.StunRemaining <= after)
            {
                float distance = Vector3.Distance(player.Position, point);
                if (distance <= player.AttackReach(self) + .3f)
                    damage += Physical(player, self, player.AttackDamage) * player.BaseAttackSpeed;
                if (player.Blade != null && player.Progress.Rank(0) > 0 && Time.time + after >= observedReady[0] && distance <= player.Blade.Tuning.qRange)
                    damage += Physical(player, self, player.Blade.QRawDamage(self)) / 1.5f;
            }
            return damage;
        }

        float SegmentLaneDamage(Vector3 start, Vector3 end, ref float elapsed)
        {
            float duration = Vector3.Distance(start, end) / Mathf.Max(.1f, self.MoveSpeed);
            int samples = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(start, end) / .7f), 2, 32);
            float damage = 0;
            for (int i = 0; i < samples; i++)
            {
                float fraction = (i + .5f) / samples;
                damage += LaneThreatDps(Vector3.Lerp(start, end, fraction), elapsed + duration * fraction) * duration / samples;
            }
            elapsed += duration;
            return damage;
        }

        float RouteLaneDamage(Vector3 waypoint, Vector3 goal, out float seconds)
        {
            laneShortRoute.Clear(); laneShortRoute.Add(waypoint);
            if ((waypoint - goal).sqrMagnitude > .01f) laneShortRoute.Add(goal);
            return PlannedLaneDamage(laneShortRoute, out seconds);
        }

        float PlannedLaneDamage(List<Vector3> points, out float seconds)
        {
            seconds = 0; float damage = 0; Vector3 start = self.Position;
            for (int leg = 0; leg < points.Count; leg++)
            {
                Vector3 end = points[leg];
                if (Vector3.Distance(start, end) < .05f) continue;
                if (leg == 0)
                {
                    List<Vector3> route = CrowdPathfinding.PreviewRoute(world, self, start, end);
                    if (route.Count == 0) return float.PositiveInfinity;
                    foreach (Vector3 point in route) { damage += SegmentLaneDamage(start, point, ref seconds); start = point; }
                }
                else
                {
                    if (!CrowdPathfinding.Free(world, self, start, end)) return float.PositiveInfinity;
                    damage += SegmentLaneDamage(start, end, ref seconds); start = end;
                }
            }
            return damage + MinionProjectilesDue(seconds);
        }

        List<Vector3> LaneRouteCandidate(Vector3 goal, float offset, bool parallel)
        {
            Vector3 side = Vector3.Cross((goal - self.Position).normalized, Vector3.up) * offset;
            if (parallel) return new List<Vector3> { world.Clamp(self.Position + side, self.Radius), world.Clamp(goal + side, self.Radius), goal };
            return new List<Vector3> { world.Clamp(Vector3.Lerp(self.Position, goal, .5f) + side, self.Radius), goal };
        }

        Vector3 LaneSafeWaypoint(Vector3 goal)
        {
            if (!world.NormalplayModeActive || Vector3.Distance(self.Position, goal) < .3f) return goal;
            bool sameGoal = (goal - laneRouteGoal).sqrMagnitude < .6f * .6f;
            if (!sameGoal) laneRoutePoints.Clear();
            while (laneRoutePoints.Count > 1 && Vector3.Distance(self.Position, laneRoutePoints[0]) < .35f) laneRoutePoints.RemoveAt(0);
            if (laneRoutePoints.Count > 0) laneRoutePoints[laneRoutePoints.Count - 1] = goal;
            if (Time.time < laneRouteAt && sameGoal && laneRoutePoints.Count > 0) return laneRoutePoints[0];
            laneRouteGoal = goal; laneRouteAt = Time.time + .55f;
            if (laneRoutePoints.Count == 0) laneRoutePoints.Add(goal);
            float seconds, best = PlannedLaneDamage(laneRoutePoints, out seconds) + seconds * self.MaxHealth * .015f;
            var direct = new List<Vector3> { goal };
            float directScore = PlannedLaneDamage(direct, out seconds) + seconds * self.MaxHealth * .015f;
            if (directScore < best * .9f) { best = directScore; laneRoutePoints.Clear(); laneRoutePoints.Add(goal); }
            foreach (float offset in LaneRouteOffsets)
                foreach (bool parallel in new[] { false, true })
                {
                    List<Vector3> candidate = LaneRouteCandidate(goal, offset, parallel);
                    float score = PlannedLaneDamage(candidate, out seconds) + seconds * self.MaxHealth * .015f;
                    if (score >= best * .9f) continue;
                    best = score; laneRoutePoints.Clear(); laneRoutePoints.AddRange(candidate);
                }
            laneRouteWaypoint = laneRoutePoints[0];
            return laneRouteWaypoint;
        }

        bool SafeLaneApproach(Vector3 point)
        {
            if (laneApproachFrame != Time.frameCount || laneApproachOrigin != self.Position ||
                laneApproachHealth != self.Health + self.TotalShield || laneApproachUnits != world.Units.Count)
            {
                laneApproaches.Clear(); laneApproachFrame = Time.frameCount; laneApproachOrigin = self.Position;
                laneApproachHealth = self.Health + self.TotalShield; laneApproachUnits = world.Units.Count;
            }
            bool safe;
            if (laneApproaches.TryGetValue(point, out safe)) return safe;
            safe = EvaluateLaneApproach(point); laneApproaches[point] = safe; return safe;
        }

        bool EvaluateLaneApproach(Vector3 point)
        {
            float seconds;
            float incoming = RouteLaneDamage(point, point, out seconds);
            float budget = (self.Health + self.TotalShield) * .4f;
            if (incoming < budget) return true;
            foreach (float offset in LaneRouteOffsets)
                foreach (bool parallel in new[] { false, true })
                    if (PlannedLaneDamage(LaneRouteCandidate(point, offset, parallel), out seconds) < budget) return true;
            return false;
        }

        float LeastLaneEscapeDamage(Vector3 goal)
        {
            float seconds, best = RouteLaneDamage(goal, goal, out seconds);
            foreach (float offset in LaneRouteOffsets)
                foreach (bool parallel in new[] { false, true })
                    best = Mathf.Min(best, PlannedLaneDamage(LaneRouteCandidate(goal, offset, parallel), out seconds));
            return best;
        }
    }
}
