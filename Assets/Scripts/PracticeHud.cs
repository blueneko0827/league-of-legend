using UnityEngine;
using System.Collections.Generic;

namespace MidLane
{
    public sealed partial class PracticeHud : MonoBehaviour
    {
        LaneWorld world;
        bool diagnostics;
        bool showStats;
        bool panelExpanded;
        readonly Dictionary<Combatant, float> shieldBarPeaks = new Dictionary<Combatant, float>();
        static PracticeHud active;
        static readonly Rect CompactHeader = new Rect(12, 8, 310, 36);
        public static readonly Rect MatchClock = new Rect(340, 8, 162, 36);
        public static readonly Rect CompactScore = new Rect(525, 8, 230, 36);
        public static readonly Rect CompactOpponent = new Rect(12, 48, 240, 69);
        public static readonly Rect PvpModeButton = new Rect(1090, 8, 178, 30);
        public static readonly Rect PracticeModeButton = new Rect(1090, 44, 178, 30);
        static readonly Archetype[] OpponentChoices = { Archetype.Prism, Archetype.Brawler, Archetype.Siphon };
        public static Rect OpponentButton(int index) { return new Rect(12, 123 + index * 36, 80, 30); }
        public string ClockText { get { int seconds = Mathf.FloorToInt(world.ElapsedSeconds); return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"); } }
        public void SelectOpponent(Archetype opponent)
        {
            // Opponent selection is unavailable in this practice-only edition.
        }
        public bool PanelExpanded { get { return panelExpanded; } }
        public bool CompactInformationVisible { get { return !panelExpanded; } }
        public static Rect AbilityRect(int slot) { return ChampionHudLayout.Ability(slot); }
        public static int AbilityAtHudPoint(Vector2 point)
        {
            for (int slot = 0; slot < 4; slot++) if (AbilityRect(slot).Contains(point)) return slot;
            return -1;
        }
        Rect DiagnosticsRect { get { return new Rect(20, panelExpanded ? 130 : 240, 350, 228); } }
        Rect StatsRect { get { return new Rect(20, diagnostics ? DiagnosticsRect.yMax + 10 : panelExpanded ? 185 : 240, 350, 214); } }
        GUIStyle title, label, small, button, passiveStackNumber, gritLabel;
        static readonly Color Panel = new Color(0.035f, 0.055f, 0.085f, 0.96f);
        public void Initialize(LaneWorld lane) { world = lane; active = this; }
        public void TogglePanel() { if (LatencyFieldFocused) EndLatencyEdit(); panelExpanded = !panelExpanded; }
        public void CollapsePanel() { if (LatencyFieldFocused) EndLatencyEdit(); panelExpanded = false; }
        public void ResetShieldBars() { shieldBarPeaks.Clear(); ResetPracticeDummyModelHold(); }
        public float ShieldBarFraction(Combatant unit)
        {
            if (unit == null) return 0;
            float amount = unit.Alive ? unit.TotalShield : 0;
            if (amount <= 0) { shieldBarPeaks.Remove(unit); return 0; }
            float peak;
            shieldBarPeaks.TryGetValue(unit, out peak);
            peak = Mathf.Max(peak, amount); shieldBarPeaks[unit] = peak;
            return amount / peak;
        }
        void OnDestroy() { if (active == this) active = null; }
        public static bool BlocksPointer(Vector2 screenPoint)
        {
            Vector2 point = new Vector2(screenPoint.x * 1280 / Screen.width, screenPoint.y * 720 / Screen.height);
            if (VisualTuningPanel.BlocksPointer(point)) return true;
            if (ChampionHudLayout.Body.Contains(point) || ChampionHudLayout.InventoryPanel.Contains(point)) return true;
            if (ChampionHudLayout.AddGold.Contains(point) || ChampionHudLayout.LevelUp.Contains(point)) return true;
            if (active != null) for (int i = 0; i < 4; i++)
                if (active.CanUpgradeSkill(i) && SkillUpgradeRect(i).Contains(point)) return true;
            if (active == null) return false;
            if (active.SummonerPickerOpen && active.SummonerPickerRect.Contains(point)) return true;
            if (active.RunePickerOpen && active.RunePickerRect.Contains(point)) return true;
            if (active.world.Player != null && active.world.Player.Alive && active.world.Player.Stacks > 0 && ChampionHudLayout.PassiveStacks.Contains(point)) return true;
            if ((active.panelExpanded ? new Rect(0, 0, 1280, 115) : CompactHeader).Contains(point)) return true;
            if (MatchClock.Contains(point)) return true;
            if (!active.panelExpanded && (CompactScore.Contains(point) || CompactOpponent.Contains(point))) return true;
            if (!active.panelExpanded && (PvpModeButton.Contains(point) || PracticeModeButton.Contains(point))) return true;
            if (!active.panelExpanded && (MuteButton.Contains(point) || LatencyControl.Contains(point))) return true;
            if (active.diagnostics && active.world.Opponent != null && active.DiagnosticsRect.Contains(point)) return true;
            return active.showStats && !ShopHud.Open && active.StatsRect.Contains(point);
        }
        GUIStyle headerStyle, clockStyle, centeredLabel, centeredSmall, tooltipStyle, noticeStyle, purchaseStyle;
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            passiveStackNumber = new GUIStyle(small) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            gritLabel = new GUIStyle(small) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            headerStyle = new GUIStyle(title) { fontSize = 21, alignment = TextAnchor.MiddleLeft };
            clockStyle = new GUIStyle(label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            centeredLabel = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            centeredSmall = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            tooltipStyle = new GUIStyle(small) { fontSize = 13, wordWrap = true };
            noticeStyle = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            purchaseStyle = new GUIStyle(small) { fontSize = 11, wordWrap = true };
        }
        void Update()
        {
            UpdatePracticeMarkInput();
            if (LatencyFieldFocused) return;
            if (Input.GetKeyDown(KeyCode.Tab)) diagnostics = !diagnostics;
            if (Input.GetKeyDown(KeyCode.C) && !ShopHud.Open) showStats = !showStats;
        }
        void LateUpdate()
        {
            if (world == null) return;
            // Observe expiration and new shields even while the unit is off-screen.
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Kind == UnitKind.Champion) ShieldBarFraction(unit);
        }
        void OnGUI()
        {
            if (world == null || world.Player == null) return;
            Styles(); GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            FlushLatencyFocus();
            Box(panelExpanded ? new Rect(0, 0, 1280, 115) : CompactHeader, Panel);
            if (GUI.Button(CompactHeader, new GUIContent("MidLane / Práctica", "Mostrar u ocultar el panel de práctica"),
                headerStyle)) TogglePanel();
            Box(MatchClock, Panel);
            GUI.Label(MatchClock, ClockText, clockStyle);
            if (panelExpanded)
            {
                GUI.Label(new Rect(20, 48, 370, 26), world.PracticeModeActive ? "Irelia · nivel " + world.Player.Level + " · práctica con dummies" : "Irelia " + world.Player.Level + " · equipo " + (world.Player.Team == Team.Blue ? "azul" : "rojo") + " vs. " + world.Opponent.DisplayName + " " + world.Opponent.Level, small);
                GUI.Label(new Rect(20, 79, 365, 24), world.PracticeModeActive ? "Q " + world.Player.Progress.Rank(0) + " · W " + world.Player.Progress.Rank(1) + " · E " + world.Player.Progress.Rank(2) + " · R " + world.Player.Progress.Rank(3) + " · Maná ilimitado" : world.PvpModeActive ?
                    "Modo PvP · " + world.PvpDeaths + " bajas · " + (world.PvpLevel == 18 ? "Nivel máximo" : world.PvpProgressDeaths % 2 + "/2 para subir") :
                    "Oleada " + world.Wave + " · " + Mathf.FloorToInt(world.MatchTime / 60) + ":" + Mathf.FloorToInt(world.MatchTime % 60).ToString("00") + " · XP " +
                    Mathf.FloorToInt(world.Player.Blade.Progress.Experience) + "/" + world.Player.Blade.Progress.NextLevelExperience, small);
                if (GUI.Button(new Rect(770, 24, 150, 32), world.Paused ? "Reanudar [Esc]" : "Pausar [Esc]", button)) world.TogglePause();
                if (GUI.Button(new Rect(930, 24, 150, 32), "Reiniciar [F5]", button)) world.Restart();
                GUI.Label(new Rect(930, 75, 325, 25), "Edición visual · solo práctica", small);
            }
            else DrawCompactInformation();

            if (!ShopHud.Open) DrawWorldBars();
            DrawNotices();
            DrawHoveredTarget();
            DrawPracticeDummyModelHold();
            DrawChampionHud();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !world.Paused;
            if (world.PracticeModeActive)
            {
                if (GUI.Button(ChampionHudLayout.AddGold, new GUIContent("Add Gold", "+900 de oro para Irelia"), button)) world.AddTrainingGold();
            }
            else
            {
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = world.ProgressionEnabled ? new Color(.45f, .9f, .65f) : new Color(1, .65f, .3f);
                if (GUI.Button(ChampionHudLayout.AddGold, new GUIContent("Stop Level: " + (world.ProgressionEnabled ? "ON" : "OFF"),
                    "ON: permite subir nivel y ganar oro. OFF: congela EXP y ganancias de oro para ambos. Las compras siguen descontando su coste."), button)) world.ToggleProgression();
                GUI.backgroundColor = previousColor;
            }
            GUI.enabled = previousEnabled && !world.Paused && world.ProgressionEnabled &&
                (world.PvpModeActive || world.Player.Level < 18 || (world.Opponent != null && world.Opponent.Level < 18));
            string levelTip = world.PvpModeActive ? (world.PvpLevel >= 18 ? "+2000 de oro para cada campeón" :
                "+1 nivel y +" + PvpProgressionRules.GoldBetweenLevels(world.PvpLevel, world.PvpLevel + 1) + " de oro para cada campeón") :
                world.NormalplayModeActive ? "+1 nivel y +" + world.Player.NormalplayPendingLevelGold + " de oro para cada campeón" : "+1 nivel para ambos campeones";
            if (GUI.Button(ChampionHudLayout.LevelUp, new GUIContent("Level Up", levelTip), button)) world.AddTrainingLevel();
            GUI.enabled = previousEnabled;
            if (world.Player.IsRecalling)
            {
                Box(new Rect(490, 125, 300, 30), Panel);
                GUI.Label(new Rect(500, 128, 280, 24), "Regresando a recuperación · " + world.Player.RecallRemaining.ToString("0.0") + " s", small);
            }
            if (diagnostics && world.Opponent != null)
            {
                float y = DiagnosticsRect.y;
                Box(DiagnosticsRect, Panel);
                ChampionBrain brain = world.Opponent.GetComponent<ChampionBrain>();
                GUI.Label(new Rect(30, y + 8, 320, 24), "IA: " + brain.Decision, small);
                GUI.Label(new Rect(30, y + 34, 320, 24), "Reacción: " + brain.Reaction.ToString("0.00") + " s · sin bonus por dificultad", small);
                GUI.Label(new Rect(30, y + 60, 340, 24), "Vel. mov.: Irelia " + Mathf.RoundToInt(world.Player.MoveSpeed * 100) + " · " +
                    world.Opponent.DisplayName + " " + Mathf.RoundToInt(world.Opponent.MoveSpeed * 100), small);
                ChampionProgress enemySkills = world.Opponent.Progress;
                GUI.Label(new Rect(30, y + 86, 340, 24), "Rangos rival: Q " + enemySkills.Rank(0) + " · W " + enemySkills.Rank(1) + " · E " + enemySkills.Rank(2) + " · R " + enemySkills.Rank(3), small);
                GUI.Label(new Rect(30, y + 112, 340, 24), world.PvpModeActive ? "PvP: " + world.PvpDeaths + " bajas · nivel " + world.PvpLevel + "/18" :
                    "Progreso: " + (world.NormalplayModeActive ? "nivel y oro por tabla · EXP normal" : world.Settings.keepOpponentProgress ? "XP y oro generado parejos" : "natural"), small);
                GUI.Label(new Rect(30, y + 140, 325, 32), brain.PurchaseDecision, purchaseStyle);
                if (brain.Human != null)
                {
                    GUI.Label(new Rect(30, y + 176, 325, 22), "Humano · " + brain.Human.Profile.Name + " · Foco: " + brain.Human.Focus, small);
                    GUI.Label(new Rect(30, y + 200, 325, 22), "Tilt " + (brain.Human.Tilt*100).ToString("0") + "% · Confianza " + (brain.Human.Confidence*100).ToString("0") + "%", small);
                }
            }
            if (showStats && !ShopHud.Open)
            {
                Combatant player = world.Player;
                float y = StatsRect.y; Box(StatsRect, Panel);
                GUI.Label(new Rect(30, y + 8, 325, 25), "Estadísticas · " + player.Kills + "/" + player.Deaths + "/" + player.Assists + " · [C]", label);
                GUI.Label(new Rect(30, y + 39, 325, 24), "Daño " + player.AttackDamage.ToString("0.0") + " · AP " + player.AbilityPower.ToString("0.0"), small);
                GUI.Label(new Rect(30, y + 67, 325, 24), "Armadura " + player.Armor.ToString("0.0") + " · RM " + player.MagicResist.ToString("0.0"), small);
                GUI.Label(new Rect(30, y + 95, 325, 24), "Ataques/s " + (1 / player.Blade.AttackInterval).ToString("0.00") + " · Aceleración " + player.AbilityHaste, small);
                GUI.Label(new Rect(30, y + 123, 325, 24), "Velocidad " + (player.MoveSpeed * 100).ToString("0") + " · Alcance " + (player.AttackRange * 100).ToString("0"), small);
                GUI.Label(new Rect(30, y + 151, 325, 24), "Crítico " + (player.CritChance * 100).ToString("0") + "% · Robo de vida " + (player.LifeSteal * 100).ToString("0") + "%", small);
                GUI.Label(new Rect(30, y + 179, 325, 24), "Regen. vida " + (player.HealthRegeneration * 5).ToString("0.0") + "/5 s · Tenacidad " + (player.Tenacity * 100).ToString("0") + "%", small);
            }
            var commands = world.GetComponent<PlayerCommands>();
            string feedback = commands.DummyPlacementActive ? "Reubicar dummy: clic izquierdo para colocar · Esc / clic derecho para cancelar" : commands.Feedback;
            if (!string.IsNullOrEmpty(feedback))
            { Box(new Rect(270, 560, 760, 30), Panel); GUI.Label(new Rect(280, 562, 750, 25), feedback, small); }
            if (!world.Player.Alive && !world.Paused && !ShopHud.Open)
            {
                Box(new Rect(435, 260, 410, 108), Panel);
                GUI.Label(new Rect(455, 278, 380, 36), "Reaparecés en " + Mathf.CeilToInt(world.Player.RespawnRemaining) + " s", title);
                GUI.Label(new Rect(455, 321, 380, 26), "P: tienda · La partida continúa", small);
            }
            if (world.Player.ItemEffects.IsReviving && !world.Paused && !ShopHud.Open)
            {
                Box(new Rect(435, 260, 410, 108), Panel);
                GUI.Label(new Rect(455, 278, 380, 36), "Guardian Angel", title);
                GUI.Label(new Rect(455, 321, 380, 26), "Revivís aquí en " + Mathf.CeilToInt(world.Player.ItemEffects.ReviveRemaining) + " s", small);
            }
            if (world.RoundOver || world.Paused)
            {
                Box(new Rect(375, 265, 530, 175), Panel);
                GUI.Label(new Rect(405, 287, 490, 40), world.Paused ? "Juego en pausa" : world.Result, title);
                GUI.Label(new Rect(405, 331, 470, 28), world.Paused ? "El duelo y sus enfriamientos están detenidos." : "CS: " + world.Player.LastHits + " · Oro: " + world.Player.Gold, label);
                if (GUI.Button(new Rect(405, 385, 210, 34), world.Paused ? "Reanudar [Esc]" : "Otra vez [F5]", button))
                { if (world.Paused) world.TogglePause(); else world.Restart(); }
                if (GUI.Button(new Rect(635, 385, 230, 34), "Reiniciar duelo", button)) world.Restart();
            }
            if (!ShopHud.Open && !world.Paused)
            {
                Vector2 pointer = new Vector2(Input.mousePosition.x * 1280 / Screen.width, (Screen.height - Input.mousePosition.y) * 720 / Screen.height);
                int slot = AbilityAtHudPoint(pointer);
                if (slot >= 0) DrawAbilityTooltip(slot);
                if (!SummonerPickerOpen && !RunePickerOpen) DrawChampionHudHover(pointer);
            }
            DrawSummonerPicker();
            DrawRunePicker();
            GUI.matrix = Matrix4x4.identity;
        }
        void DrawAbilityTooltip(int slot)
        {
            string text = IreliaAbilityHelp.Describe(world.Player, slot);
            if (slot == 2 && world.PracticeModeActive)
                text += world.PermanentPracticeMarks ? "\n\nPráctica: marcas infinitas y E sin enfriamiento. Mantené el clic izquierdo aquí durante 2 s para desactivar ambos efectos." :
                    "\n\nPráctica: mantené el clic izquierdo sobre este icono durante 2 s para activar marcas infinitas en todos los dummies y E sin enfriamiento.";
            var style = tooltipStyle;
            float height = Mathf.Max(208, style.CalcHeight(new GUIContent(text), 372) + 22);
            Rect rect = new Rect(Mathf.Clamp(AbilityRect(slot).x - 75, 20, 860), ChampionHudLayout.Origin.y - height - 8, 400, height);
            Box(rect, Panel);
            GUI.Label(new Rect(rect.x + 14, rect.y + 11, rect.width - 28, rect.height - 22), text, style);
        }
        void DrawNotices()
        {
            for (int i = 0; i < world.Notices.Count; i++)
            {
                LaneWorld.ChampionNotice notice = world.Notices[i];
                if (notice.Actor == null) continue;
                float age = Time.time - notice.StartedAt;
                Vector3 screen = world.View.WorldToScreenPoint(notice.Actor.VisualTop + Vector3.up);
                if (screen.z <= 0) continue;
                Rect rect = new Rect(screen.x * 1280 / Screen.width - 120, (Screen.height - screen.y) * 720 / Screen.height - 48 - i * 36, 240, 36);
                if (rect.y < 0 || rect.yMax > 720) continue;
                float alpha = Mathf.Clamp01(2.5f - age);
                var style = noticeStyle;
                style.normal.textColor = new Color(0, 0, 0, alpha);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), notice.Text, style);
                style.normal.textColor = new Color(notice.Color.r, notice.Color.g, notice.Color.b, alpha);
                GUI.Label(rect, notice.Text, style);
            }
        }
        void DrawCompactInformation()
        {
            Combatant player = world.Player, enemy = world.Opponent;
            Box(CompactScore, Panel);
            GUI.Label(CompactScore, "K / D / A   " + player.Kills + " / " + player.Deaths + " / " + player.Assists,
                centeredLabel);
            Box(CompactOpponent, Panel);
            GUI.Label(PvpModeButton, "Práctica · edición visual", small);
            DrawPracticeOptions();
            if (InspectedTarget != null) { DrawInspectedVitals(); return; }
            if (enemy == null)
            {
                GUI.Label(new Rect(20, 49, 224, 19), "Práctica · " + world.PracticeDummies.Count + " dummies · 0 AR / RM", small);
                GUI.Label(new Rect(20, 72, 224, 19), "Total de daño: se borra tras 5 s", small);
                GUI.Label(new Rect(20, 95, 224, 19), world.PracticeCooldownResetRemaining > 0 ? "Reinicio CD en " + world.PracticeCooldownResetRemaining.ToString("0.0") + " s sin daño" : "CD listos · Maná ilimitado", small);
                return;
            }
            GUI.Label(new Rect(20, 49, 224, 19), enemy.DisplayName + (world.OpponentActive ? " · objetos" : " · desactivado"), small);
            for (int slot = 0; slot < 6; slot++)
            {
                Rect rect = new Rect(20 + slot * 37, 70, 32, 26);
                ItemDefinition item = slot < enemy.Inventory.Items.Count ? enemy.Inventory.Items[slot] : null;
                Box(rect, new Color(0.10f, 0.14f, 0.20f));
                if (item != null) ItemIcons.Draw(new Rect(rect.x + 4, rect.y + 1, 24, 24), item.id);
                GUI.Label(rect, new GUIContent("", item == null ? "Vacío" : item.name + "\n" + item.description), small);
            }
            GUI.Label(new Rect(20, 96, 224, 19), "Oro: " + enemy.Gold, small);
        }
        static void DamageLabel(Rect rect, string text, Color color, GUIStyle style)
        {
            style.normal.textColor = new Color(0, 0, 0, color.a);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            style.normal.textColor = color; GUI.Label(rect, text, style);
        }
        void DrawPlayerPassiveStacks()
        {
            int stacks = world.Player.Stacks;
            if (!world.Player.Alive || stacks == 0) return;
            Rect badge = ChampionHudLayout.PassiveStacks;
            Box(badge, Panel);
            HudIcons.Draw(new Rect(badge.x + 2, badge.y + 2, badge.width - 4, badge.height - 4), "Passive");
            Box(badge, new Color(0, 0, 0, .25f));
            HudFrame(badge, stacks == 4 ? new Color(1, .83f, .3f) : new Color(.3f, .8f, .85f), 1.5f);
            passiveStackNumber.normal.textColor = Color.black;
            GUI.Label(new Rect(badge.x + 1, badge.y + 1, badge.width, badge.height), stacks.ToString(), passiveStackNumber);
            passiveStackNumber.normal.textColor = Color.white;
            GUI.Label(badge, stacks.ToString(), passiveStackNumber);
        }
        void DrawHoveredTarget()
        {
            Combatant target = world.GetComponent<PlayerCommands>().HoveredTarget;
            if (target == null || !target.Targetable || !target.gameObject.activeInHierarchy) return;
            Rect screen;
            if (!TargetPicker.TryScreenBounds(world.View, target, out screen)) return;
            Rect rect = new Rect(screen.xMin * 1280 / Screen.width - 3, (Screen.height - screen.yMax) * 720 / Screen.height - 3,
                screen.width * 1280 / Screen.width + 6, screen.height * 720 / Screen.height + 6);
            if (rect.yMin < 0 || rect.yMax > 720 || BlocksPointer(new Vector2(rect.center.x * Screen.width / 1280,
                rect.center.y * Screen.height / 720))) return;
            Color color = new Color(1, 0.85f, 0.25f);
            Box(new Rect(rect.x, rect.y, rect.width, 2), color);
            Box(new Rect(rect.x, rect.yMax - 2, rect.width, 2), color);
            Box(new Rect(rect.x, rect.y, 2, rect.height), color);
            Box(new Rect(rect.xMax - 2, rect.y, 2, rect.height), color);
            GUI.Label(new Rect(rect.center.x - 80, rect.yMax + 3, 160, 22), "Objetivo: " + target.DisplayName, small);
        }
        static void Box(Rect rect, Color color)
        { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
        static void Bar(Rect rect, float fraction, Color color)
        { Box(rect, new Color(0.12f, 0.13f, 0.17f)); rect.width *= Mathf.Clamp01(fraction); Box(rect, color); }
        public static Color GritBarColor(float fraction)
        { return fraction >= .9f ? new Color(1, .83f, .12f) : new Color(.75f, .78f, .82f); }
    }
}
