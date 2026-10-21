using UnityEngine;

namespace MidLane
{
    public sealed partial class PracticeHud
    {
        public const float PracticeMarkHoldSeconds = 2;
        bool practiceMarkHolding;
        float practiceMarkHoldStarted;
        public float PracticeMarkHoldProgress { get; private set; }

        internal void ResetPracticeMarkHold()
        {
            practiceMarkHolding = false;
            PracticeMarkHoldProgress = 0;
        }

        void OnApplicationFocus(bool focused) { if (!focused) { ResetPracticeMarkHold(); ResetPracticeDummyModelHold(); } }
        void OnDisable() { ResetPracticeMarkHold(); ResetPracticeDummyModelHold(); }

        void UpdatePracticeMarkInput()
        {
            Vector2 point = new Vector2(Input.mousePosition.x * 1280 / Screen.width,
                (Screen.height - Input.mousePosition.y) * 720 / Screen.height);
            ProcessPracticeMarkHold(point, Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Time.unscaledTime);
            ProcessPracticeDummyModelHold(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Time.unscaledTime);
        }

        // Require a fresh press on E and a continuous hold inside its icon.
        void ProcessPracticeMarkHold(Vector2 point, bool pressed, bool held, float now)
        {
            if (world == null || !world.PracticeModeActive || world.Paused || world.RoundOver ||
                world.Player == null || !world.Player.Alive || ShopHud.Open || SummonerPickerOpen ||
                RunePickerOpen || LatencyFieldFocused || VisualTuningPanel.BlocksPointer(point) ||
                !held || !AbilityRect(2).Contains(point))
            { ResetPracticeMarkHold(); return; }

            if (pressed) { practiceMarkHolding = true; practiceMarkHoldStarted = now; }
            if (!practiceMarkHolding) return;
            PracticeMarkHoldProgress = Mathf.Clamp01((now - practiceMarkHoldStarted) / PracticeMarkHoldSeconds);
            if (now - practiceMarkHoldStarted < PracticeMarkHoldSeconds) return;
            world.TogglePermanentPracticeMarks();
            ResetPracticeMarkHold();
        }

        void DrawPracticeMarkHold(Rect icon)
        {
            if (!world.PracticeModeActive) return;
            if (world.PermanentPracticeMarks)
            {
                Box(new Rect(icon.x + 3, icon.y + 3, 24, 23), HudInk);
                HudNumber(new Rect(icon.x + 3, icon.y + 1, 24, 25), "∞", hudNumbers);
            }
            if (practiceMarkHolding)
                Bar(new Rect(icon.x + 3, icon.yMax - 9, icon.width - 6, 6), PracticeMarkHoldProgress, Color.cyan);
        }
    }
}
