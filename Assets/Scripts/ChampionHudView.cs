using UnityEngine;
namespace MidLane
{
    public sealed partial class PracticeHud
    {
        static readonly Color HudGold = new Color(.55f, .46f, .27f), HudBright = new Color(.92f, .80f, .43f);
        static readonly Color HudInk = new Color(.018f, .037f, .045f, .98f);
        GUIStyle hudText, hudNumbers, hudKey, hudUpgrade, hudVitals, hudGoldSymbol;
        void HudStyles()
        {
            if (hudText != null) return;
            hudText = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(), margin = new RectOffset() };
            hudText.normal.textColor = new Color(.88f, .87f, .77f);
            hudNumbers = new GUIStyle(hudText) { fontSize = 23, fontStyle = FontStyle.Bold };
            hudKey = new GUIStyle(hudText) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
            hudUpgrade = new GUIStyle(hudNumbers) { fontSize = 29 };
            hudVitals = new GUIStyle(hudText) { fontSize = 22, fontStyle = FontStyle.Bold };
            hudGoldSymbol = new GUIStyle(hudNumbers); hudGoldSymbol.normal.textColor = HudBright;
        }
        static void HudFrame(Rect r, Color color, float thickness = 3)
        {
            Box(new Rect(r.x, r.y, r.width, thickness), color); Box(new Rect(r.x, r.yMax - thickness, r.width, thickness), color);
            Box(new Rect(r.x, r.y, thickness, r.height), color); Box(new Rect(r.xMax - thickness, r.y, thickness, r.height), color);
        }
        void HudNumber(Rect r, string text, GUIStyle style)
        {
            Color c = style.normal.textColor; style.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 1.8f, r.y + 1.8f, r.width, r.height), text, style);
            style.normal.textColor = c; GUI.Label(r, text, style);
        }
        public bool CanUpgradeSkill(int slot)
        { return world != null && !world.RoundOver && world.Player.Blade.Progress.CanLearn(slot); }
        public bool UpgradeSkill(int slot)
        { return CanUpgradeSkill(slot) && world.Player.Blade.Learn(slot); }
        public static Rect SkillUpgradeRect(int slot) { return ChampionHudLayout.Upgrade(slot); }
        public static Rect InventorySlotRect(int slot) { return ChampionHudLayout.Inventory(slot); }
        void DrawChampionHud()
        {
            HudStyles(); Matrix4x4 previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.TRS(ChampionHudLayout.Origin, Quaternion.identity, new Vector3(ChampionHudLayout.ScaleX, ChampionHudLayout.Scale, 1));
            Combatant p = world.Player;
            Box(new Rect(0, 28, 900, 176), HudGold); Box(new Rect(3, 31, 894, 170), HudInk);
            Box(new Rect(634, 14, 266, 190), HudGold); Box(new Rect(637, 17, 260, 184), HudInk);
            // Stepped upper edge, compact portrait and level badge.
            Box(new Rect(92, 40, 532, 3), HudGold);
            HudIcons.Draw(new Rect(12, 54, 76, 76), "Portrait"); HudFrame(new Rect(12, 54, 76, 76), HudGold);
            float levelProgress = world.PvpModeActive ? (p.Level == 18 ? 1 : world.PvpProgressDeaths % 2 / 2f) :
                p.Level == 18 ? 1 : p.Progress.Experience / p.Progress.NextLevelExperience;
            Bar(new Rect(12, 135, 76, 6), levelProgress, new Color(.63f, .31f, .88f));
            Box(new Rect(34, 113, 34, 28), HudInk); HudFrame(new Rect(34, 113, 34, 28), HudGold, 2);
            HudNumber(new Rect(34, 113, 34, 28), p.Level.ToString(), hudNumbers);
            DrawRuneButton();
            HudIcons.Draw(new Rect(102, 66, 48, 48), "Passive");
            HudFrame(new Rect(102, 66, 48, 48), p.Stacks == 4 ? new Color(.35f, 1, .87f) : HudGold, 2);
            for (int i = 0; i < 4; i++) Box(new Rect(103 + i * 12, 122, 9, 6), i < p.Stacks ? HudBright : new Color(.13f, .17f, .18f));
            for (int i = 0; i < 4; i++) DrawHudAbility(i);
            DrawHudSummoner(0, p.Summoners.Equipped(0).ToString(), "D", p.Summoners.Remaining(p.Summoners.Equipped(0)));
            DrawHudSummoner(1, p.Summoners.Equipped(1).ToString(), "F", p.Summoners.Remaining(p.Summoners.Equipped(1)));
            Bar(new Rect(100, 150, 516, 19), p.Health / p.MaxHealth, new Color(.05f, .78f, .11f));
            HudNumber(new Rect(100, 146, 516, 27), Mathf.CeilToInt(p.Health) + " / " + Mathf.CeilToInt(p.MaxHealth), hudVitals);
            Bar(new Rect(100, 176, 516, 15), p.MaxResource > 0 ? p.Resource / p.MaxResource : 0, new Color(.05f, .40f, .88f));
            HudNumber(new Rect(100, 169, 516, 28), Mathf.FloorToInt(p.Resource) + " / " + Mathf.FloorToInt(p.MaxResource), hudVitals);
            for (int slot = 0; slot < 6; slot++)
            {
                Rect r = new Rect(650 + slot % 3 * 70, 20 + slot / 3 * 70, 60, 60);
                ItemDefinition item = slot < p.Inventory.Items.Count ? p.Inventory.Items[slot] : null;
                Box(r, new Color(.025f, .065f, .075f));
                if (item != null) ItemIcons.Draw(new Rect(r.x + 2, r.y + 2, 56, 56), item.id);
                if (item != null && p.ItemEffects.ActiveCooldown(item.id) > 0)
                {
                    Box(r, new Color(0, 0, 0, .65f));
                    HudNumber(r, Mathf.CeilToInt(p.ItemEffects.ActiveCooldown(item.id)).ToString(), hudText);
                }
                HudFrame(r, HudGold, 2); HudNumber(new Rect(r.x + 30, r.y + 33, 26, 24), (slot + 1).ToString(), hudKey);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                { if (ShopHud.Open) world.GetComponent<ShopHud>().SelectInventorySlot(slot); else world.GetComponent<PlayerCommands>().RequestItem(slot); }
            }
            Box(new Rect(646, 158, 214, 32), new Color(.04f, .065f, .065f));
            HudFrame(new Rect(646, 158, 214, 32), HudGold, 2);
            GUI.Label(new Rect(650, 158, 30, 32), "●", hudGoldSymbol);
            HudNumber(new Rect(681, 158, 170, 32), p.Gold.ToString(), hudNumbers);
            if (GUI.Button(new Rect(646, 158, 214, 32), GUIContent.none, GUIStyle.none)) world.GetComponent<ShopHud>().Toggle();
            GUI.matrix = previous;
            DrawPlayerPassiveStacks();
        }
        void DrawHudAbility(int slot)
        {
            var p = world.Player; var blade = p.Blade; int rank = blade.Rank(slot);
            Rect r = new Rect(170 + slot * 76, 54, 64, 64);
            HudIcons.Draw(r, "QWER"[slot].ToString());
            bool recast = slot == 2 && blade.WaitingSecondBlade, charging = slot == 1 && blade.IsCharging;
            float remaining = recast || charging ? 0 : p.Cooldowns.Remaining(slot, Time.time);
            if (rank == 0) Box(r, new Color(0, 0, 0, .78f));
            else if (remaining > 0)
            {
                Box(r, new Color(0, 0, 0, .4f));
                Box(new Rect(r.x, r.y, r.width, r.height * Mathf.Clamp01(remaining / Mathf.Max(.01f, p.AbilityCooldown(slot)))), new Color(0, 0, 0, .55f));
                HudNumber(new Rect(r.x, r.y + 14, r.width, 30), remaining < 1 ? remaining.ToString("0.0") : Mathf.CeilToInt(remaining).ToString(), hudNumbers);
            }
            else if (p.Resource < p.AbilityCost(slot)) Box(r, new Color(.02f, .08f, .32f, .7f));
            HudFrame(r, recast || charging ? new Color(.3f, 1, .85f) : HudGold, 2);
            HudNumber(new Rect(r.x + 39, r.y + 40, 21, 22), "QWER"[slot].ToString(), hudKey);
            if (recast) HudNumber(new Rect(r.x, r.y + 8, 64, 28), "E2", hudNumbers);
            if (charging) Bar(new Rect(r.x + 3, r.y + 3, 58, 5), blade.ChargeFraction, Color.cyan);
            if (slot == 2) DrawPracticeMarkHold(r);
            int maximum = slot == 3 ? 3 : 5;
            for (int i = 0; i < maximum; i++) Box(new Rect(r.x + i * (64f / maximum), 125, 64f / maximum - 3, 5), i < rank ? HudBright : new Color(.13f, .17f, .18f));
            if (!CanUpgradeSkill(slot)) return;
            Rect upgrade = new Rect(r.x + 14, 0, 36, 30);
            Box(upgrade, new Color(.11f, .2f, .22f)); HudFrame(upgrade, HudBright, 2);
            if (GUI.Button(upgrade, "+", hudUpgrade)) UpgradeSkill(slot);
        }
        void DrawHudSummoner(int slot, string icon, string key, float remaining)
        {
            Rect r = new Rect(490 + slot * 62, 60, 52, 52); HudIcons.Draw(r, icon);
            if (remaining > 0) { Box(r, new Color(0, 0, 0, .72f)); HudNumber(new Rect(r.x, r.y + 5, r.width, 28), Mathf.CeilToInt(remaining).ToString(), hudText); }
            HudFrame(r, HudGold, 2); HudNumber(new Rect(r.x + 26, r.y + 29, 23, 22), key, hudKey);
            if (GUI.Button(r, new GUIContent("", "Cambiar hechizo de " + key), GUIStyle.none)) ToggleSummonerPicker(slot);
        }
        void DrawChampionHudHover(Vector2 pointer)
        {
            for (int i = 0; i < 4; i++) if (CanUpgradeSkill(i) && SkillUpgradeRect(i).Contains(pointer)) DrawAbilityTooltip(i);
            if (ChampionHudLayout.Passive.Contains(pointer))
                DrawHudTextTooltip(ChampionHudLayout.Passive, "Pasiva · Fervor Jonio\n\nLas habilidades acertadas generan acumulaciones y velocidad de ataque. Con 4 acumulaciones, los ataques aplican daño mágico adicional.\n\nAcumulaciones: " + world.Player.Stacks + "/4");
            for (int i = 0; i < 6; i++)
                if (InventorySlotRect(i).Contains(pointer) && i < world.Player.Inventory.Items.Count)
                { var item = world.Player.Inventory.Items[i]; DrawHudTextTooltip(InventorySlotRect(i), item.name + "\n\n" + item.description + "\n\nUsar: " + (i + 1)); }
            if (ChampionHudLayout.Gold.Contains(pointer)) DrawHudTextTooltip(ChampionHudLayout.Gold, "Oro: " + world.Player.Gold + "\n\nP: tienda · B: volver (3 s)\nC: estadísticas · Y: cámara");
            for (int i = 0; i < 2; i++) if (ChampionHudLayout.Summoner(i).Contains(pointer))
                DrawHudTextTooltip(ChampionHudLayout.Summoner(i), (i == 0 ? "D" : "F") + " · " + world.Player.Summoners.Equipped(i) + "\n\n" + SummonerDescription(world.Player.Summoners.Equipped(i)) + "\n\nClic: cambiar hechizo.");
        }
        void DrawHudTextTooltip(Rect anchor, string text)
        {
            var style = new GUIStyle(small) { fontSize = 13, wordWrap = true };
            float height = style.CalcHeight(new GUIContent(text), 292) + 24;
            Rect r = new Rect(Mathf.Clamp(anchor.center.x - 160, 12, 948), Mathf.Max(120, ChampionHudLayout.Origin.y - height - 8), 320, height);
            Box(r, Panel); GUI.Label(new Rect(r.x + 14, r.y + 12, 292, height - 24), text, style);
        }
    }
}
