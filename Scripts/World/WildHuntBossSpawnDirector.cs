using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public class WildHuntBossSpawnDirector : MonoBehaviour
    {
        [SerializeField] private float spawnDelay = 1.2f;
        [SerializeField] private Vector2 spawnPosition = new Vector2(48f, -1.58f);
        [SerializeField] private string bossName = "Black Moon Stalker";
        [SerializeField] private bool autoSpawn = true;
        [SerializeField] private bool hordeMode = true;
        [SerializeField] private float spawnInterval = 1.25f;
        [SerializeField] private int spawnBatchSize = 2;
        [SerializeField] private int maxAliveBosses = 10;
        [SerializeField] private float spawnDistanceAhead = 10f;
        [SerializeField] private float spawnDistanceBehind = 7f;
        [SerializeField] private float minStageX = -7.6f;
        [SerializeField] private float maxStageX = 55.6f;
        [SerializeField] private float minSpawnY = -2.35f;
        [SerializeField] private float maxSpawnY = 3.8f;
        [SerializeField] private int hordeHealth = 3;
        [SerializeField] private float hordeMoveSpeed = 2.15f;
        [SerializeField] private float hordeScale = 0.58f;
        [SerializeField] private bool hordeDropsEquipment;

        private readonly List<WildHuntBossController> activeBosses = new List<WildHuntBossController>();
        private GeraltController player;
        private float timer;
        private bool spawned;
        private bool spawnRequested;

        private void Update()
        {
            RemoveDestroyedBosses();

            if (!autoSpawn && !spawnRequested)
            {
                return;
            }

            timer += Time.deltaTime;
            float currentDelay = spawned && hordeMode ? spawnInterval : spawnDelay;
            if (timer < currentDelay)
            {
                return;
            }

            timer = 0f;
            if (hordeMode)
            {
                spawned = true;
                SpawnHordeBatch();
                return;
            }

            if (!spawned)
            {
                spawned = true;
                SpawnBoss(spawnPosition, false);
            }
        }

        public void RequestBossSpawn()
        {
            if (spawnRequested)
            {
                return;
            }

            spawnRequested = true;
            timer = 0f;
        }

        public void ApplySurvivorWaveTuning(int wave)
        {
            int clampedWave = Mathf.Max(1, wave);
            spawnInterval = Mathf.Max(0.48f, 1.25f - (clampedWave - 1) * 0.08f);
            spawnBatchSize = Mathf.Clamp(2 + (clampedWave - 1) / 2, 2, 5);
            maxAliveBosses = Mathf.Clamp(10 + clampedWave * 2, 10, 26);
            hordeHealth = Mathf.Clamp(3 + (clampedWave - 1) / 2, 3, 10);
            hordeMoveSpeed = Mathf.Min(3.45f, 2.15f + (clampedWave - 1) * 0.08f);
        }

        private void SpawnHordeBatch()
        {
            int openSlots = Mathf.Max(0, maxAliveBosses - activeBosses.Count);
            int count = Mathf.Min(Mathf.Max(1, spawnBatchSize), openSlots);
            for (int i = 0; i < count; i++)
            {
                SpawnBoss(GetHordeSpawnPosition(i), true);
            }
        }

        private Vector2 GetHordeSpawnPosition(int index)
        {
            if (player == null)
            {
                player = FindObjectOfType<GeraltController>();
            }

            if (player == null)
            {
                return spawnPosition;
            }

            float side = (Time.frameCount + index) % 2 == 0 ? 1f : -1f;
            float distance = side > 0f ? spawnDistanceAhead : spawnDistanceBehind;
            float x = player.transform.position.x + side * distance + Random.Range(-1.2f, 1.2f);
            float y = player.transform.position.y + Random.Range(-1.45f, 1.45f);

            return new Vector2(
                Mathf.Clamp(x, minStageX, maxStageX),
                Mathf.Clamp(y, minSpawnY, maxSpawnY));
        }

        private void SpawnBoss(Vector2 position, bool useHordeTuning)
        {
            GameObject bossObject = new GameObject(bossName);
            bossObject.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer spriteRenderer = bossObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 3;

            bossObject.AddComponent<Rigidbody2D>();
            bossObject.AddComponent<BoxCollider2D>();
            bossObject.AddComponent<WildHuntBossAnimator>();
            WildHuntBossController boss = bossObject.AddComponent<WildHuntBossController>();
            if (useHordeTuning)
            {
                boss.ConfigureHordeVariant(hordeHealth, hordeMoveSpeed, hordeScale, hordeDropsEquipment);
                activeBosses.Add(boss);
            }
        }

        private void RemoveDestroyedBosses()
        {
            for (int i = activeBosses.Count - 1; i >= 0; i--)
            {
                if (activeBosses[i] == null)
                {
                    activeBosses.RemoveAt(i);
                }
            }
        }
    }
}
