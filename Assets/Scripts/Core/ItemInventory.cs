using System;
using System.Collections.Generic;
namespace MidLane
{
    [Serializable] public sealed class ItemValue { public string name; public float value; }
    [Serializable] public sealed class ItemDefinition
    {
        public int id, price, sell, maxStack = 1;
        public string name, description;
        public int[] components;
        public bool purchasable, boots, guardian, legendary, starter, effectsImplemented;
        public float omnivamp;
        public float health, mana, damage, power, armor, resist, attackSpeed, movement, movementPercent, haste, lifeSteal, crit, tenacity, hpRegen, mpRegen, hpRegenPercent, mpRegenPercent, penetration, magicPenetration, armorPenetrationPercent, magicPenetrationPercent;
        public ItemValue[] values;
        public float Value(string key, float fallback = 0) { if (values != null) foreach (ItemValue v in values) if (v.name == key) return v.value; return fallback; }
    }
    [Serializable] public sealed class ItemCatalogData { public string patch; public ItemDefinition[] items; }
    public sealed class ItemInventory
    {
        public readonly List<ItemDefinition> Items = new List<ItemDefinition>();
        readonly Dictionary<int, ItemDefinition> catalog = new Dictionary<int, ItemDefinition>();
        public bool ArmguardUsed { get; private set; }
        static bool MatchesComponent(int owned, int required) { return owned == required || (owned == 2421 && required == 2420); }
        public bool ShatterArmguard()
        {
            int slot = Items.FindIndex(x => x.id == 2420);
            if (ArmguardUsed || slot < 0 || Get(2421) == null) return false;
            Items[slot] = Get(2421); ArmguardUsed = true; return true;
        }
        public ItemInventory(IEnumerable<ItemDefinition> definitions) { foreach (ItemDefinition item in definitions) catalog[item.id] = item; }
        public ItemDefinition Get(int id) { ItemDefinition item; return catalog.TryGetValue(id, out item) ? item : null; }
        public bool Has(int id) { return Items.Exists(x => x.id == id); }
        // Prefer completing the largest affordable branch; match each owned piece only once.
        // Duplicated components (e.g. two Long Swords) stay distinct requirements.
        public List<int> MissingRecipePurchases(int id)
        {
            var needed = new List<int>(); var matched = new HashSet<int>();
            ItemDefinition item = Get(id);
            if (item != null && item.components != null)
                foreach (int component in item.components) CollectMissing(component, matched, needed);
            return needed;
        }
        void CollectMissing(int id, HashSet<int> matched, List<int> needed)
        {
            for (int i = 0; i < Items.Count; i++)
                if (!matched.Contains(i) && MatchesComponent(Items[i].id, id)) { matched.Add(i); return; }
            ItemDefinition item = Get(id); if (item == null) return;
            needed.Add(id);
            if (item.components != null) foreach (int component in item.components) CollectMissing(component, matched, needed);
        }
        void ConsumeComponent(int id, HashSet<int> consumed)
        {
            for (int i = 0; i < Items.Count; i++) if (!consumed.Contains(i) && MatchesComponent(Items[i].id, id)) { consumed.Add(i); return; }
            ItemDefinition component = Get(id);
            if (component != null && component.components != null) foreach (int child in component.components) ConsumeComponent(child, consumed);
        }
        HashSet<int> Recipe(ItemDefinition item)
        {
            var consumed = new HashSet<int>();
            if (item.components != null) foreach (int component in item.components) ConsumeComponent(component, consumed);
            return consumed;
        }
        public int PurchaseCost(int id)
        {
            ItemDefinition item = Get(id); if (item == null) return int.MaxValue;
            int cost = item.price; foreach (int index in Recipe(item)) cost -= Items[index].price; return Math.Max(0, cost);
        }
        public bool Buy(int id, ref int gold, out string reason)
        {
            if (id == 2420 && ArmguardUsed) id = 2421;
            reason = ""; ItemDefinition item = Get(id);
            if (item == null || !item.purchasable) { reason = "Objeto no disponible en la tienda."; return false; }
            var consumed = Recipe(item); int cost = PurchaseCost(id);
            if (gold < cost) { reason = "Oro insuficiente."; return false; }
            if (Items.Count - consumed.Count >= 6) { reason = "Inventario lleno."; return false; }
            for (int i = 0; i < Items.Count; i++)
            {
                if (consumed.Contains(i)) continue;
                ItemDefinition owned = Items[i];
                bool lifeline = (item.id == 3053 || item.id == 3155 || item.id == 3156) && (owned.id == 3053 || owned.id == 3155 || owned.id == 3156);
                if ((item.boots && owned.boots) || (item.guardian && owned.guardian) || (item.starter && owned.starter) || (item.legendary && owned.id == item.id) || lifeline)
                { reason = "Ya tenés un objeto de este grupo exclusivo."; return false; }
            }
            var indices = new List<int>(consumed); indices.Sort(); indices.Reverse();
            foreach (int index in indices) Items.RemoveAt(index);
            Items.Add(item); gold -= cost; return true;
        }
        public bool Sell(int slot, ref int gold)
        {
            if (slot < 0 || slot >= Items.Count) return false;
            gold += Items[slot].sell; Items.RemoveAt(slot); return true;
        }
        public static float MovementSpeed(float raw)
        { return raw > 490 ? raw * 0.5f + 230 : raw > 415 ? raw * 0.8f + 83 : raw < 220 ? raw * 0.5f + 110 : raw; }
    }
}
