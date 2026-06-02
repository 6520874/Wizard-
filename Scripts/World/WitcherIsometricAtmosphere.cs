using UnityEngine;

namespace WitcherGame
{
    // 中文说明：给等距探索镜头添加冷色雾层和轻雪粒子，模拟 HD-2D 雪村镜头氛围。
    public class WitcherIsometricAtmosphere : MonoBehaviour
    {
        private const int SnowflakeCount = 54;

        [SerializeField] private Color32 snowColor = new Color32(225, 242, 255, 170);
        [SerializeField] private Color32 coldMistColor = new Color32(72, 134, 190, 42);
        [SerializeField] private float snowSpeed = 0.42f;
        [SerializeField] private float driftSpeed = 0.18f;

        private readonly Snowflake[] snowflakes = new Snowflake[SnowflakeCount];
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
            UpdateSnow();
        }

        private void EnsureVisuals()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null || mistRenderer != null)
            {
                return;
            }

            GameObject mist = new GameObject("Isometric Cold Mist");
            mist.transform.SetParent(transform, false);
            mistRenderer = mist.AddComponent<SpriteRenderer>();
            mistRenderer.sprite = WitcherSpriteLibrary.GetSolidSprite(coldMistColor);
            mistRenderer.color = coldMistColor;
            mistRenderer.sortingOrder = 760;

            for (int i = 0; i < snowflakes.Length; i++)
            {
                GameObject snow = new GameObject($"Isometric Snow {i + 1:00}");
                snow.transform.SetParent(transform, false);
                SpriteRenderer renderer = snow.AddComponent<SpriteRenderer>();
                renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(snowColor);
                renderer.color = snowColor;
                renderer.sortingOrder = 780 + i % 4;
                float scale = Random.Range(0.018f, 0.05f);
                snow.transform.localScale = new Vector3(scale, scale, 1f);
                snowflakes[i] = new Snowflake(renderer, Random.Range(0.55f, 1.35f), Random.Range(-1f, 1f));
                ResetSnowflake(i, true);
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

        private void UpdateSnow()
        {
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            for (int i = 0; i < snowflakes.Length; i++)
            {
                Snowflake flake = snowflakes[i];
                if (flake.Renderer == null)
                {
                    continue;
                }

                Vector3 localPosition = flake.Renderer.transform.localPosition;
                localPosition.y -= snowSpeed * flake.SpeedMultiplier * Time.unscaledDeltaTime;
                localPosition.x += driftSpeed * flake.Drift * Time.unscaledDeltaTime;
                if (localPosition.y < -height * 0.58f || localPosition.x < -width * 0.58f || localPosition.x > width * 0.58f)
                {
                    ResetSnowflake(i, false);
                    continue;
                }

                flake.Renderer.transform.localPosition = localPosition;
            }
        }

        private void ResetSnowflake(int index, bool randomY)
        {
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            Snowflake flake = snowflakes[index];
            if (flake.Renderer == null)
            {
                return;
            }

            float x = Random.Range(-width * 0.52f, width * 0.52f);
            float y = randomY ? Random.Range(-height * 0.5f, height * 0.5f) : height * 0.54f;
            flake.Renderer.transform.localPosition = new Vector3(x, y, 10.5f + index * 0.002f);
        }

        private readonly struct Snowflake
        {
            public Snowflake(SpriteRenderer renderer, float speedMultiplier, float drift)
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
