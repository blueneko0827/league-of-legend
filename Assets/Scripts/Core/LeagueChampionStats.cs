using System;

namespace MidLane
{
    public struct ScalingStat
    {
        public readonly float Base, Growth;
        public ScalingStat(float value, float growth) { Base = value; Growth = growth; }
        public float At(int level) { return Base + Growth * ChampionProgress.StatGrowth(level); }
    }

    public struct ChampionStatProfile
    {
        public string Name;
        public bool UsesMana;
        public ScalingStat Health, Mana, Armor, MagicResist, AttackDamage, HealthRegeneration, ManaRegeneration;
        public float MovementSpeedUnits, AttackRangeUnits, BaseAttackSpeed, AttackSpeedRatio, AttackSpeedGrowth;
        public float AttackWindupFraction;
        public float AttackSpeedAt(int level, float bonus = 0)
        { return BaseAttackSpeed + AttackSpeedRatio * (AttackSpeedGrowth * ChampionProgress.StatGrowth(level) + bonus); }
    }

    // Frozen PC reference: Riot Data Dragon 16.20.1 + client 16.20.
    // AD growth comes from client records: Data Dragon publishes 0 for these champions.
    // Regeneration is stored per second, not per five seconds. See CHAMPION_STATS.md.
    public static class LeagueChampionStats
    {
        public const string Patch = "16.20.1";
        public const float WorldUnitsPerLeagueUnit = 0.01f;
        public static ChampionStatProfile For(Archetype archetype)
        {
            switch (archetype)
            {
                case Archetype.Blade:
                    return new ChampionStatProfile {
                        Name = "Irelia", UsesMana = true,
                        Health = new ScalingStat(630, 115), Mana = new ScalingStat(350, 50),
                        Armor = new ScalingStat(36, 4.7f), MagicResist = new ScalingStat(30, 2.05f),
                        AttackDamage = new ScalingStat(65, 3.5f),
                        HealthRegeneration = new ScalingStat(0.7f, 0.17f), ManaRegeneration = new ScalingStat(1.6f, 0.16f),
                        MovementSpeedUnits = 335, AttackRangeUnits = 200,
                        BaseAttackSpeed = 0.656f, AttackSpeedRatio = 0.656f, AttackSpeedGrowth = 0.025f,
                        AttackWindupFraction = 0.3f / 1.525f
                    };
                case Archetype.Brawler:
                    return new ChampionStatProfile {
                        Name = "Sett", UsesMana = false,
                        Health = new ScalingStat(670, 114), Mana = new ScalingStat(0, 0),
                        Armor = new ScalingStat(33, 4.7f), MagicResist = new ScalingStat(28, 2.05f),
                        AttackDamage = new ScalingStat(60, 4),
                        HealthRegeneration = new ScalingStat(1.4f, 0.1f), ManaRegeneration = new ScalingStat(0, 0),
                        MovementSpeedUnits = 340, AttackRangeUnits = 125,
                        BaseAttackSpeed = 0.625f, AttackSpeedRatio = 0.625f, AttackSpeedGrowth = 0.0175f,
                        AttackWindupFraction = 0.375f / 1.75f
                    };
                case Archetype.Siphon:
                    return new ChampionStatProfile {
                        Name = "Vladimir", UsesMana = false,
                        Health = new ScalingStat(600, 110), Mana = new ScalingStat(0, 0),
                        Armor = new ScalingStat(24, 4.5f), MagicResist = new ScalingStat(30, 1.3f),
                        AttackDamage = new ScalingStat(55, 3),
                        HealthRegeneration = new ScalingStat(1.4f, 0.12f), ManaRegeneration = new ScalingStat(0, 0),
                        MovementSpeedUnits = 330, AttackRangeUnits = 450,
                        BaseAttackSpeed = 0.658f, AttackSpeedRatio = 0.658f, AttackSpeedGrowth = 0.02f,
                        AttackWindupFraction = 0.3f / 1.52f
                    };
                case Archetype.Prism:
                    return new ChampionStatProfile {
                        Name = "Mel", UsesMana = true,
                        Health = new ScalingStat(630, 99), Mana = new ScalingStat(480, 28),
                        Armor = new ScalingStat(21, 5.2f), MagicResist = new ScalingStat(30, 1.3f),
                        AttackDamage = new ScalingStat(54, 3.3f),
                        HealthRegeneration = new ScalingStat(1.2f, 0.11f), ManaRegeneration = new ScalingStat(1.8f, 0.18f),
                        MovementSpeedUnits = 330, AttackRangeUnits = 550,
                        BaseAttackSpeed = 0.625f, AttackSpeedRatio = 0.625f, AttackSpeedGrowth = 0.025f,
                        AttackWindupFraction = 0.156f
                    };
                default: throw new ArgumentOutOfRangeException("archetype");
            }
        }
    }
}
