using UnityEngine;

namespace MidLane
{
    // Picking uses rendered geometry independently of combat collision sizes.
    public static class TargetPicker
    {
        public static Combatant Resolve(LaneWorld world, Vector2 cursor)
        {
            if (world == null || world.View == null || world.Player == null || !world.View.pixelRect.Contains(cursor)) return null;
            float padding = Mathf.Max(0, world.Settings.selectionPadding) * world.View.pixelHeight / 720f;
            Combatant best = null;
            float bestScore = float.PositiveInfinity;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy || !world.Player.ValidEnemy(unit)) continue;
                Rect body;
                if (!TryScreenBounds(world.View, unit, out body)) continue;
                float gap = DistanceToRect(cursor, body);
                float score = float.PositiveInfinity;
                if (gap <= padding)
                {
                    // Actual visual coverage wins over nearby padding. In overlaps,
                    // favor the body whose center is closest to the pointer.
                    float centrality = Vector2.Distance(cursor, body.center) / Mathf.Max(1, body.size.magnitude);
                    score = gap == 0 ? centrality * 0.2f : 1 + gap / Mathf.Max(1, padding) + centrality * 0.05f;
                }
                Rect bar;
                if (TryHealthBar(world.View, unit, out bar) && bar.Contains(cursor))
                    score = Mathf.Min(score, 0.3f);
                if (score >= bestScore) continue;
                best = unit; bestScore = score;
            }
            return best;
        }

        public static bool TryScreenBounds(Camera camera, Combatant unit, out Rect rect)
        {
            rect = default(Rect);
            if (camera == null || unit == null) return false;
            Bounds bounds = unit.SelectionBounds;
            Vector3 center = camera.WorldToScreenPoint(bounds.center);
            if (center.z <= camera.nearClipPlane || center.z >= camera.farClipPlane) return false;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 screen = camera.WorldToScreenPoint(corner);
                        if (screen.z <= camera.nearClipPlane) return false;
                        min = Vector2.Min(min, new Vector2(screen.x, screen.y));
                        max = Vector2.Max(max, new Vector2(screen.x, screen.y));
                    }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.Overlaps(camera.pixelRect);
        }

        public static bool TryHealthBar(Camera camera, Combatant unit, out Rect rect)
        {
            rect = default(Rect);
            Vector3 point = camera.WorldToScreenPoint(unit.VisualTop + Vector3.up * .3f);
            if (point.z <= camera.nearClipPlane || point.z >= camera.farClipPlane) return false;
            float width = (unit.Kind == UnitKind.Champion ? 110 : 65) * camera.pixelWidth / 1280f;
            float barHeight = 12 * camera.pixelHeight / 720f;
            rect = new Rect(point.x - width / 2, point.y - barHeight, width, barHeight + 4 * camera.pixelHeight / 720f);
            return rect.Overlaps(camera.pixelRect);
        }

        static float DistanceToRect(Vector2 point, Rect rect)
        {
            float dx = Mathf.Max(0, Mathf.Max(rect.xMin - point.x, point.x - rect.xMax));
            float dy = Mathf.Max(0, Mathf.Max(rect.yMin - point.y, point.y - rect.yMax));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
