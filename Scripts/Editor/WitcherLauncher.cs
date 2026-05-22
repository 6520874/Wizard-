using UnityEditor;
using UnityEditor.SceneManagement;

// 中文说明：提供 Unity 编辑器菜单入口，方便打开或创建演示场景。
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
