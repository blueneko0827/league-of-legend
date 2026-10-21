using UnityEngine;

namespace MidLane
{
    public sealed partial class PracticeHud
    {
        public Combatant InspectedTarget { get { return world.GetComponent<PlayerCommands>().InspectedTarget; } }
        public bool InspectionVisible { get { return !panelExpanded && InspectedTarget != null; } }
        public bool OpponentItemsVisible { get { return !panelExpanded && InspectedTarget == null && world.Opponent != null; } }

        public string InspectedHealthText
        { get { var unit = InspectedTarget; return unit == null ? "" : VitalsText("Vida", unit.Health, unit.MaxHealth, true); } }
        public string InspectedManaText
        { get { var unit = InspectedTarget; return unit == null ? "" : VitalsText("Maná", unit.Resource, unit.MaxResource, false); } }

        static string VitalsText(string label, float current, float maximum, bool health)
        {
            int amount = health ? Mathf.CeilToInt(current) : Mathf.FloorToInt(current);
            int total = health ? Mathf.CeilToInt(maximum) : Mathf.FloorToInt(maximum);
            return label + ": " + amount + " / " + total;
        }

        void DrawInspectedVitals()
        {
            Combatant unit = InspectedTarget;
            GUI.Label(new Rect(20, 49, 224, 19), unit.DisplayName + " · nivel " + unit.Level, small);
            Color health = unit == world.Player ? SelfHealth : unit.Team == world.PlayerTeam ? AllyHealth : EnemyHealth;
            var style = centeredSmall;
            DrawInspectedBar(new Rect(20, 71, 224, 17), unit.Health, unit.MaxHealth, health, InspectedHealthText, style);
            DrawInspectedBar(new Rect(20, 94, 224, 17), unit.Resource, unit.MaxResource, ManaColor, InspectedManaText, style);
        }

        static void DrawInspectedBar(Rect rect, float current, float maximum, Color color, string text, GUIStyle style)
        {
            Box(rect, BarInk);
            var fill = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            Bar(fill, maximum > 0 ? Mathf.Clamp01(current / maximum) : 0, color);
            DamageLabel(rect, text, Color.white, style);
        }
    }
}
