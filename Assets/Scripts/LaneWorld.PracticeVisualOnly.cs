namespace MidLane
{
    // Distribution-only adapter. Do not merge into the complete simulator.
    public sealed partial class LaneWorld
    {
        void EnforcePracticeVisualMode()
        {
            if (Settings == null) return;
            Settings.practiceMode = true;
            Settings.pvpMode = false;
            Settings.spawnWaves = false;
        }
    }
}
