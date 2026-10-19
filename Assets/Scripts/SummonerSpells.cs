using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public sealed class SummonerSpells : MonoBehaviour
    {
        LaneWorld world;
        Combatant self;
        float flashReadyAt, igniteReadyAt;
        float barrierReadyAt, exhaustReadyAt, barrierShield, barrierUntil, exhaustedUntil;
        readonly SummonerSpell[] equipped = { SummonerSpell.Flash, SummonerSpell.Ignite };
        public SummonerSpell Equipped(int slot) { return equipped[slot]; }
        public void ConfigureLoadout(SummonerSpell d, SummonerSpell f)
        {
            if (!SummonerRules.Valid(d) || !SummonerRules.Valid(f) || d == f) return;
            equipped[0] = d; equipped[1] = f;
        }
        public bool CanSelect(SummonerSpell spell)
        { return SummonerRules.Valid(spell) && equipped[0] != spell && equipped[1] != spell; }
        public bool Select(int slot, SummonerSpell spell)
        {
            if (slot < 0 || slot > 1 || !CanSelect(spell)) return false;
            equipped[slot] = spell;
            if (self == world.Player)
            {
                world.Settings.dSummoner = equipped[0]; world.Settings.fSummoner = equipped[1];
                world.SaveSummonerLoadout();
            }
            return true;
        }
        public bool CastSlot(int slot, Vector3 aim, Combatant target)
        {
            if (slot < 0 || slot > 1) return false;
            switch (equipped[slot])
            {
                case SummonerSpell.Flash: return Flash(aim);
                case SummonerSpell.Ignite: return Ignite(target);
                case SummonerSpell.Barrier: return Barrier();
                case SummonerSpell.Exhaust: return Exhaust(target);
                default: return false;
            }
        }
        public float Remaining(SummonerSpell spell)
        {
            switch (spell)
            {
                case SummonerSpell.Flash: return FlashRemaining;
                case SummonerSpell.Ignite: return IgniteRemaining;
                case SummonerSpell.Barrier: return Mathf.Max(0, barrierReadyAt - Time.time);
                case SummonerSpell.Exhaust: return Mathf.Max(0, exhaustReadyAt - Time.time);
                default: return 0;
            }
        }
        public float BarrierShield { get { return Time.time < barrierUntil ? barrierShield : 0; } }
        public float ExhaustedRemaining { get { return Mathf.Max(0, exhaustedUntil - Time.time); } }
        public float OutgoingDamageMultiplier(DamageType type)
        { return type != DamageType.True && Time.time < exhaustedUntil ? 1 - SummonerRules.ExhaustDamageReduction : 1; }
        public float AbsorbBarrier(float amount)
        { float absorbed = Mathf.Min(BarrierShield, amount); barrierShield -= absorbed; return amount - absorbed; }
        public void ScaleBarrierShield(float factor) { barrierShield *= factor; }
        public void ClearTemporaryEffects() { barrierShield = 0; barrierUntil = exhaustedUntil = 0; }
        sealed class Burn { public Combatant target; public float nextTick, damage; public int ticks, targetDeaths; }
        readonly List<Burn> burns = new List<Burn>();
        public float FlashRemaining { get { return Mathf.Max(0, flashReadyAt - Time.time); } }
        public float IgniteRemaining { get { return Mathf.Max(0, igniteReadyAt - Time.time); } }
        public void ResetCooldowns() { flashReadyAt = igniteReadyAt = barrierReadyAt = exhaustReadyAt = 0; }
        public float PendingIgniteDamage(Combatant target)
        {
            float damage = 0;
            foreach (Burn burn in burns) if (burn.target == target && target != null && target.Alive && target.Deaths == burn.targetDeaths) damage += burn.damage * burn.ticks;
            return damage;
        }
        public void Initialize(LaneWorld lane, Combatant owner) { world = lane; self = owner; }
        bool Available { get { return self.gameObject.activeInHierarchy && self.Alive && !self.IsInStasis && !self.IsStunned && !world.Paused && !world.RoundOver && !self.ItemEffects.IsReviving; } }
        float Cooldown(float seconds)
        {
            float haste = 0; foreach (ItemDefinition item in self.Inventory.Items) haste += item.Value("SummonerHaste");
            return seconds * 100 / (100 + haste);
        }
        public bool Flash(Vector3 cursor)
        {
            if (!Available || self.IsRooted || FlashRemaining > 0 || (self.Sett != null && self.Sett.BlocksFlash) || (self.Blade != null && (self.Blade.IsDashing || self.Blade.IsCharging))) return false;
            Vector3 delta = cursor - self.Position; delta.y = 0;
            if (delta.sqrMagnitude < 0.0001f) return false;
            Vector3 destination = world.Clamp(self.Position + Vector3.ClampMagnitude(delta, SummonerRules.FlashRange), self.Radius);
            destination = world.AvoidTower(self, destination);
            if (Vector3.Distance(destination, self.Position) > SummonerRules.FlashRange + 0.001f || (destination - self.Position).sqrMagnitude < 0.0001f) return false;
            Vector3 before = self.Position; self.Stop(); self.transform.position = destination; world.Navigation.Forget(self);
            if(self.Runes!=null) self.Runes.NotifyDash();
            flashReadyAt = Time.time + Cooldown(SummonerRules.FlashCooldown); world.FlashLine(before + Vector3.up, destination + Vector3.up, new Color(1, 0.9f, 0.35f));
            world.FlashSounds.Play();
            world.PracticeAbilityUsed(self);
            return true;
        }
        public bool Ignite(Combatant target)
        {
            if (!Available || IgniteRemaining > 0 || !self.ValidEnemy(target) || target.Kind != UnitKind.Champion || Vector3.Distance(self.Position, target.Position) > SummonerRules.IgniteRange + target.Radius) return false;
            self.CancelRecall(); igniteReadyAt = Time.time + Cooldown(SummonerRules.IgniteCooldown);
            target.ApplyGrievousWounds(SummonerRules.IgniteDuration);
            burns.Add(new Burn { target = target, targetDeaths = target.Deaths, nextTick = Time.time, damage = SummonerRules.IgniteDamage(self.Level) / SummonerRules.IgniteTicks, ticks = SummonerRules.IgniteTicks });
            world.FlashLine(self.Position + Vector3.up, target.Position + Vector3.up, new Color(1, 0.35f, 0.1f));
            world.PracticeAbilityUsed(self); TickBurns(); return true;
        }
        public bool Barrier()
        {
            if (!Available || Remaining(SummonerSpell.Barrier) > 0) return false;
            self.CancelRecall(); barrierReadyAt = Time.time + Cooldown(SummonerRules.BarrierCooldown);
            barrierShield = SummonerRules.BarrierShield(self.Level) * self.ItemEffects.ShieldReceivedMultiplier;
            barrierUntil = Time.time + SummonerRules.BarrierDuration;
            if(self.Runes!=null) self.Runes.NotifyShield(barrierShield,SummonerRules.BarrierDuration);
            world.PracticeAbilityUsed(self); return true;
        }
        public bool Exhaust(Combatant target)
        {
            if (!Available || Remaining(SummonerSpell.Exhaust) > 0 || !self.ValidEnemy(target) || target.Kind != UnitKind.Champion ||
                Vector3.Distance(self.Position, target.Position) > SummonerRules.ExhaustRange + target.Radius || target.Summoners == null) return false;
            self.CancelRecall(); exhaustReadyAt = Time.time + Cooldown(SummonerRules.ExhaustCooldown);
            target.Summoners.exhaustedUntil = Time.time + SummonerRules.ExhaustDuration;
            target.Slow(1 - SummonerRules.ExhaustSlow, SummonerRules.ExhaustDuration);
            world.FlashLine(self.Position + Vector3.up, target.Position + Vector3.up, new Color(.8f, .65f, .9f));
            world.PracticeAbilityUsed(self); return true;
        }
        public void CancelOngoingDamage() { burns.Clear(); }
        public void CancelDamageAgainst(Combatant target) { burns.RemoveAll(burn => burn.target == target); }
        void Update() { if (world != null && !world.Paused && !world.RoundOver) TickBurns(); }
        void TickBurns()
        {
            for (int i = burns.Count - 1; i >= 0; i--)
            {
                Burn burn = burns[i];
                if (burn.target == null || !burn.target.gameObject.activeInHierarchy || !burn.target.Alive || burn.target.Deaths != burn.targetDeaths) { burns.RemoveAt(i); continue; }
                while (burn.ticks > 0 && Time.time >= burn.nextTick && burn.target.Alive)
                { burn.target.TakeDamage(burn.damage, self, DamageType.True, false, false, true, hitLabel: "Ignite"); burn.ticks--; burn.nextTick += SummonerRules.IgniteDuration / (SummonerRules.IgniteTicks - 1); }
                if (burn.ticks <= 0) burns.RemoveAt(i);
            }
        }
    }
}
