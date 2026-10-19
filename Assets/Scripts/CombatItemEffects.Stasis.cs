using UnityEngine;
namespace MidLane
{
    public sealed partial class CombatItemEffects
    {
        float stasisUntil, stasisReadyAt;
        public bool IsInStasis { get { return Time.time < stasisUntil; } }
        public float StasisRemaining { get { return Mathf.Max(0, stasisUntil - Time.time); } }
        public float ActiveCooldown(int id)
        { return Mathf.Max(0, (id == 3157 || id == 2420 ? stasisReadyAt : id == 6631 || id == 3077 ? activeReadyAt : 0) - Time.time); }
        bool UseStasis(int id)
        {
            ItemDefinition item = Item(id);
            if (item == null || !self.CanAct || Time.time < stasisReadyAt ||
                (id == 2420 && !self.Inventory.ShatterArmguard())) return false;
            self.Stop();
            if (self.Blade != null)
            {
                if (self.Blade.WaitingSecondBlade) self.Cooldowns.Start(2, Time.time, self.Blade.AbilityCooldown(2));
                self.Blade.CancelTransient();
            }
            stasisUntil = Time.time + item.Value("Duration", 2.5f);
            stasisReadyAt = Time.time + (id == 3157 ? item.Value("Cooldown", 120) : 120);
            self.RecordPvpActivity(); world.PracticeAbilityUsed(self); return true;
        }
    }
}
