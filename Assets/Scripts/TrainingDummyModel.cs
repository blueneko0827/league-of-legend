using UnityEngine;

namespace MidLane
{
    // Presentation only. Sampling the supplied rig never moves the gameplay root or hitbox.
    public sealed class TrainingDummyModel : MonoBehaviour
    {
        const string ResourceRoot = "Models/TrainingDummy/";
        public AnimationClip IdleClip { get; private set; }
        public AnimationClip HitClip { get; private set; }
        public float LastHitAt { get; private set; } = float.NegativeInfinity;
        public bool IsHitPlaying { get { return HitClip != null && Time.time < LastHitAt + HitClip.length; } }
        LaneWorld world;
        Transform[] joints;
        Transform rootBone;
        Vector3 rootRest;
        Vector3[] previousPositions, previousScales;
        Quaternion[] previousRotations;
        public static TrainingDummyModel Create(LaneWorld world, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(ResourceRoot + "Dummy");
            if (prefab == null) throw new System.InvalidOperationException("Falta el modelo de práctica: " + ResourceRoot + "Dummy");
            // Same structure as champions: Model > Geometry (camera tilt) > imported rig.
            // The rig keeps its own transform for animation sampling; the gameplay root and
            // its collider are never scaled or tilted.
            var geometry = new GameObject("Geometry").transform; geometry.SetParent(parent, false);
            var visual = Instantiate(prefab, geometry, false); visual.name = "Training dummy model";
            var animation = visual.AddComponent<TrainingDummyModel>(); animation.Initialize(world);
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); renderer.BakeMesh(mesh); renderer.localBounds = mesh.bounds; Destroy(mesh);
            }
            // Height follows the champion's visual height (dummy_height_ratio) instead of a
            // fixed 1.8 units, so the dummy keeps the Practice Tool proportion beside Irelia.
            PracticeModels.FitHeight(visual.transform, geometry, world.Presentation.DummyVisualHeight, true);
            geometry.gameObject.AddComponent<CameraFacingVisualTilt>().Initialize(world);
            UnitShadows.Attach(world, visual);
            return animation;
        }
        void Initialize(LaneWorld lane)
        {
            world = lane; IdleClip = Resources.Load<AnimationClip>(ResourceRoot + "Idle"); HitClip = Resources.Load<AnimationClip>(ResourceRoot + "Hit");
            IdleClip.SampleAnimation(gameObject, 0);
            joints = GetComponentsInChildren<Transform>();
            previousPositions = new Vector3[joints.Length]; previousScales = new Vector3[joints.Length]; previousRotations = new Quaternion[joints.Length];
            foreach (Transform joint in joints) if (joint.name == "Root") { rootBone = joint; rootRest = joint.localPosition; break; }
        }
        public void PlayHit() { LastHitAt = Time.time; }
        void LateUpdate()
        {
            if (world == null || world.Paused) return;
            for (int i = 0; i < joints.Length; i++)
            { previousPositions[i] = joints[i].localPosition; previousRotations[i] = joints[i].localRotation; previousScales[i] = joints[i].localScale; }
            bool hit = IsHitPlaying;
            (hit ? HitClip : IdleClip).SampleAnimation(gameObject, hit ? Time.time - LastHitAt : 0);
            if (rootBone != null)
            { Vector3 p = rootBone.localPosition; p.x = rootRest.x; p.z = rootRest.z; rootBone.localPosition = p; }
            float blend = 1 - Mathf.Exp(-Time.deltaTime / .04f);
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].localPosition = Vector3.Lerp(previousPositions[i], joints[i].localPosition, blend);
                joints[i].localRotation = Quaternion.Slerp(previousRotations[i], joints[i].localRotation, blend);
                joints[i].localScale = Vector3.Lerp(previousScales[i], joints[i].localScale, blend);
            }
        }
    }
}
