namespace MidLane
{
    // User-authored practice layout. These values override the Summoner's Rift reference.
    // Simulation coordinates stay at 100 LoL units per Unity unit; meters are a separate
    // conventional conversion, not a second scale applied to movement or ability ranges.
    public static class TowerToTowerRules
    {
        public const string MapName = "Proving Grounds: Tower to Tower";
        public const float UnitsToMeters = 0.006f;
        public const float GameplayUnitsPerMeter = 1 / UnitsToMeters;
        public const float WorldUnitsPerGameplayUnit = LeagueChampionStats.WorldUnitsPerLeagueUnit;
        public const float MetersPerWorldUnit = UnitsToMeters / WorldUnitsPerGameplayUnit;
        public const float VisualMeshMultiplier = 1.65f;
        public const float LengthUnits = 5150, WidthUnits = 1600;
        public const float HalfLength = LengthUnits * WorldUnitsPerGameplayUnit / 2;
        public const float HalfWidth = WidthUnits * WorldUnitsPerGameplayUnit / 2;
        public const float TowerRadius = 88.4f * WorldUnitsPerGameplayUnit;
        public const float TowerAttackRange = 800 * WorldUnitsPerGameplayUnit;
        public const float TowerAggroRadius = TowerAttackRange + TowerRadius;
        public const float NeutralLength = HalfLength * 2 - TowerAggroRadius * 2;
        public const float MidpointToSafety = HalfLength - TowerAggroRadius;
        public const float ChampionRadius = 65 * WorldUnitsPerGameplayUnit;
        public const float SmallChampionRadius = 55 * WorldUnitsPerGameplayUnit;
        public const float SuperMinionRadius = 75 * WorldUnitsPerGameplayUnit;
        public const float CannonRadius = 65 * WorldUnitsPerGameplayUnit;
        public const float MinionRadius = 48 * WorldUnitsPerGameplayUnit;
        public const float TowerSelectionHeight = 350 * WorldUnitsPerGameplayUnit;
        public const float ChampionSelectionHeight = 167.5f * WorldUnitsPerGameplayUnit;
        public const float SmallChampionSelectionHeight = 85 * WorldUnitsPerGameplayUnit;
        public const float SuperMinionSelectionHeight = 240 * WorldUnitsPerGameplayUnit;
        public const float CannonSelectionHeight = 140 * WorldUnitsPerGameplayUnit;
        public const float MinionSelectionHeight = 100 * WorldUnitsPerGameplayUnit;
        public const float RecoveryRadius = 1.5f;
        public static float Radius(UnitKind kind, MinionType minion = MinionType.Melee)
        { return kind == UnitKind.Tower ? TowerRadius : kind == UnitKind.Champion ? ChampionRadius : minion == MinionType.Cannon ? CannonRadius : MinionRadius; }
        public static float SelectionHeight(UnitKind kind, MinionType minion = MinionType.Melee)
        { return kind == UnitKind.Tower ? TowerSelectionHeight : kind == UnitKind.Champion ? ChampionSelectionHeight : minion == MinionType.Cannon ? CannonSelectionHeight : MinionSelectionHeight; }
    }
}
