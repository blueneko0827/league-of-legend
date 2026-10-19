using UnityEngine;

namespace MidLane
{
    public sealed partial class CombatItemEffects
    {
        float bloodthirsterShield, bloodthirsterCombatAt;
        float bootsShield, bootsShieldUntil, bootsShieldReadyAt;
        DamageType bootsShieldType;
        public float BloodthirsterShield { get { return bloodthirsterShield; } }
        public float BootsShield { get { return bootsShield; } }
        public float BootsShieldCooldown { get { return Mathf.Max(0, bootsShieldReadyAt - Time.time); } }

        float LevelShield(ItemDefinition item)
        {
            return item.Value("ShieldBase") + item.Value("ShieldPerLevel") *
                Mathf.Max(0, self.Level - item.Value("ShieldGrowthStartLevel") + 1);
        }
        public void OnLifestealOverflow(float excess)
        {
            ItemDefinition item = Item(3072);
            if (item == null || !self.Alive || excess <= 0) return;
            bloodthirsterCombatAt = Time.time;
            bloodthirsterShield = Mathf.Min(LevelShield(item), bloodthirsterShield + excess * ShieldReceivedMultiplier);
            if(self.Runes!=null) self.Runes.NotifyShield(bloodthirsterShield,25);
        }
        public void OnIncomingHit(Combatant source, DamageType type)
        {
            bloodthirsterCombatAt = Time.time;
            if (!self.Alive || source == null || source.Kind != UnitKind.Champion || Time.time < bootsShieldReadyAt) return;
            ItemDefinition boots = type == DamageType.Physical ? Item(3174) : type == DamageType.Magic ? Item(3173) : null;
            if (boots == null) return;
            float bonusHealth = Mathf.Max(0, self.MaxHealth - self.BaseStats.Health.At(self.Level));
            bootsShield = (LevelShield(boots) + bonusHealth * boots.Value("ShieldAmount", .08f)) *
                (type == DamageType.Physical ? ShieldReceivedMultiplier : 1);
            bootsShieldType = type;
            bootsShieldUntil = Time.time + boots.Value("ShieldDuration", 5);
            if(self.Runes!=null) self.Runes.NotifyShield(bootsShield,boots.Value("ShieldDuration",5));
            bootsShieldReadyAt = Time.time + boots.Value("Cooldown", 15);
        }
        float AbsorbSustainShields(float damage, DamageType type)
        {
            if (Time.time >= bootsShieldUntil) bootsShield = 0;
            if (type == bootsShieldType)
            {
                float absorbed = Mathf.Min(damage, bootsShield); bootsShield -= absorbed; damage -= absorbed;
            }
            float general = Mathf.Min(damage, bloodthirsterShield); bloodthirsterShield -= general; return damage - general;
        }
        void ScaleSustainShields(float factor)
        {
            bloodthirsterShield *= factor;
            if (bootsShieldType == DamageType.Physical) bootsShield *= factor;
        }
        void SustainInventoryChanged()
        {
            if (Item(3072) == null) bloodthirsterShield = 0;
            if (Item(bootsShieldType == DamageType.Physical ? 3174 : 3173) == null) bootsShield = 0;
        }
        void TickSustainShields(float dt)
        {
            if (Time.time >= bootsShieldUntil) bootsShield = 0;
            ItemDefinition item = Item(3072);
            // Reconstructed decay: five seconds after the client's 25-second quiet period.
            if (item != null && Time.time > bloodthirsterCombatAt + item.Value("DecayTime", 25))
                bloodthirsterShield = Mathf.Max(0, bloodthirsterShield - LevelShield(item) * dt / 5);
        }
    }
}
