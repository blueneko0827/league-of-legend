using System;

namespace MidLane
{
    // Riot Data Dragon 16.20.1. Vladimir still uses a prototype effect kit.
    public static class RivalAbilities
    {
        static readonly float[][] Settcooldown = {
            new float[] { 9f, 8f, 7f, 6f, 5f },
            new float[] { 18f, 16.5f, 15f, 13.5f, 12f },
            new float[] { 16f, 14.5f, 13f, 11.5f, 10f },
            new float[] { 120f, 100f, 80f },
        };
        static readonly float[][] Settcost = {
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0f, 0f, 0f },
        };
        static readonly float[][] Vladimircooldown = {
            new float[] { 9f, 7.9f, 6.8f, 5.7f, 4.6f },
            new float[] { 28f, 25f, 22f, 19f, 16f },
            new float[] { 13f, 11f, 9f, 7f, 5f },
            new float[] { 120f, 120f, 120f },
        };
        static readonly float[][] Vladimircost = {
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0f, 0f, 0f },
        };
        static readonly float[][] Melcooldown = {
            new float[] { 10f, 9f, 8f, 7f, 6f },
            new float[] { 38f, 35f, 33f, 29f, 26f },
            new float[] { 11f, 10.5f, 10f, 9.5f, 9f },
            new float[] { 120f, 100f, 80f },
        };
        static readonly float[][] Melcost = {
            new float[] { 70f, 75f, 80f, 85f, 90f },
            new float[] { 80f, 60f, 40f, 20f, 0f },
            new float[] { 50f, 60f, 70f, 80f, 90f },
            new float[] { 100f, 100f, 100f },
        };
        static float Value(float[][] values, int slot, int rank)
        { return slot < 0 || slot > 3 ? 0 : values[slot][Math.Max(0, Math.Min(values[slot].Length - 1, rank - 1))]; }
        public static float Cooldown(Archetype champion, int slot, int rank)
        { return Value(champion == Archetype.Brawler ? Settcooldown : champion == Archetype.Siphon ? Vladimircooldown : Melcooldown, slot, rank); }
        public static float Cost(Archetype champion, int slot, int rank)
        { return Value(champion == Archetype.Brawler ? Settcost : champion == Archetype.Siphon ? Vladimircost : Melcost, slot, rank); }
    }
}
