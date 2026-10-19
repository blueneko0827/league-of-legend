using System;
namespace MidLane
{
    public enum SummonerSpell { Flash, Ignite, Barrier, Exhaust }
    public static class SummonerRules
    {
        public static readonly SummonerSpell[] Choices = { SummonerSpell.Flash, SummonerSpell.Ignite, SummonerSpell.Barrier, SummonerSpell.Exhaust };
        public const float FlashRange = 4, FlashCooldown = 300, IgniteRange = 6, IgniteCooldown = 180;
        public const float BarrierCooldown = 180, BarrierDuration = 2.5f;
        public const float ExhaustCooldown = 240, ExhaustRange = 6.5f, ExhaustDuration = 3;
        public const float ExhaustSlow = .4f, ExhaustDamageReduction = .35f;
        public static bool Valid(SummonerSpell spell) { return spell >= SummonerSpell.Flash && spell <= SummonerSpell.Exhaust; }
        public static float BarrierShield(int level) { return 100 + 360f * (Math.Max(1, Math.Min(18, level)) - 1) / 17; }
        public const float IgniteDuration = 5;
        public const int IgniteTicks = 5;
        // Riot 25.09: early levels keep +20, then +25; total 70–475 at levels 1–18.
        public static float IgniteDamage(int level)
        { level = Math.Max(1, Math.Min(18, level)); return 50 + 20 * level + 5 * Math.Max(0, level - 5); }
    }
}
