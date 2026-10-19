using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        public SettFinancialConfig FinancialConfig { get; private set; }
        public float LastHitUtility { get; private set; }
        public float PokeUtility { get; private set; }
        public float GoldTrapUtility { get; private set; }
        public float AllInUtility { get; private set; }
        public float AllInRoi { get; private set; }
        public float KillProbability { get; private set; }
        public float DeathProbability { get; private set; }
        public float PlayerAttractionGold { get; private set; }
        public Combatant GoldTrapMinion { get; private set; }
        public Vector3 NeutralSpacingPoint { get; private set; }
        public float SpacingNoiseUpdatedAt { get; private set; }
        public int FinancialDecisionCount { get; private set; }
        float financialStartedAt, financialBaseReaction, nextSpacingNoise;
        Vector3 spacingNoise;
        bool financialDead;
        Combatant dashCueTarget;
        int dashCueLife;
        float dashCueAt, dashCueBaseReaction;
        bool dashCueMatured;

        void InitializeFinancialBrain()
        {
            FinancialConfig = SettFinancialConfig.Load();
            PlayerAttractionGold = FinancialConfig.champion_kill_gold;
            financialBaseReaction = SampleFinancialReaction(); financialStartedAt = Time.time;
        }
        float SampleFinancialReaction() { return Random.Range(FinancialConfig.reaction_min_seconds, FinancialConfig.reaction_max_seconds); }
        void ResetFinancialPlan()
        {
            if (FinancialConfig == null || financialDead) return;
            financialDead = true; GoldTrapMinion = dashCueTarget = null; nextSpacingNoise = 0;
            LastHitUtility = PokeUtility = GoldTrapUtility = AllInUtility = AllInRoi = KillProbability = DeathProbability = 0;
        }
        void ObserveFinancialDash(Combatant player)
        {
            Combatant target = player.Blade != null ? player.Blade.DashTarget : null;
            if (target == null || target.Team != self.Team || target.Kind != UnitKind.Minion ||
                PredictMinionHealth(target, 0) > Physical(player, target, player.Blade.QRawDamage(target))) return;
            bool alreadyWaiting = dashCueTarget != null && !dashCueMatured && dashCueLife == player.Deaths && Time.time <= dashCueAt + 0.8f;
            dashCueTarget = target; dashCueLife = player.Deaths;
            // Keep the first pending reaction clock while tracking a chain's latest jump.
            if (!alreadyWaiting)
            { dashCueAt = Time.time; dashCueBaseReaction = SampleFinancialReaction(); dashCueMatured = false; }
        }
        void UpdateFinancialBrain()
        {
            if (financialDead)
            {
                financialDead = false; financialStartedAt = Time.time; financialBaseReaction = SampleFinancialReaction();
                lastPlayerPosition = world.Player.Position; lastSampleAt = Time.time;
            }
            // Sample each pending delay once. Advance its clock each frame, including panic
            // changes, rather than resampling every frame and biasing or postponing reactions.
            if (Time.time >= nextSpacingNoise)
            {
                Vector2 jitter = Random.insideUnitCircle * (FinancialConfig.spacing_noise_units * 0.01f);
                spacingNoise = new Vector3(jitter.x, 0, jitter.y);
                SpacingNoiseUpdatedAt = Time.time; nextSpacingNoise = Time.time + FinancialConfig.spacing_noise_interval_seconds;
            }
            for (int i = 0; i < 4; i++)
                if (observeAt[i] > 0 && Time.time >= observeAt[i]) { observedReady[i] = pendingReady[i]; observeAt[i] = 0; }
            if (dashCueTarget != null && (!world.Player.Targetable || world.Player.Deaths != dashCueLife || Time.time > dashCueAt + 0.8f)) dashCueTarget = null;
            float dashDelay = FinancialUtility.Reaction(dashCueBaseReaction, self.Health / self.MaxHealth,
                FinancialConfig.panic_health_threshold, FinancialConfig.panic_reaction_multiplier);
            bool dashDue = dashCueTarget != null && !dashCueMatured && Time.time >= dashCueAt + dashDelay;
            if (Time.time < financialStartedAt + Reaction && !dashDue) return;
            if (!self.CanAct) return;
            if (dashDue) dashCueMatured = true;
            float dt = Time.time - lastSampleAt;
            estimatedVelocity = dt > 0 ? Vector3.ClampMagnitude((world.Player.Position - lastPlayerPosition) / dt, world.Player.MoveSpeed) : Vector3.zero;
            lastSampleAt = Time.time; lastPlayerPosition = world.Player.Position;
            FinancialDecisionCount++;
            ThinkFinancialLane();
            financialStartedAt = Time.time; financialBaseReaction = SampleFinancialReaction();
        }

        void ThinkFinancialLane()
        {
            if (DuelModeActive) { ThinkDuel(); return; }
            Combatant player = world.Player, tower = EnemyTower();
            float distance = Vector3.Distance(self.Position, player.Position), health = self.Health / self.MaxHealth;
            if (recovering && health >= 0.85f) recovering = false;
            if (health < 0.26f) recovering = true;
            if (recovering) { Recover(player, distance); return; }
            bool inTower = TowerRange(tower, self.Position);
            if (inTower && (TowerTargetsUs(tower) || !CanSiege(tower, player)))
            {
                tradeUntil = siegeUntil = 0; siegeRestUntil = Time.time + 4;
                Plan(LaneTactic.Retreat, "Evitar torre"); self.MoveTo(TowerExit(tower)); return;
            }
            if (DodgeWarnings(player) || self.IsWindingAttack) return;

            LastHitUtility = PokeUtility = GoldTrapUtility = AllInUtility = AllInRoi = KillProbability = DeathProbability = 0;
            PlayerAttractionGold = FinancialConfig.champion_kill_gold; GoldTrapMinion = null;
            Combatant lastHit, wave;
            FindFinancialFarm(player, tower, out lastHit, out wave);
            GoldTrapMinion = FindGoldTrap(player);
            if (GoldTrapMinion != null) PlayerAttractionGold += FinancialConfig.q_bait_attraction_gold;
            bool imminent;
            EvaluateFinancialCombat(player, tower, lastHit, out imminent);
            GoldTrapUtility = EvaluateGoldTrapUtility(player, tower);
            bool profitableKill = KillProbability >= 0.65f && AllInRoi >= FinancialConfig.all_in_roi_threshold && AllInUtility > 0;
            if (lastHit != null && !(imminent && profitableKill))
            {
                tradeUntil = 0; Plan(LaneTactic.Farm, lastHit.MinionType == MinionType.Cannon ? "Asegurar cañón · utilidad de oro" : "Último golpe · utilidad de oro");
                if (self.AttackReady) { self.OrderAttack(lastHit); self.TryAttack(lastHit); } else ApproachFarm(lastHit);
                return;
            }
            if (TryIgnite(player, tower))
            { AllInRoi = financialIgniteRoi; Plan(LaneTactic.AllIn, "Ignite para rematar"); return; }
            if (player.Targetable && self.Summoners.PendingIgniteDamage(player) >= player.Health + player.TotalShield + player.HealthRegeneration * 5)
            { Plan(LaneTactic.Zone, "Esperar el remate de Ignite"); MoveAway(player, self.AttackReach(player) + 2); return; }
            if (profitableKill && (imminent || AllInUtility > Mathf.Max(PokeUtility, GoldTrapUtility)))
            { Plan(LaneTactic.AllIn, "All-in rentable · ROI " + AllInRoi.ToString("F2")); Fight(player, true); return; }
            if (GoldTrapUtility > 0 && GoldTrapUtility >= PokeUtility && TryGoldTrap(player, tower)) return;

            bool safe = FinancialTradeSafe(player, tower);
            if (tradeUntil > 0 && (Time.time >= tradeUntil || !safe || PokeUtility <= 0))
            { tradeUntil = 0; tradeRestUntil = Time.time + 1.2f; }
            if (Time.time < tradeUntil)
            { Plan(LaneTactic.Trade, "Intercambio con retorno positivo"); Fight(player, false); return; }
            if (Time.time < tradeRestUntil && player.Targetable)
            { Plan(LaneTactic.Zone, "Cobrar el intercambio y volver al farm"); FinancialSpacing(player, wave); return; }
            if (safe && PokeUtility > 0 && distance <= self.AttackReach(player) + 0.9f)
            {
                tradeUntil = Time.time + Mathf.Clamp(2 / self.BaseAttackSpeed, 1.5f, 2.8f); tradeSpellCount = 0;
                Plan(LaneTactic.Trade, "Poke rentable sin perder último golpe"); Fight(player, false); return;
            }
            if (player.Targetable && !safe && distance < 7)
            {
                Plan(LaneTactic.Zone, "Ceder oro para evitar una pérdida mayor");
                if (health < 0.42f) { recovering = true; Recover(player, distance); } else FinancialSpacing(player, wave);
                return;
            }
            bool absent = !player.Targetable || distance > 11;
            if (absent && wave != null)
            { Plan(LaneTactic.Push, "Cobrar oleada sin rival"); self.OrderAttack(wave); return; }
            if (absent && CanSiege(tower, player) && Time.time >= siegeRestUntil)
            {
                if (siegeUntil == 0 && inTower) siegeUntil = Time.time + 7;
                if (siegeUntil == 0 || Time.time < siegeUntil)
                { Plan(LaneTactic.Push, "Presionar torre con protección rentable"); self.OrderAttack(tower); return; }
                siegeUntil = 0; siegeRestUntil = Time.time + 4;
            }
            if (tower != null && (inTower || (!player.Alive && player.RespawnRemaining < 6)))
            { Plan(LaneTactic.Retreat, "Salir antes de la reaparición rival"); self.MoveTo(TowerExit(tower)); return; }
            Plan(LaneTactic.Farm, "Spacing financiero · esperar oro seguro");
            if (player.Targetable && distance < 14) FinancialSpacing(player, wave);
            else MoveOrHold(new Vector3(absent && tower != null ? TowerExit(tower).x : -Forward * 3, 0, 0));
        }

        void FinancialSpacing(Combatant player, Combatant wave = null)
        {
            Vector3 away = self.Position - player.Position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.left * Forward;
            Vector3 point = world.AvoidTower(self, player.Position + away.normalized * (FinancialConfig.neutral_spacing_units * 0.01f) + spacingNoise);
            // Spacing is a neutral preference, not an order to follow a fleeing champion
            // out of last-hit range of our own available farm.
            if (wave != null && Vector3.Distance(point, wave.Position) > self.AttackReach(wave) - 0.08f)
                point = world.AvoidTower(self, FarmPoint(wave));
            Combatant tower = EnemyTower();
            if (TowerRange(tower, point)) point = TowerExit(tower);
            NeutralSpacingPoint = point; MoveOrHold(point);
        }
    }
}
