using UnityEngine;

namespace MidLane
{
    public sealed partial class PracticeHud
    {
        TrainingDummy modelHoldTarget;
        float modelHoldStarted;
        public float PracticeDummyModelHoldProgress { get; private set; }
        internal void ResetPracticeDummyModelHold()
        { modelHoldTarget = null; PracticeDummyModelHoldProgress = 0; }

        void ProcessPracticeDummyModelHold(Vector2 point, bool pressed, bool held, float now)
        {
            if (world == null || !world.PracticeModeActive || world.Paused || world.RoundOver ||
                world.Player == null || !world.Player.Alive || ShopHud.Open || SummonerPickerOpen ||
                RunePickerOpen || LatencyFieldFocused || !held ||
                PracticeHud.BlocksPointer(new Vector2(point.x, Screen.height - point.y)))
            { ResetPracticeDummyModelHold(); return; }
            // Reuse ordinary body/bar picking, including the smaller minion silhouettes.
            var target = TargetPicker.ResolveForInspection(world, point);
            var dummy = target != null ? target.Dummy : null;
            if (dummy == null || !world.PracticeDummies.Contains(dummy))
            { ResetPracticeDummyModelHold(); return; }
            if (pressed) { modelHoldTarget = dummy; modelHoldStarted = now; }
            if (modelHoldTarget == null) return;
            if (dummy != modelHoldTarget) { ResetPracticeDummyModelHold(); return; }
            PracticeDummyModelHoldProgress = Mathf.Clamp01((now - modelHoldStarted) / PracticeMarkHoldSeconds);
            if (now - modelHoldStarted < PracticeMarkHoldSeconds) return;
            world.TogglePracticeMinionModels();
            ResetPracticeDummyModelHold();
        }

        void DrawPracticeDummyModelHold()
        {
            if (modelHoldTarget == null || !world.PracticeModeActive || world.Paused || ShopHud.Open) return;
            Vector3 point = world.View.WorldToScreenPoint(modelHoldTarget.Actor.VisualTop + Vector3.up * 1.6f);
            if (point.z <= 0) return;
            float x = point.x * 1280 / Screen.width, y = (Screen.height - point.y) * 720 / Screen.height;
            Box(new Rect(x - 108, y - 22, 216, 36), Panel);
            GUI.Label(new Rect(x - 106, y - 22, 212, 26), world.PracticeMinionModelsActive ? "Cambiar a dummies · 2 s" : "Cambiar a minions · 2 s", centeredSmall);
            Bar(new Rect(x - 100, y + 6, 200, 5), PracticeDummyModelHoldProgress, Color.cyan);
        }
    }
}
