using System;

namespace MidLane
{
    public sealed class ChampionProgress
    {
        public int Level { get; private set; }
        public int SkillPoints { get; private set; }
        public float Experience { get; private set; }
        public float TotalExperience
        {
            get { int n = Level - 1; return 180 * n + 50 * n * (n + 1) + Experience; }
        }
        public float NextLevelExperience { get { return Level >= 18 ? 0 : 180 + Level * 100; } }
        readonly int[] ranks = new int[4];
        public bool PvpSkills { get; private set; }
        bool automaticIreliaSkills;
        bool bonusSkillPoint;
        Archetype bonusChampion;
        public bool GrantBonusSkillPoint(Archetype champion)
        {
            if(bonusSkillPoint||Level>=18) return false;
            bonusSkillPoint=true; bonusChampion=champion; ApplyBonusSkill(); return true;
        }
        void ApplyBonusSkill()
        {
            if(!bonusSkillPoint||Level>=18) return;
            // Advance the supplied order while respecting per-level rank limits (especially R).
            for(int step=Level+1;step<=18;step++)
            {
                int slot=ChampionSkillPlan.PvpSkillAtLevel(bonusChampion,step);
                if(slot==3 ? ranks[3]<3&&Level>=6+ranks[3]*5 : ranks[slot]<5&&ranks[slot]<(Level+1)/2)
                { ranks[slot]++; return; }
            }
        }
        public void UseAutomaticIreliaSkills()
        {
            automaticIreliaSkills = true; UpdateAutomaticSkills();
        }
        void UpdateAutomaticSkills()
        {
            if (!automaticIreliaSkills) return;
            Array.Clear(ranks, 0, ranks.Length);
            for (int level = 1; level <= Level; level++)
                ranks[ChampionSkillPlan.PvpSkillAtLevel(Archetype.Blade, level)]++;
            SkillPoints = 0;
            ApplyBonusSkill();
        }

        public static ChampionProgress ForPvp(int level, Archetype champion)
        {
            var progress = new ChampionProgress(Math.Max(1, Math.Min(18, level)));
            if (champion == Archetype.Blade || champion == Archetype.Brawler || champion == Archetype.Prism)
                for (int learnedLevel = 1; learnedLevel <= progress.Level; learnedLevel++)
                    progress.ranks[ChampionSkillPlan.PvpSkillAtLevel(champion, learnedLevel)]++;
            else ChampionSkillPlan.SpendAvailable(progress, champion);
            progress.PvpSkills = true; progress.SkillPoints = 0;
            return progress;
        }

        public ChampionProgress(int startingLevel = 1)
        {
            Level = Math.Max(1, Math.Min(18, startingLevel)); SkillPoints = Level;
        }
        public int Rank(int slot) { return slot >= 0 && slot < 4 ? ranks[slot] : 0; }
        public int MatchExperience(ChampionProgress leader)
        {
            if (PvpSkills || leader == null || leader.Level < Level) return 0;
            int gained = leader.Level - Level;
            Level = leader.Level; Experience = leader.Experience; SkillPoints += gained;
            UpdateAutomaticSkills();
            return gained;
        }
        public int AddExperience(float amount)
        {
            if (PvpSkills || amount <= 0 || Level == 18) return 0;
            int previous = Level; Experience += amount;
            while (Level < 18 && Experience >= NextLevelExperience)
            { Experience -= NextLevelExperience; Level++; SkillPoints++; }
            if (Level == 18) Experience = 0;
            UpdateAutomaticSkills();
            return Level - previous;
        }
        public bool CanLearn(int slot)
        {
            if (PvpSkills || slot < 0 || slot > 3 || SkillPoints <= 0) return false;
            if (slot == 3) return ranks[3] < 3 && Level >= 6 + ranks[3] * 5;
            return ranks[slot] < 5 && ranks[slot] < (Level + 1) / 2;
        }
        public bool Learn(int slot)
        {
            if (!CanLearn(slot)) return false;
            ranks[slot]++; SkillPoints--; return true;
        }
        public static float StatGrowth(int level)
        {
            float n = Math.Max(1, Math.Min(18, level)) - 1;
            return n * (0.7025f + 0.0175f * n);
        }
    }
}
