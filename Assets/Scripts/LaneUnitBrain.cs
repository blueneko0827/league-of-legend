using UnityEngine;

namespace MidLane
{
    public sealed class LaneUnitBrain : MonoBehaviour
    {
        LaneWorld world;
        Combatant self, provoker;
        float nextThink, provokedUntil;
        float laneOffset;
        public void Initialize(LaneWorld lane, Combatant actor) { world = lane; self = actor; laneOffset = actor.Position.z; }
        public void Provoke(Combatant champion)
        {
            if (!self.ValidEnemy(champion) || Vector3.Distance(self.Position, champion.Position) > self.AttackReach(champion)) return;
            provoker = champion; provokedUntil = Time.time + 2.5f; nextThink = 0;
            if (self.Kind == UnitKind.Minion) self.OrderAttack(champion);
        }
        void Update()
        {
            if (!self.Alive || world.RoundOver || world.Paused) return;
            if (self.Kind == UnitKind.Minion && provoker != null)
            {
                if (self.ValidEnemy(provoker) && Vector3.Distance(self.Position, provoker.Position) <= self.AttackReach(provoker))
                { self.OrderAttack(provoker); return; }
                if (self.AttackTarget == provoker) self.Stop();
                provoker = null; nextThink = 0;
            }
            if (Time.time < nextThink) return;
            nextThink = Time.time + .2f;
            if (self.Kind == UnitKind.Tower && Time.time < provokedUntil && self.ValidEnemy(provoker) &&
                Vector3.Distance(self.Position, provoker.Position) <= self.AttackReach(provoker))
            { self.OrderAttack(provoker); return; }
            if (self.Kind == UnitKind.Tower && self.ValidEnemy(self.AttackTarget) &&
                Vector3.Distance(self.Position, self.AttackTarget.Position) <= self.AttackReach(self.AttackTarget)) return;
            Combatant best = null; float bestScore = float.MaxValue;
            foreach (Combatant unit in world.Units)
            {
                if (!self.ValidEnemy(unit) || self.Kind == UnitKind.Tower && unit.Dummy != null) continue;
                float distance = Vector3.Distance(self.Position, unit.Position);
                float detection = self.Kind == UnitKind.Tower ? self.AttackReach(unit) : 7;
                if (distance > detection) continue;
                float score = distance + (unit.Kind == UnitKind.Minion ? 0 : unit.Kind == UnitKind.Champion ? 10 : 20);
                if (self.Kind == UnitKind.Tower && unit.Kind == UnitKind.Minion && unit.MinionType == MinionType.Cannon) score -= 5;
                if (score < bestScore) { best = unit; bestScore = score; }
            }
            if (best != null) self.OrderAttack(best);
            else if (self.Kind == UnitKind.Minion)
                self.MoveTo(new Vector3(self.Team == Team.Blue ? world.Settings.halfLength - 1 : -world.Settings.halfLength + 1, 0, laneOffset));
            else self.Stop();
        }
    }
}
