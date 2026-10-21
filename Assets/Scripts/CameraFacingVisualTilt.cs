using UnityEngine;

namespace MidLane
{
    // A presentation transform outside the sampled rig; never rotates gameplay circles.
    [DefaultExecutionOrder(200)]
    public sealed class CameraFacingVisualTilt : MonoBehaviour
    {
        LaneWorld world;
        Vector3 restPosition;
        Quaternion restRotation;
        Renderer[] geometry;
        public bool SampleDriven { get; set; }
        int poseRevision, appliedRevision = -1;
        Quaternion lastParentRotation, lastViewRotation;
        float lastY, lastDegrees;
        public void InvalidatePose() { poseRevision++; }
        public float ReferenceHeight { get; private set; }
        public void Initialize(LaneWorld lane, bool excludeBlades = false)
        {
            world = lane; restPosition = transform.localPosition; restRotation = transform.localRotation;
            geometry = System.Array.FindAll(GetComponentsInChildren<Renderer>(), r => r.enabled && (!excludeBlades || r.name != "AnimatedBlades"));
            ReferenceHeight = BoundsOfGeometry().size.y;
            Apply();
        }
        public static Quaternion WorldRotation(Camera view, float degrees)
        {
            if (view == null || degrees == 0) return Quaternion.identity;
            // Pitch the top away from the camera so the chest faces the elevated view.
            // Pitching the top toward it hides the torso and compresses the silhouette.
            Vector3 away = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            float angle = degrees * Mathf.Deg2Rad;
            return Quaternion.FromToRotation(Vector3.up, Vector3.up * Mathf.Cos(angle) + away * Mathf.Sin(angle));
        }
        public void Apply()
        {
            if (world == null || transform.parent == null) return;
            var parent = transform.parent;
            Quaternion viewRotation = world.View != null ? world.View.transform.rotation : Quaternion.identity;
            if (SampleDriven && appliedRevision == poseRevision && lastParentRotation == parent.rotation &&
                lastViewRotation == viewRotation && lastY == parent.position.y && lastDegrees == world.Presentation.model_tilt_degrees) return;
            appliedRevision = poseRevision; lastParentRotation = parent.rotation; lastViewRotation = viewRotation;
            lastY = parent.position.y; lastDegrees = world.Presentation.model_tilt_degrees;
            Quaternion lean = Quaternion.Inverse(parent.rotation) * WorldRotation(world.View, world.Presentation.model_tilt_degrees) * parent.rotation;
            transform.localRotation = lean * restRotation;
            transform.localPosition = lean * restPosition;
            // Lift only penetrations. Airborne frames retain their authored animation.
            float lift = Mathf.Max(0, parent.position.y - BoundsOfGeometry().min.y);
            transform.localPosition += parent.InverseTransformVector(Vector3.up * lift);
        }
        Bounds BoundsOfGeometry()
        {
            if (geometry.Length == 0) return new Bounds(transform.position, Vector3.zero);
            Bounds bounds = geometry[0].bounds;
            foreach (var renderer in geometry) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        void LateUpdate() { if (world != null && !world.Paused) Apply(); }
        public static Bounds RotateBounds(Bounds bounds, Vector3 pivot, Quaternion rotation)
        {
            var result = new Bounds(pivot + rotation * (bounds.center - pivot), Vector3.zero);
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                result.Encapsulate(pivot + rotation * (bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)) - pivot));
            return result;
        }
    }
}
