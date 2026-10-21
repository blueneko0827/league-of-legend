using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        void UseStartingHealthPotion()
        {
            if (!world.NormalplayModeActive || !self.Alive || self.HealthPotionActive || self.Health >= self.MaxHealth) return;
            float missing = self.MaxHealth - self.Health;
            float pressure = MinionDamageAt(self.Position, 2, true);
            Combatant player = world.Player;
            bool trading = player.Targetable && Vector3.Distance(self.Position, player.Position) < 7;
            // Do not waste most of the potion, or wait for a lethal-health panic threshold.
            if (missing < 120 && !(self.Health < self.MaxHealth * .6f && (trading || pressure > 0))) return;
            int slot = self.Inventory.Items.FindIndex(item => item.id == 2003);
            if (slot >= 0) self.UseItem(slot);
        }

        float LaneBasicDamage(Combatant target)
        {
            float raw = self.Sett != null ? self.Sett.PreviewAttackDamage(target) : self.BasicDamageAgainst(target, self.AttackDamage);
            return target.PreviewIncomingDamage(raw, self, DamageType.Physical, true);
        }
        float LaneBasicArrival(Combatant target)
        {
            if (self.PendingAttackTarget == target) return self.AttackWindupRemaining +
                (self.Mel != null ? Vector3.Distance(self.Position, target.Position) / MelRules.BasicMissileSpeed : 0);
            return self.AttackWindupDuration + (self.Mel != null ? Vector3.Distance(self.Position, target.Position) / MelRules.BasicMissileSpeed : 0);
        }
        float StationaryLaneDamage(float seconds, Combatant removed = null)
        {
            float step = seconds / 4, damage = MinionProjectilesDue(seconds);
            for (int i = 0; i < 4; i++) damage += LaneThreatDps(self.Position, step * (i + .5f), removed) * step;
            return damage;
        }
        bool CanSurviveLaneAction(float seconds)
        { return StationaryLaneDamage(seconds + .15f) * 1.35f + self.MaxHealth * .02f < self.Health + self.TotalShield; }

        bool TrySafeLaneFinisher(Combatant player)
        {
            if (!world.NormalplayModeActive || self.Health > self.MaxHealth * .42f || !self.CanAct || !self.ValidEnemy(player) || world.Settings.testMode ||
                player.Inventory.Has(3026) || TowerRange(EnemyTower(), self.Position) || TowerRange(EnemyTower(), player.Position)) return false;
            float distance = Vector3.Distance(self.Position, player.Position);
            bool basic = (self.AttackReady || self.PendingAttackTarget == player) && distance <= self.AttackReach(player) - .05f;
            if (basic)
            {
                float arrival = LaneBasicArrival(player);
                float health = player.Health + player.TotalShield + player.HealthRegeneration * (arrival + .2f);
                if (LaneBasicDamage(player) >= health * 1.12f && CanSurviveLaneAction(arrival))
                {
                    laneDefenseUntil = 0; recovering = false; tradeUntil = melLaneTradeUntil = 0;
                    Plan(LaneTactic.AllIn, "Remate corto seguro · sin perseguir");
                    self.HoldAttackPosition();
                    if (self.PendingAttackTarget != player) self.TryAttack(player);
                    return true;
                }
            }
            if (self.Mel != null && self.Ready(0) && distance <= MelRules.QRange)
            {
                float arrival = MelRules.QCastTime + distance / MelRules.QSpeed;
                float immobilized = Mathf.Max(player.StunRemaining, player.RootRemaining);
                float damage = player.PreviewIncomingDamage(MelRules.QDamage(self.Progress.Rank(0), self.AbilityPower, true), self, DamageType.Magic);
                if (immobilized > arrival + .15f && damage > (player.Health + player.TotalShield + player.HealthRegeneration * arrival) * 1.15f &&
                    CanSurviveLaneAction(arrival) && self.Cast(0, player.Position))
                {
                    laneDefenseUntil = 0; recovering = false; melCombo = MelCombo.None;
                    Plan(LaneTactic.AllIn, "Mel: Q de remate seguro desde su posición"); return true;
                }
            }
            return false;
        }

        bool CanSpendLaneFarmQ()
        {
            if (self.Mel == null || !self.Ready(0)) return false;
            float reserve = (self.Progress.Rank(2) > 0 ? self.AbilityCost(2) : 0) + (self.Progress.Rank(1) > 0 ? self.AbilityCost(1) : 0);
            return self.Resource - self.AbilityCost(0) >= reserve * .7f;
        }
        bool TryRemoveMinionThreat(Combatant player)
        {
            Combatant chosen = null; bool useQ = false; float best = float.PositiveInfinity;
            Vector3 escape = DefensiveLanePoint(player);
            float escapeDamage = LeastLaneEscapeDamage(escape);
            foreach (Combatant minion in world.Units)
            {
                if (minion == null || minion.Kind != UnitKind.Minion || !self.ValidEnemy(minion) ||
                    (minion.AttackTarget != self && minion.PendingAttackTarget != self)) continue;
                bool basic = (self.AttackReady || self.PendingAttackTarget == minion) &&
                    Vector3.Distance(self.Position, minion.Position) <= self.AttackReach(minion);
                for (int option = 0; option < 2; option++)
                {
                    bool q = option == 1;
                    if (!q && !basic || q && (!CanSpendLaneFarmQ() || Vector3.Distance(self.Position, minion.Position) > MelRules.QRange)) continue;
                    float arrival = q ? MelRules.QCastTime + Vector3.Distance(self.Position, minion.Position) / MelRules.QSpeed : LaneBasicArrival(minion);
                    if (arrival > 1) continue;
                    float damage = q ? minion.PreviewIncomingDamage(MelRules.QDamage(self.Progress.Rank(0), self.AbilityPower, true), self, DamageType.Magic) : LaneBasicDamage(minion);
                    float hp = PredictMinionHealth(minion, arrival);
                    if (hp <= 0 || hp > damage) continue;
                    if (q && minion.MinionType != MinionType.Cannon)
                    {
                        int threats = 0;
                        foreach (Combatant other in world.Units)
                            if (other != null && other.Kind == UnitKind.Minion && self.ValidEnemy(other) && other.AttackTarget == self &&
                                Vector3.Distance(other.Position, minion.Position) <= MelRules.QExplosionRadius) threats++;
                        if (threats < 2) continue;
                    }
                    float before = StationaryLaneDamage(arrival + .1f);
                    float after = StationaryLaneDamage(.8f, minion);
                    float cost = before + after;
                    if (cost * 1.35f >= self.Health + self.TotalShield || cost >= escapeDamage * .9f || cost >= best) continue;
                    chosen = minion; useQ = q; best = cost;
                }
            }
            if (chosen == null) return false;
            if (useQ)
            {
                if (!self.Cast(0, chosen.Position)) return false;
            }
            else
            {
                self.HoldAttackPosition();
                if (self.PendingAttackTarget != chosen && !self.TryAttack(chosen)) return false;
            }
            Plan(LaneTactic.Farm, useQ ? "Mel: quitar presión de minions con Q" : "Eliminar minion atacante antes de retirarse");
            return true;
        }
    }
}
