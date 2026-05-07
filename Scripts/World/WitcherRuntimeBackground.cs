using System.IO;
using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WitcherRuntimeBackground : MonoBehaviour
    {
        [SerializeField] private string imagePath = "Art/Backgrounds/Witcher_Village_Longroad.png";
        [SerializeField]
        private string[] segmentImagePaths = System.Array.Empty<string>();
        [SerializeField] private float pixelsPerUnit = 64f;
        [SerializeField] private int sortingOrder = -50;
        [SerializeField] private bool fitToCamera = false;
        [SerializeField] private float worldWidth = 64f;
        [SerializeField] private Vector2 worldCenter = new Vector2(24f, -0.35f);
        [SerializeField] private float segmentWorldWidth = 64f;
        [SerializeField] private float segmentOverlap = 1.2f;
        [SerializeField] private float seamFogWidth = 2.1f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
            if (HasSegments)
            {
                spriteRenderer.enabled = false;
                BuildSegmentedBackground();
                return;
            }

            ClearSegmentChildren();
            LoadSprite(spriteRenderer, imagePath);
        }

        private void Start()
        {
            if (fitToCamera)
            {
                FitToCamera();
                return;
            }

            FitToWorld();
        }

        private void FitToWorld()
        {
            if (HasSegments)
            {
                return;
            }

            if (spriteRenderer.sprite == null || worldWidth <= 0f)
            {
                return;
            }

            float scale = worldWidth / spriteRenderer.sprite.bounds.size.x;
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(worldCenter.x, worldCenter.y, 8f);
        }

        private bool HasSegments => segmentImagePaths != null && segmentImagePaths.Length > 0;

        private void BuildSegmentedBackground()
        {
            ClearSegmentChildren();

            float spacing = Mathf.Max(0.1f, segmentWorldWidth - segmentOverlap);
            float startX = worldCenter.x;
            for (int i = 0; i < segmentImagePaths.Length; i++)
            {
                GameObject segment = new GameObject($"Background Segment {i + 1:00}");
                segment.transform.SetParent(transform, false);
                SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = sortingOrder + i;
                renderer.color = Color.white;
                LoadSprite(renderer, segmentImagePaths[i]);

                if (renderer.sprite != null)
                {
                    float scale = segmentWorldWidth / renderer.sprite.bounds.size.x;
                    segment.transform.localScale = new Vector3(scale, scale, 1f);
                }

                segment.transform.position = new Vector3(startX + i * spacing, worldCenter.y, 8f);

                if (i > 0)
                {
                    CreateSeamFog(i, startX + i * spacing - segmentWorldWidth * 0.5f + segmentOverlap * 0.5f);
                }
            }
        }

        private void ClearSegmentChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private void CreateSeamFog(int seamIndex, float seamX)
        {
            GameObject seam = new GameObject($"Background Seam Fog {seamIndex:00}");
            seam.transform.SetParent(transform, false);
            SpriteRenderer renderer = seam.AddComponent<SpriteRenderer>();
            renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(5, 8, 12, 88));
            renderer.color = new Color32(5, 8, 12, 88);
            renderer.sortingOrder = sortingOrder + segmentImagePaths.Length + seamIndex;
            renderer.transform.position = new Vector3(seamX, worldCenter.y, 7.95f);
            renderer.transform.localScale = new Vector3(seamFogWidth, 14f, 1f);
        }

        private void LoadSprite(SpriteRenderer targetRenderer, string assetRelativePath)
        {
            string absolutePath = Path.Combine(Application.dataPath, assetRelativePath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"Background image not found: {absolutePath}", this);
                return;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Debug.LogWarning($"Could not load background image: {absolutePath}", this);
                return;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            targetRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        private void FitToCamera()
        {
            Camera camera = Camera.main;
            if (camera == null || spriteRenderer.sprite == null || !camera.orthographic)
            {
                return;
            }

            float cameraHeight = camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * camera.aspect;
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 8f);
        }
    }
}
