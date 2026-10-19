using UnityEngine;
namespace MidLane
{
    public sealed partial class LaneWorld
    {
        public IreliaRunePage SelectedRunePage { get { return (IreliaRunePage)Mathf.Clamp(PlayerPrefs.GetInt(RuneRules.PreferenceKey,0),0,2); } }
        public bool SelectRunePage(IreliaRunePage page)
        {
            if(Paused || (int)page<0 || (int)page>2 || page==SelectedRunePage) return false;
            PlayerPrefs.SetInt(RuneRules.PreferenceKey,(int)page); PlayerPrefs.Save(); Restart(); return true;
        }
        public bool RuneHalfLevel
        {
            get
            {
                if(Player==null || Player.Level>=18 || PracticeModeActive) return false;
                return PvpModeActive ? PvpProgressDeaths%PvpProgressionRules.DeathsPerLevel!=0 :
                    Player.Progress.Experience>=Player.Progress.NextLevelExperience*.5f;
            }
        }
        public void SynchronizeRunes()
        {
            if(Paused || !ProgressionEnabled) return;
            foreach(var unit in Units)
                if(unit!=null && unit.Runes!=null && unit.Dummy==null) unit.Runes.SynchronizeProgress();
        }
    }
}
