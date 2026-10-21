using UnityEngine;

namespace MidLane
{
    public sealed class LaneUnitBrain : MonoBehaviour
    {
        LaneWorld world;
        Combatant self, provoker;
        float nextThink, provokedUntil;
        Combatant defendedTower, defendedChampion;
        float defenderPriorityUntil;
        public const float DefenderPrioritySeconds = 2;
        float laneOffset;
        public void Initialize(LaneWorld lane, Combatant actor) { world = lane; self = actor; laneOffset = actor.Position.z; }
        public void Provoke(Combatant champion)
        {
            if (!self.ValidEnemy(champion) || Vector3.Distance(self.Position, champion.Position) > self.AttackReach(champion)) return;
            provoker = champion; provokedUntil = Time.time + 2.5f; nextThink = 0;
            if (self.Kind == UnitKind.Minion)
            {
                Combatant tower = DefendingTower(champion);
                if (tower != null)
                { TrackDefender(champion, tower); defenderPriorityUntil = Time.time + DefenderPrioritySeconds; }
                self.OrderAttack(champion);
            }
        }
        Combatant DefendingTower(Combatant champion)
        {
            if (self.Kind != UnitKind.Minion || !self.ValidEnemy(champion) || champion.Kind != UnitKind.Champion || champion.Dummy != null ||
                Vector3.Distance(self.Position, champion.Position) > self.AttackReach(champion)) return null;
            foreach (Combatant tower in world.Units)
                if (tower != null && tower.Kind == UnitKind.Tower && tower.Team == champion.Team && self.ValidEnemy(tower) &&
                    Vector3.Distance(self.Position, tower.Position) <= self.AttackReach(tower) &&
                    Vector3.Distance(champion.Position, tower.Position) <= tower.AttackReach(champion)) return tower;
            return null;
        }
        void TrackDefender(Combatant champion, Combatant tower)
        {
            if (defendedChampion == champion && defendedTower == tower) return;
            defendedChampion = champion; defendedTower = tower;
            // An existing attack may continue briefly; arriving minions prefer the tower immediately.
            defenderPriorityUntil = self.AttackTarget == champion ? Time.time + DefenderPrioritySeconds : 0;
        }
        bool PreferDefendingTower(Combatant champion, out Combatant tower)
        {
            tower = DefendingTower(champion);
            if (tower == null) return false;
            TrackDefender(champion, tower);
            return Time.time >= defenderPriorityUntil;
        }
        void Update()
        {
            if (!self.Alive || world.RoundOver || world.Paused) return;
            if (defendedChampion != null && DefendingTower(defendedChampion) == null)
            { defendedChampion = defendedTower = null; defenderPriorityUntil = 0; }
            if (self.Kind == UnitKind.Minion && provoker != null)
            {
                if (self.ValidEnemy(provoker) && Vector3.Distance(self.Position, provoker.Position) <= self.AttackReach(provoker))
                {
                    Combatant tower;
                    if (!PreferDefendingTower(provoker, out tower)) { self.OrderAttack(provoker); return; }
                    provoker = null; nextThink = 0;
                }
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
            if (best != null)
            {
                Combatant tower;
                if (PreferDefendingTower(best, out tower)) best = tower;
                self.OrderAttack(best);
            }
            else if (self.Kind == UnitKind.Minion)
                self.MoveTo(new Vector3(self.Team == Team.Blue ? world.Settings.halfLength - 1 : -world.Settings.halfLength + 1, 0, laneOffset));
            else self.Stop();
        }
    }
}
