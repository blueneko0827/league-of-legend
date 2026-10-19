using System;
namespace MidLane
{
    // User-authored progression, independent of PvP and elapsed game time.
    public static class NormalplayRules
    {
        public const int StartingLevel = 1, StartingGold = 600;
        static readonly int[] gold = { 600, 750, 1000, 1300, 1600, 2250, 2650, 3550, 4100, 5450, 5600, 7200, 9050, 11100, 12150, 13200, 14600, 20000 };
        static readonly int[] meleeHP = { 477, 485, 492, 501, 510, 518, 527, 536, 600, 670, 750, 810, 850, 890, 935, 970, 1020, 1080 };
        static readonly int[] meleeAD = { 12, 13, 14, 15, 16, 18, 19, 20, 21, 23, 24, 25, 26, 27, 28, 30, 31, 34 };
        static readonly int[] casterHP = { 296, 301, 307, 312, 318, 323, 329, 335, 340, 345, 355, 373, 385, 398, 410, 425, 440, 450 };
        static readonly int[] casterAD = { 24, 25, 26, 27, 28, 30, 31, 32, 33, 35, 36, 37, 38, 39, 40, 42, 43, 46 };
        // No cannon HP specified at level 1 in the revised table; retain the previous 913 HP.
        static readonly int[] cannonHP = { 913, 912, 962, 1012, 1062, 1112, 1162, 1212, 1350, 1480, 1650, 1790, 1880, 1960, 2050, 2150, 2300, 2500 };
        static readonly int[] cannonAD = { 43, 46, 49, 52, 55, 61, 64, 67, 70, 76, 79, 82, 85, 88, 91, 97, 100, 106 };
        static int Index(int level) { return Math.Max(1, Math.Min(18, level)) - 1; }
        public static int GoldAtLevel(int level) { return gold[Index(level)]; }
        public static int GoldBetweenLevels(int from, int to) { return Math.Max(0, GoldAtLevel(to) - GoldAtLevel(from)); }
        public static int GoldAtProgress(int level, float experience)
        {
            int earned = GoldAtLevel(level);
            if (level < 18 && experience >= (180 + level * 100) * .5f)
                earned += GoldBetweenLevels(level, level + 1) / 2;
            return earned;
        }
        public static MinionStats Minion(MinionType type, int level)
        {
            int i = Index(level); MinionStats basic = MinionStats.For(type);
            float hp = type == MinionType.Melee ? meleeHP[i] : type == MinionType.Caster ? casterHP[i] : cannonHP[i];
            float ad = type == MinionType.Melee ? meleeAD[i] : type == MinionType.Caster ? casterAD[i] : cannonAD[i];
            return new MinionStats(hp, ad, basic.Range, basic.AttackSpeed, basic.Windup, basic.Experience, 0, basic.Radius);
        }
    }
}
