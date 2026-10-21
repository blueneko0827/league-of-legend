using UnityEngine;

namespace MidLane
{
    // Overhead bars in the style of the LoL Practice Tool: level box, health segmented every
    // 100 HP, resource strip, Irelia's passive dots and, for dummies, the last hit / DPS /
    // total labels under the feet. Positions use the 1280x720 HUD space of PracticeHud.
    public static class WorldBarLayout
    {
        public const float ChampionWidth = 116, ChampionHeight = 20;
        public const float TowerWidth = 150, TowerHeight = 14;
        public const float MinionWidth = 62, MinionHeight = 7;

        // World point the bar hangs from: VisualTop + 0.3 up. Champions (not dummies) go higher,
        // because their official models (hair, floating blades) rise above VisualTop.
        public static Vector3 Anchor(Combatant unit, ScenePresentationConfig presentation)
        {
            float lift = presentation != null && unit.Kind == UnitKind.Champion && unit.Dummy == null ?
                presentation.champion_bar_lift * unit.VisualHeight : 0;
            return unit.VisualTop + Vector3.up * (.3f + lift);
        }

        // anchor: projection of Anchor(), in HUD space (y grows downward).
        public static Rect Frame(Combatant unit, Vector2 anchor)
        {
            if (unit.Kind == UnitKind.Tower) return new Rect(anchor.x - TowerWidth / 2, anchor.y - 4, TowerWidth, TowerHeight);
            if (unit.Kind == UnitKind.Minion) return new Rect(anchor.x - MinionWidth / 2, anchor.y - 2, MinionWidth, MinionHeight);
            return new Rect(anchor.x - ChampionWidth / 2, anchor.y - 4, ChampionWidth, ChampionHeight);
        }
    }

    public sealed partial class PracticeHud
    {
        static readonly Color BarInk = new Color(.015f, .02f, .025f, .94f);
        static readonly Color BarEmpty = new Color(.06f, .07f, .08f, 1);
        static readonly Color BarTick = new Color(0, 0, 0, .55f);
        static readonly Color SelfHealth = new Color(.31f, .72f, .18f), SelfHealthLight = new Color(.52f, .88f, .34f);
        static readonly Color AllyHealth = new Color(.2f, .52f, .88f), AllyHealthLight = new Color(.42f, .7f, .98f);
        static readonly Color EnemyHealth = new Color(.76f, .16f, .13f), EnemyHealthLight = new Color(.93f, .36f, .28f);
        static readonly Color ShieldColor = new Color(.9f, .9f, .9f);
        static readonly Color ManaColor = new Color(.24f, .55f, .95f);
        static readonly Color LevelSelf = new Color(.05f, .11f, .19f), LevelAlly = new Color(.05f, .11f, .19f), LevelEnemy = new Color(.24f, .04f, .04f);
        static readonly Color LevelBorder = new Color(.42f, .38f, .28f);
        GUIStyle barName, barLevel, dummyStats, dummyHit;
        static readonly Color StackColor = new Color(.47f, .64f, .86f), StackMaxColor = new Color(.74f, .82f, .13f);
        static Texture2D stackSegment, stackDot;

        // Built once: a parallelogram with a dark outline (tinted per state) and an empty-slot dot.
        static Texture2D StackPip(bool filled)
        {
            if (filled && stackSegment != null) return stackSegment;
            if (!filled && stackDot != null) return stackDot;
            int w = filled ? 64 : 16, h = 16;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c = new Color32(0, 0, 0, 0);
                    float px = x + .5f, py = y + .5f;
                    if (filled)
                    {
                        // Top edge shifted right (texture y grows upward).
                        const float slant = 16, border = 3.5f;
                        float left = slant * py / h, right = w - slant + slant * py / h;
                        float inside = Mathf.Min(Mathf.Min(px - left, right - px), Mathf.Min(py, h - py));
                        if (inside > 0)
                        {
                            byte v = (byte)(255 * (.82f + .18f * py / h));
                            c = inside < border ? new Color32(8, 10, 12, 255) : new Color32(v, v, v, 255);
                        }
                    }
                    else
                    {
                        float r = Mathf.Sqrt((px - w / 2f) * (px - w / 2f) + (py - h / 2f) * (py - h / 2f));
                        if (r < 7.5f) c = r < 5 ? new Color32(28, 30, 33, 235) : new Color32(92, 96, 100, 235);
                    }
                    pixels[y * w + x] = c;
                }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return filled ? (stackSegment = texture) : (stackDot = texture);
        }

        void BarStyles()
        {
            if (barName != null) return;
            barName = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(), margin = new RectOffset(), clipping = TextClipping.Overflow };
            barLevel = new GUIStyle(barName) { fontSize = 12 };
            dummyStats = new GUIStyle(barName) { fontSize = 13 };
            dummyHit = new GUIStyle(barName) { fontSize = 15 };
        }

        void DrawWorldBars()
        {
            BarStyles();
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !unit.Alive || !unit.gameObject.activeInHierarchy) continue;
                Vector3 screen = world.View.WorldToScreenPoint(WorldBarLayout.Anchor(unit, world.Presentation));
                if (screen.z <= 0) continue;
                var anchor = new Vector2(screen.x * 1280 / Screen.width, (Screen.height - screen.y) * 720 / Screen.height);
                if (anchor.y < 0 || anchor.y > 720 || anchor.x < 0 || anchor.x > 1280 || BlocksPointer(new Vector2(screen.x, Screen.height - screen.y))) continue;
                if (unit.Kind == UnitKind.Tower) DrawTowerBar(unit, anchor);
                else if (unit.Kind == UnitKind.Minion) DrawMinionBar(unit, anchor);
                else DrawChampionBar(unit, anchor);
            }
        }

        void DrawChampionBar(Combatant unit, Vector2 anchor)
        {
            Rect frame = WorldBarLayout.Frame(unit, anchor);
            bool own = unit == world.Player, enemy = unit.Team != world.PlayerTeam;
            Color health = own ? SelfHealth : enemy ? EnemyHealth : AllyHealth;
            Color healthLight = own ? SelfHealthLight : enemy ? EnemyHealthLight : AllyHealthLight;

            string title = unit.Dummy != null ? DummyName(unit.Dummy) :
                unit.IsInStasis ? "ESTASIS · " + unit.ItemEffects.StasisRemaining.ToString("0.0") + " s" : unit.DisplayName;
            DamageLabel(new Rect(frame.x - 60, frame.y - 18, frame.width + 120, 16), title, new Color(.95f, .95f, .92f), barName);

            Box(frame, BarInk);
            var level = new Rect(frame.x + 1, frame.y + 1, frame.height - 2, frame.height - 2);
            Box(level, own ? LevelSelf : enemy ? LevelEnemy : LevelAlly);
            HudFrame(level, LevelBorder, 1);
            DamageLabel(level, unit.Level.ToString(), Color.white, barLevel);

            var healthRect = new Rect(level.xMax + 2, frame.y + 2, frame.xMax - level.xMax - 4, 10);
            SegmentedHealth(healthRect, unit.Health, unit.MaxHealth, unit.TotalShield, health, healthLight);

            var resource = new Rect(healthRect.x, healthRect.yMax + 2, healthRect.width, frame.yMax - healthRect.yMax - 4);
            Box(resource, BarEmpty);
            float fraction = unit.UnlimitedMana ? 1 : unit.MaxResource > 0 ? unit.Resource / unit.MaxResource : 0;
            if (fraction > 0) Box(new Rect(resource.x, resource.y, resource.width * Mathf.Clamp01(fraction), resource.height), ManaColor);

            // Irelia's passive under her own bar, as in the client: four slots, an empty slot is a
            // small dark dot, a stack is a slanted segment (steel blue, lime at 4 stacks).
            if (own && unit.Blade != null)
            {
                float slot = (healthRect.width + 6) / 4;
                Color stackColor = unit.Stacks >= 4 ? StackMaxColor : StackColor;
                for (int i = 0; i < 4; i++)
                {
                    float x = healthRect.x - 4 + i * slot;
                    if (i < unit.Stacks)
                    {
                        Color previous = GUI.color; GUI.color = stackColor;
                        GUI.DrawTexture(new Rect(x, frame.yMax + 2, slot - 3, 6), StackPip(true));
                        GUI.color = previous;
                    }
                    else GUI.DrawTexture(new Rect(x + (slot - 3) / 2 - 2.5f, frame.yMax + 2.5f, 5, 5), StackPip(false));
                }
            }

            float below = frame.yMax + (own ? 11 : 3);
            if (unit.Sett != null)
            {
                Color gritColor = GritBarColor(unit.Sett.GritFraction);
                Bar(new Rect(healthRect.x, below, healthRect.width, 4), unit.Sett.GritFraction, gritColor);
                Color previous = GUI.color; GUI.color = gritColor;
                GUI.Label(new Rect(frame.x - 7, frame.y - 36, 130, 16), "GRIT " + unit.Sett.Grit.ToString("0") + " / " + unit.Sett.MaxGrit.ToString("0"), gritLabel);
                GUI.color = previous;
            }
            if (unit.Dummy != null) DrawDummyDamage(unit, frame);
        }

        static string DummyName(TrainingDummy dummy)
        { return dummy.PermanentMark ? "Dummy Q" : dummy.Patrolling ? "Dummy aleatorio" : dummy.Mobile ? "Dummy evasivo" : "Dummy fijo"; }

        void DrawDummyDamage(Combatant unit, Rect frame)
        {
            TrainingDummy dummy = unit.Dummy;
            // Practice Tool statistics under the dummy's feet.
            Vector3 feet = world.View.WorldToScreenPoint(unit.Position);
            if (feet.z > 0 && !BlocksPointer(new Vector2(feet.x, Screen.height - feet.y)))
            {
                float x = feet.x * 1280 / Screen.width, y = (Screen.height - feet.y) * 720 / Screen.height + 4;
                Color text = new Color(.96f, .96f, .94f);
                DamageLabel(new Rect(x - 80, y, 160, 17), "Último golpe: " + dummy.LastHitAmount.ToString("0"), text, dummyStats);
                DamageLabel(new Rect(x - 80, y + 17, 160, 17), "DPS: " + dummy.DamagePerSecond.ToString("0"), text, dummyStats);
                DamageLabel(new Rect(x - 80, y + 34, 160, 17), "Total: " + dummy.TotalDamage.ToString("0"), text, dummyStats);
            }
            // Individual hits rise above the name, colored by damage type.
            for (int i = 0; i < dummy.Notices.Count; i++)
            {
                var hit = dummy.Notices[i]; float age = Time.time - hit.StartedAt;
                Color color = hit.Type == DamageType.Magic ? new Color(.55f, .8f, 1) : hit.Type == DamageType.True ? Color.white : new Color(1, .65f, .35f);
                color.a = Mathf.Clamp01((1.5f - age) * 2);
                DamageLabel(new Rect(frame.center.x - 110, frame.y - 40 - (dummy.Notices.Count - 1 - i) * 19 - age * 18, 220, 20),
                    hit.Label + "  " + hit.Amount.ToString("0.#"), color, dummyHit);
            }
        }

        void DrawTowerBar(Combatant unit, Vector2 anchor)
        {
            Rect frame = WorldBarLayout.Frame(unit, anchor);
            bool enemy = unit.Team != world.PlayerTeam;
            DamageLabel(new Rect(frame.x - 30, frame.y - 18, frame.width + 60, 16), "Torre · Indestructible", new Color(.95f, .95f, .92f), barName);
            Box(frame, BarInk);
            HudFrame(frame, HudGold, 1);
            var healthRect = new Rect(frame.x + 3, frame.y + 3, frame.width - 6, frame.height - 6);
            SegmentedHealth(healthRect, unit.Health, unit.MaxHealth, 0, enemy ? EnemyHealth : AllyHealth, enemy ? EnemyHealthLight : AllyHealthLight);
        }

        void DrawMinionBar(Combatant unit, Vector2 anchor)
        {
            Rect frame = WorldBarLayout.Frame(unit, anchor);
            bool enemy = unit.Team != world.PlayerTeam;
            Box(frame, BarInk);
            var healthRect = new Rect(frame.x + 1, frame.y + 1, frame.width - 2, frame.height - 2);
            Box(healthRect, BarEmpty);
            Box(new Rect(healthRect.x, healthRect.y, healthRect.width * Mathf.Clamp01(unit.Health / Mathf.Max(1, unit.MaxHealth)), healthRect.height), enemy ? EnemyHealth : AllyHealth);
        }

        // Shields extend the bar in white; when health + shield exceed the maximum the whole
        // bar is rescaled, as in League. Thin ticks every 100 HP, thicker ones every 1000.
        static void SegmentedHealth(Rect rect, float health, float maxHealth, float shield, Color color, Color highlight)
        {
            Box(rect, BarEmpty);
            float total = Mathf.Max(maxHealth, health + Mathf.Max(0, shield));
            if (total <= 0) return;
            float healthWidth = rect.width * Mathf.Clamp01(health / total);
            Box(new Rect(rect.x, rect.y, healthWidth, rect.height), color);
            Box(new Rect(rect.x, rect.y, healthWidth, Mathf.Max(1, rect.height * .35f)), highlight);
            if (shield > 0) Box(new Rect(rect.x + healthWidth, rect.y, rect.width * Mathf.Clamp01(shield / total), rect.height), ShieldColor);
            float step = maxHealth > 4000 ? 1000 : 100;
            int ticks = Mathf.Min(60, Mathf.FloorToInt((total - 1) / step));
            for (int i = 1; i <= ticks; i++)
            {
                float hp = i * step;
                bool major = step < 1000 && i % 10 == 0;
                float x = Mathf.Round(rect.x + rect.width * hp / total);
                Box(new Rect(x, rect.y, major ? 2 : 1, major ? rect.height : rect.height * .65f), BarTick);
            }
        }
    }
}
