using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        float tradeUntil, tradeRestUntil, siegeUntil, siegeRestUntil;
        bool recovering, igniteOpportunityRolled;
        int tradeSpellCount;
        float Aggression { get { return Mathf.Clamp01((world.Settings.difficulty == Difficulty.Expert ? 1 : world.Settings.difficulty == Difficulty.Hard ? 0.55f : 0) * HumanAggression); } }
        float TradeRecovery { get { return Mathf.Lerp(3, 0.65f, Aggression); } }
        float EngageReach { get { return self.AttackReach(world.Player) + Mathf.Lerp(1.5f, 2.8f, Aggression); } }
        int Forward { get { return self.Team == Team.Blue ? 1 : -1; } }

        void ResetLanePlan()
        {
            tradeUntil = tradeRestUntil = siegeUntil = siegeRestUntil = 0;
            recovering = igniteOpportunityRolled = false; tradeSpellCount = 0;
            laneDefenseUntil = laneDefensePointAt = 0;
            laneRouteAt = wavePlanAt = 0;
            laneRoutePoints.Clear();
            laneApproachFrame = -1; laneApproaches.Clear();
        }
        void Plan(LaneTactic tactic, string label) { Tactic = tactic; Decision = label; HumanPlan(tactic); }

        void ThinkLane()
        {
            if (DuelModeActive) { ThinkDuel(); return; }
            Combatant player = world.Player, tower = EnemyTower();
            float distance = Vector3.Distance(self.Position, player.Position);
            float health = self.Health / self.MaxHealth;
            bool inTower = TowerRange(tower, self.Position);
            UpdateLaneWavePlan(player);
            if (TryLaneDefense(player)) return;
            if (inTower && (TowerTargetsUs(tower) || !CanSiege(tower, player)))
            {
                tradeUntil = 0; siegeUntil = 0; siegeRestUntil = Time.time + 4;
                Plan(LaneTactic.Retreat, "Evitar torre");
                self.MoveTo(TowerExit(tower)); return;
            }
            if (DodgeWarnings(player)) return;
            if (self.IsWindingAttack) return;

            bool lethal = player.Targetable && CanFinish(player, tower);
            if (TryIgnite(player, tower)) { Plan(LaneTactic.AllIn, "Ignite para rematar"); return; }
            // A burn that already secures the kill is a reason to disengage, not to dive.
            if (player.Targetable && self.Summoners.PendingIgniteDamage(player) >= player.Health + player.TotalShield + player.HealthRegeneration * 5)
            { Plan(LaneTactic.Zone, "Esperar el remate de Ignite"); MoveAway(player, self.AttackReach(player) + 2); return; }
            if (lethal)
            { Plan(LaneTactic.AllIn, "Buscar eliminación segura"); Fight(player, true); return; }

            Combatant lastHit, wave;
            FindFarm(tower, player, out lastHit, out wave);
            if (lastHit != null && !HumanNoticedFarm(lastHit)) lastHit = null;
            if (lastHit != null)
            {
                // A short trade cannot steal the next safe last-hit window.
                tradeUntil = 0;
                Plan(LaneTactic.Farm, "Último golpe");
                if (self.AttackReady) { self.OrderAttack(lastHit); self.TryAttack(lastHit); }
                else ApproachFarm(lastHit);
                return;
            }
            if (WavePlan == LaneWavePlan.Freeze && TryLaneWavePosition(player)) return;
            if (tradeUntil > 0 && Time.time >= tradeUntil)
            { tradeUntil = 0; tradeRestUntil = Time.time + TradeRecovery; }
            if (Time.time < tradeUntil)
            {
                if (player.Targetable && SafeTrade(player, tower, wave))
                { Plan(LaneTactic.Trade, "Intercambio corto"); Fight(player, false); return; }
                tradeUntil = 0; tradeRestUntil = Time.time + TradeRecovery;
            }
            if (Time.time < tradeRestUntil && player.Targetable && distance < self.AttackReach(player) + 2)
            { Plan(LaneTactic.Zone, "Salir del intercambio y volver al farm"); MoveAway(player, self.AttackReach(player) + 2); return; }

            bool playerThreat = player.Targetable && !SafeTrade(player, tower, null) && distance < 7;
            if (playerThreat)
            {
                Plan(LaneTactic.Zone, "Ceder minions para evitar un mal intercambio");
                if (health < 0.42f) { recovering = true; Recover(player, distance); }
                else MoveAway(player, Mathf.Max(5, player.AttackReach(self) + 2));
                return;
            }
            bool tradingWindow = player.Targetable && Time.time >= tradeRestUntil && SafeTrade(player, tower, wave) &&
                (distance <= self.AttackReach(player) || self.WasDamagedBy(player, 1.5f) ||
                (wave == null && distance <= self.AttackReach(player) + 2.5f) ||
                (wave != null && player.Health / player.MaxHealth + 0.18f < health && distance < 3.8f) ||
                (wave != null && Aggression > 0 && distance <= EngageReach && self.AttackReady) ||
                (Time.time < observedReady[0] && distance <= EngageReach));
            if (tradingWindow)
            {
                tradeUntil = Time.time + (world.Settings.difficulty == Difficulty.Expert ? 4.2f : 3.3f); tradeSpellCount = 0;
                Plan(LaneTactic.Trade, "Intercambio corto"); Fight(player, false); return;
            }
            bool rivalAbsent = !player.Targetable || distance > 11;
            if (TryLaneWaveClear(player) || TryLaneWavePosition(player)) return;
            if (wave != null)
            {
                Plan(rivalAbsent ? LaneTactic.Push : LaneTactic.Farm, rivalAbsent ? "Empujar la oleada hacia torre" : "Preparar último golpe");
                if (rivalAbsent) self.OrderAttack(wave);
                else ApproachFarm(wave);
                return;
            }
            if (rivalAbsent && CanSiege(tower, player) && Time.time >= siegeRestUntil)
            {
                if (siegeUntil == 0 && inTower) siegeUntil = Time.time + 7;
                if (siegeUntil == 0 || Time.time < siegeUntil)
                { Plan(LaneTactic.Push, "Presionar torre con la oleada"); self.OrderAttack(tower); return; }
                siegeUntil = 0; siegeRestUntil = Time.time + 4;
            }
            if (tower != null && (inTower || (!player.Alive && player.RespawnRemaining < 6)))
            { Plan(LaneTactic.Retreat, "Salir antes de la reaparición rival"); self.MoveTo(TowerExit(tower)); return; }
            Plan(LaneTactic.Farm, "Esperar la oleada desde una posición segura");
            float rally = rivalAbsent && tower != null ? TowerExit(tower).x : -Forward * 3;
            MoveOrHold(new Vector3(rally, 0, 0));
        }

        void Recover(Combatant player, float distance)
        {
            if (world.PvpModeActive) { recovering = false; Plan(LaneTactic.AllIn, "PvP: mantener el all-in"); self.OrderAttack(player); return; }
            tradeUntil = 0;
            Plan(LaneTactic.Recover, "Jugar seguro · salir del daño y buscar cobertura");
            // Mel's defense module owns Rebuttal and its observed threat/reaction clock.
            if (self.Mel == null && player.Targetable && distance < 4 && (self.Sett == null || ShouldUseHaymaker(player, player.Position))) self.Cast(1, player.Position, player);
            if (player.Targetable && self.Health < self.MaxHealth * 0.23f && distance < 8 && self.Summoners.FlashRemaining <= 0)
            {
                Vector3 away = self.Position - player.Position;
                Vector3 home = DefensiveLanePoint(player) - self.Position;
                if (away.sqrMagnitude < 0.01f) away = Vector3.right * -Forward;
                Vector3 direction = (away.normalized + home.normalized * 0.65f).normalized;
                Vector3 cursor = self.Position + direction * SummonerRules.FlashRange;
                Combatant tower = EnemyTower();
                if (!TowerRange(tower, world.Clamp(cursor))) self.Summoners.Flash(cursor);
            }
            MoveOrHold(DefensiveLanePoint(player));
        }

        void Fight(Combatant player, bool allIn)
        {
            Combatant tower = EnemyTower();
            if (TowerRange(tower, self.Position) || TowerRange(tower, player.Position))
            { Plan(LaneTactic.Retreat, "Evitar torre"); self.MoveTo(TowerExit(tower)); return; }
            float distance = Vector3.Distance(self.Position, player.Position);
            if (self.Archetype == Archetype.Brawler && distance <= 4.5f + player.Radius)
                self.UseItem(self.Inventory.Items.FindIndex(item => item.id == 6631));
            if (distance <= self.AttackReach(player) && self.AttackReady)
            { self.OrderAttack(player); self.TryAttack(player); return; }
            Vector3 aim = Aim(player, distance);
            if ((allIn || tradeSpellCount < (world.Settings.difficulty == Difficulty.Expert ? 2 : 1)) && CastForFight(player, aim, distance, allIn))
            { tradeSpellCount++; return; }
            if (world.PvpModeActive || distance <= (allIn ? self.AttackReach(player) + 5 : EngageReach)) self.OrderAttack(player);
            else { tradeUntil = 0; tradeRestUntil = Time.time + TradeRecovery; self.Stop(); }
        }

        bool CastForFight(Combatant player, Vector3 aim, float distance, bool allIn)
        {
            if (self.Mel != null)
            {
                // R has no range restriction. W answers projectiles instead of pretending to grant immunity.
                if (self.Ready(3) && self.Mel.OverwhelmStacks(player) > 0 &&
                    (allIn || player.Health + player.TotalShield < MelRules.RDamage(self.Progress.Rank(3), self.Mel.OverwhelmStacks(player), self.AbilityPower)) && self.Cast(3, player.Position)) return true;
                foreach (BladeWave wave in world.BladeWaves)
                    if (wave != null && wave.Threatens(self) && Vector3.Distance(wave.Head, self.Position) < 5 && self.Cast(1, self.Position)) return true;
                if (self.Health < self.MaxHealth * .4f && distance < 4 && self.Cast(1, self.Position)) return true;
                if (distance <= MelRules.ERange && !player.IsRooted && self.Cast(2, aim)) return true;
                return distance <= MelRules.QRange + player.Radius && self.Cast(0, aim);
            }
            if (self.Sett != null && distance <= SettAbilities.WRange && ShouldUseHaymaker(player, aim) && self.Cast(1, aim, player)) return true;
            if (allIn && distance < (self.Archetype == Archetype.Brawler ? SettAbilities.RRange : 9) &&
                (FinancialConfig == null || FinancialRLandingSafe(player, aim)) && self.Cast(3, aim, player)) return true;
            if (self.Sett == null && self.Health < self.MaxHealth * 0.55f && distance < 5 && self.Cast(1, aim, player)) return true;
            if (self.Archetype == Archetype.Brawler)
            {
                if (distance < 3.2f && self.Cast(0, aim, player)) return true;
                return distance < SettAbilities.ERange && self.Cast(2, aim);
            }
            if (distance < (self.Archetype == Archetype.Siphon ? 6.5f : 10) && self.Cast(0, aim, player)) return true;
            return distance < 7 && self.Cast(2, aim);
        }

        Vector3 Aim(Combatant player, float distance)
        {
            float lead = world.Settings.difficulty == Difficulty.Normal ? 0 : world.Settings.difficulty == Difficulty.Hard ? 0.35f : 0.5f;
            float error = CombatRules.AimError(world.Settings.difficulty);
            return player.Position + estimatedVelocity * ((lead + distance / 18 * lead) * HumanPrediction) + HumanAimOffset(error);
        }

        bool TryIgnite(Combatant player, Combatant tower)
        {
            if (!player.Targetable) { igniteOpportunityRolled = false; return false; }
            float ignite = SummonerRules.IgniteDamage(self.Level);
            float effectiveHealth = player.Health + player.TotalShield + player.HealthRegeneration * 5;
            bool opportunity = effectiveHealth <= ignite ||
                (player.Health < player.MaxHealth * 0.3f && effectiveHealth <= ignite + BurstDamage(player, 1.8f));
            if (!opportunity) { igniteOpportunityRolled = false; return false; }
            if (FinancialConfig != null && !FinancialIgniteProfitable(player, tower)) return false;
            if (igniteOpportunityRolled || self.Summoners.IgniteRemaining > 0 || TowerRange(tower, self.Position) ||
                Vector3.Distance(self.Position, player.Position) > SummonerRules.IgniteRange + player.Radius) return false;
            igniteOpportunityRolled = true;
            return Random.value < world.Settings.aiIgniteChance && self.Summoners.Ignite(player);
        }

        bool CanFinish(Combatant player, Combatant tower)
        {
            if (world.Settings.testMode || TowerRange(tower, self.Position) || TowerRange(tower, player.Position)) return false;
            Vector3 gap = player.Position - self.Position;
            float chaseDistance = Mathf.Max(0, gap.magnitude - self.AttackReach(player));
            float escapeSpeed = Mathf.Max(0, Vector3.Dot(estimatedVelocity, gap.normalized));
            float closingSpeed = self.MoveSpeed - escapeSpeed;
            if (chaseDistance > 0 && closingSpeed < 0.5f) return false;
            float chase = chaseDistance / Mathf.Max(0.5f, closingSpeed);
            if (chase > 1.4f) return false;
            float damage = BurstDamage(player, 2.2f - chase) + self.Summoners.PendingIgniteDamage(player);
            float incoming = ThreatDamage(player, self.Position, 2.5f + chase);
            return damage > (player.Health + player.TotalShield + player.HealthRegeneration * 3) * 1.12f &&
                self.Health + self.TotalShield > incoming * 1.35f && self.Health > self.MaxHealth * 0.35f;
        }

        float BurstDamage(Combatant player, float seconds)
        {
            float hits = Mathf.Max(0, 1 + Mathf.Floor((seconds - self.AttackCooldownRemaining) * self.BaseAttackSpeed));
            float damage = Physical(self, player, self.AttackDamage) * hits;
            int[] raw = self.Archetype == Archetype.Brawler ? new[] { 100, 95, 60, 200 } :
                self.Archetype == Archetype.Siphon ? new[] { 85, 0, 110, 160 } : new[] { 95, 0, 55, 190 };
            for (int slot = 0; slot < 4; slot++)
                if (self.Progress.Rank(slot) > 0 && self.Cooldowns.Ready(slot, Time.time) && self.Resource >= self.AbilityCost(slot))
                    damage += (self.Archetype == Archetype.Brawler ? Physical(self, player, raw[slot]) : CombatRules.Mitigate(raw[slot], player.MagicResist)) * 0.6f;
            return damage;
        }
        float ThreatDamage(Combatant player, Vector3 point, float seconds)
        {
            float damage = 0;
            if (player.Targetable && Vector3.Distance(player.Position, point) < 7)
            {
                damage = Physical(player, self, player.AttackDamage) * (1 + player.BaseAttackSpeed * seconds);
                if (Time.time >= observedReady[0]) damage += Physical(player, self, player.AttackDamage * 0.6f + 55);
                damage += CombatRules.Mitigate(65, self.MagicResist);
            }
            return damage + MinionDamageAt(point, seconds);
        }
        static float Physical(Combatant source, Combatant target, float raw)
        {
            float armor = target.Armor;
            if (armor > 0) armor = Mathf.Max(0, armor * (1 - source.PercentArmorPenetration) - source.ArmorPenetration);
            if (target.Blade != null) raw *= 1 - target.Blade.DamageReduction(DamageType.Physical);
            float damage = CombatRules.Mitigate(raw, armor);
            if (target.Inventory != null && target.Inventory.Has(3047)) damage *= 0.9f;
            return damage;
        }
        bool SafeTrade(Combatant player, Combatant tower, Combatant wave)
        {
            if (TowerRange(tower, player.Position) || TowerRange(tower, self.Position)) return false;
            float health = self.Health / self.MaxHealth, enemyHealth = player.Health / player.MaxHealth;
            if (health < Mathf.Lerp(0.42f, 0.34f, Aggression) || (enemyHealth > health + Mathf.Lerp(0.18f, 0.25f, Aggression) && health < 0.7f)) return false;
            if (ThreatDamage(player, self.Position, 2.5f) > (self.Health + self.TotalShield) * Mathf.Lerp(0.6f, 0.8f, Aggression)) return false;
            // Don't abandon the wave to chase a retreating champion across the lane.
            return wave == null || Vector3.Distance(player.Position, wave.Position) < Mathf.Lerp(5, 6.5f, Aggression);
        }

        bool DodgeWarnings(Combatant player)
        {
            if (TryDodgeBladeLinks()) return true;
            foreach (BladeWave wave in world.BladeWaves)
            {
                if (wave == null || Time.time - wave.CreatedAt < Reaction || !wave.Threatens(self)) continue;
                if (self.Mel != null && Vector3.Distance(wave.Head, self.Position) < 5)
                {
                    if (self.Cast(1, self.Position)) { Decision = "Rebuttal: devolver proyectil"; return true; }
                    if (self.Mel.Reflecting) return false;
                }
                if (world.PvpModeActive && self.Sett != null && wave.IsUltimate) continue;
                Vector3 side = Vector3.Cross(wave.Direction, Vector3.up).normalized;
                if (Vector3.Dot(self.Position - wave.Head, side) < 0) side = -side;
                self.MoveTo(self.Position + side * 2); Decision = "Esquivar W/R"; return true;
            }
            foreach (Skillshot shot in world.Shots)
            {
                if (shot == null || Time.time - shot.CreatedAt < Reaction || Vector3.Distance(self.Position, shot.Head) > 14 || !shot.Threatens(self)) continue;
                if (world.Settings.difficulty == Difficulty.Normal && Random.value < 0.35f) break;
                if (self.Archetype == Archetype.Siphon && self.Cast(1, self.Position))
                { self.MoveTo(self.Position + (self.Position - player.Position).normalized * 3); Decision = "Evasión defensiva"; return true; }
                Vector3 side = Vector3.Cross(shot.Direction, Vector3.up).normalized;
                if (Vector3.Dot(self.Position - shot.Head, side) < 0) side = -side;
                Vector3 point = world.Clamp(self.Position + side * (shot.Width + self.Radius + 1.2f));
                if (Vector3.Distance(point, self.Position) < shot.Width + self.Radius) point = world.Clamp(self.Position - side * (shot.Width + self.Radius + 1.2f));
                self.MoveTo(point); Decision = "Esquivar telegráfico"; return true;
            }
            return false;
        }

        void MoveAway(Combatant player, float distance)
        {
            Vector3 away = self.Position - player.Position;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right * -Forward;
            Vector3 point = world.Clamp(player.Position + away.normalized * distance);
            Combatant tower = EnemyTower();
            if (TowerRange(tower, point)) point = TowerExit(tower);
            MoveOrHold(point);
        }
        void MoveOrHold(Vector3 point)
        { point = LaneSafeWaypoint(point); if (Vector3.Distance(self.Position, point) > 0.15f) self.MoveTo(point); else self.Stop(); }
        bool NoHostileMinions(Vector3 point, float radius)
        {
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Alive && unit.Kind == UnitKind.Minion && unit.Team != self.Team && Vector3.Distance(point, unit.Position) < radius) return false;
            return true;
        }
    }
}
