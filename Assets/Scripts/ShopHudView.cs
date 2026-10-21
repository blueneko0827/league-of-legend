using UnityEngine;
using System.Collections.Generic;
namespace MidLane
{
    public sealed partial class ShopHud
    {
        static readonly Color Ink = new Color(0.015f, 0.065f, 0.085f), Gold = new Color(0.73f, 0.62f, 0.39f);
        static readonly Color Pale = new Color(0.85f, 0.85f, 0.76f), Muted = new Color(0.43f, 0.55f, 0.57f);
        static readonly Color Teal = new Color(0.18f, 0.81f, 0.76f), Stroke = new Color(0.24f, 0.31f, 0.29f);
        GUIStyle text, smallText, heading, center, itemName, button, searchStyle, description;
        Font headingFont;
        void Styles()
        {
            if (text != null) return;
            headingFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 14);
            text = new GUIStyle(GUI.skin.label) { fontSize = 13, padding = new RectOffset(0, 0, 0, 0) };
            text.normal.textColor = Pale;
            smallText = new GUIStyle(text) { fontSize = 11 }; smallText.normal.textColor = Muted;
            heading = new GUIStyle(text) { font = headingFont, fontSize = 12, fontStyle = FontStyle.Bold };
            heading.normal.textColor = Gold;
            center = new GUIStyle(smallText) { alignment = TextAnchor.MiddleCenter };
            itemName = new GUIStyle(heading) { fontSize = 13, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            itemName.normal.textColor = Pale;
            button = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
            button.normal.textColor = Pale; button.hover.textColor = Teal; button.active.textColor = Color.white;
            searchStyle = new GUIStyle(GUIStyle.none) { fontSize = 12, padding = new RectOffset(27, 6, 5, 4) };
            searchStyle.normal.background = searchStyle.hover.background = searchStyle.focused.background = searchStyle.active.background = null;
            searchStyle.normal.textColor = searchStyle.focused.textColor = Pale;
            description = new GUIStyle(text) { fontSize = 12, wordWrap = true, richText = false };
        }
        static void Fill(Rect rect, Color color)
        { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        static void Frame(Rect rect, Color color, float thickness = 1)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color); Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color); Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        bool FlatButton(Rect rect, string label, bool active = false)
        {
            bool hover = rect.Contains(Event.current.mousePosition) && GUI.enabled;
            Fill(rect, active || hover ? new Color(0.035f, 0.17f, 0.19f) : new Color(0.025f, 0.1f, 0.12f));
            Frame(rect, GUI.enabled ? active || hover ? Teal : Gold : Stroke);
            return GUI.Button(rect, label, button);
        }
        void OnGUI()
        {
            if (world == null || !Open) return;
            Styles(); Matrix4x4 oldMatrix = GUI.matrix; int oldDepth = GUI.depth; bool oldEnabled = GUI.enabled;
            Color oldColor = GUI.color;
            GUI.depth = -20;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            try
            {
                Fill(new Rect(0, 0, 1280, 720), new Color(0, 0.025f, 0.04f, 0.38f));
                Fill(new Rect(PanelRect.x + 5, PanelRect.y + 7, PanelRect.width, PanelRect.height), new Color(0, 0, 0, 0.45f));
                Fill(PanelRect, Ink); Frame(PanelRect, Gold);
                Frame(new Rect(PanelRect.x + 3, PanelRect.y + 3, PanelRect.width - 6, PanelRect.height - 6), new Color(0.07f, 0.16f, 0.18f));
                Fill(new Rect(758, 125, 1, 468), Stroke);
                DrawTabs(); DrawQuickItems(); DrawCatalogue(); DrawInventory(); DrawDetails();
                if (!string.IsNullOrEmpty(message)) GUI.Label(new Rect(172, 575, 560, 16), message, smallText);
                DrawTooltip();
            }
            finally { GUI.matrix = oldMatrix; GUI.depth = oldDepth; GUI.color = oldColor; GUI.enabled = oldEnabled; }
        }
        static void DrawSearchIcon(Vector2 center)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16, b = (i + 1) * Mathf.PI * 2 / 16;
                DrawStroke(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 4, Muted);
            }
            DrawStroke(center + Vector2.one * 3, center + Vector2.one * 7, Muted);
        }
        static void DrawStroke(Vector2 from, Vector2 to, Color color)
        {
            Matrix4x4 old = GUI.matrix; Vector2 delta = to - from;
            GUI.matrix = old * Matrix4x4.TRS(from, Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg), Vector3.one);
            Fill(new Rect(0, 0, delta.magnitude, 1), color); GUI.matrix = old;
        }
        void DrawTabs()
        {
            string[] names = { "RECOMMENDED", "ALL ITEMS", "IRELIA" };
            for (int i = 0; i < names.Length; i++)
            {
                Rect rect = new Rect(172 + i * 170, 132, 160, 30);
                if (GUI.Button(rect, names[i], new GUIStyle(Tab == (ShopTab)i ? itemName : heading) { alignment = TextAnchor.MiddleCenter })) SelectTab((ShopTab)i);
                if (Tab == (ShopTab)i) Fill(new Rect(rect.x, 163, rect.width, 2), Teal);
            }
            Fill(new Rect(172, 166, 563, 1), Stroke);
            Fill(SearchRect, new Color(0.005f, 0.035f, 0.05f)); Frame(SearchRect, Gold);
            GUI.SetNextControlName("ShopSearch"); string value = GUI.TextField(SearchRect, search, searchStyle);
            if (clearSearchFocus) { GUI.FocusControl(null); clearSearchFocus = false; }
            SearchHasFocus = GUI.GetNameOfFocusedControl() == "ShopSearch";
            if (value != search) SetSearch(value);
            DrawSearchIcon(new Vector2(185, 183));
            if (string.IsNullOrEmpty(search) && GUI.GetNameOfFocusedControl() != "ShopSearch")
                GUI.Label(new Rect(200, 178, 470, 20), "Type to search items or components...", smallText);
            if (GUI.Button(new Rect(1090, 132, 24, 25), "×", new GUIStyle(button) { fontSize = 23 })) Close();
            GUI.Label(new Rect(784, 141, 292, 20), "SHOP AVAILABLE", smallText);
        }
        void DrawQuickItems()
        {
            Rect tray = new Rect(98, 171, 47, 304); Fill(tray, Ink); Frame(tray, Stroke);
            for (int i = 0; i < QuickItems.Length; i++)
            {
                ItemDefinition item = world.Player.Inventory.Get(QuickItems[i]); if (!Available(item)) continue;
                Rect icon = new Rect(107, 181 + i * 57, 29, 29);
                ItemButton(icon, item, false);
                GUI.Label(new Rect(100, icon.yMax + 3, 43, 15), world.Player.Inventory.PurchaseCost(item.id).ToString(), center);
            }
        }
        bool ItemButton(Rect rect, ItemDefinition item, bool toggle, bool? ownedOverride = null, bool preserveRecipe = false, bool favoriteAction = false)
        {
            Fill(rect, new Color(0.035f, 0.12f, 0.14f));
            ItemIcons.Draw(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), item.id);
            bool owned = ownedOverride ?? world.Player.Inventory.Has(item.id);
            Frame(rect, selected == item ? Teal : owned ? Gold : Stroke, selected == item ? 2 : 1);
            if (owned) { Fill(new Rect(rect.xMax - 9, rect.yMax - 9, 8, 8), Teal); }
            if (favoriteAction) DrawFavoriteAction(rect, item);
            bool clicked = GUI.Button(rect, new GUIContent("", item.name + "\n" + world.Player.Inventory.PurchaseCost(item.id) + " gold"), GUIStyle.none);
            if (clicked) { if (toggle) ToggleRecipe(item.id); else if (preserveRecipe) SelectRecipeComponent(item.id); else SelectItem(item.id); }
            return clicked;
        }
        void DrawCatalogue()
        {
            List<int> ids = VisibleRootItems();
            if (Tab == ShopTab.Recommended)
            {
                GUI.Label(new Rect(172, 211, 563, 22), "SUGGESTED ITEMS", new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter });
                for (int i = 0; i < ids.Count; i++) DrawRecommendation(new Rect(172 + i * 190, 242, 181, 229), world.Player.Inventory.Get(ids[i]));
                if (ids.Count == 0) GUI.Label(new Rect(190, 290, 520, 50), "No suggested items match this search. Browse ALL ITEMS.", description);
                return;
            }
            if (Tab == ShopTab.AllItems)
            {
                if (FlatButton(new Rect(172, 207, 110, 23), "LEGENDARY", !showOtherItems)) { showOtherItems = false; scroll = Vector2.zero; }
                if (FlatButton(new Rect(292, 207, 205, 23), "COMPONENTS & MORE", showOtherItems)) { showOtherItems = true; scroll = Vector2.zero; }
            }
            else GUI.Label(new Rect(172, 211, 563, 20), "IRELIA  /  ITEM SET", heading);
            const int columns = 7;
            scroll = GUI.BeginScrollView(new Rect(172, 242, 563, 235), scroll, new Rect(0, 0, 540, Mathf.Max(234, Mathf.Ceil(ids.Count / (float)columns) * 80)), false, false);
            for (int i = 0; i < ids.Count; i++)
            {
                ItemDefinition item = world.Player.Inventory.Get(ids[i]); float x = i % columns * 77, y = i / columns * 80;
                ItemButton(new Rect(x + 8, y + 2, 52, 52), item, item.legendary || IreliaTab, favoriteAction: true);
                int cost = world.Player.Inventory.PurchaseCost(item.id);
                var price = new GUIStyle(center); price.normal.textColor = cost <= world.Player.Gold ? Gold : Muted;
                GUI.Label(new Rect(x, y + 57, 70, 18), cost.ToString(), price);
            }
            GUI.EndScrollView();
            if (ids.Count == 0) GUI.Label(new Rect(190, 280, 500, 40), "No items match this search.", description);
        }
        void DrawRecommendation(Rect rect, ItemDefinition item)
        {
            bool active = selected == item, hover = rect.Contains(Event.current.mousePosition);
            Fill(rect, active || hover ? new Color(0.025f, 0.14f, 0.16f) : new Color(0.02f, 0.085f, 0.105f));
            Frame(rect, active ? Teal : Stroke);
            if (GUI.Button(rect, new GUIContent("", item.name), GUIStyle.none)) ToggleRecipe(item.id);
            GUI.Label(new Rect(rect.x + 9, rect.y + 14, rect.width - 18, 38), item.name, itemName);
            ItemIcons.Draw(new Rect(rect.center.x - 38, rect.y + 60, 76, 76), item.id);
            Frame(new Rect(rect.center.x - 39, rect.y + 59, 78, 78), active ? Teal : Gold);
            GUI.Label(new Rect(rect.x, rect.y + 143, rect.width, 22), world.Player.Inventory.PurchaseCost(item.id) + "  gold", new GUIStyle(itemName) { fontSize = 14 });
            string role = item.id == 3153 ? "DUELING & SUSTAIN" : item.id == 6672 ? "ON-HIT DAMAGE" : item.id == 3302 ? "MIXED PENETRATION" : "SURVIVABILITY";
            GUI.Label(new Rect(rect.x + 8, rect.y + 180, rect.width - 16, 22), role, center);
            GUI.Label(new Rect(rect.x + 8, rect.y + 204, rect.width - 16, 18), active ? "SELECTED  ·  VIEW BUILD" : "VIEW BUILD", center);
            if (active) Fill(new Rect(rect.x + 1, rect.yMax - 3, rect.width - 2, 2), Teal);
        }
        void DrawInventory()
        {
            Fill(new Rect(172, 487, 563, 1), Stroke);
            GUI.Label(new Rect(172, 499, 320, 19), "INVENTORY", heading);
            for (int i = 0; i < 6; i++)
            {
                Rect rect = InventoryRect(i); Fill(rect, new Color(0.005f, 0.035f, 0.045f));
                ItemDefinition item = i < world.Player.Inventory.Items.Count ? world.Player.Inventory.Items[i] : null;
                if (item != null) ItemIcons.Draw(new Rect(rect.x + 3, rect.y + 3, 40, 40), item.id);
                Frame(rect, HasSelectedInventoryItem && i == selectedInventorySlot ? Teal : Stroke);
                if (item != null && GUI.Button(rect, new GUIContent("", item.name), GUIStyle.none)) SelectInventorySlot(i);
            }
            GUI.Label(new Rect(544, 521, 190, 28), world.Player.Gold + "  GOLD", new GUIStyle(itemName) { fontSize = 19 });
            bool old = GUI.enabled; GUI.enabled = HasSelectedInventoryItem && world.Player.CanShop && !world.Paused;
            if (FlatButton(SellRect, "SELL" + (HasSelectedInventoryItem ? "  +" + selected.sell : ""))) SellSelected();
            GUI.enabled = old;
        }
        void DrawDetails()
        {
            GUI.Label(new Rect(782, 174, 326, 16), "BUILDS INTO", smallText);
            List<int> upgrades = BuildsIntoItems();
            for (int i = 0; i < Mathf.Min(upgrades.Count, 8); i++) ItemButton(new Rect(782 + i * 38, 193, 28, 28), world.Player.Inventory.Get(upgrades[i]), false);
            if (upgrades.Count == 0) GUI.Label(new Rect(782, 194, 326, 22), selected == null ? "Select an item" : "Final item", smallText);
            DrawRecipeGraph();
            Fill(new Rect(782, 380, 326, 1), Stroke);
            if (selected == null) return;
            ItemIcons.Draw(new Rect(782, 394, 36, 36), selected.id); Frame(new Rect(782, 394, 36, 36), Gold);
            GUI.Label(new Rect(830, 391, 278, 34), selected.name, new GUIStyle(itemName) { alignment = TextAnchor.UpperLeft });
            int cost = world.Player.Inventory.PurchaseCost(selected.id);
            GUI.Label(new Rect(830, 426, 278, 18), cost + " gold" + (cost != selected.price ? "  /  total " + selected.price : ""), new GUIStyle(smallText) { normal = { textColor = Gold } });
            float height = Mathf.Max(88, description.CalcHeight(new GUIContent(selectedDescription), 307));
            descriptionScroll = GUI.BeginScrollView(new Rect(782, 450, 326, 88), descriptionScroll, new Rect(0, 0, 307, height), false, false);
            GUI.Label(new Rect(0, 0, 307, height), selectedDescription, description); GUI.EndScrollView();
            bool old = GUI.enabled;
            GUI.enabled = world.Player.CanBuy && !world.Paused && Available(selected) && world.Player.Gold >= cost;
            if (FlatButton(PurchaseRect, "PURCHASE  ·  " + cost + " GOLD", true)) PurchaseSelected();
            GUI.enabled = old;
        }
        void DrawTooltip()
        {
            if (string.IsNullOrEmpty(GUI.tooltip)) return;
            Vector2 pointer = Event.current.mousePosition;
            Rect rect = new Rect(Mathf.Clamp(pointer.x + 16, 160, 850), Mathf.Clamp(pointer.y + 18, 170, 522), 265, 53);
            Fill(rect, new Color(0.005f, 0.035f, 0.05f)); Frame(rect, Gold);
            GUI.Label(new Rect(rect.x + 9, rect.y + 7, rect.width - 18, rect.height - 14), GUI.tooltip, description);
        }
    }
}
