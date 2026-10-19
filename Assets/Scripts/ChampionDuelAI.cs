using UnityEngine;
namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        public bool DuelModeActive { get { return !world.Settings.spawnWaves; } }
        void ThinkDuel()
        {
            Combatant player = world.Player, tower = EnemyTower();
            float distance = Vector3.Distance(self.Position, player.Position), hp = self.Health / self.MaxHealth;
            if (world.PvpModeActive) recovering = false;
            if (recovering && hp >= .85f && (self.MaxResource == 0 || self.Resource >= self.MaxResource * .5f)) recovering = false;
            if (!world.PvpModeActive && hp < .23f) recovering = true;
            if (recovering) { Recover(player, distance); return; }
            if (TowerRange(tower, self.Position))
            { Plan(LaneTactic.Retreat, "Duelo: respetar torre rival"); MoveOrHold(TowerExit(tower)); return; }
            if (player.Targetable && TowerRange(tower, player.Position))
            { FightFromTowerEdge(player, tower, distance); return; }
            if (!player.Targetable)
            {
                if (!world.PvpModeActive && hp < .85f) { recovering = true; Recover(player, distance); }
                else { Plan(LaneTactic.Zone, "Duelo: esperar al rival"); MoveOrHold(new Vector3(-Forward * 3, 0, 0)); }
                return;
            }
            if (DodgeWarnings(player) || self.IsWindingAttack) return;
            float effectiveHP = player.Health + player.TotalShield + player.HealthRegeneration * 2;
            float burning = self.Summoners.PendingIgniteDamage(player);
            if (!world.PvpModeActive && burning >= effectiveHP && !world.Settings.testMode)
            { Plan(LaneTactic.Zone, "Duelo: Ignite ya asegura el remate"); MoveAway(player, self.AttackReach(player) + 1); return; }
            float followup = self.Sett != null ? Physical(self, player, self.Sett.PreviewAttackSequenceDamage(player, 1.2f, false)) : Physical(self, player, self.AttackDamage);
            bool canFollow = distance <= self.AttackReach(player) + .4f || (self.Sett != null && self.Ready(2) && distance <= SettAbilities.ERange);
            if (!world.Settings.testMode && burning <= 0 && self.Summoners.IgniteRemaining <= 0 &&
                effectiveHP + player.HealthRegeneration * 3 <= SummonerRules.IgniteDamage(self.Level) + (canFollow ? followup * .7f : 0) && self.Summoners.Ignite(player))
            { Plan(LaneTactic.AllIn, "Duelo: Ignite para asegurar la eliminación"); return; }
            if (!world.PvpModeActive && hp < .38f && player.Health / player.MaxHealth > hp + .25f && effectiveHP > followup * 1.2f)
            { recovering = true; Recover(player, distance); return; }
            if (TryDuelFlash(player, tower, effectiveHP, followup)) return;
            Plan(LaneTactic.AllIn, "Duelo: presionar y combinar habilidades");
            if (self.Sett == null) { Fight(player, true); return; }
            DuelSettCombo(player, distance);
        }
        void FightFromTowerEdge(Combatant player, Combatant tower, float distance)
        {
            // Holding outside tower range must not cancel a valid basic or forbid safe retaliation.
            // No attack order is left chasing the target into tower range between decisions.
            self.HoldAttackPosition();
            if (self.IsWindingAttack) return;
            self.Stop();
            if (self.Sett != null)
            {
                Vector3 wAim = Aim(player, distance);
                if (self.Ready(1) && distance <= SettAbilities.WRange && ShouldUseHaymaker(player, wAim) &&
                    self.Cast(1, wAim, player))
                { Plan(LaneTactic.AllIn, "Duelo: W desde fuera de torre"); return; }
                if (self.Ready(2) && distance <= SettAbilities.ERange && !player.IsStunned && self.Cast(2, player.Position))
                { Plan(LaneTactic.AllIn, "Duelo: atraer al rival desde fuera de torre"); return; }
            }
            if (distance <= self.AttackReach(player))
            {
                Plan(LaneTactic.AllIn, "Duelo: responder con básicos desde fuera de torre");
                if (self.AttackReady) self.TryAttack(player);
                else if (self.Sett != null && !self.Sett.QEmpowered && !self.Sett.NextPunchIsRight) self.Cast(0, player.Position);
                return;
            }
            Plan(LaneTactic.Retreat, "Duelo: esperar fuera de torre sin perseguir");
            Vector3 away = player.Position - tower.Position;
            Vector3 edge = world.Clamp(tower.Position + away.normalized * (tower.TowerAggroRadius + 1.5f), self.Radius);
            if (TowerRange(tower, edge)) edge = TowerExit(tower);
            MoveOrHold(edge);
        }
        bool TryDuelFlash(Combatant player, Combatant tower, float effectiveHP, float followup)
        {
            if (world.Settings.testMode || player.Health / player.MaxHealth > .1f || (!world.PvpModeActive && self.Health < self.MaxHealth * .3f) || self.Summoners.FlashRemaining > 0) return false;
            Vector3 delta = player.Position - self.Position;
            if (world.PvpModeActive && Vector3.Dot(player.Velocity, delta.normalized) <= .5f) return false;
            bool igniteFinisher = self.Summoners.IgniteRemaining <= 0 && SummonerRules.IgniteDamage(self.Level) >= effectiveHP + player.HealthRegeneration * 3;
            float finishRange = igniteFinisher ? SummonerRules.IgniteRange + player.Radius - .2f : self.AttackReach(player) - .15f;
            float distance = delta.magnitude, gap = Mathf.Max(0, distance - finishRange);
            float closing = self.MoveSpeed - Mathf.Max(0, Vector3.Dot(estimatedVelocity, delta.normalized));
            if (gap <= .7f || (closing > .25f && gap / closing <= 1.2f)) return false;
            if (self.Sett != null)
            {
                if (self.Ready(2) && distance <= SettAbilities.ERange) return false;
                if (self.Ready(3) && distance <= SettAbilities.RRange) return false;
                if (self.Ready(1) && distance <= SettAbilities.WRange && ShouldUseHaymaker(player, player.Position) &&
                    self.Sett.HaymakerDamage >= effectiveHP) return false;
            }
            else if (self.Archetype == Archetype.Prism)
            { if ((self.Ready(0) && distance <= 10) || (self.Ready(2) && distance <= 9) || (self.Ready(3) && distance <= 12)) return false; }
            else if ((self.Ready(0) && distance <= 6.5f) || (self.Ready(2) && distance <= 7) || (self.Ready(3) && distance <= 9)) return false;
            if (!igniteFinisher && (followup < effectiveHP || self.AttackCooldownRemaining > .35f)) return false;
            Vector3 landing = world.AvoidTower(self, player.Position + estimatedVelocity * .15f - delta.normalized * finishRange);
            if (Vector3.Distance(landing, self.Position) > SummonerRules.FlashRange || TowerRange(tower, landing) || TowerRange(tower, player.Position)) return false;
            if ((!world.PvpModeActive && ThreatDamage(player, landing, 1) > self.Health + self.TotalShield) || !self.Summoners.Flash(landing)) return false;
            Plan(LaneTactic.AllIn, "Duelo: Flash ofensivo para rematar");
            if (igniteFinisher) self.Summoners.Ignite(player); else { self.OrderAttack(player); self.TryAttack(player); } return true;
        }
        void DuelSettCombo(Combatant player, float distance)
        {
            // Finish the fast right before resetting with Q or spending a cast animation.
            if (self.Sett.NextPunchIsRight && self.AttackReady && distance <= self.AttackReach(player))
            { self.OrderAttack(player); self.TryAttack(player); return; }
            Vector3 wAim = Aim(player, distance);
            if (distance <= SettAbilities.WRange && self.Ready(1) && ShouldUseHaymaker(player, wAim))
            { if (self.Cast(1, wAim, player)) return; }
            if (!player.IsStunned && distance <= SettAbilities.ERange && self.Cast(2, player.Position)) return;
            if (distance <= SettAbilities.RRange && self.Progress.Rank(3) > 0 &&
                (player.Health < player.MaxHealth * .5f || self.Sett.Grit > self.MaxHealth * .35f) &&
                FinancialRLandingSafe(player, player.Position) && self.Cast(3, player.Position, player)) return;
            if (distance <= self.AttackReach(player) && self.AttackReady)
            { self.OrderAttack(player); self.TryAttack(player); return; }
            if (!self.Sett.QEmpowered && !self.Sett.NextPunchIsRight && distance < self.AttackReach(player) + 1.5f)
            { self.OrderAttack(player); if (self.Cast(0, player.Position, player)) return; }
            self.OrderAttack(player);
        }
    }
}
