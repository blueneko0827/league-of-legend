using UnityEngine;
namespace MidLane
{
    // Native HUD reference at 1080p, enlarged 25% from its former 50% display scale.
    public static class ChampionHudLayout
    {
        public const float Scale = .625f * 720 / 1080;
        public const float Width = 900, Height = 204;
        public static float ScaleX { get { return Scale * Mathf.Max(1, Screen.height) * 1280 / (Mathf.Max(1, Screen.width) * 720f); } }
        public static Vector2 Origin { get { return new Vector2((1280 - Width * ScaleX) / 2, 720 - Height * Scale - 6); } }
        public static Rect Body { get { return Map(new Rect(0, 28, 900, 176)); } }
        public static Rect Map(Rect rect)
        { return new Rect(Origin.x + rect.x * ScaleX, Origin.y + rect.y * Scale, rect.width * ScaleX, rect.height * Scale); }
        public static Rect Ability(int slot) { return Map(new Rect(170 + slot * 76, 54, 64, 64)); }
        public static Rect Upgrade(int slot) { return Map(new Rect(184 + slot * 76, 0, 36, 30)); }
        public static Rect Inventory(int slot) { return Map(new Rect(650 + slot % 3 * 70, 20 + slot / 3 * 70, 60, 60)); }
        public static Rect InventoryPanel { get { return Map(new Rect(634, 14, 266, 190)); } }
        public static Rect Passive { get { return Map(new Rect(102, 66, 48, 48)); } }
        public static Rect Runes { get { return Map(new Rect(12,150,76,40)); } }
        public static Rect PassiveStacks { get { return Map(new Rect(110, -44, 72, 72)); } }
        public static Rect Summoner(int slot) { return Map(new Rect(490 + slot * 62, 60, 52, 52)); }
        public static Rect Gold { get { return Map(new Rect(646, 158, 214, 32)); } }
        public static Rect AddGold { get { return new Rect(Body.xMax + 8, Body.yMax - 32, 118, 28); } }
        public static Rect LevelUp { get { return new Rect(AddGold.xMax + 6, AddGold.y, 88, 28); } }
    }
}
