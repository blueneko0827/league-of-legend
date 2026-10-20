using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MidLane
{
    // Visual tuning tools for this practice edition (presentation only):
    //   F8  shows/hides the panel: camera, scale, light and shadows. Camera and light apply
    //       live; scale values apply when Practice restarts (button or F5).
    //   F9  shows/hides a reference capture from /References over the game view, to compare
    //       framing and proportions with League of Legends at the same position.
    // In the Editor, "Guardar JSON" writes the values to Assets/Resources/ScenePresentation.json.
    public sealed class VisualTuningPanel : MonoBehaviour
    {
        static VisualTuningPanel active;
        static readonly Rect PanelRect = new Rect(895, 112, 375, 528);
        static readonly Color Panel = new Color(0.035f, 0.055f, 0.085f, 0.96f);
        LaneWorld world;
        bool open, overlay;
        // Scale is edited on a copy and only committed by "Aplicar escala", because health
        // bars and click areas read these values live while the meshes are rebuilt on restart.
        float pendingMesh, pendingDummy, pendingTower;
        float overlayOpacity = .45f;
        readonly List<Texture2D> references = new List<Texture2D>();
        readonly List<string> referenceNames = new List<string>();
        int referenceIndex;
        bool referencesLoaded;
        string status = "";
        GUIStyle title, label, small, button;

        public void Initialize(LaneWorld lane) { world = lane; active = this; }
        void OnDestroy()
        {
            if (active == this) active = null;
            foreach (Texture2D texture in references) if (texture != null) Destroy(texture);
        }

        // Point in the 1280x720 HUD space.
        public static bool BlocksPointer(Vector2 hudPoint) { return active != null && active.open && PanelRect.Contains(hudPoint); }

        void Update()
        {
            if (world == null) return;
            var hud = world.GetComponent<PracticeHud>();
            if (hud != null && hud.LatencyFieldFocused) return;
            if (Input.GetKeyDown(KeyCode.F8)) { open = !open; if (open) ReadPendingScale(); }
            if (Input.GetKeyDown(KeyCode.F9)) { LoadReferences(); overlay = !overlay && references.Count > 0; }
        }

        void LoadReferences()
        {
            if (referencesLoaded) return;
            referencesLoaded = true;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../References"));
            if (!Directory.Exists(folder)) { status = "No se encontró la carpeta References"; return; }
            var files = new List<string>(Directory.GetFiles(folder, "imagelol*"));
            files.Sort();
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png") continue;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileName(file) };
                if (ImageConversion.LoadImage(texture, File.ReadAllBytes(file))) { references.Add(texture); referenceNames.Add(Path.GetFileName(file)); }
                else Destroy(texture);
            }
            if (references.Count == 0) status = "No hay capturas imagelol* en References";
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        }

        void OnGUI()
        {
            if (world == null || world.Presentation == null) return;
            // Drawn after (above) PracticeHud.
            GUI.depth = -10;
            if (overlay && references.Count > 0)
            {
                Color previous = GUI.color; GUI.color = new Color(1, 1, 1, overlayOpacity);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), references[referenceIndex], ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }
            if (!open) return;
            Styles();
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            Color previousColor = GUI.color; GUI.color = Panel;
            GUI.DrawTexture(PanelRect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            ScenePresentationConfig c = world.Presentation;
            float x = PanelRect.x + 12, y = PanelRect.y + 8;
            GUI.Label(new Rect(x, y, 350, 22), "Ajuste visual  [F8]", title); y += 26;

            GUI.changed = false;
            Header(ref y, x, "Cámara (en vivo)");
            c.camera_fov_degrees = Slider(ref y, x, "Campo de visión", c.camera_fov_degrees, 25, 45, "0.0");
            c.camera_pitch_degrees = Slider(ref y, x, "Inclinación", c.camera_pitch_degrees, 45, 70, "0.0");
            c.camera_yaw_degrees = Slider(ref y, x, "Giro", c.camera_yaw_degrees, 0, 90, "0.0");
            c.camera_zoom = Slider(ref y, x, "Distancia (zoom)", c.camera_zoom, .6f, 1.6f, "0.00");
            c.model_tilt_degrees = Slider(ref y, x, "Inclinación de modelos", c.model_tilt_degrees, 0, 20, "0.0");
            if (GUI.changed) { c.Clamp(); world.ApplyCameraPresentation(); }

            GUI.changed = false;
            Header(ref y, x, "Luz y sombras (en vivo)");
            c.sun_pitch_degrees = Slider(ref y, x, "Altura del sol", c.sun_pitch_degrees, 20, 89, "0");
            c.sun_yaw_degrees = Slider(ref y, x, "Dirección del sol", c.sun_yaw_degrees, 0, 360, "0");
            c.sun_intensity = Slider(ref y, x, "Intensidad del sol", c.sun_intensity, 0, 3, "0.00");
            c.shadow_strength = Slider(ref y, x, "Fuerza de sombra", c.shadow_strength, 0, 1, "0.00");
            float ambient = Mathf.Max(c.ambient_color.r, Mathf.Max(c.ambient_color.g, c.ambient_color.b));
            float newAmbient = Slider(ref y, x, "Luz ambiente", ambient, .05f, 1, "0.00");
            if (GUI.changed)
            {
                if (ambient > .001f && !Mathf.Approximately(ambient, newAmbient))
                { Color scaled = c.ambient_color * (newAmbient / ambient); scaled.a = 1; c.ambient_color = scaled; }
                c.Clamp(); world.ApplyLighting();
            }

            Header(ref y, x, "Escala (al reiniciar)");
            pendingMesh = Slider(ref y, x, "Tamaño de campeón", pendingMesh, .5f, 3, "0.00");
            pendingDummy = Slider(ref y, x, "Dummy / campeón", pendingDummy, .5f, 1.2f, "0.00");
            pendingTower = Slider(ref y, x, "Torre / campeón", pendingTower, 2.5f, 3.5f, "0.00");
            y += 4;
            if (GUI.Button(new Rect(x, y, 170, 24), "Aplicar escala (reinicia)", button))
            {
                c.visual_mesh_multiplier = pendingMesh; c.dummy_height_ratio = pendingDummy; c.tower_height_ratio = pendingTower;
                c.Clamp(); ReadPendingScale(); world.Restart(); status = "Escala aplicada";
            }
            if (GUI.Button(new Rect(x + 178, y, 170, 24), "Restaurar del JSON", button))
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(ScenePresentationConfig.Load()), c);
                ReadPendingScale(); world.Restart(); world.ApplyCameraPresentation(); world.ApplyLighting();
                status = "Valores del JSON restaurados";
            }
            y += 30;

            Header(ref y, x, "Referencia de LoL  [F9]");
            if (GUI.Button(new Rect(x, y, 110, 22), overlay ? "Ocultar" : "Mostrar", button)) { LoadReferences(); overlay = !overlay && references.Count > 0; }
            if (GUI.Button(new Rect(x + 116, y, 110, 22), "Siguiente", button) && references.Count > 0) referenceIndex = (referenceIndex + 1) % references.Count;
            GUI.Label(new Rect(x + 234, y + 2, 120, 20), references.Count > 0 ? referenceNames[referenceIndex] : "", label);
            y += 26;
            overlayOpacity = Slider(ref y, x, "Opacidad", overlayOpacity, .1f, .9f, "0.00");

#if UNITY_EDITOR
            if (GUI.Button(new Rect(x, y + 2, 170, 24), "Guardar JSON", button)) Save(c);
#endif
            GUI.Label(new Rect(x, y + 30, 350, 34), status, small);
            GUI.matrix = previousMatrix;
        }

        void ReadPendingScale()
        {
            ScenePresentationConfig c = world.Presentation;
            pendingMesh = c.visual_mesh_multiplier; pendingDummy = c.dummy_height_ratio; pendingTower = c.tower_height_ratio;
        }

        void Header(ref float y, float x, string text)
        {
            y += 4; GUI.Label(new Rect(x, y, 350, 18), text, new GUIStyle(label) { fontStyle = FontStyle.Bold }); y += 18;
        }

        float Slider(ref float y, float x, string text, float value, float min, float max, string format)
        {
            GUI.Label(new Rect(x, y, 150, 18), text, label);
            float result = GUI.HorizontalSlider(new Rect(x + 152, y + 4, 140, 14), value, min, max);
            GUI.Label(new Rect(x + 300, y, 55, 18), result.ToString(format), label);
            y += 19; return result;
        }

#if UNITY_EDITOR
        void Save(ScenePresentationConfig config)
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/ScenePresentation.json"));
                File.WriteAllText(path, JsonUtility.ToJson(config, true));
                // Without a reimport, Resources.Load would keep returning the previous text.
                UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/ScenePresentation.json");
                status = "Guardado en Assets/Resources/ScenePresentation.json";
            }
            catch (System.Exception error) { status = "No se pudo guardar: " + error.Message; }
        }
#endif
    }
}
