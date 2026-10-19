using System;
using UnityEngine;

namespace MidLane
{
    [Serializable] public sealed class ChampionStatValues
    {
        public float attackDamage, maxHealth, resourceMax, armor, magicResist, attackSpeed;
        public float healthRegenRate, resourceRegenRate, moveSpeed, attackRange;
        public float abilityPower, abilityHaste, lifeSteal, omnivamp, critChance, tenacity;
        public float physicalLethality, magicPenetrationFlat;
        // Live Client uses remaining resistance multipliers: 1 means no penetration.
        public float armorPenetrationPercent = 1, magicPenetrationPercent = 1;
    }

    // All modes resolve the same champion + level through this single reference.
    // Inventory, passive stacks and temporary buffs are applied by the combat layer.
    public static class ChampionStatReference
    {
        public static ChampionStatValues AtLevel(Archetype champion, int level)
        {
            level = Mathf.Clamp(level, 1, 18);
            // Every champion uses its intrinsic PC stats. Archived live captures include runes
            // and must not silently become permanent base attributes in any mode.
            var profile = LeagueChampionStats.For(champion);
            return new ChampionStatValues {
                attackDamage = profile.AttackDamage.At(level), maxHealth = profile.Health.At(level),
                resourceMax = profile.UsesMana ? profile.Mana.At(level) : 0,
                armor = profile.Armor.At(level), magicResist = profile.MagicResist.At(level),
                attackSpeed = profile.AttackSpeedAt(level),
                healthRegenRate = profile.HealthRegeneration.At(level),
                resourceRegenRate = profile.UsesMana ? profile.ManaRegeneration.At(level) : 0,
                moveSpeed = profile.MovementSpeedUnits, attackRange = profile.AttackRangeUnits
            };
        }
    }
}
