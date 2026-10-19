namespace MidLane
{
    public static class SummonersRiftGeometry
    {
        // Mid outer turret centers: blue (5846, 6396), red (8955, 8510).
        // https://hextechdocs.dev/map-data/ ; sqrt(3109^2 + 2114^2) / 100.
        // Rotate that center-to-center segment onto the simulator's X axis.
        public const float MidOuterTowerDistance = 37.5963788f;
        public const float MinimumLaneHalfLength = MidOuterTowerDistance * 0.5f + 7;
    }
}
