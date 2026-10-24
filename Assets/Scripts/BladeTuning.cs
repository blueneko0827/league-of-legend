using UnityEngine;

namespace MidLane
{
    [CreateAssetMenu(menuName = "MidLane/Blade player tuning")]
    public sealed class BladeTuning : ScriptableObject
    {
        [Header("Practice progression")]
        [Min(0.1f)] public float experienceRadius = 16;
        [Header("Cooldowns by skill rank / seconds")]
        public float[] qCooldown = { 10, 9, 8, 7, 6 };
        public float[] wCooldown = { 20, 18, 16, 14, 12 };
        public float[] eCooldown = { 16, 14.5f, 13, 11.5f, 10 };
        public float[] rCooldown = { 125, 105, 85 };
        [Min(0)] public float abilityHaste;
        [Header("Q")]
        public float qRange = PracticeAbilityGeometry.IreliaQRange, qMana = 15, qSpeed = 14;
        public float[] qDamage = { 5, 25, 45, 65, 85 };
        public float qAttackDamageRatio = 0.8f;
        public float[] qHealingRatio = { 0.09f, 0.10f, 0.11f, 0.12f, 0.13f };
        // LoL: hitting a marked target drops the current cooldown to 0.2 s (from the contact).
        public float qResetLock = 0.2f;
        // LoL: the dash ends 100 units past the target's location.
        public float qPassThrough = 100 * LeagueChampionStats.WorldUnitsPerLeagueUnit;
        [Header("W / hold then release")]
        public float wRange = PracticeAbilityGeometry.IreliaWRange, wHalfWidth = PracticeAbilityGeometry.IreliaWHalfWidth;
        public float wMaxHold = 1.5f, wFullCharge = 0.75f, wReleaseTime = 0.25f;
        public float wPhysicalReductionStart = 0.4f, wPhysicalReductionEnd = 0.7f;
        public float wMagicReductionStart = 0.2f, wMagicReductionEnd = 0.35f;
        [Header("E / two independent placements")]
        [Min(0.1f)] public float eBladeVisualMultiplier = 2;
        public float eRange = PracticeAbilityGeometry.IreliaEPlacementRange, eMana = 50, ePlacementTime = 0.25f;
        // LoL: the second blade can be cast 0.15 s after the first.
        public float eRecastLock = 0.15f, eSecondCastTime = 0.125f, eRecastWindow = 3.5f;
        public float eConvergeTime = 0.35f, eHalfWidth = PracticeAbilityGeometry.IreliaEHalfWidth, eStun = 0.75f, markDuration = 5;
        [Header("R")]
        public float rRange = PracticeAbilityGeometry.IreliaRRange, rMana = 100, rCastTime = 0.4f, rSpeed = 20, rHalfWidth = PracticeAbilityGeometry.IreliaRHalfWidth;
        public float rWallLifetime = PracticeAbilityGeometry.IreliaRWallLifetime, rWallSlowDuration = 1.5f;
        // The blades drop on the wall a moment after the explosion (game footage: a target standing
        // on the wall line is hit 0.13-0.18 s after it); the wall hits from then on.
        public float rWallArmTime = 0.15f;
        // Presentation: an R cast within this time after the end of a W release is a W → R
        // (higher hop, IreliaRigAnimation.TickR).
        public float rAfterWWindow = 0.3f;
        [Header("Passive and attack timing")]
        public float passiveDuration = 6, passiveAttackSpeedStart = 0.1f, passiveAttackSpeedEnd = 0.25f;
        // LoL's queue threshold (Q and R: 0.5 s): an ability pressed this long before it can be
        // cast (cooldown or another ability's cast time) is queued instead of dropped.
        public float inputBufferWindow = 0.5f;

        public float Cooldown(int slot, int rank)
        {
            float[] values = slot == 0 ? qCooldown : slot == 1 ? wCooldown : slot == 2 ? eCooldown : rCooldown;
            return values[Mathf.Clamp(rank - 1, 0, values.Length - 1)] * 100 / (100 + Mathf.Max(0, abilityHaste));
        }
        public float Cost(int slot, int rank)
        { return slot == 0 ? qMana : slot == 1 ? 65 + rank * 5 : slot == 2 ? eMana : rMana; }
    }
}
