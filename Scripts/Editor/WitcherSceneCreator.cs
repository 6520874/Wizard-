using WitcherGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WitcherSceneCreator
{
    private const string ScenePath = "Assets/Scenes/WitcherHuntDemo.unity";

    [MenuItem("Tools/Witcher Hunt/Create Playable Scene")]
    public static void CreatePlayableScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateBackground();
        CreatePlayer();
        CreateBossSpawner();
        CreateStoryBootstrap();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);

        GameObject loadedPlayer = GameObject.Find("Player");
        if (loadedPlayer != null)
        {
            Selection.activeObject = loadedPlayer;
        }

        Debug.Log("Geralt showcase scene created at Assets/Scenes/WitcherHuntDemo.unity");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 3.8f;
        camera.backgroundColor = new Color32(4, 5, 6, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<WitcherCameraFollow>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateBackground()
    {
        GameObject backgroundObject = new GameObject("Background");
        SpriteRenderer spriteRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = -50;
        backgroundObject.AddComponent<WitcherRuntimeBackground>();
        backgroundObject.transform.position = new Vector3(0f, 0f, 8f);
    }

    private static GeraltController CreatePlayer()
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(0f, -1.65f, 0f);
        playerObject.transform.localScale = Vector3.one * 0.65f;

        SpriteRenderer spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;

        playerObject.AddComponent<GeraltAnimator>();

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D boxCollider = playerObject.AddComponent<BoxCollider2D>();
        boxCollider.offset = new Vector2(0f, 0.65f);
        boxCollider.size = new Vector2(0.75f, 1.25f);

        GeraltController player = playerObject.AddComponent<GeraltController>();
        return player;
    }

    private static void CreateBossSpawner()
    {
        GameObject spawnerObject = new GameObject("BossSpawner");
        spawnerObject.AddComponent<WildHuntBossSpawnDirector>();
    }

    private static void CreateStoryBootstrap()
    {
        GameObject storyObject = new GameObject("Story Bootstrap");
        storyObject.AddComponent<OpeningStoryManager>();
    }
}
