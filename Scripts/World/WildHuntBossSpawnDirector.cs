using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public enum WitcherFixedEncounterKind
    {
        CorruptedWolf,
        BloodWraith,
        BlackMoonKnight
    }

    [System.Serializable]
    public struct WitcherFixedEncounterPoint
    {
        public WitcherFixedEncounterKind kind;
        public Vector2 position;
        public int enemyCount;
    }

    public class WildHuntBossSpawnDirector : MonoBehaviour
    {
        [SerializeField] private float spawnDelay = 1.2f;
        [SerializeField] private Vector2 spawnPosition = new Vector2(48f, -1.58f);
        [SerializeField] private string bossName = "Black Moon Stalker";
        [SerializeField] private bool autoSpawn = true;
        [SerializeField] private bool hordeMode = true;
        [SerializeField] private bool turnBasedEncounterMode = true;
        [SerializeField] private bool useFixedEncounterPositions = true;
        [SerializeField] private WitcherFixedEncounterPoint[] fixedEncounterPoints =
        {
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.CorruptedWolf, position = new Vector2(5.8f, -1.55f), enemyCount = 2 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(13.2f, -0.65f), enemyCount = 1 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.CorruptedWolf, position = new Vector2(21.4f, 0.35f), enemyCount = 3 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(31.2f, -1.85f), enemyCount = 2 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BlackMoonKnight, position = new Vector2(43.6f, -0.9f), enemyCount = 1 }
        };
        [SerializeField] private float spawnInterval = 1.45f;
        [SerializeField] private int spawnBatchSize = 2;
        [SerializeField] private int maxAliveBosses = 8;
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
        [SerializeField, Range(0f, 1f)] private float corruptedWolfWeight = 0.42f;
        [SerializeField, Range(0f, 1f)] private float bloodWraithWeight = 0.34f;
        [SerializeField, Range(0f, 1f)] private float blackKnightWeight = 0.24f;

        private readonly List<GameObject> activeHordeEnemies = new List<GameObject>();
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
            if (turnBasedEncounterMode && useFixedEncounterPositions)
            {
                if (!spawned)
                {
                    spawned = true;
                    SpawnFixedEncounterSet();
                }

                return;
            }

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

        private void SpawnFixedEncounterSet()
        {
            WitcherFixedEncounterPoint[] points = fixedEncounterPoints == null || fixedEncounterPoints.Length == 0
                ? GetDefaultFixedEncounters()
                : fixedEncounterPoints;

            for (int i = 0; i < points.Length; i++)
            {
                WitcherFixedEncounterPoint point = points[i];
                Vector2 position = new Vector2(
                    Mathf.Clamp(point.position.x, minStageX, maxStageX),
                    Mathf.Clamp(point.position.y, minSpawnY, maxSpawnY));

                switch (point.kind)
                {
                    case WitcherFixedEncounterKind.BloodWraith:
                        SpawnHordeMonster(position, WitcherHordeMonsterKind.BloodWraith, Mathf.Max(1, point.enemyCount));
                        break;
                    case WitcherFixedEncounterKind.BlackMoonKnight:
                        SpawnBoss(position, true);
                        break;
                    default:
                        SpawnHordeMonster(position, WitcherHordeMonsterKind.CorruptedWolf, Mathf.Max(1, point.enemyCount));
                        break;
                }
            }
        }

        private WitcherFixedEncounterPoint[] GetDefaultFixedEncounters()
        {
            return new[]
            {
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.CorruptedWolf, position = new Vector2(5.8f, -1.55f), enemyCount = 2 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(13.2f, -0.65f), enemyCount = 1 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.CorruptedWolf, position = new Vector2(21.4f, 0.35f), enemyCount = 3 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(31.2f, -1.85f), enemyCount = 2 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BlackMoonKnight, position = new Vector2(43.6f, -0.9f), enemyCount = 1 }
            };
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
            spawnInterval = Mathf.Max(0.62f, 1.45f - (clampedWave - 1) * 0.07f);
            spawnBatchSize = Mathf.Clamp(2 + (clampedWave - 1) / 3, 2, 4);
            maxAliveBosses = Mathf.Clamp(8 + clampedWave * 2, 8, 22);
            hordeHealth = Mathf.Clamp(3 + (clampedWave - 1) / 3, 3, 8);
            hordeMoveSpeed = Mathf.Min(3.15f, 2.05f + (clampedWave - 1) * 0.07f);
        }

        private void SpawnHordeBatch()
        {
            int openSlots = Mathf.Max(0, maxAliveBosses - activeHordeEnemies.Count);
            int count = turnBasedEncounterMode ? Mathf.Min(1, openSlots) : Mathf.Min(Mathf.Max(1, spawnBatchSize), openSlots);
            for (int i = 0; i < count; i++)
            {
                SpawnMixedHordeEnemy(GetHordeSpawnPosition(i));
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
                activeHordeEnemies.Add(bossObject);
            }

            if (turnBasedEncounterMode)
            {
                BattleEncounterTrigger trigger = bossObject.AddComponent<BattleEncounterTrigger>();
                trigger.ConfigureBoss(spriteRenderer.sprite, Mathf.Max(0, hordeHealth - 3));
            }
        }

        private void SpawnMixedHordeEnemy(Vector2 position)
        {
            float totalWeight = Mathf.Max(0.01f, corruptedWolfWeight + bloodWraithWeight + blackKnightWeight);
            float roll = Random.value * totalWeight;
            if (roll < corruptedWolfWeight)
            {
                SpawnHordeMonster(position, WitcherHordeMonsterKind.CorruptedWolf);
                return;
            }

            if (roll < corruptedWolfWeight + bloodWraithWeight)
            {
                SpawnHordeMonster(position, WitcherHordeMonsterKind.BloodWraith);
                return;
            }

            SpawnBoss(position, true);
        }

        private void SpawnHordeMonster(Vector2 position, WitcherHordeMonsterKind kind)
        {
            int encounterCount = kind == WitcherHordeMonsterKind.CorruptedWolf ? Random.Range(2, 4) : Random.Range(1, 3);
            SpawnHordeMonster(position, kind, encounterCount);
        }

        private void SpawnHordeMonster(Vector2 position, WitcherHordeMonsterKind kind, int encounterCount)
        {
            string enemyName = kind == WitcherHordeMonsterKind.BloodWraith ? "Blood Wraith" : "Corrupted Wolf";
            GameObject enemyObject = new GameObject(enemyName);
            enemyObject.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 3;

            enemyObject.AddComponent<BoxCollider2D>();
            WitcherHordeMonsterController enemy = enemyObject.AddComponent<WitcherHordeMonsterController>();
            int waveHealthBonus = Mathf.Max(0, hordeHealth - 3);
            float speedBonus = Mathf.Max(0f, hordeMoveSpeed - 2.05f) * 0.35f;
            enemy.Configure(kind, waveHealthBonus, speedBonus);
            if (turnBasedEncounterMode)
            {
                BattleEncounterTrigger trigger = enemyObject.AddComponent<BattleEncounterTrigger>();
                trigger.ConfigureMonster(kind, spriteRenderer.sprite, Mathf.Max(1, encounterCount), waveHealthBonus);
            }

            activeHordeEnemies.Add(enemyObject);
        }

        private void RemoveDestroyedBosses()
        {
            for (int i = activeHordeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeHordeEnemies[i] == null)
                {
                    activeHordeEnemies.RemoveAt(i);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!useFixedEncounterPositions)
            {
                return;
            }

            WitcherFixedEncounterPoint[] points = fixedEncounterPoints == null || fixedEncounterPoints.Length == 0
                ? GetDefaultFixedEncounters()
                : fixedEncounterPoints;
            for (int i = 0; i < points.Length; i++)
            {
                switch (points[i].kind)
                {
                    case WitcherFixedEncounterKind.BloodWraith:
                        Gizmos.color = new Color(0.78f, 0.25f, 0.92f, 0.85f);
                        break;
                    case WitcherFixedEncounterKind.BlackMoonKnight:
                        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.85f);
                        break;
                    default:
                        Gizmos.color = new Color(0.45f, 0.85f, 0.45f, 0.85f);
                        break;
                }

                Gizmos.DrawWireSphere(points[i].position, 0.55f);
            }
        }
    }
}
