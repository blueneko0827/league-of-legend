using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    // Item state belongs to the champion; LaneWorld resets cooldowns on champion deaths.
    public sealed partial class CombatItemEffects
    {
        readonly LaneWorld world;
        readonly Combatant self;
        sealed class Bleed { public Combatant source; public float left, rate; }
        readonly List<Bleed> bleeds = new List<Bleed>();
        sealed class Blaze { public Combatant target; public int deaths; public float ends, nextTick; }
        readonly List<Blaze> blazes = new List<Blaze>();
        float chapterUntil, chapterRate;
        float doranDrainUntil;
        public float DoranDrainRegeneration
        {
            get
            {
                ItemDefinition ring = Item(1056);
                return ring == null ? 0 : ring.Value(Time.time < doranDrainUntil ? "ManaRestorePerSecondUpgraded" : "ManaRestorePerSecond", Time.time < doranDrainUntil ? 2 : 1);
            }
        }
        public float BlackfireAPBonus
        {
            get
            {
                ItemDefinition torch = Item(2503); if (torch == null) return 0;
                int count = 0;
                foreach (Blaze blaze in blazes) if (blaze.target != null && blaze.target.Kind == UnitKind.Champion && blaze.target.Alive && blaze.target.Deaths == blaze.deaths && Time.time < blaze.ends) count++;
                return count * torch.Value("APPerStack", 0.04f);
            }
        }
        public float ModifyOutgoingDamage(float raw, Combatant target, DamageType type)
        {
            ItemDefinition flame = Item(4645);
            return flame != null && type != DamageType.Physical && target.Health < target.MaxHealth * flame.Value("HealthThreshold", 0.4f)
                ? raw * (1 + flame.Value("SpellItemDamageAmp", 0.2f)) : raw;
        }
        public void OnAbilityHit(Combatant target)
        {
            ItemDefinition burn = Item(2503) ?? Item(2508);
            if (burn == null || target.Kind == UnitKind.Tower || !target.Alive) return;
            Blaze blaze = blazes.Find(value => value.target == target && value.deaths == target.Deaths);
            if (blaze == null) { blaze = new Blaze { target = target, deaths = target.Deaths, nextTick = Time.time + burn.Value("TickFrequency", 0.5f) }; blazes.Add(blaze); }
            blaze.ends = Time.time + burn.Value("BurnDuration", 3);
        }
        public void OnLevelUp()
        {
            ItemDefinition chapter = Item(3802); if (chapter == null || !self.Alive) return;
            chapterUntil = Time.time + chapter.Value("RestorationDuration", 3);
            chapterRate = self.MaxResource * chapter.Value("ManaRestorePercent", 0.2f) / chapter.Value("RestorationDuration", 3);
        }
        void TickBlazes()
        {
            ItemDefinition burn = Item(2503) ?? Item(2508);
            if (burn == null) { blazes.Clear(); return; }
            for (int i = blazes.Count - 1; i >= 0; i--)
            {
                Blaze blaze = blazes[i];
                if (blaze.target == null || !blaze.target.Alive || blaze.target.Deaths != blaze.deaths) { blazes.RemoveAt(i); continue; }
                float frequency = burn.Value("TickFrequency", 0.5f);
                while (blaze.nextTick <= Time.time && blaze.nextTick <= blaze.ends && blaze.target.Alive && blaze.target.Deaths == blaze.deaths)
                {
                    float dps = burn.Value("BurnFlatDamagePerSecond", 20) + self.AbilityPower * burn.Value("APRatio");
                    blaze.target.TakeDamage(dps * frequency, self, DamageType.Magic, false, false, true);
                    blaze.nextTick += frequency;
                }
                if (Time.time >= blaze.ends || !blaze.target.Alive || blaze.target.Deaths != blaze.deaths) blazes.RemoveAt(i);
            }
        }
        int krakenHits, lightStacks, darkStacks, shojinStacks;
        int hullbreakerStacks;
        float hullbreakerExpires;
        bool lightAttack = true;
        float krakenExpires, terminusExpires, shojinExpires;
        readonly Dictionary<int, float> shojinCastReady = new Dictionary<int, float>();
        float lifelineReadyAt, shield, shieldEnds, shieldDecayAt, magicShield, magicShieldEnds;
        float reviveReadyAt, reviveAt, warmogReadyAt, vampUntil, activeReadyAt, speedUntil, speedBonus;
        float defyHealUntil, defyHealRate;
        float shieldReaverUntil, shieldReaverReduction;
        public float ShieldReceivedMultiplier { get { return Time.time < shieldReaverUntil ? 1 - shieldReaverReduction : 1; } }
        public void ApplyShieldReaver(Combatant target)
        {
            ItemDefinition fang = Item(6695);
            if (fang == null || target.Kind != UnitKind.Champion || target.ItemEffects == null) return;
            target.ItemEffects.ReceiveShieldReaver(fang.Value(Ranged ? "ShieldShredRange" : "ShieldShred", Ranged ? 35 : 50) * .01f,
                fang.Value(Ranged ? "ShieldWoundsRange" : "ShieldWounds", Ranged ? 35 : 50) * .01f, fang.Value("DebuffDuration", 3));
        }
        void ReceiveShieldReaver(float shred, float reduction, float duration)
        {
            bool active = Time.time < shieldReaverUntil;
            if (active && reduction < shieldReaverReduction) return;
            // First contact cuts current general shields once. Repeated on-hits only refresh.
            // A stronger source replaces a weaker one rather than multiplying reductions.
            if (!active || reduction > shieldReaverReduction)
            {
                float factor = active ? (1 - Mathf.Clamp01(shred)) / Mathf.Max(.001f, 1 - shieldReaverReduction) : 1 - Mathf.Clamp01(shred);
                self.ScaleAbilityShield(factor); shield *= factor; eclipseShield *= factor;
                ScaleSustainShields(factor);
                if (self.Summoners != null) self.Summoners.ScaleBarrierShield(factor);
            }
            shieldReaverReduction = Mathf.Clamp01(reduction); shieldReaverUntil = Time.time + duration;
        }
        public CombatItemEffects(LaneWorld lane, Combatant actor) { world = lane; self = actor; }
        ItemDefinition Item(int id) { return self.Inventory != null && self.Inventory.Has(id) ? self.Inventory.Get(id) : null; }
        bool Ranged { get { return self.AttackRange > 3; } }
        public bool IsReviving { get { return reviveAt > 0; } }
        public float ReviveRemaining { get { return IsReviving ? Mathf.Max(0, reviveAt - Time.time) : 0; } }
        public float TotalShield { get { return shield + magicShield + eclipseShield + bloodthirsterShield + bootsShield; } }
        public float TerminusResistance { get { return Item(3302) != null && Time.time < terminusExpires ? lightStacks * (self.Level >= 14 ? 8 : self.Level >= 11 ? 7 : 6) : 0; } }
        public float TerminusPenetration { get { return Item(3302) != null && Time.time < terminusExpires ? darkStacks * Item(3302).Value("PenPerHit") : 0; } }
        public float MoveSpeedBonus { get { return Time.time < speedUntil ? speedBonus : 0; } }
        public float StoredDamage { get { float total = 0; foreach (Bleed bleed in bleeds) total += bleed.left; return total; } }
        public float AdjustAttackDamage(float raw)
        {
            ItemDefinition sterak = Item(3053), bloodmail = Item(2501);
            if (sterak != null) raw += self.BaseStats.AttackDamage.At(self.Level) * sterak.Value("ADtoAD");
            if (bloodmail != null)
            {
                raw += Mathf.Max(0, self.MaxHealth - self.BaseStats.Health.At(self.Level)) * bloodmail.Value("HPToADPercentage");
                float missing = 1 - self.Health / Mathf.Max(1, self.MaxHealth);
                raw *= 1 + bloodmail.Value("MissingHealthAD") * Mathf.Clamp01(missing / bloodmail.Value("MissingHealthThreshold", 0.7f));
            }
            return raw;
        }
        public float ModifyAbilityDamage(float raw, int castId)
        {
            ItemDefinition shojin = Item(3161); if (shojin == null) return raw;
            if (Time.time >= shojinExpires) { shojinStacks = 0; shojinCastReady.Clear(); }
            float result = raw * (1 + shojinStacks * shojin.Value("SpellDamageIncrease"));
            float readyAt;
            if (castId >= 0)
            {
                if (!shojinCastReady.TryGetValue(castId, out readyAt) || Time.time >= readyAt)
                { shojinStacks = Mathf.Min(4, shojinStacks + 1); shojinCastReady[castId] = Time.time + shojin.Value("CastIDLockout", 1); }
                shojinExpires = Time.time + shojin.Value("StackDuration", 6);
            }
            return result;
        }
        public float OnAttackHit(Combatant target)
        {
            float dealt = OnHullbreakerHit(target);
            ItemDefinition terminus = Item(3302);
            if (terminus != null)
            {
                dealt += target.TakeDamage(30 + 0.1f * (self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level)) + 0.1f * self.AbilityPower, self, DamageType.Magic);
                if (target.Kind == UnitKind.Champion)
                {
                    if (Time.time >= terminusExpires) { lightStacks = darkStacks = 0; lightAttack = true; }
                    if (lightAttack) lightStacks = Mathf.Min(3, lightStacks + 1); else darkStacks = Mathf.Min(3, darkStacks + 1);
                    lightAttack = !lightAttack; terminusExpires = Time.time + terminus.Value("BuffDuration", 5);
                }
            }
            ItemDefinition kraken = Item(6672);
            if (kraken != null)
            {
                if (Time.time >= krakenExpires) krakenHits = 0;
                krakenExpires = Time.time + kraken.Value("BuffDuration", 4);
                if (++krakenHits >= 3)
                {
                    krakenHits = 0;
                    float missing = 1 - target.Health / target.MaxHealth;
                    float raw = (150 + 5 * Mathf.Max(0, self.Level - 8)) * (1 + Mathf.Clamp01(missing) * (kraken.Value("MaxAmpNumber", 1.75f) - 1));
                    dealt += target.TakeDamage(raw * (Ranged ? kraken.Value("RangedDamageMultiplier", 0.8f) : 1), self);
                }
            }
            ItemDefinition cleave = Item(6631) ?? Item(3077);
            if (cleave != null)
                foreach (Combatant other in world.Units)
                    if (other != target && other.Kind != UnitKind.Tower && self.ValidEnemy(other) && Vector3.Distance(other.Position, target.Position) <= 3.5f + other.Radius)
                        dealt += other.TakeDamage(self.AttackDamage * (Ranged ? 0.2f : 0.4f), self);
            return dealt;
        }
        public float OnHullbreakerHit(Combatant target)
        {
            ItemDefinition hullbreaker = Item(3181);
            if (hullbreaker == null) { hullbreakerStacks = 0; return 0; }
            if (Time.time >= hullbreakerExpires) hullbreakerStacks = 0;
            hullbreakerExpires = Time.time + hullbreaker.Value("SkipperStackDuration", 10);
            hullbreakerStacks = Mathf.Min(5, hullbreakerStacks + 1);
            if (hullbreakerStacks < 5 || target.Kind == UnitKind.Minion) return 0;
            hullbreakerStacks = 0;
            bool structure = target.Kind == UnitKind.Tower;
            float damage = self.BaseStats.AttackDamage.At(self.Level) * hullbreaker.Value(structure ? "SkipperADRatioVSStructures" : "SkipperADRatio", structure ? 3 : 1.2f) +
                self.MaxHealth * hullbreaker.Value(structure ? "MaxStackDamageVSStructuresHPRatio" : "MaxStackDamageHPRatio", structure ? .1f : .05f);
            if (Ranged) damage *= hullbreaker.Value("RangedDamageMultiplier", .7f);
            return target.TakeDamage(damage, self, DamageType.Physical);
        }
        public void OnInventoryChanged()
        {
            SustainInventoryChanged();
            if (Item(3181) == null) { hullbreakerStacks = 0; hullbreakerExpires = 0; }
            if (Item(6692) == null) { eclipseShield = 0; eclipseHits.Clear(); }
        }
        public static float BoardingPartyResistance(LaneWorld lane, Combatant minion)
        {
            if (lane == null || minion.Kind != UnitKind.Minion || minion.MinionType != MinionType.Cannon) return 0;
            float bonus = 0;
            foreach (Combatant owner in lane.Units)
            {
                if (owner == null || !owner.Alive || owner.Kind != UnitKind.Champion || owner.Team != minion.Team || !owner.Inventory.Has(3181)) continue;
                ItemDefinition item = owner.Inventory.Get(3181);
                float range = item.Value("BoardingRadiusUnits", 1050) * LeagueChampionStats.WorldUnitsPerLeagueUnit;
                if ((owner.Position - minion.Position).sqrMagnitude > range * range) continue;
                float resistance = Mathf.Lerp(item.Value("BoardingResistanceMin", 70), item.Value("BoardingResistanceMax", 130), Mathf.InverseLerp(8, 18, owner.Level));
                if (owner.AttackRange > 3) resistance *= .5f;
                bonus = Mathf.Max(bonus, resistance);
            }
            return bonus;
        }
        public void OnDamageDealt(Combatant target, float damage, bool areaOrPeriodic = false, bool canOmnivamp = true)
        {
            if (damage <= 0) return;
            bloodthirsterCombatAt = Time.time;
            if (target.Kind == UnitKind.Champion) doranDrainUntil = Time.time + 5;
            if (canOmnivamp && target.Kind != UnitKind.Tower)
                self.Heal(damage * self.Omnivamp * (target.Kind == UnitKind.Minion && areaOrPeriodic ? 1f / 3 : 1));
            if (target.Kind == UnitKind.Champion && Time.time < vampUntil) vampUntil = Mathf.Max(vampUntil, Time.time + 3);
            if (Time.time < vampUntil && Item(3156) != null) self.Heal(damage * Item(3156).Value("BuffVamp", 0.1f));
        }
        public float ReceiveDamage(float damage, DamageType type, Combatant source)
        {
            if (damage <= 0) return damage;
            warmogReadyAt = Mathf.Max(warmogReadyAt, Time.time + (source.Kind == UnitKind.Champion ? 8 : 3));
            if (Time.time < vampUntil) vampUntil = Mathf.Max(vampUntil, Time.time + 3);
            damage = AbsorbSustainShields(damage, type);
            if (damage <= 0) return 0;
            if (Time.time >= lifelineReadyAt && self.Health - damage <= self.MaxHealth * 0.3f)
            {
                ItemDefinition sterak = Item(3053), maw = Item(3156) ?? Item(3155);
                if (sterak != null)
                {
                    shield = Mathf.Max(0, self.MaxHealth - self.BaseStats.Health.At(self.Level)) * sterak.Value("BaseShieldRatio", 0.6f) * ShieldReceivedMultiplier;
                    shieldEnds = Time.time + sterak.Value("ShieldDuration", 4.5f); shieldDecayAt = Time.time + sterak.Value("TimeBeforeDecay", 0.75f);
                    if(self.Runes!=null) self.Runes.NotifyShield(shield,sterak.Value("ShieldDuration",4.5f));
                    lifelineReadyAt = Time.time + sterak.Value("Cooldown", 90);
                }
                else if (maw != null && type == DamageType.Magic)
                {
                    magicShield = maw.id == 3156 ? maw.Value("ShieldSize", 200) + self.AttackDamage * maw.Value("ShieldADScaling", 1.5f) : Mathf.Lerp(110, 280, (self.Level - 1) / 17f);
                    if (Ranged) magicShield *= 0.75f;
                    magicShieldEnds = Time.time + maw.Value(maw.id == 3156 ? "ShieldDuration" : "ShieldLifetime", 3);
                    if(self.Runes!=null) self.Runes.NotifyShield(magicShield,maw.Value(maw.id==3156?"ShieldDuration":"ShieldLifetime",3));
                    lifelineReadyAt = Time.time + maw.Value("Cooldown", 90);
                    if (maw.id == 3156) vampUntil = Time.time + maw.Value("BuffDuration", 5);
                }
            }
            float absorbed = Mathf.Min(shield, damage); shield -= absorbed; damage -= absorbed;
            absorbed = Mathf.Min(eclipseShield, damage); eclipseShield -= absorbed; damage -= absorbed;
            if (type == DamageType.Magic) { absorbed = Mathf.Min(magicShield, damage); magicShield -= absorbed; damage -= absorbed; }
            if (Item(6333) != null && type != DamageType.True && damage > 0)
            {
                float delayed = damage * (Ranged ? 0.1f : 0.3f);
                bleeds.Add(new Bleed { source = source, left = delayed, rate = delayed / 3 }); damage -= delayed;
            }
            return damage;
        }
        public bool TryRebirth()
        {
            if (Item(3026) == null || Time.time < reviveReadyAt) return false;
            reviveReadyAt = Time.time + Item(3026).Value("Cooldown", 300); reviveAt = Time.time + 4;
            shield = magicShield = eclipseShield = bloodthirsterShield = bootsShield = 0; bleeds.Clear(); return true;
        }
        public void CancelRebirthForPractice() { reviveAt = 0; }
        public void ResetCooldowns()
        {
            lifelineReadyAt = reviveReadyAt = warmogReadyAt = activeReadyAt = stasisReadyAt = eclipseReadyAt = 0;
            bootsShieldReadyAt = 0;
            eclipseHits.Clear(); shojinCastReady.Clear();
        }
        public void OnTakedown()
        {
            ItemDefinition dance = Item(6333); if (dance == null) return;
            bleeds.Clear(); defyHealUntil = Time.time + dance.Value("HealDuration", 2);
            defyHealRate = Mathf.Max(0, self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level)) * dance.Value("BonusADRatio", 0.75f) / 2;
        }
        public void OnDeath()
        {
            stasisUntil = 0;
            chapterUntil = 0;
            bleeds.Clear(); lightStacks = darkStacks = shojinStacks = krakenHits = 0;
            hullbreakerStacks = 0; hullbreakerExpires = 0;
            shield = magicShield = eclipseShield = bloodthirsterShield = bootsShield = 0; eclipseHits.Clear(); vampUntil = speedUntil = defyHealUntil = 0;
            shieldReaverUntil = shieldReaverReduction = 0;
        }
        public bool UseActive(int id)
        {
            if (id == 3157 || id == 2420) return UseStasis(id);
            ItemDefinition item = Item(id);
            if ((id != 6631 && id != 3077) || item == null || !self.CanAct || Time.time < activeReadyAt) return false;
            activeReadyAt = Time.time + item.Value("Cooldown", 15);
            int champions = 0;
            foreach (Combatant target in world.Units)
                if (target.Kind != UnitKind.Tower && self.ValidEnemy(target) && Vector3.Distance(self.Position, target.Position) <= 4.5f + target.Radius)
                {
                    target.TakeDamage(self.AttackDamage * item.Value(id == 6631 ? "ADRatio" : "ActiveADRatio", 0.8f), self);
                    if (id == 6631) target.Slow(1 + item.Value("MSSlow", -0.35f), item.Value("Duration", 3));
                    if (target.Kind == UnitKind.Champion) champions++;
                }
            if (id == 6631 && champions > 0)
            { speedBonus = self.MoveSpeed * item.Value("ActiveMS", 0.35f) * champions; speedUntil = Time.time + item.Value("Duration", 3); }
            return true;
        }
        public void Tick(float dt)
        {
            TickSustainShields(dt);
            TickBlazes();
            if (Time.time >= eclipseShieldUntil) eclipseShield = 0;
            if (self.Alive && Time.time < chapterUntil) self.RestoreResource(chapterRate * dt);
            if (Time.time >= shieldEnds) shield = 0;
            else if (Time.time > shieldDecayAt) shield *= Mathf.Max(0, 1 - dt / Mathf.Max(dt, shieldEnds - Time.time + dt));
            if (Time.time >= magicShieldEnds) magicShield = 0;
            if (speedUntil > Time.time) speedBonus *= Mathf.Max(0, 1 - dt / Mathf.Max(dt, speedUntil - Time.time + dt));
            if (IsReviving)
            {
                if (Time.time >= reviveAt) { reviveAt = 0; self.CompleteGuardianRebirth(); }
                return;
            }
            if (!self.Alive) return;
            for (int i = bleeds.Count - 1; i >= 0; i--)
            {
                Bleed bleed = bleeds[i]; float amount = Mathf.Min(bleed.left, bleed.rate * dt); bleed.left -= amount;
                if (bleed.left <= 0) bleeds.RemoveAt(i);
                if (bleed.source != null) self.TakeDamage(amount, bleed.source, DamageType.True, false, false, true);
                if (!self.Alive || IsReviving) return;
            }
            if (Time.time < defyHealUntil) self.Heal(defyHealRate * dt);
            ItemDefinition warmog = Item(3083);
            if (warmog != null && Time.time >= warmogReadyAt && self.MaxHealth - self.BaseStats.Health.At(self.Level) >= warmog.Value("HealthThreshold", 2000))
                self.Heal(self.MaxHealth * warmog.Value("MaxHealthRatio", 0.015f) / warmog.Value("SecondsPerHeal", 0.5f) * dt);
        }
    }
}
