using UnityEngine;

namespace MidLane
{
    public sealed class SettModelAssets : MonoBehaviour
    {
        public const string Root = "Models/SettOfficial/";
        public Material BodyMaterial { get { return Resources.Load<Material>(Root + "Material0"); } }
        public GameObject CreateBody(Transform parent)
        {
            var rig = Instantiate(Resources.Load<GameObject>(Root + "Model"), parent, false);
            rig.name = "SettOfficial";
            Resources.Load<AnimationClip>(Root + "Idle").SampleAnimation(rig, 0);
            foreach (var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); renderer.BakeMesh(mesh); renderer.localBounds = mesh.bounds;
                renderer.updateWhenOffscreen = true; Destroy(mesh);
            }
            foreach (Collider collider in rig.GetComponentsInChildren<Collider>())
            { collider.enabled = false; Destroy(collider); }
            return rig;
        }
    }
}
