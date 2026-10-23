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
        // Informative only: recalculated from the FOV and camera_zoom when loading.
        public float camera_distance = 34.3084457f;
        // Multiplies the derived camera distance. 1 keeps the original framing.
        public float camera_zoom = 1;

        public float model_tilt_degrees = 15;
        public float melee_height_ratio = .6f;
        public float caster_height_ratio = .6f;
        public float cannon_height_ratio = .6f;
        public float tower_height_ratio = 2.7f;
        // Training dummy height relative to the champion's visual height. Measured from the
        // LoL Practice Tool references (References/imagelol3.jpg): a dummy is ~0.86 of Irelia.
        public float dummy_height_ratio = .86f;

        // Lighting only affects lit surfaces (ground, rocks). Champion, dummy and tower
        // materials are unlit, so their colors stay as authored; they only cast shadows.
        public float sun_pitch_degrees = 50;
        public float sun_yaw_degrees = 120;
        public float sun_intensity = 1;
        public Color sun_color = new Color(1, .96f, .88f, 1);
        public float shadow_strength = .8f;
        public Color ambient_color = new Color(.5f, .56f, .68f, 1);
        public bool unit_shadows = true;
        // Official Riot particle systems (basic attack, passive, Q). vfx_scale multiplies their
        // size around the attachment point; 1 keeps the proportion of the enlarged champion.
        public bool official_vfx = true;
        public float vfx_scale = 1;
        // Size of the W aiming indicator. The champions are drawn 1.65 times larger than the
        // gameplay distances, so an indicator exactly as long as the W's range looks short next
        // to Irelia; 1.3 gives it LoL's proportion to her (measured on in-game footage: its point
        // is 3.9 times her height away). 1 = exactly the W's hit range and width.
        public float w_indicator_scale = 1.3f;
        // R as in LoL's quick cast: pressing R sends it toward the cursor, with no aiming arrow (the
        // game footage shows none). true = the previous mode: hold R to aim with an arrow, release to cast.
        public bool r_aim_indicator = false;
        // Extra height of the champions' overhead bar, as a fraction of the champion's visual
        // height. The official models (hair, floating blades) rise above the selection height,
        // so without it the bar covers Irelia's face; LoL leaves a gap above the head.
        public float champion_bar_lift = .36f;

        public float ChampionVisualHeight { get { return TowerToTowerRules.ChampionSelectionHeight * visual_mesh_multiplier; } }
        public float MinionVisualHeight(MinionType type)
        { return ChampionVisualHeight * (type == MinionType.Melee ? melee_height_ratio : type == MinionType.Caster ? caster_height_ratio : cannon_height_ratio); }
        public float TowerVisualHeight { get { return ChampionVisualHeight * tower_height_ratio; } }
        public float DummyVisualHeight { get { return ChampionVisualHeight * dummy_height_ratio; } }

        public float CameraMinDistance { get { return 8.5f / Mathf.Tan(camera_fov_degrees * Mathf.Deg2Rad / 2); } }
        public float CameraMaxDistance { get { return 10 / Mathf.Tan(camera_fov_degrees * Mathf.Deg2Rad / 2); } }
        public float CameraDistance { get { return CameraMaxDistance * camera_zoom; } }

        public static ScenePresentationConfig Load()
        {
            var config = new ScenePresentationConfig();
            TextAsset asset = Resources.Load<TextAsset>("ScenePresentation");
            if (asset != null) JsonUtility.FromJsonOverwrite(asset.text, config);
            config.Clamp();
            return config;
        }

        // Shared by Load and the in-game tuning panel (F8), so both accept the same ranges.
        public void Clamp()
        {
            visual_mesh_multiplier = Mathf.Clamp(visual_mesh_multiplier, 0.1f, 4);
            lane_width_unity = Mathf.Clamp(lane_width_unity, 15, 18);
            camera_fov_degrees = Mathf.Clamp(camera_fov_degrees, 25, 45);
            camera_pitch_degrees = Mathf.Clamp(camera_pitch_degrees, 45, 70);
            camera_zoom = Mathf.Clamp(camera_zoom, .6f, 1.6f);
            model_tilt_degrees = Mathf.Clamp(model_tilt_degrees, 0, 20);
            melee_height_ratio = Mathf.Clamp(melee_height_ratio, .4f, .8f);
            caster_height_ratio = Mathf.Clamp(caster_height_ratio, .4f, .8f);
            cannon_height_ratio = Mathf.Clamp(cannon_height_ratio, .4f, 1);
            tower_height_ratio = Mathf.Clamp(tower_height_ratio, 2.5f, 3.5f);
            dummy_height_ratio = Mathf.Clamp(dummy_height_ratio, .5f, 1.2f);
            sun_pitch_degrees = Mathf.Clamp(sun_pitch_degrees, 20, 89);
            sun_intensity = Mathf.Clamp(sun_intensity, 0, 3);
            shadow_strength = Mathf.Clamp01(shadow_strength);
            vfx_scale = Mathf.Clamp(vfx_scale, .5f, 2);
            w_indicator_scale = Mathf.Clamp(w_indicator_scale, .5f, 2);
            champion_bar_lift = Mathf.Clamp(champion_bar_lift, 0, 1);
            // Fixed at the farthest framing previously reachable with the scroll wheel, times the zoom.
            camera_distance = CameraDistance;
        }
    }
}
