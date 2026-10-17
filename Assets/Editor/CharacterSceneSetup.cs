using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Refresh the open gameplay scene after the artwork is imported, without reopening it.
[InitializeOnLoad]
public static class CharacterSceneSetup
{
    private const string Revision = "RPG.CharacterArtwork.v4";
    private const string HeroPath = "Assets/Characters/Hero.png";
    private const string BackgroundPath = "Assets/Characters/MedievalTown.png";

    static CharacterSceneSetup()
    {
        EditorApplication.delayCall += RefreshAfterImport;
    }

    private static void RefreshAfterImport()
    {
        if (SessionState.GetBool(Revision, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (AssetDatabase.LoadAssetAtPath<Sprite>(HeroPath) == null ||
            AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath) == null)
            return;
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
        if (!scene.IsValid() || !scene.isLoaded)
            return;
        Apply(scene);
        SessionState.SetBool(Revision, true);
    }

    [MenuItem("Tools/RPG/Refresh Character And Background")]
    private static void RefreshCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path == "Assets/Scenes/SampleScene.unity")
            Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        var hero = AssetDatabase.LoadAssetAtPath<Sprite>(HeroPath);
        var background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (hero == null || background == null)
            return;
        var player = FindOrCreate(scene, "Player");
        var playerRenderer = player.GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
            playerRenderer = player.AddComponent<SpriteRenderer>();
        playerRenderer.sprite = hero;
        playerRenderer.sortingOrder = 1;
        var backdrop = FindOrCreate(scene, "Background");
        var backgroundRenderer = backdrop.GetComponent<SpriteRenderer>();
        if (backgroundRenderer == null)
            backgroundRenderer = backdrop.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = background;
        backgroundRenderer.sortingOrder = -100;
        backdrop.transform.position = new Vector3(0, 2, 1);
        backdrop.transform.localScale = Vector3.one;
        var movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
            movement = player.AddComponent<PlayerMovement>();
        foreach (var root in scene.GetRootGameObjects())
        {
            var camera = root.GetComponent<Camera>();
            if (camera == null || !camera.CompareTag("MainCamera"))
                continue;
            camera.orthographic = true;
            camera.orthographicSize = 3.2f;
            camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);
            camera.transform.rotation = Quaternion.identity;
            movement.Configure(backgroundRenderer, camera);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = player;
    }

    private static GameObject FindOrCreate(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        var result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        Undo.RegisterCreatedObjectUndo(result, "Add " + name);
        return result;
    }
}
