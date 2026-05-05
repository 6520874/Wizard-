using System.IO;
using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WitcherRuntimeBackground : MonoBehaviour
    {
        [SerializeField] private string imagePath = "Art/Backgrounds/Witcher_Village_Longroad.png";
        [SerializeField] private float pixelsPerUnit = 64f;
        [SerializeField] private int sortingOrder = -50;
        [SerializeField] private bool fitToCamera = false;
        [SerializeField] private float worldWidth = 64f;
        [SerializeField] private Vector2 worldCenter = new Vector2(24f, -0.35f);

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
            LoadSprite();
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
            if (spriteRenderer.sprite == null || worldWidth <= 0f)
            {
                return;
            }

            float scale = worldWidth / spriteRenderer.sprite.bounds.size.x;
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(worldCenter.x, worldCenter.y, 8f);
        }

        private void LoadSprite()
        {
            string absolutePath = Path.Combine(Application.dataPath, imagePath);
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
            spriteRenderer.sprite = Sprite.Create(
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
