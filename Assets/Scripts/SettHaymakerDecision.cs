using UnityEngine;
namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        public bool ShouldUseHaymaker(Combatant target, Vector3 aim)
        {
            if (self.Sett == null || target == null || !self.ValidEnemy(target) || self.Progress.Rank(1) == 0) return false;
            if (self.Sett.Grit >= self.Sett.MaxGrit) return true;
            // Below full Grit, only spend W on a conservative, immobilized finisher.
            // A stationary opponent can still move during its .75 s preparation.
            if (self.Sett.Grit < self.Sett.MaxGrit * .85f || target.StunRemaining < .8f || target.Inventory.Has(3026) ||
                (world.Settings.testMode && target == world.Player)) return false;
            Vector3 direction = aim - self.Position; direction.y = 0;
            if (direction.sqrMagnitude < .001f) return false;
            direction.Normalize();
            Vector3 offset = target.Position - self.Position;
            float forward = Vector3.Dot(offset, direction), side = Vector3.Dot(offset, Vector3.Cross(direction, Vector3.up));
            if (!CombatRules.CircleIntersectsTrapezoid(forward, side, target.Radius, SettAbilities.WRange, SettAbilities.WNearHalfWidth, SettAbilities.WFarHalfWidth)) return false;
            bool center = CombatRules.CircleIntersectsRectangle(forward, side, target.Radius, 0, SettAbilities.WRange, SettAbilities.WTrueHalfWidth);
            float raw = self.Sett.HaymakerDamage;
            if (target.Inventory.Has(2051)) raw = Mathf.Max(0, raw - 15);
            float damage = raw;
            if (!center)
            {
                float armor = target.Armor;
                if (armor > 0) armor = Mathf.Max(0, armor * (1 - self.PercentArmorPenetration) - self.ArmorPenetration);
                if (target.Blade != null) raw *= 1 - target.Blade.DamageReduction(DamageType.Physical);
                damage = CombatRules.Mitigate(raw, armor);
                if (target.Inventory.Has(6333)) damage *= target.AttackRange > 3 ? .9f : .7f;
            }
            float shielding = target.TotalShield;
            if (target.Inventory.Has(3053))
                shielding += Mathf.Max(0, target.MaxHealth - target.BaseStats.Health.At(target.Level)) * target.Inventory.Get(3053).Value("BaseShieldRatio", .6f) * target.ItemEffects.ShieldReceivedMultiplier;
            float effectiveHealth = target.Health + shielding + target.HealthRegeneration * .8f;
            if (world.InFountain(target)) effectiveHealth += target.MaxHealth * .104f * .8f;
            return damage >= effectiveHealth * 1.15f;
        }
    }
}
