using UnityEngine;

namespace MidLane
{
    public static class MinionVisuals
    {
        public static void Create(LaneWorld world, Transform parent, Team team, MinionType type)
        {
            string name = (team == Team.Blue ? "Blue" : "Red") +
                (type == MinionType.Melee ? "Melee" : type == MinionType.Caster ? "Caster" : "Cannon");
            var geometry = new GameObject("Geometry").transform; geometry.SetParent(parent, false);
            GameObject rig = OfficialLaneUnitAssets.Create(name, geometry);
            PracticeModels.FitHeight(geometry, parent, world.Presentation.MinionVisualHeight(type), true);
            geometry.gameObject.AddComponent<CameraFacingVisualTilt>().Initialize(world);
            parent.gameObject.AddComponent<MinionModelAnimation>().Configure(rig, name);
        }
    }
}
