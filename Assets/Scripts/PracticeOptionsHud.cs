using UnityEngine;

namespace MidLane
{
    public sealed partial class PracticeHud
    {
        public static readonly Rect MuteButton = new Rect(766, 8, 100, 36);
        public static readonly Rect LatencyControl = new Rect(872, 8, 196, 36);
        public static readonly Rect LatencyField = new Rect(932, 8, 136, 36);
        const string LatencyControlName = "PracticeLatencyMilliseconds";
        string latencyText;
        bool blurLatencyField;
        public bool LatencyFieldFocused { get; private set; }
        public bool ApplyLatencyText(string value)
        {
            int milliseconds;
            if (!int.TryParse(value, out milliseconds)) return false;
            world.SetSimulatedPing(milliseconds); world.SaveSimulatedPing(); latencyText = world.SimulatedPingMs.ToString(); return true;
        }
        public void EndLatencyEdit()
        {
            if (string.IsNullOrEmpty(latencyText)) latencyText = "0";
            if (!ApplyLatencyText(latencyText)) latencyText = world.SimulatedPingMs.ToString();
            LatencyFieldFocused = false; blurLatencyField = true;
        }
        void FlushLatencyFocus()
        { if (blurLatencyField) { GUI.FocusControl(null); blurLatencyField = false; } }
        void DrawPracticeOptions()
        {
            if (GUI.Button(MuteButton, "Mute: " + (world.Muted ? "ON" : "OFF"), button)) world.ToggleMute();
            Vector2 point = new Vector2(Input.mousePosition.x * 1280 / Screen.width, (Screen.height - Input.mousePosition.y) * 720 / Screen.height);
            if (LatencyFieldFocused && Event.current.type == EventType.MouseDown && !LatencyField.Contains(point)) EndLatencyEdit();
            FlushLatencyFocus();
            if (!LatencyFieldFocused) latencyText = world.SimulatedPingMs.ToString();
            if (GUI.Button(new Rect(LatencyControl.x, LatencyControl.y, 54, LatencyControl.height),
                new GUIContent("ms", "Latencia simulada de 0 a 1000 ms. Retraso total visible de las órdenes. Enter o clic en ms para aplicar."), button)) EndLatencyEdit();
            GUI.SetNextControlName(LatencyControlName);
            string edited = GUI.TextField(LatencyField, latencyText ?? "0", 4, new GUIStyle(GUI.skin.textField)
            { fontSize = 15, alignment = TextAnchor.MiddleCenter });
            string digits = "";
            foreach (char c in edited) if (c >= '0' && c <= '9') digits += c;
            latencyText = digits;
            bool wasFocused = LatencyFieldFocused;
            LatencyFieldFocused = GUI.GetNameOfFocusedControl() == LatencyControlName;
            if (wasFocused && !LatencyFieldFocused) EndLatencyEdit();
            if (LatencyFieldFocused && Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Escape))
            { EndLatencyEdit(); FlushLatencyFocus(); Event.current.Use(); }
        }
    }
}
