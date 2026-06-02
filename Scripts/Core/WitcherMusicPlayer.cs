using UnityEngine;

namespace WitcherGame
{
    // 中文说明：运行时加载并循环播放探索地图背景音乐。
    public class WitcherMusicPlayer : MonoBehaviour
    {
        private const string PlayerName = "Witcher Music Player";
        private const string DefaultMusicResourcePath = "Music/UserProvided_Velen_BGM";

        [SerializeField] private string musicResourcePath = DefaultMusicResourcePath;
        [SerializeField] private float volume = 0.34f;

        private AudioSource audioSource;

        public static WitcherMusicPlayer CreateIfMissing()
        {
            WitcherMusicPlayer existing = FindObjectOfType<WitcherMusicPlayer>();
            if (existing != null)
            {
                existing.PlayIfNeeded();
                return existing;
            }

            GameObject playerObject = new GameObject(PlayerName);
            WitcherMusicPlayer musicPlayer = playerObject.AddComponent<WitcherMusicPlayer>();
            musicPlayer.PlayIfNeeded();
            return musicPlayer;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureAudioSource();
        }

        private void Start()
        {
            PlayIfNeeded();
        }

        public void PlayIfNeeded()
        {
            EnsureAudioSource();
            if (audioSource == null)
            {
                return;
            }

            if (audioSource.clip == null)
            {
                AudioClip clip = Resources.Load<AudioClip>(musicResourcePath);
                if (clip == null)
                {
                    Debug.LogWarning($"Music clip not found at Resources/{musicResourcePath}");
                    return;
                }

                audioSource.clip = clip;
            }

            audioSource.volume = volume;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.priority = 40;
            audioSource.volume = volume;
        }
    }
}
