using System;
using System.Collections.Generic;

namespace MidLane
{
    public enum MinionType { Melee, Caster, Cannon }

    public static class SwiftplayRules
    {
        // Legacy/practice initialization; Normalplay uses NormalplayRules instead.
        public const int StartingLevel = 3, StartingGold = 1400;
        public const float OpeningClock = 28, FirstWaveTime = 30;
        static readonly float[] deathTimes = { 10, 10, 12, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40 };
        public static float RespawnSeconds(int level, float matchTime)
        {
            float increment = matchTime < 1500 ? 0 : Math.Min(0.5f, (float)Math.Floor((matchTime - 1500) / 15) * 0.05f);
            return deathTimes[Math.Max(0, Math.Min(17, level - 1))] * (1 + increment);
        }
        public static float WaveInterval(float matchTime) { return matchTime >= 1800 ? 20 : matchTime >= 720 ? 25 : 30; }
        public static MinionType[] Composition(int wave, float matchTime)
        {
            if (wave < 1) return new MinionType[0];
            bool cannon = wave >= 3;
            var units = new List<MinionType>();
            int melee = cannon && matchTime >= 720 ? 2 : 3;
            int casters = matchTime >= 1800 ? 2 : 3;
            for (int i = 0; i < melee; i++) units.Add(MinionType.Melee);
            if (cannon) units.Add(MinionType.Cannon);
            for (int i = 0; i < casters; i++) units.Add(MinionType.Caster);
            return units.ToArray();
        }
    }

    // Root CharacterRecords in client 16.20; economy overrides from Riot patch 26.1.
    // Base references outside Normalplay; NormalplayRules supplies player-level HP/AD and zero gold.
    public struct MinionStats
    {
        public readonly float Health, Damage, Range, AttackSpeed, Windup, Experience, Radius;
        public readonly int Gold;
        public MinionStats(float hp, float ad, float range, float speed, float windup, float xp, int gold, float radius)
        { Health = hp; Damage = ad; Range = range; AttackSpeed = speed; Windup = windup; Experience = xp; Gold = gold; Radius = radius; }
        public static MinionStats For(MinionType type)
        {
            if (type == MinionType.Caster) return new MinionStats(275, 19.5f, 5.5f, 0.667f, 0.47f, 31, 14, 0.48f);
            if (type == MinionType.Cannon) return new MinionStats(750, 36, 3, 1, 0.3f, 75, 50, 0.65f);
            return new MinionStats(430, 11, 1.1f, 1.25f, 0.393f, 62, 20, 0.48f);
        }
    }

    public static class ChampionSkillPlan
    {
        // Level-by-level orders supplied in the user's Irelia and Sett screenshots.
        public static int PvpSkillAtLevel(Archetype champion, int level)
        {
            if (level < 1 || level > 18) throw new ArgumentOutOfRangeException("level");
            string order = champion == Archetype.Brawler ? "EQWQQRQWQWRWWEEREE" : champion == Archetype.Prism ? "QEWQEREEEQRQQWWRWW" : "QEWQQRQWQWRWWEEREE";
            return "QWER".IndexOf(order[level - 1]);
        }
        public static void LearnBasics(ChampionProgress progress)
        {
            for (int slot = 0; slot < 3; slot++)
                if (progress.Rank(slot) == 0) progress.Learn(slot);
        }
        public static void SpendAvailable(ChampionProgress progress, Archetype champion)
        {
            LearnBasics(progress);
            int[] priority = champion == Archetype.Brawler ? new[] { 3, 0, 1, 2 } : new[] { 3, 0, 2, 1 };
            while (progress.SkillPoints > 0)
            {
                bool spent = false;
                foreach (int slot in priority)
                    if (progress.Learn(slot)) { spent = true; break; }
                if (!spent) break;
            }
        }
    }
}
