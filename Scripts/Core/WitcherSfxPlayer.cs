using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public enum WitcherSfxCue
    {
        UiClick,
        ChoiceConfirm,
        QuestUpdate,
        ClueFound,
        NightTransition,
        EncounterSpawn,
        BossSpawn,
        SwordHit,
        FireCast,
        MagicWard,
        EnemyAttack,
        BattleVictory
    }

    // 中文说明：集中播放短音效，音频从 Resources/Sfx 按需加载并缓存。
    public class WitcherSfxPlayer : MonoBehaviour
    {
        private const string PlayerName = "Witcher Sfx Player";

        private static readonly Dictionary<WitcherSfxCue, string> CuePaths = new Dictionary<WitcherSfxCue, string>
        {
            { WitcherSfxCue.UiClick, "Sfx/ui_click" },
            { WitcherSfxCue.ChoiceConfirm, "Sfx/choice_confirm" },
            { WitcherSfxCue.QuestUpdate, "Sfx/quest_update" },
            { WitcherSfxCue.ClueFound, "Sfx/clue_found" },
            { WitcherSfxCue.NightTransition, "Sfx/night_transition" },
            { WitcherSfxCue.EncounterSpawn, "Sfx/encounter_spawn" },
            { WitcherSfxCue.BossSpawn, "Sfx/boss_spawn" },
            { WitcherSfxCue.SwordHit, "Sfx/sword_hit" },
            { WitcherSfxCue.FireCast, "Sfx/fire_cast" },
            { WitcherSfxCue.MagicWard, "Sfx/magic_ward" },
            { WitcherSfxCue.EnemyAttack, "Sfx/enemy_attack" },
            { WitcherSfxCue.BattleVictory, "Sfx/battle_victory" }
        };

        private static WitcherSfxPlayer instance;

        [SerializeField, Range(0f, 1f)] private float volume = 0.72f;

        private readonly Dictionary<WitcherSfxCue, AudioClip> clipCache = new Dictionary<WitcherSfxCue, AudioClip>();
        private AudioSource audioSource;

        public static WitcherSfxPlayer CreateIfMissing()
        {
            if (instance != null)
            {
                return instance;
            }

            WitcherSfxPlayer existing = FindObjectOfType<WitcherSfxPlayer>();
            if (existing != null)
            {
                instance = existing;
                instance.EnsureAudioSource();
                return existing;
            }

            WitcherSfxPlayer player = new GameObject(PlayerName).AddComponent<WitcherSfxPlayer>();
            player.EnsureAudioSource();
            return player;
        }

        public static void Play(WitcherSfxCue cue, float volumeScale = 1f)
        {
            WitcherSfxPlayer player = CreateIfMissing();
            player.PlayCue(cue, volumeScale);
        }

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSource();
        }

        private void PlayCue(WitcherSfxCue cue, float volumeScale)
        {
            EnsureAudioSource();
            AudioClip clip = GetClip(cue);
            if (audioSource == null || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume * Mathf.Max(0f, volumeScale)));
        }

        private AudioClip GetClip(WitcherSfxCue cue)
        {
            if (clipCache.TryGetValue(cue, out AudioClip cached))
            {
                return cached;
            }

            if (!CuePaths.TryGetValue(cue, out string path))
            {
                return null;
            }

            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"SFX clip not found at Resources/{path}");
                return null;
            }

            clipCache[cue] = clip;
            return clip;
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
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.priority = 32;
        }
    }
}
