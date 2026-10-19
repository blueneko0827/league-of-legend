using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace MidLane
{
    public enum ShopTab { Recommended, AllItems, Irelia }
    public sealed partial class ShopHud : MonoBehaviour
    {
        public static bool Open { get; private set; }
        public ShopTab Tab { get; private set; }
        public bool IreliaTab { get { return Tab == ShopTab.Irelia; } }
        public bool SearchHasFocus { get; private set; }
        public int SelectedItemId { get { return selected != null ? selected.id : 0; } }
        public string Message { get { return message; } }
        public bool RecipeExpanded { get { return recipeRoot != null; } }
        public static readonly int[] IreliaItems = { 6672, 3153, 3302, 3026, 3053, 3156, 6333, 3181, 6695, 3111, 3047, 3172, 3072, 3173, 3174 };
        static readonly int[] RecommendedItems = { 3153, 6672, 3302, 3026, 3053, 3156 };
        static readonly int[] QuickItems = { 2003, 2031, 1001, 3111, 3047 };
        public static readonly Rect PanelRect = new Rect(150, 124, 980, 470);
        public static readonly Rect SearchRect = new Rect(172, 171, 563, 28);
        public static readonly Rect RecipeRect = new Rect(782, 222, 326, 148);
        public static readonly Rect PurchaseRect = new Rect(782, 550, 326, 30);
        public static readonly Rect SellRect = new Rect(544, 556, 190, 26);
        public static Rect InventoryRect(int slot) { return new Rect(172 + slot * 55, 524, 46, 46); }
        LaneWorld world;
        Vector2 scroll, recipeScroll, descriptionScroll;
        string search = "", message = "", selectedDescription = "";
        ItemDefinition selected, recipeRoot;
        public int RecipeRootItemId { get { return recipeRoot != null ? recipeRoot.id : 0; } }
        bool showOtherItems;
        bool clearSearchFocus;
        int selectedInventorySlot = -1;
        public void Initialize(LaneWorld lane)
        {
            world = lane; Open = false; Tab = ShopTab.Recommended;
            LoadIreliaFavorites(); SelectItem(3153);
        }
        public void Toggle() { Open = !Open; SearchHasFocus = false; clearSearchFocus = true; }
        public void Close() { Open = false; SearchHasFocus = false; clearSearchFocus = true; }
        public void SelectIreliaTab(bool enabled) { SelectTab(enabled ? ShopTab.Irelia : ShopTab.AllItems); }
        public void SelectTab(ShopTab tab) { Tab = tab; scroll = Vector2.zero; showOtherItems = false; }
        public void SetSearch(string value) { search = value ?? ""; scroll = Vector2.zero; }
        static bool Available(ItemDefinition item) { return item != null && item.purchasable && item.effectsImplemented; }
        public List<int> VisibleRootItems()
        {
            var result = new List<int>();
            if (Tab == ShopTab.Recommended)
            {
                foreach (int id in RecommendedItems)
                {
                    ItemDefinition item = world.Player.Inventory.Get(id);
                    if (Available(item) && !world.Player.Inventory.Has(id) && MatchesRecipe(item, new HashSet<int>())) result.Add(id);
                    if (result.Count == 3) break;
                }
                return result;
            }
            foreach (ItemDefinition item in LeagueItemCatalog.Items)
                if (Available(item) && (IreliaTab ? IsIreliaFavorite(item.id) :
                    showOtherItems ? !item.legendary : item.legendary) && MatchesRecipe(item, new HashSet<int>())) result.Add(item.id);
            return result;
        }
        bool MatchesRecipe(ItemDefinition item, HashSet<int> visited)
        {
            if (!visited.Add(item.id)) return false;
            if (string.IsNullOrWhiteSpace(search) || item.name.IndexOf(search.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (item.components != null) foreach (int id in item.components)
            {
                ItemDefinition child = world.Player.Inventory.Get(id);
                if (child != null && MatchesRecipe(child, visited)) return true;
            }
            return false;
        }
        public void ToggleRecipe(int id)
        {
            ItemDefinition item = world.Player.Inventory.Get(id);
            if (!Available(item) || (!item.legendary && !(IreliaTab && IsIreliaFavorite(id)))) return;
            SetSelected(item); recipeRoot = item;
            recipeScroll = Vector2.zero;
        }
        void SetSelected(ItemDefinition item)
        { selected = item; selectedDescription = FormatDescription(item.description); selectedInventorySlot = -1; descriptionScroll = Vector2.zero; }
        static string FormatDescription(string source)
        {
            var lines = new List<string>();
            var pattern = new Regex(@"^\s*\d+(?:\.\d+)?%?\s+(?:Base Health Regen|Base Mana Regen|Critical Strike Chance|Armor Penetration|Magic Penetration|Attack Damage|Ability Power|Attack Speed|Ability Haste|Magic Resist|Life Steal|Move Speed|Health Regen|Mana Regen|Tenacity|Omnivamp|Lethality|Health|Mana|Armor)\b");
            Match match = pattern.Match(source ?? "");
            while (match.Success)
            {
                lines.Add("+" + match.Value.Trim()); source = source.Substring(match.Length).TrimStart(); match = pattern.Match(source);
            }
            string stats = string.Join("\n", lines);
            string body = Regex.Replace(source ?? "", @"\s+\.\s+", ".\n\n");
            return stats + (stats.Length > 0 && body.Length > 0 ? "\n\n" : "") + body;
        }
        public void SelectItem(int id)
        {
            ItemDefinition item = world.Player.Inventory.Get(id); if (!Available(item)) return;
            SetSelected(item); recipeRoot = item; recipeScroll = Vector2.zero;
        }
        public void SelectRecipeComponent(int id)
        {
            if (!VisibleRecipeItems().Contains(id)) return;
            ItemDefinition item = world.Player.Inventory.Get(id);
            if (Available(item)) SetSelected(item);
        }
        public void SelectInventorySlot(int slot)
        {
            if (slot < 0 || slot >= world.Player.Inventory.Items.Count) return;
            SetSelected(world.Player.Inventory.Items[slot]); selectedInventorySlot = slot; recipeRoot = selected; recipeScroll = Vector2.zero;
        }
        public List<int> VisibleRecipeItems()
        {
            var result = new List<int>();
            if (recipeRoot != null) CollectRecipe(recipeRoot, result, new HashSet<int>(), RecipeExpanded);
            return result;
        }
        void CollectRecipe(ItemDefinition item, List<int> result, HashSet<int> path, bool children)
        {
            if (!path.Add(item.id)) return;
            result.Add(item.id);
            if (children && item.components != null) foreach (int id in item.components)
            {
                ItemDefinition child = world.Player.Inventory.Get(id);
                if (child != null) CollectRecipe(child, result, path, true);
            }
            path.Remove(item.id);
        }
        public List<int> BuildsIntoItems()
        {
            var result = new List<int>();
            if (selected != null) foreach (ItemDefinition item in LeagueItemCatalog.Items)
                if (Available(item) && item.components != null && System.Array.IndexOf(item.components, selected.id) >= 0) result.Add(item.id);
            return result;
        }
        public bool PurchaseSelected()
        {
            if (selected == null) return false;
            string reason; bool bought = world.Player.BuyItem(selected.id, out reason);
            message = bought ? selected.name + " comprado." : reason;
            if (bought) selectedInventorySlot = -1;
            return bought;
        }
        public bool SellSelected()
        {
            if (!HasSelectedInventoryItem) return false;
            bool sold = world.Player.SellItem(selectedInventorySlot);
            message = sold ? "Objeto vendido." : "Vendé en la zona de recuperación.";
            if (sold) selectedInventorySlot = -1;
            return sold;
        }
        bool HasSelectedInventoryItem { get { return selected != null && selectedInventorySlot >= 0 &&
            selectedInventorySlot < world.Player.Inventory.Items.Count && world.Player.Inventory.Items[selectedInventorySlot].id == selected.id; } }
        void OnDestroy() { Open = false; if (headingFont != null) Destroy(headingFont); }
    }
}
