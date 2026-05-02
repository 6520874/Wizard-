using PixelRaid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PixelRaidSceneCreator
{
    private const string ScenePath = "Assets/Scenes/PixelRaidDemo.unity";

    [MenuItem("Tools/Pixel Raid/Create Playable Scene")]
    public static void CreatePlayableScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateBackground();
        CreatePlayer();
        CreateBossSpawner();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);

        GameObject loadedPlayer = GameObject.Find("Player");
        if (loadedPlayer != null)
        {
            Selection.activeObject = loadedPlayer;
        }

        Debug.Log("Geralt showcase scene created at Assets/Scenes/PixelRaidDemo.unity");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 3f;
        camera.backgroundColor = new Color32(4, 5, 6, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateBackground()
    {
        GameObject backgroundObject = new GameObject("Background");
        SpriteRenderer spriteRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = -50;
        backgroundObject.AddComponent<PixelRaidRuntimeBackground>();
        backgroundObject.transform.position = new Vector3(0f, 0f, 8f);
    }

    private static PixelRaidPlayerController CreatePlayer()
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(0f, -1.65f, 0f);
        playerObject.transform.localScale = Vector3.one * 0.65f;

        SpriteRenderer spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 2;

        playerObject.AddComponent<PixelRaidGeraltAnimator>();

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D boxCollider = playerObject.AddComponent<BoxCollider2D>();
        boxCollider.offset = new Vector2(0f, 0.65f);
        boxCollider.size = new Vector2(0.75f, 1.25f);

        PixelRaidPlayerController player = playerObject.AddComponent<PixelRaidPlayerController>();
        return player;
    }

    private static void CreateBossSpawner()
    {
        GameObject spawnerObject = new GameObject("BossSpawner");
        spawnerObject.AddComponent<PixelRaidBossSpawnDirector>();
    }
}
