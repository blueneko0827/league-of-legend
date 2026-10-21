using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        float laneDefenseUntil, laneDefensePointAt;
        Vector3 laneDefensePoint;

        float MinionDamageAt(Vector3 point, float seconds, bool committedOnly = false)
        {
            float damage = 0;
            foreach (BasicProjectile shot in world.BasicProjectiles)
                if (shot != null && shot.Target == self && shot.Source != null && shot.Source.Kind == UnitKind.Minion && shot.TimeToImpact <= seconds)
                    damage += self.PreviewIncomingDamage(shot.Source.BasicDamageAgainst(self, shot.RawDamage), shot.Source, DamageType.Physical);
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit.Kind != UnitKind.Minion || !self.ValidEnemy(unit)) continue;
                bool targeting = unit.AttackTarget == self || unit.PendingAttackTarget == self;
                if (committedOnly && !targeting) continue;
                if (Vector3.Distance(unit.Position, point) > unit.AttackReach(self) + .35f) continue;
                Combatant siegeTarget = unit.AttackTarget;
                if (!targeting && siegeTarget != null && siegeTarget.Kind == UnitKind.Tower && siegeTarget.Team == self.Team &&
                    Vector3.Distance(unit.Position, siegeTarget.Position) <= unit.AttackReach(siegeTarget) &&
                    Vector3.Distance(point, siegeTarget.Position) <= siegeTarget.AttackReach(self)) continue;
                float hit = self.PreviewIncomingDamage(unit.BasicDamageAgainst(self, unit.AttackDamage), unit, DamageType.Physical);
                damage += hit * Mathf.Max(1, unit.BaseAttackSpeed * seconds) * (targeting ? 1 : .5f);
            }
            return damage;
        }

        bool MinionFarmSafe(Vector3 point)
        { return MinionDamageAt(point, 2) < (self.Health + self.TotalShield) * .35f; }

        bool TryLaneDefense(Combatant player)
        {
            if (DuelModeActive || !self.CanAct) return false;
            if (TrySafeLaneFinisher(player)) return true;
            float incoming = MinionDamageAt(self.Position, 2, true);
            bool minionThreat = incoming > (self.Health + self.TotalShield) * .25f;
            float distance = Vector3.Distance(self.Position, player.Position), hp = self.Health / self.MaxHealth;
            bool championThreat = player.Targetable && (hp < .26f && distance < 7 ||
                hp < .42f && distance < Mathf.Max(3.5f, player.AttackReach(self) + 1));
            if (minionThreat || championThreat) laneDefenseUntil = Time.time + .8f;
            if (minionThreat && TryRemoveMinionThreat(player)) return true;
            recovering = Time.time < laneDefenseUntil;
            if (!recovering) return false;
            if (self.Mel != null) { melCombo = MelCombo.None; melLaneTradeUntil = 0; }
            Recover(player, Vector3.Distance(self.Position, player.Position));
            return true;
        }

        Vector3 DefensiveLanePoint(Combatant player)
        {
            if (Time.time < laneDefensePointAt) return laneDefensePoint;
            laneDefensePointAt = Time.time + .6f;
            Vector3 home = Vector3.left * Forward;
            Combatant ownTower = null, screen = null;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !unit.Alive || unit.Team != self.Team) continue;
                if (unit.Kind == UnitKind.Tower) ownTower = unit;
                if (unit.Kind == UnitKind.Minion && Vector3.Distance(self.Position, unit.Position) < 12 &&
                    (screen == null || unit.Position.x * Forward > screen.Position.x * Forward)) screen = unit;
            }
            Vector3 anchor = screen != null ? screen.Position + home * 2 :
                ownTower != null ? ownTower.Position - home * 2.5f : new Vector3(-Forward * 3, 0, 0);
            Vector3 away = player.Targetable ? self.Position - player.Position : home;
            away.y = 0;
            if (away.sqrMagnitude < .01f) away = home;
            Vector3 minionAway = Vector3.zero;
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Kind == UnitKind.Minion && self.ValidEnemy(unit) && unit.AttackTarget == self)
                    minionAway += (self.Position - unit.Position).normalized;
            var candidates = new[] { anchor, self.Position + home * 3, self.Position + away.normalized * 3,
                self.Position + minionAway.normalized * 3, self.Position + home * 2 + Vector3.forward * 2,
                self.Position + home * 2 - Vector3.forward * 2, self.Position };
            float best = float.PositiveInfinity;
            laneDefensePoint = self.Position;
            foreach (Vector3 candidate in candidates)
            {
                Vector3 point = world.Clamp(world.AvoidTower(self, candidate), self.Radius);
                if (TowerRange(EnemyTower(), point)) continue;
                float danger = ThreatDamage(player, point, 2);
                float travel;
                danger += RouteLaneDamage(point, point, out travel);
                if (player.Targetable)
                    danger += Mathf.Max(0, Mathf.Max(5.5f, player.AttackReach(self) + 1.5f) -
                        Vector3.Distance(player.Position, point)) * player.AttackDamage;
                float score = danger + Vector3.Distance(point, anchor) * 3 + Vector3.Distance(point, self.Position) * 2;
                if (score >= best) continue;
                best = score; laneDefensePoint = point;
            }
            return laneDefensePoint;
        }
    }
}
