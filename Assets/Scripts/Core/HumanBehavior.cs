using System;

namespace MidLane
{
    public enum AttentionFocus { Farm, Combat, Defense }

    // Shared, immutable tuning. Runtime emotions/memory always belong to one bot.
    public sealed class HumanBehaviorProfile
    {
        public readonly string Name;
        public readonly float PeripheralDelay, FocusCommitment, ErrorPersistence, StressSensitivity;
        HumanBehaviorProfile(string name, float peripheral, float commitment, float persistence, float stress)
        { Name = name; PeripheralDelay = peripheral; FocusCommitment = commitment; ErrorPersistence = persistence; StressSensitivity = stress; }
        public static HumanBehaviorProfile For(Difficulty difficulty)
        {
            return difficulty == Difficulty.Expert ? new HumanBehaviorProfile("Oro", .06f, .35f, .6f, .8f) :
                difficulty == Difficulty.Hard ? new HumanBehaviorProfile("Plata", .1f, .5f, .75f, 1) :
                new HumanBehaviorProfile("Bronce", .16f, .65f, .9f, 1.2f);
        }
    }

    public sealed class HumanBehaviorState
    {
        public HumanBehaviorProfile Profile { get; }
        public float Tilt { get; private set; }
        public float Confidence { get; private set; } = .5f;
        public AttentionFocus Focus { get; private set; }
        public float Age { get; private set; }
        public float AimX { get; private set; }
        public float AimZ { get; private set; }
        public float AggressionMultiplier => 1 + (Confidence - .5f) * .6f;
        public float TetherMultiplier => 1 - (Confidence - .5f) * .4f;
        public float RiskThresholdMultiplier => 1 - (Confidence - .5f) * .3f;
        public float AimErrorMultiplier => 1 + Tilt * .8f;
        public float PredictionMultiplier => 1 - Tilt * .25f;
        public float ReactionMultiplier => 1 + Tilt * .2f;
        public float PeripheralCombatDelay => Focus == AttentionFocus.Farm ? Profile.PeripheralDelay : 0;
        public float PeripheralFarmDelay => Focus == AttentionFocus.Combat ? Profile.PeripheralDelay : 0;
        public bool SimplifyCombos => Tilt >= .65f;
        uint random;
        float nextErrorAt, focusUntil, dangerUntil, lastStressAt = -10, exchangeUntil;
        float outgoing, incoming;
        AttentionFocus requested;
        public HumanBehaviorState(HumanBehaviorProfile profile, int seed, AttentionFocus initial = AttentionFocus.Farm)
        {
            Profile = profile; random = unchecked((uint)seed); if (random == 0) random = 1;
            Focus = requested = initial; SampleError(); nextErrorAt = profile.ErrorPersistence;
        }
        static float Clamp(float n, float low, float high) { return Math.Max(low, Math.Min(high, n)); }
        float NextSigned()
        { random ^= random << 13; random ^= random >> 17; random ^= random << 5; return (random & 0xFFFFFF) / 8388607.5f - 1; }
        void SampleError() { AimX = NextSigned(); AimZ = NextSigned(); }
        void Adjust(float tilt, float confidence)
        {
            Tilt = Clamp(Tilt + tilt * Profile.StressSensitivity, 0, .85f);
            Confidence = Clamp(Confidence + confidence, .2f, .8f);
            if (tilt > 0) lastStressAt = Age;
        }
        public void Tick(float dt)
        {
            if (dt <= 0) return;
            Age += dt;
            while (Age >= nextErrorAt) { SampleError(); nextErrorAt += Profile.ErrorPersistence; }
            if (Age - lastStressAt > 4) Tilt = Math.Max(0, Tilt - dt * .012f);
            Confidence += Clamp(.5f - Confidence, -dt * .004f, dt * .004f);
            if (exchangeUntil > 0 && Age >= exchangeUntil)
            {
                float advantage = outgoing - incoming;
                if (Math.Abs(advantage) > .025f) Adjust(advantage < 0 ? .04f : -.035f, Clamp(advantage * .25f, -.06f, .06f));
                incoming = outgoing = exchangeUntil = 0;
            }
            if (Age >= dangerUntil && Age >= focusUntil && Focus != requested)
            { Focus = requested; focusUntil = Age + Profile.FocusCommitment; }
        }
        public void RequestFocus(AttentionFocus focus)
        {
            requested = focus;
            if (Age < dangerUntil || Age < focusUntil || Focus == focus) return;
            Focus = focus; focusUntil = Age + Profile.FocusCommitment;
        }
        public void NoticeDanger()
        { Focus = AttentionFocus.Defense; dangerUntil = Math.Max(dangerUntil, Age + .9f); }
        public void RecordDamage(float fraction, bool dealt, bool champion)
        {
            fraction = Clamp(fraction, 0, 1);
            if (fraction <= 0 || !champion) return;
            if (dealt) outgoing += fraction; else incoming += fraction;
            exchangeUntil = Age + 2;
            if (!dealt && fraction >= .08f) NoticeDanger();
        }
        public void RecordFarm(bool secured) { Adjust(secured ? -.008f : .025f, 0); }
        public void RecordMechanicalResult(bool hit) { Adjust(hit ? -.015f : .045f, hit ? .005f : -.015f); }
        public void RecordDeath()
        { incoming = outgoing = exchangeUntil = 0; Adjust(.18f, -.15f); NoticeDanger(); }
        public void RecordKill()
        { incoming = outgoing = exchangeUntil = 0; Adjust(-.12f, .12f); }
    }
}
