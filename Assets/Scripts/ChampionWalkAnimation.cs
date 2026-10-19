using UnityEngine;
namespace MidLane
{
    public sealed class ChampionWalkAnimation : MonoBehaviour
    {
        Transform rig, hips;
        AnimationClip clip;
        Transform[] joints;
        Vector3[] positions, scales;
        Quaternion[] rotations;
        Vector3 hipsRest;
        float time, referenceSpeed;
        public bool Walking { get; private set; }
        public float CycleTime { get { return time; } }
        public AnimationClip Clip { get { return clip; } }
        public void Configure(Transform root, Transform pelvis, AnimationClip walking, float normalSpeed = 3.35f)
        {
            rig = root; hips = pelvis; clip = walking; referenceSpeed = normalSpeed;
            joints = rig.GetComponentsInChildren<Transform>(); positions = new Vector3[joints.Length];
            scales = new Vector3[joints.Length]; rotations = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            { positions[i] = joints[i].localPosition; scales[i] = joints[i].localScale; rotations[i] = joints[i].localRotation; }
            hipsRest = rig.InverseTransformPoint(hips.position);
        }
        public void Tick(bool moving, float speed, float deltaTime)
        {
            if (!moving) { if (Walking) ResetPose(); return; }
            Walking = true; time = Mathf.Repeat(time + deltaTime * Mathf.Clamp(speed / referenceSpeed, 0.1f, 2.5f), clip.length);
            clip.SampleAnimation(rig.gameObject, time);
            // Restore the imported root/mesh/armature anchors; only joint animation drives the visual.
            for (int i = 0; i < joints.Length; i++)
                if (joints[i] == rig || joints[i].parent == rig)
                { joints[i].localPosition = positions[i]; joints[i].localRotation = rotations[i]; joints[i].localScale = scales[i]; }
            Vector3 pelvis = rig.InverseTransformPoint(hips.position); pelvis.x = hipsRest.x; pelvis.z = hipsRest.z;
            hips.position = rig.TransformPoint(pelvis);
        }
        public void ResetPose()
        {
            Walking = false; time = 0;
            for (int i = 0; i < joints.Length; i++)
            { joints[i].localPosition = positions[i]; joints[i].localRotation = rotations[i]; joints[i].localScale = scales[i]; }
        }
    }
}
