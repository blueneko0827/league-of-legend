using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public static class HudIcons
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        public static Texture2D Get(string name)
        {
            Texture2D icon;
            if (!cache.TryGetValue(name, out icon)) { icon = Resources.Load<Texture2D>("HudIcons/" + name); cache[name] = icon; }
            return icon;
        }
        public static void Draw(Rect rect, string name)
        { var texture = Get(name); if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit); }
    }
}
