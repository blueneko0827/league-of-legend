namespace MidLane
{
    public sealed partial class LaneWorld
    {
        public bool NormalplayModeActive { get { return Settings != null && !Settings.pvpMode && !Settings.practiceMode; } }
        internal void SynchronizeNormalplayProgress()
        {
            if (!NormalplayModeActive || !ProgressionEnabled || Paused || Player == null) return;
            Player.GrantNormalplayProgressGold();
            if (Opponent != null)
            {
                Opponent.MatchNormalplayProgress(Player);
                Opponent.GrantNormalplayProgressGold();
            }
            foreach (var unit in Units)
                if (unit != null && unit.Kind == UnitKind.Minion) unit.ScaleNormalplayMinion(Player.Level);
            SynchronizeRunes();
        }
    }
}
