using System;
using UnityEngine;

namespace MidLane
{
    // Loaded once per scene. Presentation scale never reaches a Combatant transform or radius.
    [Serializable]
    public sealed class ScenePresentationConfig
    {
        public float visual_mesh_multiplier = TowerToTowerRules.VisualMeshMultiplier;
        public float lane_width_unity = TowerToTowerRules.HalfWidth * 2;
        public float camera_fov_degrees = 32.5f;
        public float camera_pitch_degrees = 55;
        public float camera_yaw_degrees = 45;
        public float camera_distance = 34.3084457f;

        public float model_tilt_degrees = 15;
        public float melee_height_ratio = .6f;
        public float caster_height_ratio = .6f;
        public float cannon_height_ratio = .6f;
        public float tower_height_ratio = 3f;
        public float ChampionVisualHeight { get { return TowerToTowerRules.ChampionSelectionHeight * visual_mesh_multiplier; } }
        public float MinionVisualHeight(MinionType type)
        { return ChampionVisualHeight * (type == MinionType.Melee ? melee_height_ratio : type == MinionType.Caster ? caster_height_ratio : cannon_height_ratio); }
        public float TowerVisualHeight { get { return ChampionVisualHeight * tower_height_ratio; } }

        public float CameraMinDistance { get { return 8.5f / Mathf.Tan(camera_fov_degrees * Mathf.Deg2Rad / 2); } }
        public float CameraMaxDistance { get { return 10 / Mathf.Tan(camera_fov_degrees * Mathf.Deg2Rad / 2); } }

        public static ScenePresentationConfig Load()
        {
            var config = new ScenePresentationConfig();
            TextAsset asset = Resources.Load<TextAsset>("ScenePresentation");
            if (asset != null) JsonUtility.FromJsonOverwrite(asset.text, config);
            config.visual_mesh_multiplier = Mathf.Clamp(config.visual_mesh_multiplier, 0.1f, 4);
            config.lane_width_unity = Mathf.Clamp(config.lane_width_unity, 15, 18);
            config.camera_fov_degrees = Mathf.Clamp(config.camera_fov_degrees, 30, 35);
            config.camera_pitch_degrees = Mathf.Clamp(config.camera_pitch_degrees, 45, 65);
            config.model_tilt_degrees = Mathf.Clamp(config.model_tilt_degrees, 0, 20);
            config.melee_height_ratio = Mathf.Clamp(config.melee_height_ratio, .4f, .8f);
            config.caster_height_ratio = Mathf.Clamp(config.caster_height_ratio, .4f, .8f);
            config.cannon_height_ratio = Mathf.Clamp(config.cannon_height_ratio, .4f, 1);
            config.tower_height_ratio = Mathf.Clamp(config.tower_height_ratio, 2.5f, 3.5f);
            // Fixed at the farthest framing previously reachable with the scroll wheel.
            config.camera_distance = config.CameraMaxDistance;
            return config;
        }
    }
}
