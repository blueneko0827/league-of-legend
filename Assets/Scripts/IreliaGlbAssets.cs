using UnityEngine;

namespace MidLane
{
    public sealed class IreliaGlbAssets : MonoBehaviour
    {
        public const string Root = "Models/IreliaOfficial/";
        public GameObject CreateBody(Transform parent)
        {
            GameObject visual = Instantiate(Resources.Load<GameObject>(Root + "Irelia"), parent, false);
            visual.name = "IreliaOfficial";
            AnimationClip idle = Resources.Load<AnimationClip>(Root + "Idle");
            idle.SampleAnimation(visual, 0);
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked = new Mesh(); renderer.BakeMesh(baked); renderer.localBounds = baked.bounds; Destroy(baked);
            }
            var rig = visual.AddComponent<IreliaRigAnimation>();
            rig.Configure(idle,
                Resources.Load<AnimationClip>(Root + "Run"), Resources.Load<AnimationClip>(Root + "Attack"),
                Resources.Load<AnimationClip>(Root + "WCharge"), Resources.Load<AnimationClip>(Root + "WRelease"),
                Resources.Load<AnimationClip>(Root + "WRecovery"), Resources.Load<AnimationClip>(Root + "Attack2"),
                Resources.Load<AnimationClip>(Root + "QDash"), Resources.Load<AnimationClip>(Root + "QToIdle"),
                Resources.Load<AnimationClip>(Root + "QToRun"), Resources.Load<AnimationClip>(Root + "QChain"));
            rig.ConfigureEAndR(Resources.Load<AnimationClip>(Root + "EFirst"), Resources.Load<AnimationClip>(Root + "EFirstRun"),
                Resources.Load<AnimationClip>(Root + "EFirstToIdle"), Resources.Load<AnimationClip>(Root + "ESecond"),
                Resources.Load<AnimationClip>(Root + "ESecondRun"), Resources.Load<AnimationClip>(Root + "ESecondToIdle"),
                Resources.Load<AnimationClip>(Root + "RCast"), Resources.Load<AnimationClip>(Root + "RHold"),
                Resources.Load<AnimationClip>(Root + "RToIdle"), Resources.Load<AnimationClip>(Root + "RToRun"));
            return visual;
        }
        public GameObject CreateSpellBlade(Transform parent, string label, Vector3 position)
        {
            var root = new GameObject(label); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            var visual = new GameObject("IreliaBlade"); visual.transform.SetParent(root.transform, false);
            Mesh mesh = Resources.Load<Mesh>(Root + "SpellBlade");
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>(Root + "Material2");
            float scale = 0.9f / mesh.bounds.size.y;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = -mesh.bounds.center * scale;
            return root;
        }
    }
}
