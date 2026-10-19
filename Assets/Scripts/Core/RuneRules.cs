using System;
namespace MidLane
{
    public enum IreliaRunePage { Resolve, Domination, Inspiration }
    public enum Rune { Conqueror, PresenceOfMind, Alacrity, LastStand, BonePlating, Unflinching,
        SuddenImpact, TreasureHunter, JackOfAllTrades, TripleTonic, Comet, Manaflow,
        Transcendence, Scorch, LegendHaste, CutDown, ShieldBash, SecondWind, Triumph }

    // PC 16.20.1 tooltips, confirmed by the user's client captures. Virtual progression is custom.
    public static class RuneRules
    {
        public const string PreferenceKey = "MidLane.PracticeVisual.IreliaRunes.v1";
        public static readonly int[] Farm = { 0,7,13,24,35,48,60,73,86,100,114,128,145,161,180,201,223,240 };
        static readonly int[] takedowns = { 0,0,0,0,1,3,4,5,6,7,8,9,10,11,12,12,13,14 };
        static readonly int[] manaStacks = { 1,2,3,5,6,8,9,10,10,10,10,10,10,10,10,10,10,10 };
        public static int ClampLevel(int level) { return Math.Max(1,Math.Min(18,level)); }
        public static float Scale(int level, float first, float last) { return first+(last-first)*(ClampLevel(level)-1)/17f; }
        static int Interpolate(int current, int next, bool half) { return half ? (current+next)/2 : current; }
        public static int LegendPoints(int level, bool half = false)
        {
            int i=ClampLevel(level)-1, j=Math.Min(17,i+1);
            return Interpolate(Farm[i]*4+takedowns[i]*100,Farm[j]*4+takedowns[j]*100,half);
        }
        public static int LegendStacks(int level, bool half = false) { return Math.Min(10,LegendPoints(level,half)/100); }
        public static int ManaflowStacks(int level, bool half = false)
        { int i=ClampLevel(level)-1; return Interpolate(manaStacks[i],manaStacks[Math.Min(17,i+1)],half); }
        public static int TreasureGold(int level, bool finalReward)
        { return (level>=9?50:0)+(level>=11?70:0)+(level>=12?90:0)+(level>=15?110:0)+(level>=18&&finalReward?130:0); }
        public static bool Has(Archetype champion, IreliaRunePage page, Rune rune)
        {
            if(champion==Archetype.Blade || champion==Archetype.Brawler)
            {
                if(rune==Rune.Conqueror||rune==Rune.Alacrity||rune==Rune.LastStand) return true;
                if(champion==Archetype.Brawler) return rune==Rune.Triumph||rune==Rune.ShieldBash||rune==Rune.SecondWind;
                if(rune==Rune.PresenceOfMind) return true;
                return page==IreliaRunePage.Resolve ? rune==Rune.BonePlating||rune==Rune.Unflinching :
                    page==IreliaRunePage.Domination ? rune==Rune.SuddenImpact||rune==Rune.TreasureHunter :
                    rune==Rune.JackOfAllTrades||rune==Rune.TripleTonic;
            }
            return champion==Archetype.Prism && (rune==Rune.Comet||rune==Rune.Manaflow||rune==Rune.Transcendence||
                rune==Rune.Scorch||rune==Rune.LegendHaste||rune==Rune.CutDown);
        }
    }
}
