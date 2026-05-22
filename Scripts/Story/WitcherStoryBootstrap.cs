using UnityEngine;

namespace WitcherGame
{
    /// <summary>
    /// Ensures the opening story exists even if the scene has not been manually wired.
    /// This keeps the playable demo resilient while scenes are being rebuilt by tools.
    /// </summary>
    // 中文说明：在场景启动时补齐开场剧情所需的故事管理对象。
    public static class WitcherStoryBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOpeningStoryIfMissing()
        {
            if (Object.FindObjectOfType<OpeningStoryManager>() != null)
            {
                return;
            }

            GameObject storyObject = new GameObject("Story Bootstrap");
            storyObject.AddComponent<OpeningStoryManager>();
        }
    }
}
