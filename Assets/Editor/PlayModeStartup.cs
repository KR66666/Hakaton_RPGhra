using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Start Play Mode from the menu, regardless of the currently edited scene.</summary>
[InitializeOnLoad]
public static class PlayModeStartup
{
    private const string MenuPath = "Assets/Scenes/MainMenu.unity";

    static PlayModeStartup()
    {
        // Wait until the AssetDatabase is ready after script import.
        EditorApplication.delayCall += Configure;
    }

    [MenuItem("Tools/RPG/Start Play Mode From Main Menu")]
    private static void Configure()
    {
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath);
        if (menu == null)
        {
            Debug.LogWarning($"Play Mode startup scene is missing: {MenuPath}");
            return;
        }

        EditorSceneManager.playModeStartScene = menu;
    }
}
