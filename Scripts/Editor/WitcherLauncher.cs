using UnityEditor;
using UnityEditor.SceneManagement;

public static class WitcherLauncher
{
    private const string ScenePath = "Assets/Scenes/WitcherHuntDemo.unity";

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
