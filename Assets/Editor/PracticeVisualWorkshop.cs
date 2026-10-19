using UnityEditor;
using UnityEngine;

namespace MidLane.Editor
{
    // These menus belong only to the artist's copy, not to the complete simulator.
    public static class PracticeVisualWorkshop
    {
        [MenuItem("Visual Workshop/Abrir práctica")]
        public static void OpenPractice() { ProjectSetup.OpenScene(); }

        [MenuItem("Visual Workshop/Seleccionar configuración visual")]
        public static void SelectPresentation()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/ScenePresentation.json");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Visual Workshop/Abrir guía de trabajo")]
        public static void OpenGuide() { Application.OpenURL("file:///" + System.IO.Path.GetFullPath("ARTIST_GUIDE.md").Replace('\\', '/')); }
    }
}
