using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        Object melDefenseCue;
        float melDefenseAt = -1;
        bool melDefenseSpent;
        void UpdateMelDefense(Combatant player)
        {
            Object cue = null;
            foreach (BladeWave wave in world.BladeWaves)
                if (wave != null && wave.Threatens(self) &&
                    (wave.IsUltimate || world.Settings.difficulty != Difficulty.Expert)) { cue = wave; break; }
            if (cue == null)
                foreach (BladeLink link in world.BladeLinks)
                    if (link != null && link.HasSecond && link.Threatens(self)) { cue = link; break; }
            float distance = Vector3.Distance(self.Position,player.Position);
            if (cue == null && player.Targetable && world.Settings.difficulty != Difficulty.Expert)
            {
                if (player.Blade != null && player.Blade.IsCharging && distance < player.Blade.Tuning.wRange + self.Radius) cue = player.Blade;
                bool approaching = Vector3.Dot(player.Velocity,(self.Position-player.Position).normalized) > .3f;
                bool panic = self.Health < self.MaxHealth * .35f &&
                    (distance < 3 && self.WasDamagedBy(player,1) || world.Settings.difficulty == Difficulty.Normal && distance < 5 && approaching);
                if (cue == null && panic) cue = player;
            }
            if (cue == null) { melDefenseCue = null; melDefenseAt = -1; melDefenseSpent = false; return; }
            if (cue != melDefenseCue)
            { melDefenseCue = cue; melDefenseAt = Time.time + MelWReaction; melDefenseSpent = false; }
            if (!melDefenseSpent && Time.time >= melDefenseAt && self.Cast(1,self.Position))
            { melDefenseSpent = true; Plan(LaneTactic.Trade,"Mel: Rebuttal ante amenaza visible"); }
        }
    }
}
