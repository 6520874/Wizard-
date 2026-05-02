using UnityEditor;
using UnityEditor.SceneManagement;

public static class PixelRaidLauncher
{
    private const string ScenePath = "Assets/Scenes/PixelRaidDemo.unity";

    public static void OpenAndPlay()
    {
        EditorApplication.isPlaying = false;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.delayCall += StartPlayMode;
    }

    private static void StartPlayMode()
    {
        EditorApplication.delayCall -= StartPlayMode;
        EditorApplication.isPlaying = true;
    }
}
