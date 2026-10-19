using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MidLane.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/MidLane.unity";
        const string PipelinePath = "Assets/Rendering/MidLaneURP.asset";
        static ProjectSetup() { EditorApplication.delayCall += EnsureSetup; }

        [MenuItem("MidLane/Preparar proyecto")]
        public static void EnsureSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += EnsureSetup; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory("Assets/Rendering"); Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Scenes"); AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                // LoadBuiltinRendererData also fills URP's required renderer resources.
                pipeline = UniversalRenderPipelineAsset.Create();
                pipeline.LoadBuiltinRendererData(RendererType.UniversalRenderer);
                pipeline.name = "MidLane URP"; AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            if (AssetDatabase.LoadAssetAtPath<SandboxSettings>("Assets/Resources/SandboxSettings.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SandboxSettings>(), "Assets/Resources/SandboxSettings.asset");
            if (AssetDatabase.LoadAssetAtPath<BladeTuning>("Assets/Resources/BladeTuning.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BladeTuning>(), "Assets/Resources/BladeTuning.asset");
            PlayerSettings.companyName = "MidLane"; PlayerSettings.productName = "MidLane Practice Visual";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            // The prototype uses the built-in Input Manager, without an extra package or action map.
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var inputMode = playerSettings.FindProperty("activeInputHandler");
            if (inputMode != null && inputMode.intValue != 0)
            { inputMode.intValue = 0; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                GameObject root = new GameObject("MidLane Practice Visual"); SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<LaneWorld>(); EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
            }
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            // Runtime-created primitive materials still need their shader included in a Windows player.
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit"); bool included = false;
            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == lit) included = true;
            if (!included && lit != null)
            { int index = shaders.arraySize; shaders.InsertArrayElementAtIndex(index); shaders.GetArrayElementAtIndex(index).objectReferenceValue = lit; graphics.ApplyModifiedPropertiesWithoutUndo(); }
            AssetDatabase.SaveAssets();
            if (SceneManager.GetActiveScene().path == "" && !SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("MidLane/Abrir escena")]
        public static void OpenScene()
        {
            EnsureSetup();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("MidLane/Compilar Windows")]
        public static void BuildWindows()
        {
            EnsureSetup(); Directory.CreateDirectory("Builds/Windows");
            var options = new BuildPlayerOptions { scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/MidLanePracticeVisual.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("La compilación Windows falló: " + report.summary.result);
            Debug.Log("MidLane listo en Builds/Windows/MidLanePracticeVisual.exe");
        }
    }
}
