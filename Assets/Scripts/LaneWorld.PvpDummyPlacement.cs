using UnityEngine;

namespace MidLane
{
    public sealed partial class LaneWorld
    {
        public const string PvpDummyOffsetPreferenceKey = "MidLane.PracticeVisual.PvpDummy.OffsetX";
        public const string PvpDummyZPreferenceKey = "MidLane.PracticeVisual.PvpDummy.Z";
        public bool CanRepositionPvpDummy { get { return PvpModeActive && !Paused && !RoundOver && PvpQDummy != null; } }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        float PlayerLaneDirection { get { return Player.Team == Team.Blue ? 1 : -1; } }
        float PlayerTowerX { get { return -PlayerLaneDirection * TowerToTowerRules.HalfLength; } }
        void RestorePvpDummyPosition()
        {
            if (!PlayerPrefs.HasKey(PvpDummyOffsetPreferenceKey) || !PlayerPrefs.HasKey(PvpDummyZPreferenceKey)) return;
            float offset = PlayerPrefs.GetFloat(PvpDummyOffsetPreferenceKey), z = PlayerPrefs.GetFloat(PvpDummyZPreferenceKey);
            if (Finite(offset) && Finite(z)) MovePvpDummy(new Vector3(PlayerTowerX + PlayerLaneDirection * offset, 0, z), false);
        }
        public bool RepositionPvpDummy(Vector3 point) { return MovePvpDummy(point, true); }
        bool MovePvpDummy(Vector3 point, bool save)
        {
            if (!CanRepositionPvpDummy || !Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
            var actor = PvpQDummy.Actor;
            point = AvoidTower(actor, point);
            foreach (var unit in Units)
                if (unit != null && unit != actor && unit.Kind != UnitKind.Tower && unit.Targetable &&
                    Vector3.Distance(unit.Position, point) < unit.Radius + actor.Radius + .03f) return false;
            PvpQDummy.RelocatePermanentTarget(point); Navigation.Forget(actor);
            if (save)
            {
                // Mirror the lane axis only: retain the same edge relative to the recovery circle.
                PlayerPrefs.SetFloat(PvpDummyOffsetPreferenceKey, (point.x - PlayerTowerX) * PlayerLaneDirection);
                PlayerPrefs.SetFloat(PvpDummyZPreferenceKey, point.z); PlayerPrefs.Save();
            }
            return true;
        }
    }
}
