using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public static class ItemIcons
    {
        static readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();
        public static Texture2D Get(int id)
        {
            Texture2D icon;
            if (!cache.TryGetValue(id, out icon))
            { icon = Resources.Load<Texture2D>("ItemIcons/" + id); cache[id] = icon; }
            return icon;
        }
        public static void Draw(Rect rect, int id)
        { Texture2D icon = Get(id); if (icon != null) GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit); }
    }
}
