using UnityEditor;
using VRBuilder.Core.Editor.Debug;

namespace VRBuilder.Core.Editor.BuilderMenu
{
    internal static class SceneObjectRegistryVisualizerMenuEntry
    {
        /// <summary>
        /// Allows to open the URL to Creator Documentation.
        /// </summary>
        [MenuItem("Tools/VR Builder/Developer/Scene Object Registry Visualizer", false, 1000)]
        private static void OpenSceneObjectRegistryVisualizer()
        {
            EditorWindow.GetWindow<SceneObjectRegistryV2EditorWindow>().Show();
        }
    }
}
