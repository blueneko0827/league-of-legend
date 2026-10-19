using UnityEngine;

namespace MidLane
{
    public static class OfficialLaneUnitAssets
    {
        public const string Root = "Models/LaneUnitsOfficial/";
        public static GameObject Create(string name, Transform parent)
        {
            string folder = Root + name + "/";
            var prefab = Resources.Load<GameObject>(folder + "Model");
            if (prefab == null) throw new System.InvalidOperationException("Falta el modelo oficial: " + name);
            var rig = Object.Instantiate(prefab, parent, false); rig.name = name + "Official";
            Resources.Load<AnimationClip>(folder + "Idle").SampleAnimation(rig, 0);
            foreach (var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); renderer.BakeMesh(mesh); renderer.localBounds = mesh.bounds;
                renderer.updateWhenOffscreen = true; Object.Destroy(mesh);
            }
            // Only the actor root owns gameplay collision and targeting.
            foreach (var collider in rig.GetComponentsInChildren<Collider>())
            { collider.enabled = false; Object.Destroy(collider); }
            return rig;
        }
    }
}
