using System;
using UnityEngine;

namespace MidLane
{
    public static class IreliaRGeometry
    {
        [Serializable] sealed class WallProfile { public Vector2[] points; }
        static Vector2[] localPoints;
        public static Vector3[] WallPoints(Vector3 impact, Vector3 direction)
        {
            if (localPoints == null)
            {
                var source = Resources.Load<TextAsset>("LeagueData/IreliaRWall");
                localPoints = JsonUtility.FromJson<WallProfile>(source.text).points;
            }
            Vector3 forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            var points = new Vector3[localPoints.Length];
            for (int i = 0; i < points.Length; i++) points[i] = impact + side * localPoints[i].x + forward * localPoints[i].y;
            return points;
        }
    }
}
