using UnityEngine;

namespace PixelRaid
{
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelRaidCoin : MonoBehaviour
    {
        [SerializeField] private float floatAmplitude = 0.12f;
        [SerializeField] private float floatSpeed = 3f;
        [SerializeField] private PixelRaidGameManager gameManager;

        private Vector3 startPosition;

        private void Awake()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(255, 221, 89, 255));
            transform.localScale = Vector3.one * 0.45f;

            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            startPosition = transform.position;
        }

        private void Update()
        {
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
        }

        public void SetGameManager(PixelRaidGameManager manager)
        {
            gameManager = manager;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PixelRaidPlayerController _))
            {
                return;
            }

            if (gameManager != null)
            {
                gameManager.RegisterCoin();
            }

            Destroy(gameObject);
        }
    }
}
