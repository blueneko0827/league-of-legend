using UnityEngine;

namespace MidLane
{
    public sealed partial class LaneWorld
    {
        public const string SimulatedPingPreferenceKey = "MidLane.PracticeVisual.SimulatedPingMs";
        public const string GameModePreferenceKey = "MidLane.PracticeVisual.GameMode";
        public const string DSummonerPreferenceKey = "MidLane.PracticeVisual.Summoner.D";
        public const string FSummonerPreferenceKey = "MidLane.PracticeVisual.Summoner.F";
        float audibleVolume = 1;
        public bool Muted { get { return Settings != null && Settings.muteAllAudio; } }
        public int SimulatedPingMs { get { return Settings != null ? Mathf.Clamp(Settings.simulatedPingMs, 0, PlayerLatency.MaxMilliseconds) : 0; } }
        void InitializePracticeOptions()
        {
            int savedMode = PlayerPrefs.GetInt(GameModePreferenceKey, -1);
            if (savedMode >= 0 && savedMode <= 2)
            {
                Settings.practiceMode = savedMode == 2;
                Settings.pvpMode = savedMode == 1;
                Settings.spawnWaves = savedMode == 0;
            }
            audibleVolume = AudioListener.volume;
            if (Muted) AudioListener.volume = 0;
            SetSimulatedPing(PlayerPrefs.GetInt(SimulatedPingPreferenceKey, SimulatedPingMs));
            var d = (SummonerSpell)PlayerPrefs.GetInt(DSummonerPreferenceKey, (int)Settings.dSummoner);
            var f = (SummonerSpell)PlayerPrefs.GetInt(FSummonerPreferenceKey, (int)Settings.fSummoner);
            if (SummonerRules.Valid(d) && SummonerRules.Valid(f) && d != f)
            { Settings.dSummoner = d; Settings.fSummoner = f; }
        }
        internal void SaveSummonerLoadout()
        {
            PlayerPrefs.SetInt(DSummonerPreferenceKey, (int)Settings.dSummoner);
            PlayerPrefs.SetInt(FSummonerPreferenceKey, (int)Settings.fSummoner);
            PlayerPrefs.Save();
        }
        void SaveGameMode()
        {
            PlayerPrefs.SetInt(GameModePreferenceKey, PracticeModeActive ? 2 : PvpModeActive ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void ToggleMute() { SetMuted(!Muted); }
        public void SetMuted(bool muted)
        {
            if (Settings == null || muted == Muted) return;
            if (muted) { audibleVolume = AudioListener.volume; AudioListener.volume = 0; }
            else AudioListener.volume = audibleVolume;
            Settings.muteAllAudio = muted;
        }
        public void SetSimulatedPing(int milliseconds)
        { Settings.simulatedPingMs = Mathf.Clamp(milliseconds, 0, PlayerLatency.MaxMilliseconds); }
        public void SaveSimulatedPing()
        {
            PlayerPrefs.SetInt(SimulatedPingPreferenceKey, SimulatedPingMs);
            PlayerPrefs.Save();
        }
        void RestorePracticeAudio() { if (Muted) AudioListener.volume = audibleVolume; }
    }
}
