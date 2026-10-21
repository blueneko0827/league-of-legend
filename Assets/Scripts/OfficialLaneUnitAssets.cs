using UnityEngine;

namespace MidLane
{
    public static class OfficialLaneUnitAssets
    {
        public const string Root = "Models/LaneUnitsOfficial/";
        public sealed class AssetSet
        {
            public GameObject Model;
            public AnimationClip Idle, Run, Attack;
        }
        static readonly System.Collections.Generic.Dictionary<string, AssetSet> cache =
            new System.Collections.Generic.Dictionary<string, AssetSet>();
        public static AssetSet Load(string name)
        {
            if (cache.TryGetValue(name, out var assets) && assets.Model != null) return assets;
            string folder = Root + name + "/";
            assets = new AssetSet { Model = Resources.Load<GameObject>(folder + "Model"),
                Idle = Resources.Load<AnimationClip>(folder + "Idle") };
            if (!name.EndsWith("Tower"))
            { assets.Run = Resources.Load<AnimationClip>(folder + "Run"); assets.Attack = Resources.Load<AnimationClip>(folder + "Attack"); }
            if (assets.Model == null || assets.Idle == null) throw new System.InvalidOperationException("Falta el modelo oficial: " + name);
            cache[name] = assets; return assets;
        }
        public static GameObject Create(string name, Transform parent)
        {
            var assets = Load(name);
            var rig = Object.Instantiate(assets.Model, parent, false); rig.name = name + "Official";
            assets.Idle.SampleAnimation(rig, 0);
            foreach (var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh(); renderer.BakeMesh(mesh); renderer.localBounds = mesh.bounds;
                renderer.updateWhenOffscreen = true; Object.Destroy(mesh);
            }
            // Only the actor root owns gameplay collision and targeting.
            foreach (var collider in rig.GetComponentsInChildren<Collider>(true))
            { collider.enabled = false; Object.Destroy(collider); }
            return rig;
        }
    }
}
