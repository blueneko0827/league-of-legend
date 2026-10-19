using System;
namespace MidLane
{
    public static class PvpProgressionRules
    {
        public const int StartingLevel = 1, StartingGold = 600, DeathsPerLevel = 2, MaxLevel = 18, MaxLevelButtonGold = 2000;
        static readonly int[] totals = { 600, 750, 1000, 1300, 1600, 2250, 2650, 3550, 4100, 5450, 5600, 7200, 9050, 11100, 12150, 13200, 14600, 19600 };
        public static int GoldAtLevel(int level) { return totals[Math.Max(1, Math.Min(MaxLevel, level)) - 1]; }
        public static int GoldBetweenLevels(int from, int to) { return Math.Max(0, GoldAtLevel(to) - GoldAtLevel(from)); }
    }
}
