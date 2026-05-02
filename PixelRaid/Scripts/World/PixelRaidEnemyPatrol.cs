using UnityEngine;

namespace PixelRaid
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelRaidEnemyPatrol : MonoBehaviour
    {
        [SerializeField] private Vector2 patrolOffset = new Vector2(1.5f, 0f);
        [SerializeField] private float speed = 2f;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private bool movingToEnd = true;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = PixelRaidSpriteLibrary.GetSolidSprite(new Color32(255, 107, 107, 255));
            transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            startPosition = transform.position;
            endPosition = startPosition + (Vector3)patrolOffset;
        }

        private void Update()
        {
            Vector3 target = movingToEnd ? endPosition : startPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.02f)
            {
                movingToEnd = !movingToEnd;
            }

            spriteRenderer.flipX = movingToEnd;
        }
    }
}
