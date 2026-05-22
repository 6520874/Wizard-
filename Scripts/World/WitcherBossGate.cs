using UnityEngine;

namespace WitcherGame
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    // 中文说明：处理地图首领入口或战斗门禁的触发逻辑。
    public class WitcherBossGate : MonoBehaviour
    {
        [SerializeField] private float openFadeDuration = 0.45f;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private bool opening;
        private float openTimer;
        private Vector3 startScale;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(17, 23, 29, 220));
            spriteRenderer.color = new Color32(17, 23, 29, 220);
            spriteRenderer.sortingOrder = 4;

            boxCollider = GetComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(1.8f, 2.6f);
            boxCollider.offset = new Vector2(0f, 0.85f);

            transform.localScale = new Vector3(1.15f, 2.4f, 1f);
            startScale = transform.localScale;
        }

        private void Update()
        {
            if (!opening)
            {
                return;
            }

            openTimer += Time.deltaTime;
            float t = Mathf.Clamp01(openTimer / openFadeDuration);
            transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x * 0.35f, startScale.y * 1.08f, 1f), t);
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(0.86f, 0f, t);
            spriteRenderer.color = color;

            if (t >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (opening || !other.TryGetComponent(out GeraltController _))
            {
                return;
            }

            WildHuntBossSpawnDirector spawner = FindObjectOfType<WildHuntBossSpawnDirector>();
            if (spawner != null)
            {
                spawner.RequestBossSpawn();
            }

            WitcherCombatText.Spawn("门后的寒意苏醒了", transform.position + Vector3.up * 1.8f, new Color32(138, 222, 255, 255));
            opening = true;
            openTimer = 0f;
            boxCollider.enabled = false;
        }
    }
}
