using UnityEngine;

namespace WitcherGame
{
    /// <summary>
    /// Ensures the opening story exists even if the scene has not been manually wired.
    /// This keeps the playable demo resilient while scenes are being rebuilt by tools.
    /// </summary>
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
