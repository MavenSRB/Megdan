using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuardsDigital.EditorTools
{
    public static class CurrentPrototypeLauncher
    {
        public static void OpenCurrentSceneAndEnterPlayMode()
        {
            EditorApplication.delayCall += OpenCurrentSceneAndEnterPlayModeWhenReady;
        }

        private static void OpenCurrentSceneAndEnterPlayModeWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenCurrentSceneAndEnterPlayModeWhenReady;
                return;
            }

            string scenePath = GetFirstEnabledBuildScenePath();
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogError("No enabled scene is configured in Build Settings.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError($"Current prototype scene was not found: {scenePath}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.isPlaying = true;
        }

        private static string GetFirstEnabledBuildScenePath()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    return scene.path;
                }
            }

            return string.Empty;
        }
    }
}
