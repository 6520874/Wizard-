using System.Collections;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：统一触发战斗打击反馈，例如镜头震动和命中特效提示。
    public class WitcherCombatFeedback : MonoBehaviour
    {
        private static WitcherCombatFeedback instance;

        private Coroutine hitStopRoutine;
        private float currentStopUntilRealtime;

        public static void HeavyEnemyHit(Vector3 position)
        {
            EnsureInstance().TriggerHitStop(0.055f);
            ShakeCamera(0.13f, 0.16f);
        }

        public static void PlayerHit(Vector3 position)
        {
            ShakeCamera(0.18f, 0.2f);
        }

        private static WitcherCombatFeedback EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            WitcherCombatFeedback existing = FindObjectOfType<WitcherCombatFeedback>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject feedbackObject = new GameObject("Witcher Combat Feedback");
            instance = feedbackObject.AddComponent<WitcherCombatFeedback>();
            return instance;
        }

        private static void ShakeCamera(float strength, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            WitcherCameraFollow follow = camera.GetComponent<WitcherCameraFollow>();
            if (follow != null)
            {
                follow.AddShake(strength, duration);
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void TriggerHitStop(float duration)
        {
            if (Time.timeScale <= 0.01f)
            {
                return;
            }

            currentStopUntilRealtime = Mathf.Max(currentStopUntilRealtime, Time.realtimeSinceStartup + duration);
            if (hitStopRoutine == null)
            {
                hitStopRoutine = StartCoroutine(HitStopRoutine());
            }
        }

        private IEnumerator HitStopRoutine()
        {
            float previousScale = Time.timeScale;
            Time.timeScale = 0.08f;
            while (Time.realtimeSinceStartup < currentStopUntilRealtime)
            {
                yield return null;
            }

            if (Time.timeScale > 0.01f)
            {
                Time.timeScale = previousScale <= 0.01f ? 1f : previousScale;
            }

            hitStopRoutine = null;
        }
    }
}
