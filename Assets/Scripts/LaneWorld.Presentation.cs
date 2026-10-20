using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Presentation only: light, ambient and shadows read ScenePresentation.json and can be
    // re-applied live from the tuning panel (F8). Gameplay never reads these values.
    public sealed partial class LaneWorld
    {
        public Light Sun { get; private set; }

        public void ApplyLighting()
        {
            ScenePresentationConfig config = Presentation;
            if (Sun != null)
            {
                Sun.color = config.sun_color;
                Sun.intensity = config.sun_intensity;
                Sun.shadowStrength = config.shadow_strength;
                Sun.transform.rotation = Quaternion.Euler(config.sun_pitch_degrees, config.sun_yaw_degrees, 0);
            }
            // The scene was saved with skybox ambient, which ignores ambientLight. Use a flat
            // ambient color and refresh the probe so lit surfaces pick it up immediately.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.ambientLight = config.ambient_color;
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(config.ambient_color);
            RenderSettings.ambientProbe = probe;
        }

        public void ApplyCameraPresentation()
        {
            if (View == null) return;
            var camera = View.GetComponent<IsometricCamera>();
            if (camera != null) camera.ApplyPresentation();
        }
    }
}
