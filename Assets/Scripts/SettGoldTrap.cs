using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        float EvaluateGoldTrapUtility(Combatant player, Combatant tower)
        {
            if (!FinancialTradeSafe(player, tower) || !self.Cooldowns.Ready(2, Time.time) || self.Progress.Rank(2) == 0) return 0;
            bool reacting = dashCueTarget != null && dashCueMatured;
            if (!reacting && GoldTrapMinion == null) return 0;
            Combatant bait = reacting ? dashCueTarget : GoldTrapMinion;
            float travel = Mathf.Max(0, Vector3.Distance(self.Position, bait.Position) - FinancialConfig.trap_spacing_units * 0.01f) / self.MoveSpeed;
            float risk = FinancialConfig.estimated_death_loss_gold * ThreatDamage(player, bait.Position, Reaction + 0.25f) * 0.25f / Mathf.Max(1, self.Health + self.TotalShield);
            float reward = FinancialConfig.q_bait_attraction_gold * (reacting ? 1 : 0.5f);
            return (1 - FinancialConfig.lasthit_priority_over_poke) * FinancialUtility.GoldPerMinute(reward, risk, travel + Reaction + 0.25f);
        }
        Combatant FindGoldTrap(Combatant player)
        {
            if (!player.Targetable || player.Blade == null || player.Blade.Rank(0) == 0) return null;
            bool canQ = player.Blade.Ready(0);
            if (!canQ && !player.Blade.IsDashing) return null;
            Combatant best = null; float bestValue = float.NegativeInfinity;
            foreach (Combatant minion in world.Units)
            {
                if (minion == null || !minion.Targetable || minion.Kind != UnitKind.Minion || minion.Team != self.Team) continue;
                if (Vector3.Distance(self.Position, minion.Position) > 10 || Vector3.Distance(player.Position, minion.Position) > player.Blade.Tuning.qRange) continue;
                if (!canQ && player.Blade.DashTarget != minion) continue;
                float arrival = Vector3.Distance(player.Position, minion.Position) / (player.Blade.Tuning.qSpeed + player.MoveSpeed);
                float predicted = PredictMinionHealth(minion, arrival);
                // Conservative estimate: real Q rank, AD, level minion bonus and armor;
                // unspent item procs are not assumed to be available.
                if (predicted <= 0 || predicted > Physical(player, minion, player.Blade.QRawDamage(minion))) continue;
                float value = FinancialConfig.MinionGold(minion.MinionType) / (0.5f + Vector3.Distance(self.Position, minion.Position) / self.MoveSpeed);
                if (value <= bestValue) continue;
                bestValue = value; best = minion;
            }
            return best;
        }

        bool TryGoldTrap(Combatant player, Combatant tower)
        {
            if (!FinancialTradeSafe(player, tower) || !self.Cooldowns.Ready(2, Time.time) || self.Progress.Rank(2) == 0) return false;
            if (dashCueTarget != null && dashCueMatured && player.Deaths == dashCueLife && Time.time <= dashCueAt + 0.8f)
            {
                Vector3 intercept = player.Blade.IsDashing ? player.Blade.PredictDashPosition(0.25f) : player.Position + estimatedVelocity * 0.25f;
                float reach = SettAbilities.ERange + player.Radius;
                if (!TowerRange(tower, intercept) && Vector3.Distance(self.Position, intercept) <= reach && self.Cast(2, intercept))
                {
                    dashCueTarget = null; tradeUntil = 0;
                    Plan(LaneTactic.GoldTrap, "Interceptar Q con Facebreaker"); return true;
                }
            }
            if (GoldTrapMinion == null) return false;
            Vector3 away = self.Position - GoldTrapMinion.Position; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = Vector3.left * Forward;
            float spacing = FinancialConfig.trap_spacing_units * 0.01f;
            Vector3 point = world.AvoidTower(self, GoldTrapMinion.Position + away.normalized * spacing);
            if (TowerRange(tower, point) || Mathf.Abs(Vector3.Distance(point, GoldTrapMinion.Position) - spacing) > 0.1f) return false;
            tradeUntil = 0;
            Plan(LaneTactic.GoldTrap, "Trampa de oro · reservar E junto al minion"); MoveOrHold(point); return true;
        }
    }
}
