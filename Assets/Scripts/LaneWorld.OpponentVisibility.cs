using UnityEngine;

namespace MidLane
{
    public sealed partial class LaneWorld
    {
        public bool OpponentActive { get { return Opponent != null && Opponent.gameObject.activeInHierarchy; } }
        public bool ToggleOpponentActive()
        {
            if (PracticeModeActive || PvpModeActive || Opponent == null) return false;
            SetOpponentActive(!OpponentActive); return true;
        }
        void CancelOpponentEffects()
        {
            for (int i = BasicProjectiles.Count - 1; i >= 0; i--)
            {
                var shot = BasicProjectiles[i];
                if (shot == null || shot.Source != Opponent && shot.Target != Opponent) continue;
                DiscardOpponentEffect(shot); BasicProjectiles.RemoveAt(i);
            }
            for (int i = Shots.Count - 1; i >= 0; i--)
            {
                var shot = Shots[i];
                if (shot == null || shot.Owner != Opponent) continue;
                DiscardOpponentEffect(shot); Shots.RemoveAt(i);
            }
            foreach (var shot in roundRoot.GetComponentsInChildren<MelBolt>()) if (shot.Owner == Opponent) DiscardOpponentEffect(shot);
            foreach (var shot in roundRoot.GetComponentsInChildren<MelVolley>()) if (shot.Owner == Opponent) DiscardOpponentEffect(shot);
            foreach (var shot in roundRoot.GetComponentsInChildren<MelSnare>()) if (shot.Owner == Opponent) DiscardOpponentEffect(shot);
            foreach (var shot in roundRoot.GetComponentsInChildren<MelEclipse>()) if (shot.Owner == Opponent) DiscardOpponentEffect(shot);
        }
        static void DiscardOpponentEffect(MonoBehaviour effect)
        { effect.gameObject.SetActive(false); Destroy(effect.gameObject); }
        void SetOpponentActive(bool active)
        {
            if (Opponent == null || Opponent.gameObject.activeSelf == active) return;
            if (!active)
            {
                Opponent.Stop(); Opponent.CancelRecall();
                if (Opponent.Sett != null) Opponent.Sett.Cancel();
                if (Opponent.Mel != null) Opponent.Mel.ClearTemporaryEffects();
                Opponent.Summoners.CancelOngoingDamage();
                foreach (Combatant unit in Units)
                {
                    if (unit == null) continue;
                    if (unit.AttackTarget == Opponent) unit.Stop();
                    if (unit.Summoners != null) unit.Summoners.CancelDamageAgainst(Opponent);
                }
                CancelOpponentEffects();
                Navigation.Forget(Opponent); Units.Remove(Opponent);
                Opponent.gameObject.SetActive(false);
            }
            else
            {
                // Keep the same actor, progress, inventory and gold; this is not a respawn.
                if (!Units.Contains(Opponent)) Units.Add(Opponent);
                Opponent.gameObject.SetActive(true);
            }
        }
    }
}
