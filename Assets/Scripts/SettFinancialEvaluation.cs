using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        float financialIgniteRoi;
        bool FinancialRLandingSafe(Combatant player, Vector3 aim)
        {
            Vector3 direction = (aim - self.Position).normalized;
            Vector3 landing = world.AvoidTower(player, player.Position + direction * PracticeAbilityGeometry.SettRDisplacement);
            Vector3 settLanding = world.AvoidTower(self, landing - direction * (self.Radius + player.Radius));
            return !TowerRange(EnemyTower(), landing) && !TowerRange(EnemyTower(), settLanding);
        }
        bool FinancialIgniteProfitable(Combatant player, Combatant tower)
        {
            if (world.Settings.testMode || !player.Targetable || TowerRange(tower, self.Position) || TowerRange(tower, player.Position)) return false;
            float hp = player.Health + player.TotalShield + player.HealthRegeneration * 5;
            float damage = SummonerRules.IgniteDamage(self.Level);
            float probability = damage >= hp ? 1 : KillProbability;
            float risk = Mathf.Clamp(ThreatDamage(player, self.Position, 1) / Mathf.Max(1, self.Health + self.TotalShield) * 0.5f, 0.05f, 1);
            financialIgniteRoi = FinancialUtility.Roi(probability, risk, PlayerAttractionGold, FinancialConfig.estimated_death_loss_gold, 0);
            return financialIgniteRoi >= FinancialConfig.all_in_roi_threshold;
        }
        void FindFinancialFarm(Combatant player, Combatant tower, out Combatant lastHit, out Combatant wave)
        {
            lastHit = wave = null; float nearest = float.PositiveInfinity;
            foreach (Combatant minion in world.Units)
            {
                if (minion == null || minion.Kind != UnitKind.Minion || !self.ValidEnemy(minion)) continue;
                float distance = Vector3.Distance(self.Position, minion.Position);
                // Zero intrinsic combat risk for a minion does not remove champion/turret hazards.
                if (distance > 14 || !SafeFarm(minion, player, tower)) continue;
                if (distance < nearest) { nearest = distance; wave = minion; }
                float travel = Mathf.Max(0, distance - self.AttackReach(minion)) / self.MoveSpeed;
                float horizon = Mathf.Max(travel, self.AttackCooldownRemaining) + self.AttackWindupDuration;
                float hp = PredictMinionHealth(minion, horizon), damage = Physical(self, minion, self.Sett.PreviewAttackDamage(minion));
                if (hp <= 0 || hp > damage || horizon > 2) continue;
                float utility = FinancialConfig.lasthit_priority_over_poke * FinancialUtility.GoldPerMinute(
                    FinancialConfig.MinionGold(minion.MinionType), 0, horizon + Reaction);
                if (utility <= LastHitUtility) continue;
                LastHitUtility = utility; lastHit = minion;
            }
        }

        float ForegoneFarmGold(float horizon)
        {
            float gold = 0;
            foreach (Combatant minion in world.Units)
            {
                if (minion == null || minion.Kind != UnitKind.Minion || !self.ValidEnemy(minion) || Vector3.Distance(self.Position, minion.Position) > 10) continue;
                float hp = PredictMinionHealth(minion, horizon);
                if (hp <= self.Sett.PreviewAttackDamage(minion)) gold += FinancialConfig.MinionGold(minion.MinionType);
            }
            return gold;
        }

        bool FinancialTradeSafe(Combatant player, Combatant tower)
        {
            if (!player.Targetable || TowerRange(tower, self.Position) || TowerRange(tower, player.Position)) return false;
            float health = self.Health / self.MaxHealth;
            return health >= 0.42f && !(player.Health / player.MaxHealth > health + 0.18f && health < 0.7f) &&
                ThreatDamage(player, self.Position, 2.5f) < (self.Health + self.TotalShield) * 0.65f;
        }

        void EvaluateFinancialCombat(Combatant player, Combatant tower, Combatant lastHit, out bool imminent)
        {
            imminent = false;
            if (!player.Targetable || TowerRange(tower, self.Position) || TowerRange(tower, player.Position)) return;
            float distance = Vector3.Distance(self.Position, player.Position);
            float health = player.Health + player.TotalShield + player.HealthRegeneration * 3;
            float selfHealth = Mathf.Max(1, self.Health + self.TotalShield);
            float basic = Physical(self, player, self.Sett.PreviewAttackDamage(player));
            float travel = Mathf.Max(0, distance - self.AttackReach(player)) / self.MoveSpeed;
            float pokeSeconds = Mathf.Max(0.5f, travel + 1 / self.BaseAttackSpeed + self.AttackWindupDuration);
            bool pokeQ = self.Cooldowns.Ready(0, Time.time) && self.Progress.Rank(0) > 0;
            float pokeDamage = Physical(self, player, self.Sett.PreviewAttackSequenceDamage(player, pokeSeconds - travel, pokeQ));
            float incomingBasics = Physical(player, self, player.AttackDamage) * (1 + Mathf.Floor(pokeSeconds * player.BaseAttackSpeed));
            float counter = incomingBasics;
            if (Time.time >= observedReady[0] && player.Resource >= player.AbilityCost(0)) counter += Physical(player, self, player.Blade.QRawDamage(self)) * 0.5f;
            if (player.Blade.Stacks == 4) counter += CombatRules.Mitigate(10 + 3 * (player.Level - 1), self.MagicResist) * 2;
            if (player.IsStunned) counter *= 0.4f;
            float pokeReward = PlayerAttractionGold * Mathf.Min(0.5f, pokeDamage / Mathf.Max(1, health));
            float pokeCost = FinancialConfig.estimated_death_loss_gold * counter / selfHealth;
            PokeUtility = (1 - FinancialConfig.lasthit_priority_over_poke) * FinancialUtility.GoldPerMinute(pokeReward, pokeCost, pokeSeconds);
            if (distance > self.AttackReach(player) + 0.9f && Time.time >= tradeUntil) PokeUtility = 0;

            if (world.Settings.testMode || self.Health < self.MaxHealth * 0.35f) return;
            Vector3 gap = player.Position - self.Position;
            float closingSpeed = self.MoveSpeed - Mathf.Max(0, Vector3.Dot(estimatedVelocity, gap.normalized));
            float chase = distance <= self.AttackReach(player) ? 0 : Mathf.Max(0, distance - self.AttackReach(player)) / Mathf.Max(0.05f, closingSpeed);
            if (chase > 1.4f || (chase > 0 && closingSpeed < 0.5f)) return;
            float seconds = 3.2f, available = seconds - chase, damage = 0;
            float bonusAD = self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level);
            if (self.Cooldowns.Ready(2, Time.time) && self.Progress.Rank(2) > 0 && available >= 0.25f)
            { damage += Physical(self, player, 30 + self.Progress.Rank(2) * 20 + self.AttackDamage * 0.6f) * 0.85f; available -= 0.25f; }
            if (self.Cooldowns.Ready(3, Time.time) && self.Progress.Rank(3) > 0 && distance <= SettAbilities.RRange && available >= 1.75f)
            {
                Vector3 landing = world.Clamp(player.Position + gap.normalized * PracticeAbilityGeometry.SettRDisplacement);
                if (!TowerRange(tower, landing) && FinancialRLandingSafe(player, player.Position))
                { damage += Physical(self, player, 100 + self.Progress.Rank(3) * 100 + bonusAD * 1.2f); available -= 1.75f; }
            }
            if (self.Cooldowns.Ready(1, Time.time) && available >= 0.75f && ShouldUseHaymaker(player, player.Position))
            { damage += Physical(self, player, self.Sett.HaymakerDamage) * 0.6f; available -= 0.75f; }
            bool q = self.Cooldowns.Ready(0, Time.time) && self.Progress.Rank(0) > 0 && available > 0.15f;
            if (q) available -= 0.15f;
            damage += Physical(self, player, self.Sett.PreviewAttackSequenceDamage(player, available, q));
            damage += self.Summoners.PendingIgniteDamage(player);
            KillProbability = Mathf.Clamp01((damage / Mathf.Max(1, health) - 0.55f) / 0.65f);
            float incoming = ThreatDamage(player, self.Position, seconds + chase);
            DeathProbability = Mathf.Clamp((incoming / selfHealth - 0.2f) / 0.9f, 0.05f, 1);
            float missedFarm = ForegoneFarmGold(seconds);
            AllInRoi = FinancialUtility.Roi(KillProbability, DeathProbability, PlayerAttractionGold,
                FinancialConfig.estimated_death_loss_gold, missedFarm);
            AllInUtility = FinancialUtility.GoldPerMinute(KillProbability * PlayerAttractionGold,
                DeathProbability * FinancialConfig.estimated_death_loss_gold + missedFarm, seconds + chase);
            // Only a confirmed next basic can interrupt a last-hit commitment for a kill.
            imminent = self.AttackReady && distance <= self.AttackReach(player) && basic >= health;
        }
    }
}
