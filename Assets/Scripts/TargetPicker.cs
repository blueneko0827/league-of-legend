using UnityEngine;

namespace MidLane
{
    // Picking uses presentation-sized body proxies independently of combat collisions.
    public static class TargetPicker
    {
        public static Combatant Resolve(LaneWorld world, Vector2 cursor)
        { return Resolve(world, cursor, false); }

        // Inspection includes allies and the player; combat picking remains enemy-only.
        public static Combatant ResolveForInspection(LaneWorld world, Vector2 cursor)
        { return Resolve(world, cursor, true); }

        static Combatant Resolve(LaneWorld world, Vector2 cursor, bool inspection)
        {
            if (world == null || world.View == null || world.Player == null || !world.View.pixelRect.Contains(cursor)) return null;
            float padding = Mathf.Max(0, world.Settings.selectionPadding) * world.View.pixelHeight / 720f;
            Combatant best = null;
            int bestPriority = int.MaxValue;
            float bestDistance = float.PositiveInfinity, bestDepth = float.PositiveInfinity;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy ||
                    (inspection ? !unit.Alive : !world.Player.ValidEnemy(unit))) continue;
                Rect body;
                if (!TryScreenBounds(world.View, unit, out body)) continue;
                float gap = BodyDistance(cursor, body);
                int priority = gap <= 0 ? 1 : gap <= padding ? 2 : int.MaxValue;
                float distance = priority == 1 ? Vector2.Distance(cursor, body.center) : gap;
                Rect bar;
                if (TryHealthBar(world.View, unit, out bar) && bar.Contains(cursor))
                {
                    priority = 0;
                    distance = Vector2.Distance(cursor, bar.center);
                }
                if (priority == int.MaxValue) continue;
                float depth = world.View.WorldToScreenPoint((unit.Position + unit.VisualTop) * .5f).z;
                // Pixel distances avoid favoring large champions. Depth breaks ties.
                if (priority > bestPriority || (priority == bestPriority &&
                    (distance > bestDistance + .01f ||
                    (Mathf.Abs(distance - bestDistance) <= .01f &&
                     (depth > bestDepth || (depth == bestDepth && best != null && unit.GetHashCode() > best.GetHashCode())))))) continue;
                best = unit; bestPriority = priority; bestDistance = distance; bestDepth = depth;
            }
            return best;
        }

        public static bool TryScreenBounds(Camera camera, Combatant unit, out Rect rect)
        {
            rect = default(Rect);
            if (camera == null || unit == null) return false;
            Vector3 feet = camera.WorldToScreenPoint(unit.Position);
            Vector3 head = camera.WorldToScreenPoint(unit.VisualTop);
            if (Mathf.Min(feet.z, head.z) <= camera.nearClipPlane ||
                Mathf.Max(feet.z, head.z) >= camera.farClipPlane) return false;
            Vector3 center = (unit.Position + unit.VisualTop) * .5f;
            float scale = unit.VisualHeight / unit.SelectionHeight;
            float widthFactor = unit.Kind == UnitKind.Champion && unit.Dummy == null ? .68f :
                unit.Kind == UnitKind.Tower ? 1f : .85f;
            Vector3 side = camera.WorldToScreenPoint(center + camera.transform.right * unit.Radius * scale * widthFactor);
            float halfWidth = Mathf.Abs(side.x - camera.WorldToScreenPoint(center).x);
            float height = Mathf.Max(1, Mathf.Abs(head.y - feet.y));
            halfWidth = Mathf.Clamp(halfWidth, .5f, height * .5f);
            float x = (feet.x + head.x) * .5f;
            // Rounded body proxy: weapons, VFX, shadows and empty box corners don't
            // enlarge selection. This is independent of logical combat radii.
            rect = new Rect(x - halfWidth, Mathf.Min(feet.y, head.y), halfWidth * 2, height);
            return rect.Overlaps(camera.pixelRect);
        }

        public static bool TryHealthBar(Camera camera, Combatant unit, out Rect rect)
        {
            rect = default(Rect);
            if (camera == null || unit == null) return false;
            Vector3 point = camera.WorldToScreenPoint(unit.VisualTop + Vector3.up * .3f);
            if (point.z <= camera.nearClipPlane || point.z >= camera.farClipPlane) return false;
            // Same frame PracticeHud draws (HUD space, y down), converted to pixels (y up).
            Rect frame = WorldBarLayout.Frame(unit, Vector2.zero);
            float sx = camera.pixelWidth / 1280f, sy = camera.pixelHeight / 720f;
            rect = new Rect(point.x + frame.x * sx, point.y - frame.yMax * sy, frame.width * sx, frame.height * sy);
            return rect.Overlaps(camera.pixelRect);
        }

        static float BodyDistance(Vector2 point, Rect rect)
        {
            float radius = rect.width * .5f;
            Vector2 nearest = new Vector2(rect.center.x,
                Mathf.Clamp(point.y, rect.yMin + radius, rect.yMax - radius));
            return Vector2.Distance(point, nearest) - radius;
        }
    }
}
