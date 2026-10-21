namespace MidLane
{
    public sealed partial class LaneWorld
    {
        bool progressionEnabled = true;
        public bool ProgressionEnabled { get { return PracticeModeActive || progressionEnabled; } }
        public void ToggleProgression() { if (!Paused && !PracticeModeActive) progressionEnabled = !progressionEnabled; }
        public void AddTrainingGold()
        {
            if (!PracticeModeActive || Paused) return;
            if (Player != null) Player.GrantGold(900);
            if (Opponent != null) Opponent.GrantGold(900);
        }
        void ApplyPvpStartingItems()
        {
            if (!PvpModeActive && !NormalplayModeActive) return;
            foreach (var actor in new[] { Player, Opponent })
            {
                if (actor == null) continue;
                int starter = actor.Archetype == Archetype.Prism ? 1056 :
                    actor.Archetype == Archetype.Blade || actor.Archetype == Archetype.Brawler ? 1055 : 0;
                if (starter != 0) actor.EquipPvpStarter(starter);
                if (NormalplayModeActive) actor.EquipPvpStarter(2003);
            }
        }
        void ApplyPvpProgression()
        {
            foreach (var actor in new[] { Player, Opponent })
            {
                if (actor == null) continue;
                int gold = PvpProgressionRules.GoldBetweenLevels(actor.Level, PvpLevel);
                if (actor.Level != PvpLevel) actor.ApplyPvpLevel(PvpLevel);
                actor.GrantGold(gold);
            }
            SynchronizeRunes();
        }
        public void AddTrainingLevel()
        {
            if (Paused || !ProgressionEnabled) return;
            if (PvpModeActive)
            {
                if (PvpLevel >= PvpProgressionRules.MaxLevel)
                {
                    if (Player != null) Player.GrantGold(PvpProgressionRules.MaxLevelButtonGold);
                    if (Opponent != null) Opponent.GrantGold(PvpProgressionRules.MaxLevelButtonGold);
                    return;
                }
                pvpBonusLevels++; ApplyPvpProgression(); return;
            }
            if (NormalplayModeActive) { if (Player != null) Player.GrantTrainingLevel(); return; }
            if (Player != null) Player.GrantTrainingLevel();
            if (Opponent != null) Opponent.GrantTrainingLevel();
        }
    }
}
