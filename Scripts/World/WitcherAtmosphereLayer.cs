using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：为地图叠加暗色雾气和氛围层，增强场景层次。
    public class WitcherAtmosphereLayer : MonoBehaviour
    {
        private const int TextureWidth = 192;
        private const int TextureHeight = 96;

        private static readonly Dictionary<string, Sprite> GeneratedSprites = new Dictionary<string, Sprite>();

        [SerializeField] private Vector2 worldCenter = new Vector2(24f, -0.35f);
        [SerializeField] private float worldWidth = 66f;
        [SerializeField] private float worldHeight = 9.4f;
        [SerializeField] private int sortingOrder = -45;
        [SerializeField] private Color32 atmosphereTint = new Color32(42, 19, 17, 104);
        [SerializeField] private Color32 fogColor = new Color32(120, 112, 100, 82);
        [SerializeField] private Color32 emberColor = new Color32(224, 82, 36, 148);
        [SerializeField, Range(0f, 1f)] private float fogStrength = 0.48f;
        [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.88f;
        [SerializeField, Range(0f, 1f)] private float emberStrength = 0.78f;

        private readonly Dictionary<string, SpriteRenderer> layers = new Dictionary<string, SpriteRenderer>();
        private Vector3 lastParentScale;

        private void Awake()
        {
            Rebuild();
        }

        private void Start()
        {
            Rebuild();
            ApplyCurrentColors();
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
            emberStrength = 0f;
            fogStrength = Mathf.Clamp01(fogAmount);
            vignetteStrength = Mathf.Clamp01(vignetteAmount);

            Rebuild();
            ApplyCurrentColors();
        }

        public void ApplyCursedEmbers(
            Color32 tint,
            Color32 fog,
            Color32 ember,
            float fogAmount,
            float vignetteAmount,
            float emberAmount)
        {
            atmosphereTint = tint;
            fogColor = fog;
            emberColor = ember;
            fogStrength = Mathf.Clamp01(fogAmount);
            vignetteStrength = Mathf.Clamp01(vignetteAmount);
            emberStrength = Mathf.Clamp01(emberAmount);

            Rebuild();
            ApplyCurrentColors();
        }

        private void ApplyCurrentColors()
        {
            SetColor("Atmosphere Ember Glow", WithAlpha(emberColor, emberStrength * 0.62f));
            SetColor("Atmosphere Ember Drift 01", WithAlpha(emberColor, emberStrength * 0.82f));
            SetColor("Atmosphere Ember Drift 02", WithAlpha(emberColor, emberStrength * 0.56f));
            SetColor("Atmosphere Ember Drift 03", WithAlpha(emberColor, emberStrength * 0.68f));
            SetColor("Atmosphere Ember Drift 04", WithAlpha(emberColor, emberStrength * 0.48f));
        }

        private void Rebuild()
        {
            layers.Clear();
            lastParentScale = transform.lossyScale;

            RemoveLayer("Atmosphere Tint");
            RemoveLayer("Atmosphere Horizon Shadow");
            RemoveLayer("Atmosphere Fog Far");
            RemoveLayer("Atmosphere Fog Near");
            RemoveLayer("Atmosphere Vignette");

            SpriteRenderer emberGlow = EnsureLayer("Atmosphere Ember Glow", sortingOrder + 5, GetRadialGlowSprite());
            emberGlow.transform.position = new Vector3(worldCenter.x + 20f, worldCenter.y - 1.32f, 7.93f);
            ScaleToWorld(emberGlow, worldWidth * 0.42f, worldHeight * 0.36f, lastParentScale);

            CreateEmber("Atmosphere Ember Drift 01", worldCenter + new Vector2(10.6f, -0.9f), 0.16f, 0.28f);
            CreateEmber("Atmosphere Ember Drift 02", worldCenter + new Vector2(20.2f, 0.22f), 0.11f, 0.22f);
            CreateEmber("Atmosphere Ember Drift 03", worldCenter + new Vector2(30.8f, -0.18f), 0.14f, 0.26f);
            CreateEmber("Atmosphere Ember Drift 04", worldCenter + new Vector2(38.4f, 0.74f), 0.09f, 0.2f);
        }

        private void CreateEmber(string layerName, Vector2 position, float width, float height)
        {
            SpriteRenderer ember = EnsureLayer(layerName, sortingOrder + 6, GetEmberSprite());
            ember.transform.position = new Vector3(position.x, position.y, 7.92f);
            ScaleToWorld(ember, width, height, lastParentScale);
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

        private void RemoveLayer(string layerName)
        {
            Transform existing = transform.Find(layerName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }

            layers.Remove(layerName);
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

        private static Sprite GetRadialGlowSprite()
        {
            return GetOrCreateSprite("CursedEmberGlow", CreateRadialGlowTexture, 64f);
        }

        private static Sprite GetEmberSprite()
        {
            return GetOrCreateSprite("CursedEmberSpark", CreateEmberTexture, 32f);
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

        private static Texture2D CreateRadialGlowTexture()
        {
            Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                float ny = ((y + 0.5f) / texture.height - 0.5f) * 2f;
                for (int x = 0; x < texture.width; x++)
                {
                    float nx = ((x + 0.5f) / texture.width - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny * 2.6f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.85f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateEmberTexture()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                float ny = ((y + 0.5f) / texture.height - 0.5f) * 2f;
                for (int x = 0; x < texture.width; x++)
                {
                    float nx = ((x + 0.5f) / texture.width - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 0.72f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
