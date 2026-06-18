using UnityEngine;

namespace WitcherGame
{
    // Runtime exploration music player with async loading to keep map entry smooth.
    // 中文说明：负责播放和循环地图探索背景音乐，进入场景时自动补齐 AudioSource。
    public class WitcherMusicPlayer : MonoBehaviour
    {
        private const string PlayerName = "Witcher Music Player";
        private const string DefaultMusicResourcePath = "Music/UserProvided_Velen_BGM";
        private const float FadeInDuration = 2.4f;

        [SerializeField] private string musicResourcePath = DefaultMusicResourcePath;
        [SerializeField] private float volume = 0.34f;

        private AudioSource audioSource;
        private bool loadingClip;
        private Coroutine loadingRoutine;

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

        public static void PlayMusic(string resourcePath)
        {
            CreateIfMissing().SwitchMusic(resourcePath);
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
                StartAsyncLoadIfNeeded();
                return;
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

        private void StartAsyncLoadIfNeeded()
        {
            if (loadingClip)
            {
                return;
            }

            loadingClip = true;
            loadingRoutine = StartCoroutine(LoadAndPlayMusic(musicResourcePath));
        }

        private void SwitchMusic(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            EnsureAudioSource();
            if (resourcePath == musicResourcePath && audioSource != null && audioSource.clip != null)
            {
                PlayIfNeeded();
                return;
            }

            musicResourcePath = resourcePath;
            if (loadingRoutine != null)
            {
                StopCoroutine(loadingRoutine);
                loadingRoutine = null;
            }

            loadingClip = true;
            loadingRoutine = StartCoroutine(LoadAndPlayMusic(musicResourcePath));
        }

        private System.Collections.IEnumerator LoadAndPlayMusic(string resourcePath)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>(resourcePath);
            yield return request;

            loadingClip = false;
            loadingRoutine = null;
            if (resourcePath != musicResourcePath)
            {
                yield break;
            }

            AudioClip clip = request.asset as AudioClip;
            if (clip == null)
            {
                Debug.LogWarning($"Music clip not found at Resources/{resourcePath}");
                yield break;
            }

            EnsureAudioSource();
            if (audioSource == null)
            {
                yield break;
            }

            audioSource.clip = clip;
            audioSource.volume = 0f;
            audioSource.Play();
            float elapsed = 0f;
            while (elapsed < FadeInDuration && audioSource != null && audioSource.isPlaying)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(0f, volume, Mathf.Clamp01(elapsed / FadeInDuration));
                yield return null;
            }

            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }
    }
}
