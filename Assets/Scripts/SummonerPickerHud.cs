using UnityEngine;

namespace MidLane
{
    public sealed partial class PracticeHud
    {
        int summonerPickerSlot = -1;
        public bool SummonerPickerOpen { get { return summonerPickerSlot >= 0 && !ShopHud.Open && !world.Paused; } }
        public int SummonerPickerSlot { get { return summonerPickerSlot; } }
        public Rect SummonerPickerRect
        {
            get
            {
                Rect anchor = ChampionHudLayout.Summoner(Mathf.Clamp(summonerPickerSlot, 0, 1));
                return new Rect(Mathf.Clamp(anchor.center.x - 118, 12, 1032), ChampionHudLayout.Body.yMin - 218, 236, 208);
            }
        }
        public Rect SummonerChoiceRect(int index)
        { Rect r = SummonerPickerRect; return new Rect(r.x + 8, r.y + 36 + index * 40, r.width - 16, 36); }
        public void ToggleSummonerPicker(int slot)
        {
            if (slot < 0 || slot > 1 || ShopHud.Open || world.Paused) return;
            CloseRunePicker();
            summonerPickerSlot = summonerPickerSlot == slot ? -1 : slot;
        }
        public void CloseSummonerPicker() { summonerPickerSlot = -1; }
        public bool SelectSummoner(SummonerSpell spell)
        {
            if (!SummonerPickerOpen || !world.Player.Summoners.Select(summonerPickerSlot, spell)) return false;
            CloseSummonerPicker(); return true;
        }
        public bool SummonerPickerContainsScreenPoint(Vector2 point)
        {
            point = new Vector2(point.x * 1280 / Screen.width, point.y * 720 / Screen.height);
            return SummonerPickerRect.Contains(point) || ChampionHudLayout.Summoner(0).Contains(point) || ChampionHudLayout.Summoner(1).Contains(point);
        }
        static string SummonerDescription(SummonerSpell spell)
        {
            switch (spell)
            {
                case SummonerSpell.Flash: return "Desplazamiento instantáneo hacia el cursor. Alcance: 400 unidades.";
                case SummonerSpell.Ignite: return "Quema al campeón seleccionado durante 5 segundos y reduce su curación.";
                case SummonerSpell.Barrier: return "Escudo de 100–460 según el nivel durante 2,5 s. Enfriamiento: 180 s.";
                case SummonerSpell.Exhaust: return "Ralentiza al campeón seleccionado un 40% y reduce su daño un 35% durante 3 s. Alcance: 650 unidades. Enfriamiento: 240 s.";
                default: return "";
            }
        }
        void DrawSummonerPicker()
        {
            if (ShopHud.Open || world.Paused) { CloseSummonerPicker(); return; }
            if (!SummonerPickerOpen) return;
            Rect panel = SummonerPickerRect; Box(panel, HudInk); HudFrame(panel, HudGold, 1);
            GUI.Label(new Rect(panel.x + 12, panel.y + 6, panel.width - 45, 24), "Hechizo de " + (summonerPickerSlot == 0 ? "D" : "F"), label);
            if (GUI.Button(new Rect(panel.xMax - 30, panel.y + 6, 22, 22), "×", button)) { CloseSummonerPicker(); return; }
            bool enabled = GUI.enabled;
            var choiceStyle = new GUIStyle(button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(42, 6, 0, 0) };
            for (int i = 0; i < SummonerRules.Choices.Length; i++)
            {
                SummonerSpell spell = SummonerRules.Choices[i]; Rect r = SummonerChoiceRect(i);
                bool selectable = world.Player.Summoners.CanSelect(spell);
                Box(r, selectable ? new Color(.04f, .10f, .13f) : new Color(.025f, .045f, .055f));
                GUI.enabled = enabled && selectable;
                string suffix = world.Player.Summoners.Equipped(0) == spell ? " · D" : world.Player.Summoners.Equipped(1) == spell ? " · F" : "";
                if (GUI.Button(r, new GUIContent(spell + suffix, SummonerDescription(spell)), choiceStyle))
                { GUI.enabled = enabled; SelectSummoner(spell); return; }
                Color tint = GUI.color; if (!selectable) GUI.color = new Color(.45f, .45f, .45f);
                HudIcons.Draw(new Rect(r.x + 3, r.y + 3, 30, 30), spell.ToString()); GUI.color = tint;
            }
            GUI.enabled = enabled;
        }
    }
}
