namespace MidLane
{
    // Behaviour tuning only. Damage, geometry and cooldowns stay in MelRules.
    public static class MelDecisionRules
    {
        public const float MacroInterval = .4f;
        public const float LaneLeash = 3.5f, TradeDuration = 1.75f, ReturnRest = 1.2f;
        public static float FarmLookahead(Difficulty difficulty)
        { return difficulty == Difficulty.Expert ? 2 : difficulty == Difficulty.Hard ? 1.5f : 1.1f; }
        public static float FarmReaction(Difficulty difficulty)
        { return difficulty == Difficulty.Expert ? .08f : difficulty == Difficulty.Hard ? .12f : .18f; }
        public static float LastHitSafety(Difficulty difficulty)
        { return difficulty == Difficulty.Expert ? 1 : difficulty == Difficulty.Hard ? .94f : .87f; }
        public static float WReaction(Difficulty difficulty)
        { return difficulty == Difficulty.Expert ? .25f : difficulty == Difficulty.Hard ? .3f : .35f; }
        public static float MissInertia(Difficulty difficulty)
        { return difficulty == Difficulty.Expert ? .1f : difficulty == Difficulty.Hard ? .4f : 1; }
        public static float OffensiveFlashChance(Difficulty difficulty)
        { return difficulty == Difficulty.Expert ? .7f : difficulty == Difficulty.Hard ? .2f : 0; }
        public static bool ShouldRecover(float health, float enemyHealth, bool enemyQReady, bool pvp)
        { return !pvp && health < .25f && enemyHealth > .6f && enemyQReady; }
        public static bool CanUseUltimate(int marks, float enemyHealth, bool controlled)
        { return marks >= 2 || marks > 0 && enemyHealth < .4f && controlled; }
    }
}
