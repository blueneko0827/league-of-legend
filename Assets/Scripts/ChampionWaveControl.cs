using UnityEngine;

namespace MidLane
{
    public enum LaneWavePlan { Hold, Freeze, ClearUnderTower, Push, Advance }

    public sealed partial class ChampionBrain
    {
        public LaneWavePlan WavePlan { get; private set; }
        float wavePlanAt;
        Combatant laneWave, laneScreen, laneOwnTower;

        void UpdateLaneWavePlan(Combatant player)
        {
            if (!world.NormalplayModeActive || Time.time < wavePlanAt) return;
            wavePlanAt = Time.time + .45f;
            laneWave = laneScreen = laneOwnTower = null;
            float nearest = float.PositiveInfinity, alliedPower = 0, enemyPower = 0;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !unit.Alive || !unit.gameObject.activeInHierarchy) continue;
                if (unit.Kind == UnitKind.Tower && unit.Team == self.Team) laneOwnTower = unit;
                if (unit.Kind != UnitKind.Minion || Vector3.Distance(unit.Position, self.Position) > 13) continue;
                float power = unit.Health / Mathf.Max(1, unit.MaxHealth) * (unit.MinionType == MinionType.Cannon ? 2 : 1);
                if (unit.Team == self.Team)
                {
                    alliedPower += power;
                    if (laneScreen == null || unit.Position.x * Forward > laneScreen.Position.x * Forward) laneScreen = unit;
                }
                else
                {
                    enemyPower += power;
                    float distance = (unit.Position - self.Position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; laneWave = unit; }
                }
            }
            bool absent = !player.Targetable || Vector3.Distance(player.Position, self.Position) > 11;
            bool disadvantaged = self.Health < self.MaxHealth * .55f || self.MaxResource > 0 && self.Resource < self.MaxResource * .2f ||
                player.Targetable && player.Health / player.MaxHealth > self.Health / self.MaxHealth + .2f;
            if (laneWave == null) WavePlan = absent && laneScreen != null ? LaneWavePlan.Advance : LaneWavePlan.Hold;
            else if (laneOwnTower != null && Vector3.Distance(laneWave.Position, laneOwnTower.Position) <= laneOwnTower.AttackReach(laneWave) &&
                Vector3.Distance(self.Position, laneOwnTower.Position) < laneOwnTower.TowerAggroRadius + 2)
                WavePlan = LaneWavePlan.ClearUnderTower;
            else if (!absent && (disadvantaged || enemyPower > alliedPower + 1.5f)) WavePlan = LaneWavePlan.Freeze;
            else if (absent || !disadvantaged && alliedPower > enemyPower + 1.5f &&
                self.Health / self.MaxHealth > player.Health / player.MaxHealth + .2f) WavePlan = LaneWavePlan.Push;
            else WavePlan = LaneWavePlan.Hold;
        }

        bool TryLaneWaveClear(Combatant player)
        {
            if (!world.NormalplayModeActive || self.IsWindingAttack ||
                (WavePlan != LaneWavePlan.Push && WavePlan != LaneWavePlan.ClearUnderTower)) return false;
            Combatant best = null; float score = float.PositiveInfinity;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit.Kind != UnitKind.Minion || !self.ValidEnemy(unit) || Vector3.Distance(self.Position, unit.Position) > 13) continue;
                Vector3 point = self.Mel != null ? MelFarmPoint(unit) : FarmPoint(unit);
                if (!(self.Mel != null ? MelFarmSafe(player, point) : SafeFarm(unit, player, EnemyTower()))) continue;
                float utility = Vector3.Distance(self.Position, unit.Position) + unit.Health / Mathf.Max(1, unit.MaxHealth);
                // Under tower, thin the minion currently being shot and collect it before the next tower hit.
                if (laneOwnTower != null && laneOwnTower.AttackTarget == unit) utility -= 2;
                if (utility < score) { score = utility; best = unit; }
            }
            if (best == null) return false;
            if (self.Mel != null && CanSpendLaneFarmQ())
            {
                int cluster = 0;
                foreach (Combatant unit in world.Units)
                    if (unit != null && unit.Kind == UnitKind.Minion && self.ValidEnemy(unit) &&
                        Vector3.Distance(unit.Position, best.Position) <= MelRules.QExplosionRadius) cluster++;
                if (cluster >= 2 && Vector3.Distance(self.Position, best.Position) <= MelRules.QRange && self.Cast(0, best.Position))
                { melCombo = MelCombo.None; Plan(LaneTactic.Push, "Mel: limpiar oleada con Q conservando maná defensivo"); return true; }
            }
            Plan(LaneTactic.Push, WavePlan == LaneWavePlan.ClearUnderTower ? "Limpiar y rematar bajo torre" : "Empujar con cobertura de la oleada");
            if (Vector3.Distance(self.Position, best.Position) <= self.AttackReach(best))
            { self.OrderAttack(best); if (self.AttackReady) self.TryAttack(best); }
            else MoveOrHold(self.Mel != null ? MelFarmPoint(best) : FarmPoint(best));
            return true;
        }

        bool TryLaneWavePosition(Combatant player)
        {
            if (!world.NormalplayModeActive) return false;
            if (WavePlan == LaneWavePlan.Advance && laneScreen != null)
            {
                if (CanSiege(EnemyTower(), player)) return false;
                Vector3 point = laneScreen.Position - Vector3.right * Forward * 2;
                if (TowerRange(EnemyTower(), point)) point = TowerExit(EnemyTower());
                if (!SafeLaneApproach(point)) return false;
                Plan(LaneTactic.Farm, "Acompañar la oleada desde su cobertura"); MoveOrHold(point); return true;
            }
            if (WavePlan == LaneWavePlan.Freeze && laneWave != null)
            {
                Vector3 point = self.Mel != null ? melLaneAnchor : FarmPoint(laneWave);
                if (laneScreen != null && Vector3.Distance(laneScreen.Position, laneWave.Position) < 8)
                    point = laneScreen.Position - Vector3.right * Forward * 1.5f;
                if (player.Targetable && Vector3.Distance(point, player.Position) < 5)
                    point -= Vector3.right * Forward * 1.5f;
                if (player.Targetable && self.Health < self.MaxHealth * .55f && Vector3.Distance(point, player.Position) < 7)
                {
                    Vector3 away = point - player.Position;
                    if (away.sqrMagnitude < .01f) away = Vector3.left * Forward;
                    point = player.Position + away.normalized * 7;
                }
                Plan(LaneTactic.Farm, "Conservar oleada · esperar remates desde cobertura");
                MoveOrHold(world.AvoidTower(self, point)); return true;
            }
            return false;
        }
    }
}
