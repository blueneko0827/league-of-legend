namespace MidLane
{
    // User-supplied ability blueprint for the Tower to Tower practice profile.
    // One simulation unit remains 100 gameplay units, equivalent to 0.6 meters.
    public static class PracticeAbilityGeometry
    {
        const float Scale = LeagueChampionStats.WorldUnitsPerLeagueUnit;
        public const float IreliaQRange = 600 * Scale;
        public const float IreliaWRange = 825 * Scale, IreliaWHalfWidth = 240 * Scale / 2;
        public const float IreliaEPlacementRange = 775 * Scale, IreliaEHalfWidth = 120 * Scale / 2;
        public const float IreliaRRange = 1000 * Scale, IreliaRWallLifetime = 2.5f;
        public const float IreliaRHalfWidth = 320 * Scale / 2, IreliaRWallHalfWidth = 50 * Scale / 2;
        public const float IreliaRExplosionRadius = 320 * Scale, IreliaROriginRadius = 90 * Scale;
        public const float SettLeftRange = 125 * Scale, SettRightRange = 175 * Scale;
        public const float SettQRange = 175 * Scale;
        public const float SettWLength = 790 * Scale;
        public const float SettWFarHalfWidth = 805 * Scale / 2, SettWTrueHalfWidth = 180 * Scale / 2;
        public const float SettELength = 490 * Scale, SettEHalfWidth = 220 * Scale / 2;
        public const float SettRGrabRange = 400 * Scale, SettRDisplacement = 600 * Scale, SettRImpactRadius = 300 * Scale;
    }
}
