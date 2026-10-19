using UnityEngine;
namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        bool pvpCombatUnlocked;
        public bool WaitingForPvpOpener { get { return world.PvpModeActive && self.Sett != null && !pvpCombatUnlocked; } }
        public Vector3 PvpWaitingPoint
        {
            get
            {
                float offset = Mathf.Clamp(world.Settings.pvpSettWaitOffset, 0, TowerToTowerRules.MidpointToSafety - self.Radius - .75f);
                return new Vector3(world.Player.Team == Team.Blue ? -offset : offset, 0, 0);
            }
        }
        internal void ResetPvpEngagement()
        {
            pvpCombatUnlocked = false;
            if (world.PvpModeActive && self.Sett != null)
            {
                self.Stop();
                // A previous duel's Ignite must not reopen combat after Irelia respawns.
                if (!world.Player.Alive) world.Player.Summoners.CancelDamageAgainst(self);
            }
        }
        internal void ReceivePvpOpener(Combatant source)
        {
            if (!WaitingForPvpOpener || source != world.Player || source.Blade == null || !source.Alive) return;
            pvpCombatUnlocked = true;
            nextThink = Time.time + Reaction;
            financialStartedAt = Time.time; financialBaseReaction = SampleFinancialReaction();
            lastSampleAt = Time.time; lastPlayerPosition = source.Position;
        }
        bool WaitForPvpOpener()
        {
            if (!WaitingForPvpOpener) return false;
            Plan(LaneTactic.Zone, "PvP: volver al límite y esperar daño de Irelia");
            if (!self.CanAct) { self.HoldAttackPosition(); return true; }
            Combatant tower = EnemyTower();
            if (TowerRange(tower, self.Position)) { MoveOrHold(TowerExit(tower)); return true; }
            MoveOrHold(world.AvoidTower(self, PvpWaitingPoint));
            return true;
        }
    }
}
