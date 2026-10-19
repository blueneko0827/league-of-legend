using System;

namespace MidLane
{
    // PC 16.20 client snapshot + Data Dragon 16.20.1. See MEL_ABILITY_AUDIT.md.
    // Distances are gameplay units converted at 100:1, never visual mesh scale.
    public static class MelRules
    {
        public const float BasicMissileSpeed = 16, PassiveMissileSpeed = 50;
        public const float StackDuration = 5, QRange = 9.5f, QAreaRadius = 2.5f,
            QExplosionRadius = 2, QCastTime = .35f, QDuration = .5f, QSpeed = 38,
            WRadius = 1.75f, WDuration = .75f, WSpeedBonus = .4f,
            ERange = 10, ECoreRadius = .7f, EAuraRadius = 2.3f, ESpeed = 11,
            ECastTime = .25f, ERoot = 1.5f, ESlow = .3f, ETickInterval = .125f,
            EAuraGrowthTime = .5f, EEndLinger = .25f, EBurnDuration = .5f, RCastTime = .75f;
        static int Rank(int rank) { return Math.Max(1, Math.Min(5, rank)); }
        public static int QMissiles(int rank) { return 5 + Rank(rank); }
        public static float QDamage(int rank, float ap, bool first)
        { return first ? 35 + 25 * Rank(rank) + .55f * ap : 3 + 2 * Rank(rank) + .05f * ap; }
        public static float WShield(int rank, float ap) { return 50 + 30 * Rank(rank) + .7f * ap; }
        public static float ReflectionMultiplier(int rank, float ap, bool physical)
        { return (.35f + .05f * Rank(rank) + .0005f * ap) * (physical ? .7f : 1); }
        public static float EDamage(int rank, float ap) { return 15 + 45 * Rank(rank) + .7f * ap; }
        public static float ETickDamage(int rank, float ap) { return (4 + 12 * Rank(rank) + .08f * ap) * ETickInterval; }
        public static float PassiveProjectile(int level, float ap) { return 8 + 22f * (Math.Max(1, Math.Min(18, level)) - 1) / 17 + .04f * ap; }
        public static float ExecuteDamage(int rRank, int stacks, float ap)
        { return stacks <= 0 ? 0 : 50 + 10 * rRank + .1f * ap + (stacks - 1) * (2 + rRank + .0075f * ap); }
        public static float RDamage(int rank, int stacks, float ap)
        { return 50 + 75 * rank + .3f * ap + stacks * (1 + 3 * rank + .04f * ap); }
    }
}
