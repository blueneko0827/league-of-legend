using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        void FindFarm(Combatant tower, Combatant player, out Combatant lastHit, out Combatant wave)
        {
            lastHit = wave = null;
            float bestLastHit = float.PositiveInfinity, nearest = float.PositiveInfinity;
            foreach (Combatant minion in world.Units)
            {
                if (minion == null || minion.Kind != UnitKind.Minion || !self.ValidEnemy(minion)) continue;
                float distance = Vector3.Distance(self.Position, minion.Position);
                if (distance > 14 || !SafeFarm(minion, player, tower)) continue;
                if (distance < nearest) { nearest = distance; wave = minion; }
                float reach = self.AttackReach(minion);
                float travel = Mathf.Max(0, distance - reach) / self.MoveSpeed;
                float horizon = Mathf.Max(travel, self.AttackCooldownRemaining) + self.AttackWindupDuration;
                float predicted = PredictMinionHealth(minion, horizon);
                float basic = Physical(self, minion, self.AttackDamage);
                // If allied damage will kill it before our hit arrives, don't chase that minion.
                if (predicted <= 0 || predicted > basic || horizon > 2) continue;
                float score = horizon + predicted / Mathf.Max(1, basic) * 0.1f - (minion.MinionType == MinionType.Cannon ? 0.15f : 0);
                if (score < bestLastHit) { bestLastHit = score; lastHit = minion; }
            }
        }
        bool SafeFarm(Combatant minion, Combatant player, Combatant tower)
        {
            Vector3 point = FarmPoint(minion);
            if (TowerRange(tower, point) && !CanSiege(tower, player)) return false;
            if (!player.Targetable || Vector3.Distance(point, player.Position) > 7) return true;
            float health = self.Health / self.MaxHealth, enemyHealth = player.Health / player.MaxHealth;
            return health >= 0.42f && !(enemyHealth > health + 0.18f && health < 0.7f) &&
                ThreatDamage(player, point, 2.5f) < (self.Health + self.TotalShield) * 0.6f;
        }
        Vector3 FarmPoint(Combatant minion)
        {
            Vector3 away = self.Position - minion.Position;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right * -Forward;
            float distance = Mathf.Min(away.magnitude, self.AttackReach(minion) - 0.08f);
            return world.Clamp(minion.Position + away.normalized * distance);
        }
        void ApproachFarm(Combatant minion) { MoveOrHold(FarmPoint(minion)); }

        public float PredictMinionHealth(Combatant minion, float horizon)
        {
            float health = minion.Health;
            foreach (BasicProjectile shot in world.BasicProjectiles)
                if (shot != null && shot.Target == minion && shot.Source != null && shot.TimeToImpact <= horizon)
                    health -= CombatRules.Mitigate(shot.Source.BasicDamageAgainst(minion, shot.RawDamage), minion.Armor);
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit == self || !unit.Alive || unit.PendingAttackTarget != minion) continue;
                float travel = unit.Kind == UnitKind.Minion && unit.MinionType != MinionType.Melee ?
                    Vector3.Distance(unit.Position, minion.Position) / (unit.MinionType == MinionType.Cannon ? 12 : 6.5f) : 0;
                if (unit.AttackWindupRemaining + travel <= horizon)
                    health -= CombatRules.Mitigate(unit.BasicDamageAgainst(minion, unit.AttackDamage), minion.Armor);
            }
            return health;
        }

        Combatant EnemyTower()
        {
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Alive && unit.Kind == UnitKind.Tower && unit.Team != self.Team) return unit;
            return null;
        }
        bool TowerRange(Combatant tower, Vector3 point)
        { return tower != null && Vector3.Distance(tower.Position, point) <= tower.TowerAggroRadius + 0.6f; }
        bool TowerTargetsUs(Combatant tower)
        { return tower != null && (tower.AttackTarget == self || tower.PendingAttackTarget == self); }
        Vector3 TowerExit(Combatant tower)
        {
            if (tower == null) return world.FountainPoint(self.Team);
            return world.Clamp(tower.Position - Vector3.right * Forward * (tower.TowerAggroRadius + 1.5f));
        }
        float EscapeSeconds(Combatant tower)
        {
            return tower == null ? 0 : Mathf.Max(0, tower.TowerAggroRadius + 1.5f - Vector3.Distance(self.Position, tower.Position)) / self.MoveSpeed + Reaction + 0.8f;
        }
        bool CanSiege(Combatant tower, Combatant player)
        {
            if (tower == null || TowerTargetsUs(tower) || self.Health < self.MaxHealth * 0.55f || Time.time < siegeRestUntil) return false;
            float escape = EscapeSeconds(tower);
            if (!player.Alive && player.RespawnRemaining <= Mathf.Max(6, escape + 3)) return false;
            if (player.ItemEffects.IsReviving && player.ItemEffects.ReviveRemaining <= escape + 3) return false;
            if (player.Targetable && (Vector3.Distance(player.Position, tower.Position) < 11 ||
                Vector3.Distance(player.Position, tower.Position) / player.MoveSpeed < escape + 2 || self.Summoners.PendingIgniteDamage(player) > 0)) return false;
            int attacking = 0;
            float protection = 0;
            foreach (Combatant minion in world.Units)
            {
                if (minion == null || !minion.Alive || minion.Team != self.Team || minion.Kind != UnitKind.Minion ||
                    minion.AttackTarget != tower || Vector3.Distance(minion.Position, tower.Position) > tower.AttackReach(minion)) continue;
                attacking++;
                float damagePerSecond = CombatRules.Mitigate(tower.BasicDamageAgainst(minion, tower.AttackDamage), minion.Armor) * tower.BaseAttackSpeed;
                // Nearby hostile minions can remove the screen earlier than turret damage alone.
                foreach (Combatant other in world.Units)
                    if (other != null && other.Alive && other.Team != self.Team && other.Kind == UnitKind.Minion && Vector3.Distance(other.Position, minion.Position) < 4.5f)
                        damagePerSecond += CombatRules.Mitigate(other.BasicDamageAgainst(minion, other.AttackDamage), minion.Armor) * other.BaseAttackSpeed;
                protection += Mathf.Max(0, minion.Health - tower.AttackDamage * 0.5f) / Mathf.Max(1, damagePerSecond);
            }
            return attacking >= 2 && protection > escape + 2;
        }
    }
}
