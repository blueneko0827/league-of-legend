using System;

namespace MidLane
{
    public enum Team { Blue, Red }
    public enum UnitKind { Champion, Minion, Tower }
    public enum DamageType { Physical, Magic, True }
    public enum Archetype { Blade, Brawler, Siphon, Prism }
    public enum Difficulty { Normal, Hard, Expert }
    public enum Intent { Retreat, LastHit, Trade, Position }

    // Independent of Unity: these rules can be checked without the editor.
    public static class CombatRules
    {
        public static float Mitigate(float raw, float armor)
        {
            if (raw <= 0) return 0;
            return raw * (armor >= 0 ? 100f / (100f + armor) : 2f - 100f / (100f - armor));
        }

        public static float SegmentDistanceSquared(float px, float pz, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az;
            float length = dx * dx + dz * dz;
            float t = length < 0.00001f ? 0 : Math.Max(0, Math.Min(1, ((px - ax) * dx + (pz - az) * dz) / length));
            float ex = px - ax - t * dx, ez = pz - az - t * dz;
            return ex * ex + ez * ez;
        }

        public static bool CircleIntersectsRectangle(float x, float z, float radius, float start, float end, float halfWidth)
        {
            float dx = Math.Max(0, Math.Max(start - x, x - end));
            float dz = Math.Max(0, Math.Abs(z) - halfWidth);
            return dx * dx + dz * dz <= radius * radius;
        }

        public static bool CircleIntersectsTrapezoid(float x, float z, float radius, float length, float nearHalfWidth, float farHalfWidth)
        {
            if (x >= 0 && x <= length && Math.Abs(z) <= nearHalfWidth + (farHalfWidth - nearHalfWidth) * x / length) return true;
            float r2 = radius * radius;
            return SegmentDistanceSquared(x, z, 0, -nearHalfWidth, length, -farHalfWidth) <= r2 ||
                SegmentDistanceSquared(x, z, length, -farHalfWidth, length, farHalfWidth) <= r2 ||
                SegmentDistanceSquared(x, z, length, farHalfWidth, 0, nearHalfWidth) <= r2 ||
                SegmentDistanceSquared(x, z, 0, nearHalfWidth, 0, -nearHalfWidth) <= r2;
        }

        public static float Reaction(Difficulty difficulty)
        {
            return difficulty == Difficulty.Expert ? 0.18f : difficulty == Difficulty.Hard ? 0.34f : 0.65f;
        }

        public static float AimError(Difficulty difficulty)
        {
            return difficulty == Difficulty.Expert ? 0.15f : difficulty == Difficulty.Hard ? 0.65f : 1.5f;
        }

        public static Intent Decide(float healthFraction, float enemyHealthFraction, bool towerDanger,
            bool lastHit, bool observedEnemyDashDown, float distance, float attackRange)
        {
            // Safety has precedence over farm or a favorable trade.
            if (towerDanger || healthFraction < 0.26f) return Intent.Retreat;
            if (enemyHealthFraction < 0.2f && healthFraction > 0.4f) return Intent.Trade;
            if (lastHit) return Intent.LastHit;
            if (observedEnemyDashDown || healthFraction > enemyHealthFraction + 0.12f || distance <= attackRange)
                return Intent.Trade;
            return Intent.Position;
        }
    }

    public sealed class AbilityClock
    {
        readonly float[] readyAt = new float[4];
        public float Remaining(int slot, float now) { return Math.Max(0, readyAt[slot] - now); }
        public bool Ready(int slot, float now) { return now >= readyAt[slot]; }
        public void Start(int slot, float now, float duration) { readyAt[slot] = now + Math.Max(0, duration); }
        public void Reset(int slot) { readyAt[slot] = 0; }
    }
}
