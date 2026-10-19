using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Original character, with an editable PC blade-duelist practice ruleset.
    public sealed class BladeDuelist : MonoBehaviour
    {
        public BladeTuning Tuning { get; private set; }
        public ChampionProgress Progress { get { return self.Progress; } }
        public int Stacks { get; private set; }
        public bool IsDashing { get; private set; }
        public int QSequence { get; private set; }
        public int QFinishedSpellId { get; private set; }
        public bool QChained { get; private set; }
        public float QElapsed { get { return Time.time - dashStartedAt; } }
        public float QRecoveryElapsed { get { return Time.time - qCompletedAt; } }
        public float QDashProgress { get; private set; }
        public Vector3 QDirection { get; private set; }
        public Combatant DashTarget { get { return IsDashing ? dashTarget : null; } }
        public Vector3 PredictDashPosition(float seconds)
        {
            if (!IsDashing || dashTarget == null) return self.Position;
            Vector3 endpoint = world.AvoidTower(self, dashTarget.Position + dashDirection * (self.Radius + dashTarget.Radius + 0.05f));
            return Vector3.MoveTowards(self.Position, endpoint, (Tuning.qSpeed + self.MoveSpeed) * Mathf.Max(0, seconds));
        }
        public float QMinionBonusDamage { get { return 50 + 11 * (Progress.Level - 1); } }
        public float QBaseDamage { get { return Tuning.qDamage[Mathf.Clamp(Rank(0) - 1, 0, 4)] + self.AttackDamage * Tuning.qAttackDamageRatio; } }
        public float QRawDamage(Combatant target)
        {
            return QBaseDamage + (target.Kind == UnitKind.Minion ? QMinionBonusDamage : 0);
        }
        public bool IsCharging { get; private set; }
        bool fullWChargeSoundPlayed;
        // Presentation reads the actual cast clock, including a press/release within one frame.
        public float WChargeElapsed { get { return IsCharging ? Time.time - chargeAt : 0; } }
        public float WReleaseElapsed { get { return Time.time - wReleasedAt; } }
        public int WCastId { get; private set; }
        public Vector3 WDirection { get { return Direction(chargeAim); } }
        public int ESequence { get; private set; }
        public int ESpellCastId { get; private set; }
        public int EBasicSequence { get; private set; }
        public bool ESecondCast { get; private set; }
        public float EAnimationElapsed { get { return Time.time - eAnimationAt; } }
        public Vector3 EDirection { get; private set; }
        public int RSequence { get; private set; }
        public int RSpellCastId { get; private set; }
        public float RAnimationElapsed { get { return Time.time - rAnimationAt; } }
        public Vector3 RDirection { get; private set; }
        public bool WaitingSecondBlade { get; private set; }
        public bool BlocksActions { get { return IsDashing || IsCharging; } }
        public float ERemaining { get { return WaitingSecondBlade ? Mathf.Max(0, eExpiresAt - Time.time) : 0; } }
        public float ChargeFraction { get { return IsCharging ? Mathf.Clamp01((Time.time - chargeAt) / Tuning.wFullCharge) : 0; } }
        public float AttackInterval
        {
            get
            {
                float perStack = Mathf.Lerp(Tuning.passiveAttackSpeedStart, Tuning.passiveAttackSpeedEnd, Mathf.InverseLerp(1, 18, Progress.Level));
                return 1 / Mathf.Min(2.5f, self.BaseStats.AttackSpeedAt(Progress.Level, self.BonusAttackSpeed + Stacks * perStack));
            }
        }
        LaneWorld world;
        Combatant self, dashTarget;
        Vector3 dashDirection, chargeAim, firstBladePoint;
        float dashStartedAt, chargeAt, guardUntil, ePlacedAt, eExpiresAt, passiveExpiresAt;
        float wReleasedAt = float.NegativeInfinity;
        float qCompletedAt = float.NegativeInfinity, qTraveled;
        float eAnimationAt = float.NegativeInfinity, rAnimationAt = float.NegativeInfinity;
        bool attackAfterDash;
        GameObject chargeVisual;
        IreliaDirectionIndicator wIndicator;
        public GameObject WIndicator { get { return wIndicator!=null?wIndicator.gameObject:null; } }
        BladeLink pendingLink;
        int bufferedSlot = -1;
        Vector3 bufferedAim;
        Combatant bufferedTarget;
        float bufferExpiresAt;
        Combatant approachTarget;
        int approachTargetDeaths;
        bool bufferedWReleased;
        public bool ApproachingForQ { get { return approachTarget != null; } }
        public void CancelInput() { approachTarget = null; bufferedSlot = -1; bufferedWReleased = false; }

        public void Initialize(LaneWorld lane, Combatant actor)
        {
            world = lane; self = actor;
            var stored = Resources.Load<BladeTuning>("BladeTuning");
            Tuning = stored != null ? Instantiate(stored) : ScriptableObject.CreateInstance<BladeTuning>();
            ChampionSkillPlan.LearnBasics(Progress);
        }
        public int Rank(int slot) { return Progress.Rank(slot); }
        public bool Learn(int slot) { return Progress.Learn(slot); }
        public void GainExperience(float amount)
        {
            self.GainExperience(amount);
        }
        public float AbilityCost(int slot) { return Tuning.Cost(slot, Mathf.Max(1, Rank(slot))); }
        public float AbilityCooldown(int slot)
        {
            float[] values = slot == 0 ? Tuning.qCooldown : slot == 1 ? Tuning.wCooldown : slot == 2 ? Tuning.eCooldown : Tuning.rCooldown;
            float raw = values[Mathf.Clamp(Rank(slot) - 1, 0, values.Length - 1)];
            if (slot == 0) raw -= 0.5f * Rank(3);
            return raw * 100 / (100 + self.HasteForAbility(slot) + Tuning.abilityHaste);
        }
        public bool Ready(int slot)
        {
            if (!self.Alive || self.IsInStasis || world.Paused || world.RoundOver || Rank(slot) == 0) return false;
            if (slot == 1 && IsCharging) return true;
            if (self.IsStunned || IsCharging) return false;
            if (slot == 0 && self.IsRooted) return false;
            if (slot == 2 && WaitingSecondBlade) return Time.time >= ePlacedAt + Tuning.eRecastLock;
            bool canCast = slot == 2 || self.CanAct;
            return canCast && self.Cooldowns.Ready(slot, Time.time) && self.Resource >= AbilityCost(slot);
        }
        public bool Cast(int slot, Vector3 aim, Combatant target)
        {
            if (!Ready(slot)) return false;
            // Settle any due basic before Q cancels preparation or arms spell on-hit effects.
            self.ResolveAttackWindup();
            if (slot == 1 && IsCharging) return ReleaseW(aim);
            if (slot == 2 && WaitingSecondBlade) return PlaceSecondBlade(aim);
            if (slot == 0 && (!self.ValidEnemy(target) || target.Kind == UnitKind.Tower || Vector3.Distance(self.Position, target.Position) > Tuning.qRange)) return false;
            if (!self.SpendResource(AbilityCost(slot))) return false;
            self.NotifySpellCast();
            if (slot != 2) self.Stop();
            // E starts its cooldown when its second blade is committed, including auto-recast.
            if (slot != 2) self.Cooldowns.Start(slot, Time.time, AbilityCooldown(slot));
            if (slot == 0)
            {
                dashTarget = target; IsDashing = true; dashStartedAt = Time.time; attackAfterDash = true;
                if(self.Runes!=null) self.Runes.NotifyDash();
                QChained = Time.time - qCompletedAt < 0.4f; QSequence++;
                qCompletedAt = float.NegativeInfinity; qTraveled = 0; QDashProgress = 0;
                dashDirection = Direction(target.Position);
                QDirection = dashDirection;
                self.ResetAttackTimer();
                if (self.Audio != null) self.Audio.QStart();
            }
            else if (slot == 1)
            {
                IsCharging = true; chargeAt = Time.time; chargeAim = aim;
                fullWChargeSoundPlayed = false;
                if (self.Audio != null) self.Audio.WStart();
                wReleasedAt = float.NegativeInfinity; WCastId = self.SpellCastId;
                chargeVisual = world.Shape("Charging guard", PrimitiveType.Cylinder, transform, Vector3.up * 0.1f,
                    new Vector3(2.2f, 0.035f, 2.2f), self.Color * 0.6f);
                if (wIndicator==null) wIndicator=IreliaDirectionIndicator.Create(transform,"W · charge direction");
                PresentWIndicator();
            }
            else if (slot == 2)
            {
                WaitingSecondBlade = true; ePlacedAt = Time.time; eExpiresAt = Time.time + Tuning.eRecastWindow;
                firstBladePoint = Placement(aim);
                RecordEAnimation(false, firstBladePoint);
                pendingLink = world.CreateBladeLink(this, firstBladePoint);
            }
            else
            {
                rAnimationAt = Time.time; RSequence++; RSpellCastId = self.SpellCastId; RDirection = Direction(aim);
                self.LockActions(Tuning.rCastTime);
                if (self.Audio != null) self.Audio.RStart();
                world.CreateBladeUltimate(this, Direction(aim));
            }
            if (slot != 2) world.AbilityUsed(self, slot);
            return true;
        }
        public bool Command(int slot, Vector3 aim, Combatant target)
        {
            if (self.IsInStasis) return false;
            CancelInput();
            if (Cast(slot, aim, target)) return true;
            bool eligible = slot >= 0 && slot < 4 && Rank(slot) > 0 && self.Alive && !world.Paused && !world.RoundOver;
            if (!eligible) return false;
            if (slot == 0 && (!self.ValidEnemy(target) || target.Kind == UnitKind.Tower)) return false;
            if (slot == 0 && Vector3.Distance(self.Position, target.Position) > Tuning.qRange &&
                self.Resource >= AbilityCost(0) && self.Cooldowns.Remaining(0, Time.time) <= Tuning.inputBufferWindow && !IsCharging)
            {
                self.MoveForAbility(target.Position);
                approachTarget = target; approachTargetDeaths = target.Deaths; return true;
            }
            if (!(slot == 2 && WaitingSecondBlade) && self.Resource < AbilityCost(slot)) return false;
            float cooldown = self.Cooldowns.Remaining(slot, Time.time);
            bool soon = (cooldown > 0 && cooldown <= Tuning.inputBufferWindow) ||
                (self.ActionLockRemaining > 0 && self.ActionLockRemaining <= Tuning.inputBufferWindow);
            if (eligible && (IsDashing || (slot == 2 && WaitingSecondBlade) || soon) && !IsCharging)
            {
                bufferedSlot = slot; bufferedAim = aim; bufferedTarget = target;
                bufferExpiresAt = Time.time + Tuning.inputBufferWindow;
                if (IsDashing) bufferExpiresAt = Mathf.Max(bufferExpiresAt, dashStartedAt + 1);
                if (slot == 2 && WaitingSecondBlade) bufferExpiresAt = Mathf.Max(bufferExpiresAt, ePlacedAt + Tuning.eRecastLock + 0.03f);
                return true;
            }
            return false;
        }
        public void RequestWRelease(Vector3 aim)
        {
            if (IsCharging) ReleaseW(aim);
            else if (bufferedSlot == 1) { bufferedWReleased = true; bufferedAim = aim; }
        }
        public void AimW(Vector3 aim) { if (IsCharging) { chargeAim = aim; PresentWIndicator(); } }
        void PresentWIndicator()
        { if (wIndicator!=null) wIndicator.Present(self.Position,WDirection,Tuning.wRange,Tuning.wHalfWidth*2); }
        public bool ReleaseW(Vector3 aim)
        {
            if (!IsCharging || world.Paused || world.RoundOver || !self.Alive) return false;
            chargeAim = aim;
            NotifyFullWCharge();
            float multiplier = 1 + 2 * ChargeFraction;
            float damage = (10 * Rank(1) + self.AttackDamage * 0.4f + self.AbilityPower * 0.5f) * multiplier;
            IsCharging = false;
            if (self.Audio != null) self.Audio.WRelease();
            if (wIndicator!=null) wIndicator.Hide();
            wReleasedAt = Time.time;
            world.PracticeAbilityUsed(self);
            self.RecordPvpActivity();
            guardUntil = Time.time + Tuning.wReleaseTime;
            if (chargeVisual != null) Destroy(chargeVisual);
            self.LockActions(Tuning.wReleaseTime);
            world.CreateBladeSlash(this, Direction(aim), damage);
            return true;
        }
        void NotifyFullWCharge()
        {
            if (!IsCharging || fullWChargeSoundPlayed || ChargeFraction < 1) return;
            fullWChargeSoundPlayed = true;
            if (self.Audio != null) self.Audio.WFullyCharged();
        }
        bool PlaceSecondBlade(Vector3 aim)
        {
            Vector3 point = Placement(aim);
            RecordEAnimation(true, point);
            WaitingSecondBlade = false;
            self.CancelAttackWindup();
            self.LockActions(Tuning.eSecondCastTime);
            self.Cooldowns.Start(2, Time.time, AbilityCooldown(2));
            if (pendingLink != null) pendingLink.PlaceSecond(point);
            pendingLink = null; world.AbilityUsed(self, 2); return true;
        }
        void RecordEAnimation(bool second, Vector3 point)
        {
            eAnimationAt = Time.time; ESequence++; ESecondCast = second;
            EDirection = Direction(point); ESpellCastId = self.SpellCastId; EBasicSequence = self.BasicAttackSequence;
        }
        public void Tick()
        {
            if (Time.time >= passiveExpiresAt) Stacks = 0;
            if (IsCharging)
            {
                NotifyFullWCharge();
                PresentWIndicator();
                if (Stacks > 0) passiveExpiresAt = Time.time + 0.5f;
                if (Time.time >= chargeAt + Tuning.wMaxHold) ReleaseW(chargeAim);
            }
            if (WaitingSecondBlade && Time.time >= eExpiresAt && !self.IsStunned && !IsCharging)
                PlaceSecondBlade(self.Position);
            if (IsDashing) AdvanceDash();
            if (approachTarget != null)
            {
                if (!self.ValidEnemy(approachTarget) || approachTarget.Deaths != approachTargetDeaths || self.Resource < AbilityCost(0))
                { CancelInput(); self.Stop(); }
                else if (Ready(0))
                {
                    Combatant target = approachTarget;
                    if (Vector3.Distance(self.Position, target.Position) <= Tuning.qRange) Cast(0, target.Position, target);
                    else self.MoveForAbility(target.Position);
                }
            }
            if (bufferedSlot >= 0)
            {
                if (Time.time > bufferExpiresAt) CancelInput();
                else
                {
                    int slot = bufferedSlot; Vector3 aim = bufferedAim; bool release = bufferedWReleased;
                    if (Cast(slot, aim, bufferedTarget))
                    { bufferedSlot = -1; bufferedWReleased = false; if (slot == 1 && release) ReleaseW(aim); }
                }
            }
        }
        void AdvanceDash()
        {
            if (dashTarget == null || !dashTarget.Targetable || self.IsStunned || Time.time - dashStartedAt > 1)
            { InterruptDash(); return; }
            // Follow a moving target; stop just beyond it, rather than teleporting to a cached position.
            Vector3 endpoint = world.AvoidTower(self, dashTarget.Position + dashDirection * (self.Radius + dashTarget.Radius + 0.05f));
            Vector3 before = self.Position;
            self.transform.position = Vector3.MoveTowards(self.Position, endpoint, (Tuning.qSpeed + self.MoveSpeed) * Time.deltaTime);
            Vector3 traveled = self.Position - before; qTraveled += traveled.magnitude;
            if (traveled.sqrMagnitude > 0.000001f) QDirection = traveled.normalized;
            QDashProgress = Mathf.Max(QDashProgress, qTraveled / Mathf.Max(0.001f, qTraveled + Vector3.Distance(self.Position, endpoint)));
            if (Vector3.Distance(self.Position, endpoint) > 0.04f) return;
            Combatant victim = dashTarget; bool marked = victim.Marked;
            IsDashing = false; dashTarget = null;
            qCompletedAt = Time.time; QFinishedSpellId = self.SpellCastId; QDashProgress = 1;
            float raw = QRawDamage(victim);
            self.ApplyAttackHit(victim, raw, false);
            if (self.Audio != null) self.Audio.QImpact();
            OnBasicAttackHit(victim); AwardSpellHits(new[] { victim });
            self.Heal(self.AttackDamage * Tuning.qHealingRatio[Mathf.Clamp(Rank(0) - 1, 0, 4)]);
            // A lethal champion hit already cleared both champions' clocks in UnitDied.
            if ((marked || !victim.Alive) && self.Cooldowns.Remaining(0, Time.time) > 0)
                self.Cooldowns.Start(0, Time.time, Tuning.qResetLock);
            victim.ClearMark();
            self.ResetAttackTimer();
            if (attackAfterDash && victim.Targetable) self.OrderAttackAfterAbility(victim);
        }
        public void MovementOrdered() { CancelInput(); if (IsDashing) attackAfterDash = false; }
        public void InterruptDash() { IsDashing = false; dashTarget = null; qCompletedAt = float.NegativeInfinity; }
        public void CancelTransient()
        {
            if (self.Audio != null) self.Audio.StopWCharge();
            if (wIndicator!=null) wIndicator.Hide();
            CancelInput();
            InterruptDash(); IsCharging = false; guardUntil = 0; WaitingSecondBlade = false; bufferedSlot = -1;
            wReleasedAt = float.NegativeInfinity;
            eAnimationAt = rAnimationAt = float.NegativeInfinity;
            if (pendingLink != null) Destroy(pendingLink.gameObject);
            if (chargeVisual != null) Destroy(chargeVisual);
            pendingLink = null;
        }
        public void ResetPassive() { CancelTransient(); Stacks = 0; passiveExpiresAt = 0; }
        public float DamageReduction(DamageType type)
        {
            if ((!IsCharging && Time.time >= guardUntil) || type == DamageType.True) return 0;
            float level = Mathf.InverseLerp(1, 18, Progress.Level);
            return Mathf.Clamp01(type == DamageType.Physical ? Mathf.Lerp(Tuning.wPhysicalReductionStart, Tuning.wPhysicalReductionEnd, level) + self.AbilityPower * 0.0008f :
                Mathf.Lerp(Tuning.wMagicReductionStart, Tuning.wMagicReductionEnd, level) + self.AbilityPower * 0.0004f);
        }
        public void OnBasicAttackHit(Combatant target)
        {
            if (Stacks > 0 && target.Kind != UnitKind.Tower) passiveExpiresAt = Time.time + Tuning.passiveDuration;
            if (Stacks == 4 && target.Alive)
            {
                float damage = 10 + 3 * (Progress.Level - 1) + 0.2f * (self.AttackDamage - self.BaseStats.AttackDamage.At(Progress.Level));
                self.DealAbilityDamage(target, damage * (target.Kind == UnitKind.Tower ? 0.5f : 1), DamageType.Magic, -1);
            }
        }
        public void AwardSpellHits(IEnumerable<Combatant> victims)
        {
            if (!self.Alive) return;
            int count = 0; bool nonChampionHit = false;
            foreach (Combatant victim in victims)
            {
                if (victim.Kind == UnitKind.Champion) count++;
                else if (victim.Kind == UnitKind.Minion) nonChampionHit = true;
            }
            if (nonChampionHit) count++;
            if (count == 0) return;
            Stacks = Mathf.Min(4, Stacks + count); passiveExpiresAt = Time.time + Tuning.passiveDuration;
        }
        public Combatant Actor { get { return self; } }
        public Vector3 FirstBlade { get { return firstBladePoint; } }
        public Vector3 Placement(Vector3 point)
        { Vector3 delta = point - self.Position; delta.y = 0; return world.Clamp(self.Position + Vector3.ClampMagnitude(delta, Tuning.eRange)); }
        Vector3 Direction(Vector3 point)
        { Vector3 delta = point - self.Position; delta.y = 0; return delta.sqrMagnitude > 0.001f ? delta.normalized : Vector3.right; }
        void OnDestroy() { if (Tuning != null) Destroy(Tuning); }
    }
}
