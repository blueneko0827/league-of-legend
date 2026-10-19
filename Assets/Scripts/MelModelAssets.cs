using UnityEngine;

namespace MidLane
{
    public static class MelModelAssets
    {
        public const string Root = "Models/MelOfficial/";
        public static GameObject CreateBody(Transform parent)
        {
            var rig=Object.Instantiate(Resources.Load<GameObject>(Root+"Mel"),parent,false);
            rig.name="MelOfficial";
            Resources.Load<AnimationClip>(Root+"Idle").SampleAnimation(rig,0);
            foreach (SkinnedMeshRenderer renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh(); renderer.BakeMesh(mesh); renderer.localBounds=mesh.bounds;
                renderer.updateWhenOffscreen=true; Object.Destroy(mesh);
            }
            return rig;
        }
    }
}
