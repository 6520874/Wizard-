using UnityEngine;

namespace WitcherGame
{
    // 中文说明：给等距探索镜头添加冷色雾层和密集斜雨，模拟威伦式阴冷雨幕。
    public class WitcherIsometricAtmosphere : MonoBehaviour
    {
        private const int RainStreakCount = 120;

        [SerializeField] private Color32 rainColor = new Color32(174, 214, 232, 118);
        [SerializeField] private Color32 coldMistColor = new Color32(62, 118, 164, 36);
        [SerializeField] private float rainSpeed = 5.6f;
        [SerializeField] private float rainSlantSpeed = 1.25f;

        private readonly RainStreak[] rainStreaks = new RainStreak[RainStreakCount];
        private Camera targetCamera;
        private SpriteRenderer mistRenderer;

        public static WitcherIsometricAtmosphere EnsureOn(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            WitcherIsometricAtmosphere atmosphere = camera.GetComponent<WitcherIsometricAtmosphere>();
            if (atmosphere == null)
            {
                atmosphere = camera.gameObject.AddComponent<WitcherIsometricAtmosphere>();
            }

            atmosphere.targetCamera = camera;
            atmosphere.EnsureVisuals();
            return atmosphere;
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            EnsureVisuals();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            UpdateMist();
            UpdateRain();
        }

        private void EnsureVisuals()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            RemoveLegacyWeatherObjects();

            if (mistRenderer == null)
            {
                GameObject mist = new GameObject("Isometric Cold Mist");
                mist.transform.SetParent(transform, false);
                mistRenderer = mist.AddComponent<SpriteRenderer>();
                mistRenderer.sprite = WitcherSpriteLibrary.GetSolidSprite(coldMistColor);
                mistRenderer.color = coldMistColor;
                mistRenderer.sortingOrder = 760;
            }

            Sprite rainSprite = WitcherSpriteLibrary.GetSolidSprite(rainColor);
            for (int i = 0; i < rainStreaks.Length; i++)
            {
                GameObject rain = new GameObject($"Isometric Rain {i + 1:000}");
                rain.transform.SetParent(transform, false);
                rain.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
                SpriteRenderer renderer = rain.AddComponent<SpriteRenderer>();
                renderer.sprite = rainSprite;
                renderer.color = new Color32(rainColor.r, rainColor.g, rainColor.b, (byte)Random.Range(72, 132));
                renderer.sortingOrder = 780 + i % 4;
                rain.transform.localScale = new Vector3(Random.Range(0.012f, 0.022f), Random.Range(0.22f, 0.42f), 1f);
                rainStreaks[i] = new RainStreak(renderer, Random.Range(0.72f, 1.22f), Random.Range(-0.18f, 0.18f));
                ResetRainStreak(i, true);
            }

            UpdateMist();
        }

        private void UpdateMist()
        {
            if (mistRenderer == null)
            {
                return;
            }

            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            mistRenderer.transform.localPosition = new Vector3(0f, -height * 0.32f, 11f);
            mistRenderer.transform.localScale = new Vector3(width * 1.15f, height * 0.42f, 1f);
        }

        private void UpdateRain()
        {
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            for (int i = 0; i < rainStreaks.Length; i++)
            {
                RainStreak streak = rainStreaks[i];
                if (streak.Renderer == null)
                {
                    continue;
                }

                Vector3 localPosition = streak.Renderer.transform.localPosition;
                float deltaTime = Time.unscaledDeltaTime;
                localPosition.y -= rainSpeed * streak.SpeedMultiplier * deltaTime;
                localPosition.x -= (rainSlantSpeed * streak.SpeedMultiplier + streak.Drift) * deltaTime;
                if (localPosition.y < -height * 0.6f || localPosition.x < -width * 0.62f)
                {
                    ResetRainStreak(i, false);
                    continue;
                }

                streak.Renderer.transform.localPosition = localPosition;
            }
        }

        private void ResetRainStreak(int index, bool randomY)
        {
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            RainStreak streak = rainStreaks[index];
            if (streak.Renderer == null)
            {
                return;
            }

            float x = randomY ? Random.Range(-width * 0.52f, width * 0.62f) : Random.Range(-width * 0.18f, width * 0.62f);
            float y = randomY ? Random.Range(-height * 0.54f, height * 0.58f) : height * 0.6f;
            streak.Renderer.transform.localPosition = new Vector3(x, y, 10.5f + index * 0.002f);
        }

        private void RemoveLegacyWeatherObjects()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && (child.name.StartsWith("Isometric Snow") || child.name.StartsWith("Isometric Rain")))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private readonly struct RainStreak
        {
            public RainStreak(SpriteRenderer renderer, float speedMultiplier, float drift)
            {
                Renderer = renderer;
                SpeedMultiplier = speedMultiplier;
                Drift = drift;
            }

            public SpriteRenderer Renderer { get; }
            public float SpeedMultiplier { get; }
            public float Drift { get; }
        }
    }
}
