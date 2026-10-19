using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public sealed partial class ShopHud
    {
        public const string IreliaFavoritesPreferenceKey = "MidLane.PracticeVisual.Shop.IreliaFavorites.v1";
        readonly HashSet<int> ireliaFavorites = new HashSet<int>();
        void LoadIreliaFavorites()
        {
            ireliaFavorites.Clear();
            if (!PlayerPrefs.HasKey(IreliaFavoritesPreferenceKey))
            { foreach(int id in IreliaItems) ireliaFavorites.Add(id); return; }
            foreach(string token in PlayerPrefs.GetString(IreliaFavoritesPreferenceKey).Split(','))
            {
                int id;
                if (int.TryParse(token, out id) && Available(world.Player.Inventory.Get(id))) ireliaFavorites.Add(id);
            }
        }
        public bool IsIreliaFavorite(int id) { return ireliaFavorites.Contains(id); }
        public bool SetIreliaFavorite(int id, bool included)
        {
            if (!Available(world.Player.Inventory.Get(id))) return false;
            bool changed = included ? ireliaFavorites.Add(id) : ireliaFavorites.Remove(id);
            if (!changed) return false;
            var ids = new List<int>(ireliaFavorites); ids.Sort();
            PlayerPrefs.SetString(IreliaFavoritesPreferenceKey, string.Join(",", ids)); PlayerPrefs.Save();
            return true;
        }
        public bool ApplySelectedFavoriteAction()
        {
            if (selected == null || (Tab != ShopTab.Irelia && Tab != ShopTab.AllItems)) return false;
            return SetIreliaFavorite(selected.id, Tab == ShopTab.AllItems);
        }
        public static Rect FavoriteActionRect(Rect icon) { return new Rect(icon.xMax - 17, icon.y, 17, 17); }
        static void BadgeLine(Vector2 from, Vector2 to, Color color)
        {
            // Small raster strokes respect the scroll view clip without rotating GUI.matrix.
            int steps=Mathf.CeilToInt(Vector2.Distance(from,to)*2);
            for(int i=0;i<=steps;i++)
            {
                Vector2 point=Vector2.Lerp(from,to,i/(float)Mathf.Max(1,steps));
                Fill(new Rect(point.x,point.y,1.5f,1.5f),color);
            }
        }
        void DrawFavoriteAction(Rect icon, ItemDefinition item)
        {
            if (selected != item || (Tab != ShopTab.Irelia && Tab != ShopTab.AllItems)) return;
            Rect rect = FavoriteActionRect(icon); bool remove = IreliaTab;
            Fill(rect, Ink); Frame(rect, remove ? Gold : Teal);
            if (remove)
            {
                BadgeLine(new Vector2(rect.x+4,rect.y+4),new Vector2(rect.xMax-4,rect.yMax-4),Pale);
                BadgeLine(new Vector2(rect.xMax-4,rect.y+4),new Vector2(rect.x+4,rect.yMax-4),Pale);
            }
            else
            {
                BadgeLine(new Vector2(rect.x+3,rect.y+8),new Vector2(rect.x+7,rect.y+12),Teal);
                BadgeLine(new Vector2(rect.x+7,rect.y+12),new Vector2(rect.x+14,rect.y+4),Teal);
            }
            if (GUI.Button(rect, new GUIContent("", remove ? "Remove from Irelia items" : IsIreliaFavorite(item.id) ? "Already in Irelia items" : "Add to Irelia items"), GUIStyle.none))
                ApplySelectedFavoriteAction();
        }
    }
}
