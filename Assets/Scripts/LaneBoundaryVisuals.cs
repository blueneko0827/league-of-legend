using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    public static class LaneBoundaryVisuals
    {
        public static void Build(LaneWorld world, Transform parent)
        {
            float x = TowerToTowerRules.HalfLength, z = world.Settings.halfWidth;
            Draw(world, parent, "Playable boundary", new[] { new Vector3(-x, 0, -z), new Vector3(-x, 0, z),
                new Vector3(x, 0, z), new Vector3(x, 0, -z), new Vector3(-x, 0, -z) }, new Color(0.32f, 0.42f, 0.32f), 0.035f);
            for (int team = 0; team < 2; team++)
            {
                int forward = team == 0 ? 1 : -1;
                float radius = TowerToTowerRules.TowerAggroRadius;
                float angle = Mathf.Asin(Mathf.Min(1, z / radius));
                var points = new List<Vector3>();
                for (int i = 0; i <= 48; i++)
                {
                    float a = Mathf.Lerp(-angle, angle, i / 48f);
                    points.Add(new Vector3(-forward * x + forward * Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
                }
                Draw(world, parent, "Turret defensive arc " + (Team)team, points.ToArray(),
                    team == 0 ? new Color(0.1f, 0.52f, 0.7f) : new Color(0.65f, 0.2f, 0.22f), 0.045f);
            }
        }
        static void Draw(LaneWorld world, Transform parent, string name, Vector3[] points, Color color, float width)
        {
            var visual = new GameObject(name); visual.transform.SetParent(parent, false);
            for (int i = 0; i < points.Length; i++) points[i].y = 0.035f;
            var line = visual.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.positionCount = points.Length; line.SetPositions(points); line.widthMultiplier = width;
            line.sharedMaterial = world.Material(color); line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        }
    }
}
