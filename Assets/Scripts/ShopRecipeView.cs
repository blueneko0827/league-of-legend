using UnityEngine;
using System.Collections.Generic;
namespace MidLane
{
    public sealed partial class ShopHud
    {
        sealed class RecipeNode
        {
            public ItemDefinition Item;
            public Rect Rect;
            public int Depth, Leaves = 1;
            public readonly List<RecipeNode> Children = new List<RecipeNode>();
        }
        RecipeNode RecipeTree(ItemDefinition item, int depth, HashSet<int> path)
        {
            if (!path.Add(item.id)) return null;
            var node = new RecipeNode { Item = item, Depth = depth };
            if (RecipeExpanded && item.components != null) foreach (int id in item.components)
            {
                ItemDefinition child = world.Player.Inventory.Get(id); if (child == null) continue;
                RecipeNode branch = RecipeTree(child, depth + 1, path); if (branch != null) node.Children.Add(branch);
            }
            if (node.Children.Count > 0) { node.Leaves = 0; foreach (var child in node.Children) node.Leaves += child.Leaves; }
            path.Remove(item.id); return node;
        }
        int LayoutRecipe(RecipeNode node, float left, float width)
        {
            float size = node.Depth == 0 ? 34 : node.Depth == 1 ? 28 : 26;
            node.Rect = new Rect(left + (width - size) / 2, 4 + node.Depth * 50, size, size);
            int depth = node.Depth; float x = left;
            foreach (RecipeNode child in node.Children)
            {
                float span = width * child.Leaves / node.Leaves;
                depth = Mathf.Max(depth, LayoutRecipe(child, x, span)); x += span;
            }
            return depth;
        }
        void DrawRecipeGraph()
        {
            if (recipeRoot == null) return;
            RecipeNode root = RecipeTree(recipeRoot, 0, new HashSet<int>());
            float width = Mathf.Max(305, root.Leaves * 38);
            int depth = LayoutRecipe(root, 0, width);
            recipeScroll = GUI.BeginScrollView(RecipeRect, recipeScroll, new Rect(0, 0, width, Mathf.Max(147, depth * 50 + 47)), false, false);
            var owned = new Dictionary<int, int>();
            foreach (ItemDefinition item in world.Player.Inventory.Items) { int count; owned.TryGetValue(item.id, out count); owned[item.id] = count + 1; }
            DrawRecipeNode(root, owned); GUI.EndScrollView();
        }
        void DrawRecipeNode(RecipeNode node, Dictionary<int, int> owned)
        {
            foreach (RecipeNode child in node.Children)
            {
                float y = node.Rect.yMax + 15;
                Fill(new Rect(node.Rect.center.x, node.Rect.yMax, 1, y - node.Rect.yMax), Stroke);
                Fill(new Rect(Mathf.Min(node.Rect.center.x, child.Rect.center.x), y, Mathf.Abs(node.Rect.center.x - child.Rect.center.x) + 1, 1), Gold);
                Fill(new Rect(child.Rect.center.x, y, 1, child.Rect.y - y), Stroke);
                DrawRecipeNode(child, owned);
            }
            int count; owned.TryGetValue(node.Item.id, out count); bool has = count > 0;
            if (has) owned[node.Item.id] = count - 1;
            ItemButton(node.Rect, node.Item, node.Depth == 0 && (node.Item.legendary || IreliaTab), has, true);
            var price = new GUIStyle(center) { fontSize = 10 }; price.normal.textColor = has ? Teal : Gold;
            GUI.Label(new Rect(node.Rect.center.x - 27, node.Rect.yMax + 3, 54, 12), has ? "OWNED" : world.Player.Inventory.PurchaseCost(node.Item.id).ToString(), price);
        }
    }
}
