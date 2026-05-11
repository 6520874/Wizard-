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
        [SerializeField] private bool autoSpawn = true;
        [SerializeField] private float spawnDelay = 0.35f;
        [SerializeField] private float minStageX = -7.6f;
        [SerializeField] private float maxStageX = 55.6f;
        [SerializeField] private float minStageY = -2.35f;
        [SerializeField] private float maxStageY = 3.8f;
        [SerializeField] private WitcherFixedEncounterPoint[] fixedEncounterPoints =
        {
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BlackMoonKnight, position = new Vector2(5.8f, -1.55f), enemyCount = 1 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(13.2f, -0.65f), enemyCount = 1 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.CorruptedWolf, position = new Vector2(21.4f, 0.35f), enemyCount = 3 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(31.2f, -1.85f), enemyCount = 2 },
            new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BlackMoonKnight, position = new Vector2(43.6f, -0.9f), enemyCount = 1 }
        };

        private readonly List<GameObject> activeEncounters = new List<GameObject>();
        private float timer;
        private bool spawned;
        private bool spawnRequested;

        private void Update()
        {
            RemoveDestroyedEncounters();
            if (spawned || (!autoSpawn && !spawnRequested))
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer < spawnDelay)
            {
                return;
            }

            spawned = true;
            SpawnFixedEncounterSet();
        }

        public void RequestBossSpawn()
        {
            spawnRequested = true;
            timer = 0f;
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
                    Mathf.Clamp(point.position.y, minStageY, maxStageY));

                switch (point.kind)
                {
                    case WitcherFixedEncounterKind.BloodWraith:
                        SpawnEncounter(position, WitcherHordeMonsterKind.BloodWraith, Mathf.Max(1, point.enemyCount));
                        break;
                    case WitcherFixedEncounterKind.BlackMoonKnight:
                        SpawnKnightEncounter(position);
                        break;
                    default:
                        SpawnEncounter(position, WitcherHordeMonsterKind.CorruptedWolf, Mathf.Max(1, point.enemyCount));
                        break;
                }
            }
        }

        private static WitcherFixedEncounterPoint[] GetDefaultFixedEncounters()
        {
            return new[]
            {
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BlackMoonKnight, position = new Vector2(5.8f, -1.55f), enemyCount = 1 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(13.2f, -0.65f), enemyCount = 1 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.CorruptedWolf, position = new Vector2(21.4f, 0.35f), enemyCount = 3 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BloodWraith, position = new Vector2(31.2f, -1.85f), enemyCount = 2 },
                new WitcherFixedEncounterPoint { kind = WitcherFixedEncounterKind.BlackMoonKnight, position = new Vector2(43.6f, -0.9f), enemyCount = 1 }
            };
        }

        private void SpawnEncounter(Vector2 position, WitcherHordeMonsterKind kind, int encounterCount)
        {
            string enemyName = kind == WitcherHordeMonsterKind.BloodWraith ? "Blood Wraith Encounter" : "Corrupted Wolf Encounter";
            GameObject enemyObject = CreateEncounterObject(enemyName, position);
            BattleEncounterTrigger trigger = enemyObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureMonster(kind, enemyObject.GetComponent<SpriteRenderer>().sprite, Mathf.Max(1, encounterCount), 0);
            activeEncounters.Add(enemyObject);
        }

        private void SpawnKnightEncounter(Vector2 position)
        {
            GameObject knightObject = CreateEncounterObject("Moonlit Knight Encounter", position);
            BattleEncounterTrigger trigger = knightObject.AddComponent<BattleEncounterTrigger>();
            trigger.ConfigureBoss(knightObject.GetComponent<SpriteRenderer>().sprite, 0);
            activeEncounters.Add(knightObject);
        }

        private static GameObject CreateEncounterObject(string objectName, Vector2 position)
        {
            GameObject encounterObject = new GameObject(objectName);
            encounterObject.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer spriteRenderer = encounterObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 3;

            Rigidbody2D body = encounterObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = encounterObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return encounterObject;
        }

        private void RemoveDestroyedEncounters()
        {
            for (int i = activeEncounters.Count - 1; i >= 0; i--)
            {
                if (activeEncounters[i] == null)
                {
                    activeEncounters.RemoveAt(i);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
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
