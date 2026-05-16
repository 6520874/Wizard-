using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public class WitcherAtmosphereLayer : MonoBehaviour
    {
        private const int TextureWidth = 192;
        private const int TextureHeight = 96;

        private static readonly Dictionary<string, Sprite> GeneratedSprites = new Dictionary<string, Sprite>();

        [SerializeField] private Vector2 worldCenter = new Vector2(24f, -0.35f);
        [SerializeField] private float worldWidth = 66f;
        [SerializeField] private float worldHeight = 9.4f;
        [SerializeField] private int sortingOrder = -45;
        [SerializeField] private Color32 atmosphereTint = new Color32(28, 50, 61, 84);
        [SerializeField] private Color32 fogColor = new Color32(148, 174, 176, 112);
        [SerializeField, Range(0f, 1f)] private float fogStrength = 0.72f;
        [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.82f;

        private readonly Dictionary<string, SpriteRenderer> layers = new Dictionary<string, SpriteRenderer>();
        private Vector3 lastParentScale;

        private void Awake()
        {
            Rebuild();
        }

        private void Start()
        {
            ApplyLook(atmosphereTint, fogColor, fogStrength, vignetteStrength);
        }

        private void LateUpdate()
        {
            if (transform.lossyScale != lastParentScale)
            {
                Rebuild();
                ApplyCurrentColors();
            }
        }

        public void ApplyLook(Color32 tint, Color32 fog, float fogAmount, float vignetteAmount)
        {
            atmosphereTint = tint;
            fogColor = fog;
            fogStrength = Mathf.Clamp01(fogAmount);
            vignetteStrength = Mathf.Clamp01(vignetteAmount);

            Rebuild();
            ApplyCurrentColors();
        }

        private void ApplyCurrentColors()
        {
            SetColor("Atmosphere Tint", atmosphereTint);
            SetColor("Atmosphere Horizon Shadow", new Color32(4, 8, 12, 118));
            SetColor("Atmosphere Fog Near", WithAlpha(fogColor, fogStrength * 0.92f));
            SetColor("Atmosphere Fog Far", WithAlpha(fogColor, fogStrength * 0.56f));
            SetColor("Atmosphere Vignette", new Color32(0, 0, 0, (byte)Mathf.RoundToInt(210f * vignetteStrength)));
        }

        private void Rebuild()
        {
            layers.Clear();
            lastParentScale = transform.lossyScale;

            SpriteRenderer tint = EnsureLayer("Atmosphere Tint", sortingOrder, GetSolidSprite());
            tint.transform.position = new Vector3(worldCenter.x, worldCenter.y, 7.98f);
            ScaleToWorld(tint, worldWidth, worldHeight, lastParentScale);

            SpriteRenderer horizon = EnsureLayer("Atmosphere Horizon Shadow", sortingOrder + 1, GetVerticalFadeSprite());
            horizon.transform.position = new Vector3(worldCenter.x, worldCenter.y - 1.55f, 7.97f);
            ScaleToWorld(horizon, worldWidth, 3.8f, lastParentScale);

            SpriteRenderer farFog = EnsureLayer("Atmosphere Fog Far", sortingOrder + 2, GetFogSprite());
            farFog.transform.position = new Vector3(worldCenter.x + 2.2f, worldCenter.y - 0.9f, 7.96f);
            ScaleToWorld(farFog, worldWidth * 0.92f, 1.55f, lastParentScale);

            SpriteRenderer nearFog = EnsureLayer("Atmosphere Fog Near", sortingOrder + 3, GetFogSprite());
            nearFog.transform.position = new Vector3(worldCenter.x - 1.4f, worldCenter.y - 2.22f, 7.95f);
            ScaleToWorld(nearFog, worldWidth * 1.08f, 1.28f, lastParentScale);

            SpriteRenderer vignette = EnsureLayer("Atmosphere Vignette", sortingOrder + 4, GetVignetteSprite());
            vignette.transform.position = new Vector3(worldCenter.x, worldCenter.y, 7.94f);
            ScaleToWorld(vignette, worldWidth, worldHeight, lastParentScale);
        }

        private SpriteRenderer EnsureLayer(string layerName, int order, Sprite sprite)
        {
            Transform existing = transform.Find(layerName);
            GameObject layerObject = existing != null ? existing.gameObject : new GameObject(layerName);
            layerObject.transform.SetParent(transform, false);

            SpriteRenderer renderer = layerObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = layerObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = Color.white;
            layers[layerName] = renderer;
            return renderer;
        }

        private void SetColor(string layerName, Color32 color)
        {
            if (layers.TryGetValue(layerName, out SpriteRenderer renderer))
            {
                renderer.color = color;
            }
        }

        private static Color32 WithAlpha(Color32 color, float alpha01)
        {
            color.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha01) * 255f);
            return color;
        }

        private static void ScaleToWorld(SpriteRenderer renderer, float width, float height, Vector3 parentScale)
        {
            if (renderer.sprite == null)
            {
                return;
            }

            Vector2 size = renderer.sprite.bounds.size;
            float parentX = Mathf.Approximately(parentScale.x, 0f) ? 1f : Mathf.Abs(parentScale.x);
            float parentY = Mathf.Approximately(parentScale.y, 0f) ? 1f : Mathf.Abs(parentScale.y);
            renderer.transform.localScale = new Vector3(width / (size.x * parentX), height / (size.y * parentY), 1f);
        }

        private static Sprite GetSolidSprite()
        {
            return WitcherSpriteLibrary.GetSolidSprite(Color.white);
        }

        private static Sprite GetFogSprite()
        {
            return GetOrCreateSprite("ColdFogBand", CreateFogTexture, 64f);
        }

        private static Sprite GetVerticalFadeSprite()
        {
            return GetOrCreateSprite("ColdHorizonShadow", CreateVerticalFadeTexture, 64f);
        }

        private static Sprite GetVignetteSprite()
        {
            return GetOrCreateSprite("ColdVignette", CreateVignetteTexture, 64f);
        }

        private static Sprite GetOrCreateSprite(string key, System.Func<Texture2D> factory, float pixelsPerUnit)
        {
            if (GeneratedSprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            Texture2D texture = factory();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.name = key;
            GeneratedSprites[key] = sprite;
            return sprite;
        }

        private static Texture2D CreateFogTexture()
        {
            Texture2D texture = new Texture2D(TextureWidth, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                float vertical = 1f - Mathf.Abs((y + 0.5f) / texture.height - 0.5f) * 2f;
                vertical = Mathf.Pow(Mathf.Clamp01(vertical), 0.65f);
                for (int x = 0; x < texture.width; x++)
                {
                    float horizontal = 1f - Mathf.Abs((x + 0.5f) / texture.width - 0.5f) * 2f;
                    float noise = Mathf.PerlinNoise(x * 0.075f, y * 0.18f);
                    float alpha = Mathf.Clamp01(vertical * Mathf.Lerp(0.34f, 1f, horizontal) * Mathf.Lerp(0.7f, 1.18f, noise));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateVerticalFadeTexture()
        {
            Texture2D texture = new Texture2D(8, TextureHeight, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                float t = (y + 0.5f) / texture.height;
                float alpha = Mathf.SmoothStep(1f, 0f, t);
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateVignetteTexture()
        {
            Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                float ny = ((y + 0.5f) / texture.height - 0.5f) * 2f;
                for (int x = 0; x < texture.width; x++)
                {
                    float nx = ((x + 0.5f) / texture.width - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny * 1.85f);
                    float alpha = Mathf.SmoothStep(0.42f, 1.18f, distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
