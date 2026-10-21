using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        public HumanBehaviorState Human { get; private set; }
        struct HumanCast { public bool Pending, Hit; public int PlayerLife; public float Deadline; }
        readonly HumanCast[] humanCasts = new HumanCast[4];
        Combatant humanFarmTarget;
        float humanFarmReadyAt;
        public float HumanAggression => Human != null ? Human.AggressionMultiplier : 1;
        public float HumanTether => Human != null ? Human.TetherMultiplier : 1;
        float HumanRiskThreshold => Human != null ? Human.RiskThresholdMultiplier : 1;
        float HumanFarmDelay => Human != null ? Human.PeripheralFarmDelay : 0;
        float HumanReaction(float baseline, bool peripheral = false)
        { return Human == null ? baseline : baseline * Human.ReactionMultiplier + (peripheral ? Human.PeripheralCombatDelay : 0); }
        Vector3 HumanAimOffset(float error)
        { return Human == null ? Vector3.zero : new Vector3(Human.AimX, 0, Human.AimZ) * (error * Human.AimErrorMultiplier); }
        float HumanPrediction => Human != null ? Human.PredictionMultiplier : 1;

        void InitializeHuman()
        {
            int seed = unchecked(world.Settings.aiHumanSeed * 397 ^ (int)self.Archetype * 31 ^ (int)self.Team);
            Human = new HumanBehaviorState(HumanBehaviorProfile.For(world.Settings.difficulty), seed,
                DuelModeActive ? AttentionFocus.Combat : AttentionFocus.Farm);
            world.OnObservedDamage += ObserveHumanDamage;
            world.OnUnitDeath += ObserveHumanDeath;
        }
        void TickHuman()
        {
            Human.Tick(Time.deltaTime);
            if (DuelModeActive && Human.Focus == AttentionFocus.Farm) Human.RequestFocus(AttentionFocus.Combat);
            for (int slot = 0; slot < humanCasts.Length; slot++)
            {
                var cast = humanCasts[slot];
                if (!cast.Pending || Human.Age < cast.Deadline) continue;
                if (self.Alive && world.Player.Alive && world.Player.Deaths == cast.PlayerLife)
                    Human.RecordMechanicalResult(cast.Hit);
                humanCasts[slot] = default(HumanCast);
            }
        }
        bool HumanCombatCast(int slot, Vector3 aim, Combatant target = null)
        {
            if (!self.Cast(slot, aim, target)) return false;
            ObserveHumanOwnCast(slot, true); return true;
        }
        void ObserveHumanOwnCast(int slot, bool combatIntent = false)
        {
            bool skillshot = self.Mel != null && (slot == 0 || slot == 2) || self.Sett != null && (slot == 1 || slot == 2);
            bool fighting = combatIntent || Tactic == LaneTactic.Trade || Tactic == LaneTactic.AllIn || self.AttackTarget == world.Player ||
                self.Mel != null && melCombo != MelCombo.None;
            if (!skillshot || !fighting || !world.Player.Targetable || Vector3.Distance(self.Position, world.Player.Position) > 12) return;
            humanCasts[slot] = new HumanCast { Pending = true, PlayerLife = world.Player.Deaths, Deadline = Human.Age + 1.8f };
        }
        void ObserveHumanDamage(Combatant source, Combatant target, float damage, int slot)
        {
            if (damage <= 0 || Human == null || world.Paused || world.RoundOver) return;
            if (target == self)
            {
                Human.RecordDamage(damage / Mathf.Max(1, self.MaxHealth), false, source.Kind == UnitKind.Champion);
                // Attention can narrow, but actual incoming minion/tower damage is never ignored.
                if (source.Kind == UnitKind.Tower || source.Kind == UnitKind.Minion ||
                    self.Health - damage < self.MaxHealth * .4f) Human.NoticeDanger();
            }
            if (source == self && target == world.Player)
            {
                Human.RecordDamage(damage / Mathf.Max(1, target.MaxHealth), true, true);
                if (slot >= 0 && slot < 4 && humanCasts[slot].Pending)
                { var cast = humanCasts[slot]; cast.Hit = true; humanCasts[slot] = cast; }
            }
        }
        void ObserveHumanDeath(Combatant victim, Combatant killer)
        {
            if (world.Paused || world.RoundOver) return;
            if (victim == self)
            {
                Human.RecordDeath(); humanFarmTarget = null;
                for (int slot = 0; slot < 4; slot++) humanCasts[slot] = default(HumanCast);
            }
            else if (killer == self && victim.Kind == UnitKind.Champion && victim.Dummy == null) Human.RecordKill();
            else if (victim.Kind == UnitKind.Minion && victim.Team != self.Team)
            {
                if (killer == self) Human.RecordFarm(true);
                else if (self.AttackTarget == victim || self.PendingAttackTarget == victim ||
                    PreparedFarmTarget == victim || melPrepared == victim || melFarm == victim) Human.RecordFarm(false);
            }
        }
        void HumanPlan(LaneTactic tactic)
        {
            if (Human == null) return;
            Human.RequestFocus(tactic == LaneTactic.Retreat || tactic == LaneTactic.Recover ? AttentionFocus.Defense :
                tactic == LaneTactic.Trade || tactic == LaneTactic.AllIn || tactic == LaneTactic.GoldTrap || DuelModeActive ?
                AttentionFocus.Combat : AttentionFocus.Farm);
        }
        bool HumanNoticedFarm(Combatant target)
        {
            if (target != humanFarmTarget) { humanFarmTarget = target; humanFarmReadyAt = Time.time + HumanFarmDelay; }
            return target == null || Time.time >= humanFarmReadyAt;
        }
    }
}
