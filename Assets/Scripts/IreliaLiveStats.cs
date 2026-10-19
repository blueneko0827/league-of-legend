using System;
using UnityEngine;

namespace MidLane
{
    // Historical capture for comparisons only: includes the observed rune page.
    // Gameplay uses ChampionStatReference/LeagueChampionStats, without these fixed modifiers.
    public static class IreliaLiveStats
    {
        [Serializable] public sealed class LevelSample { public int level; public ChampionStatValues championStats; }
        [Serializable] sealed class Profile { public LevelSample[] levels; }
        static Profile profile;

        public static ChampionStatValues AtLevel(int level)
        {
            if (profile == null)
            {
                var asset = Resources.Load<TextAsset>("LeagueData/IreliaLiveStats");
                if (asset == null) throw new InvalidOperationException("Missing Irelia Live Client stat profile.");
                var loaded = JsonUtility.FromJson<Profile>(asset.text);
                if (loaded == null || loaded.levels == null || loaded.levels.Length != 18)
                    throw new InvalidOperationException("Irelia's recorded profile must include all 18 levels.");
                for (int i = 0; i < 18; i++)
                    if (loaded.levels[i] == null || loaded.levels[i].level != i + 1 || loaded.levels[i].championStats == null ||
                        loaded.levels[i].championStats.maxHealth <= 0 || loaded.levels[i].championStats.attackSpeed <= 0)
                        throw new InvalidOperationException("Invalid recorded Irelia stats at level " + (i + 1));
                profile = loaded;
            }
            return profile.levels[Mathf.Clamp(level, 1, 18) - 1].championStats;
        }
    }
}
