using UnityEngine;

namespace WitcherGame
{
    // Adds a camera-attached day/night grade plus soft sun and moon glows.
    // 中文说明：给主相机挂载昼夜色调、太阳/月亮辉光和时间循环效果。
    public class WitcherDayNightCycle : MonoBehaviour
    {
        [SerializeField] private float cycleDuration = 210f;
        [SerializeField] private float startTimeOfDay = 0.28f;
        [SerializeField] private Color32 baseRoomTint = new Color32(168, 196, 214, 255);

        private static Sprite cachedRadialSprite;

        private Camera targetCamera;
        private SpriteRenderer gradeRenderer;
        private SpriteRenderer sunGlowRenderer;
        private SpriteRenderer moonGlowRenderer;
        private float elapsedTime;

        public static WitcherDayNightCycle EnsureOn(Camera camera, Color32 roomTint)
        {
            if (camera == null)
            {
                return null;
            }

            WitcherDayNightCycle cycle = camera.GetComponent<WitcherDayNightCycle>();
            if (cycle == null)
            {
                cycle = camera.gameObject.AddComponent<WitcherDayNightCycle>();
            }

            cycle.targetCamera = camera;
            cycle.baseRoomTint = roomTint;
            cycle.EnsureVisuals();
            return cycle;
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
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            EnsureVisuals();
            elapsedTime += Time.unscaledDeltaTime;
            ApplyCycle();
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

            if (gradeRenderer == null)
            {
                GameObject grade = new GameObject("Day Night Color Grade");
                grade.transform.SetParent(transform, false);
                gradeRenderer = grade.AddComponent<SpriteRenderer>();
                gradeRenderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 255, 255, 255));
                gradeRenderer.sortingOrder = 1450;
            }

            if (sunGlowRenderer == null)
            {
                sunGlowRenderer = CreateGlowRenderer("Day Sun Glow", 730);
            }

            if (moonGlowRenderer == null)
            {
                moonGlowRenderer = CreateGlowRenderer("Night Moon Glow", 731);
            }
        }

        private SpriteRenderer CreateGlowRenderer(string objectName, int sortingOrder)
        {
            GameObject glow = new GameObject(objectName);
            glow.transform.SetParent(transform, false);
            SpriteRenderer renderer = glow.AddComponent<SpriteRenderer>();
            renderer.sprite = GetRadialSprite();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplyCycle()
        {
            float timeOfDay = Mathf.Repeat(startTimeOfDay + elapsedTime / Mathf.Max(1f, cycleDuration), 1f);
            float sunHeight = Mathf.Sin(timeOfDay * Mathf.PI * 2f - Mathf.PI * 0.5f);
            float dayAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.22f, 0.58f, sunHeight));
            float nightAmount = 1f - dayAmount;
            float duskAmount = Mathf.Pow(1f - Mathf.Abs(sunHeight), 3f);

            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            UpdateFullScreenGrade(width, height, nightAmount, duskAmount);
            UpdateLightSource(sunGlowRenderer, timeOfDay, dayAmount, duskAmount, width, height, true);
            UpdateLightSource(moonGlowRenderer, Mathf.Repeat(timeOfDay + 0.5f, 1f), nightAmount, 0f, width, height, false);
            UpdateWorldLight(dayAmount, duskAmount, nightAmount);
        }

        private void UpdateFullScreenGrade(float width, float height, float nightAmount, float duskAmount)
        {
            if (gradeRenderer == null)
            {
                return;
            }

            Color nightColor = new Color32(7, 18, 37, 255);
            Color duskColor = new Color32(120, 58, 34, 255);
            Color gradeColor = Color.Lerp(nightColor, duskColor, duskAmount * 0.55f);
            gradeColor.a = Mathf.Clamp01(nightAmount * 0.34f + duskAmount * 0.12f);

            gradeRenderer.color = gradeColor;
            gradeRenderer.transform.localPosition = new Vector3(0f, 0f, 12.2f);
            gradeRenderer.transform.localScale = new Vector3(width * 1.08f, height * 1.08f, 1f);
        }

        private void UpdateLightSource(SpriteRenderer renderer, float orbitTime, float strength, float duskAmount, float width, float height, bool isSun)
        {
            if (renderer == null)
            {
                return;
            }

            float x = Mathf.Lerp(-width * 0.46f, width * 0.46f, orbitTime);
            float arc = Mathf.Sin(orbitTime * Mathf.PI);
            float y = Mathf.Lerp(-height * 0.2f, height * 0.43f, Mathf.Clamp01(arc));
            float alpha = isSun
                ? Mathf.Clamp01(strength * 0.18f + duskAmount * 0.16f)
                : Mathf.Clamp01(strength * 0.2f);
            Color color = isSun
                ? Color.Lerp(new Color32(255, 174, 92, 255), new Color32(255, 230, 165, 255), strength)
                : new Color32(148, 190, 230, 255);
            color.a = alpha;

            renderer.color = color;
            renderer.transform.localPosition = new Vector3(x, y, 11.6f);
            float scale = isSun ? Mathf.Lerp(2.4f, 3.8f, strength) : 2.6f;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void UpdateWorldLight(float dayAmount, float duskAmount, float nightAmount)
        {
            Color dayAmbient = new Color32(194, 207, 202, 255);
            Color nightAmbient = new Color32(54, 73, 92, 255);
            Color duskAmbient = new Color32(158, 106, 78, 255);
            RenderSettings.ambientLight = Color.Lerp(Color.Lerp(nightAmbient, dayAmbient, dayAmount), duskAmbient, duskAmount * 0.45f);

            Color dayBackground = Color.Lerp(new Color32(65, 100, 120, 255), baseRoomTint, 0.35f);
            Color nightBackground = new Color32(5, 10, 20, 255);
            Color duskBackground = new Color32(70, 42, 35, 255);
            targetCamera.backgroundColor = Color.Lerp(Color.Lerp(nightBackground, dayBackground, dayAmount), duskBackground, duskAmount * 0.35f + nightAmount * 0.05f);
        }

        private static Sprite GetRadialSprite()
        {
            if (cachedRadialSprite != null)
            {
                return cachedRadialSprite;
            }

            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.25f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            cachedRadialSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            cachedRadialSprite.name = "RuntimeDayNightRadialGlow";
            return cachedRadialSprite;
        }
    }
}
