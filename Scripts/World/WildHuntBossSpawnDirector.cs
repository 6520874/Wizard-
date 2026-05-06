using UnityEngine;

namespace WitcherGame
{
    public class WildHuntBossSpawnDirector : MonoBehaviour
    {
        [SerializeField] private float spawnDelay = 2.25f;
        [SerializeField] private Vector2 spawnPosition = new Vector2(224f, -1.58f);
        [SerializeField] private string bossName = "Wild Hunt Boss";
        [SerializeField] private bool autoSpawn;

        private float timer;
        private bool spawned;
        private bool spawnRequested;

        private void Update()
        {
            if (spawned)
            {
                return;
            }

            if (!autoSpawn && !spawnRequested)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= spawnDelay)
            {
                spawned = true;
                SpawnBoss();
            }
        }

        public void RequestBossSpawn()
        {
            if (spawned || spawnRequested)
            {
                return;
            }

            spawnRequested = true;
            timer = 0f;
        }

        private void SpawnBoss()
        {
            GameObject bossObject = new GameObject(bossName);
            bossObject.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);

            SpriteRenderer spriteRenderer = bossObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 3;

            bossObject.AddComponent<Rigidbody2D>();
            bossObject.AddComponent<BoxCollider2D>();
            bossObject.AddComponent<WildHuntBossAnimator>();
            bossObject.AddComponent<WildHuntBossController>();
        }
    }
}
