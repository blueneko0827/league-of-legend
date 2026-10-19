using System;

namespace MidLane
{
    public static class FinancialUtility
    {
        public static float GoldPerMinute(float expectedGold, float costGold, float seconds)
        { return (expectedGold - costGold) * 60 / Math.Max(0.1f, seconds); }

        public static float Roi(float killProbability, float deathProbability, float killGold, float deathLossGold, float missedFarmGold)
        {
            float reward = Math.Max(0, Math.Min(1, killProbability)) * Math.Max(0, killGold);
            float cost = Math.Max(0, Math.Min(1, deathProbability)) * Math.Max(0, deathLossGold) + Math.Max(0, missedFarmGold);
            return reward / Math.Max(1, cost);
        }

        public static float Reaction(float sampledSeconds, float healthFraction, float panicThreshold, float panicMultiplier)
        { return sampledSeconds * (healthFraction < panicThreshold ? panicMultiplier : 1); }
    }
}
