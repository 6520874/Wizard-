using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class WitcherEquipmentDrop : MonoBehaviour
    {
        [SerializeField] private float floatAmplitude = 0.16f;
        [SerializeField] private float floatSpeed = 3.2f;
        [SerializeField] private int restoreHealth = 35;
        [SerializeField] private int restoreMana = 45;

        private Vector3 startPosition;

        public static WitcherEquipmentDrop SpawnAt(Vector3 position)
        {
            GameObject dropObject = new GameObject("Witcher Relic Equipment Drop");
            dropObject.transform.position = position;
            return dropObject.AddComponent<WitcherEquipmentDrop>();
        }

        private void Awake()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateEquipmentSprite();
            spriteRenderer.sortingOrder = 8;
            transform.localScale = Vector3.one * 0.82f;

            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.45f;

            startPosition = transform.position;
        }

        private void Update()
        {
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out GeraltController player))
            {
                return;
            }

            player.RestoreHealth(restoreHealth);
            player.RestoreMana(restoreMana);
            WitcherCombatText.Spawn("获得狂猎遗物", transform.position + Vector3.up * 0.45f, new Color32(255, 222, 126, 255));
            Destroy(gameObject);
        }

        private static Sprite CreateEquipmentSprite()
        {
            Texture2D texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 outline = new Color32(20, 16, 12, 255);
            Color32 gold = new Color32(235, 181, 73, 255);
            Color32 blue = new Color32(90, 205, 255, 255);
            Color32 steel = new Color32(180, 193, 202, 255);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            Fill(texture, 6, 5, 17, 18, outline);
            Fill(texture, 7, 6, 16, 17, gold);
            Fill(texture, 9, 8, 14, 14, outline);
            Fill(texture, 10, 9, 13, 13, blue);
            Fill(texture, 4, 18, 19, 20, outline);
            Fill(texture, 5, 18, 18, 19, steel);
            Fill(texture, 11, 3, 12, 21, new Color32(255, 240, 154, 170));

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 24f, 24f), new Vector2(0.5f, 0.12f), 24f);
        }

        private static void Fill(Texture2D texture, int minX, int minY, int maxX, int maxY, Color32 color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}
