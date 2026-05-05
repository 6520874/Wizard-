using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public class WitcherWorldDirector : MonoBehaviour
    {
        private const float StartY = -1.65f;
        private const float MinStageX = -7.6f;
        private const float MaxStageX = 54f;
        private const float MinStageY = -2.55f;
        private const float MaxStageY = 5f;

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        private GeraltController player;
        private WitcherHud hud;
        private int roomIndex;

        private readonly RoomDefinition[] rooms =
        {
            new RoomDefinition(
                "威伦荒村长路",
                new Color32(170, 190, 205, 255),
                new EnemySpawn(MonsterKind.Nekker, 4.8f, -2.06f),
                new EnemySpawn(MonsterKind.Drowner, 9.6f, -1.28f),
                new EnemySpawn(MonsterKind.Wraith, 15.4f, -2.2f),
                new EnemySpawn(MonsterKind.Nekker, 22.5f, -1.08f),
                new EnemySpawn(MonsterKind.Drowner, 30.6f, -1.82f),
                new EnemySpawn(MonsterKind.Wraith, 39.4f, -1.16f))
        };

        public static WitcherWorldDirector CreateIfMissing(GeraltController target)
        {
            WitcherWorldDirector existing = FindObjectOfType<WitcherWorldDirector>();
            if (existing != null)
            {
                existing.SetPlayer(target);
                return existing;
            }

            GameObject directorObject = new GameObject("Witcher World Director");
            WitcherWorldDirector director = directorObject.AddComponent<WitcherWorldDirector>();
            director.SetPlayer(target);
            return director;
        }

        public void SetPlayer(GeraltController target)
        {
            player = target;
        }

        private void Start()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            hud = FindObjectOfType<WitcherHud>();
            EnterRoom(0, false);
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindObjectOfType<GeraltController>();
                return;
            }
        }

        private void EnterRoom(int requestedRoomIndex, bool preserveDirection)
        {
            if (rooms.Length == 0)
            {
                return;
            }

            int previousRoom = roomIndex;
            roomIndex = (requestedRoomIndex + rooms.Length) % rooms.Length;
            RoomDefinition room = rooms[roomIndex];

            player.ConfigureStage(MinStageX, MaxStageX, MinStageY, MaxStageY);
            if (preserveDirection)
            {
                float spawnX = requestedRoomIndex > previousRoom ? MinStageX + 0.45f : MaxStageX - 0.45f;
                player.WarpTo(new Vector2(spawnX, StartY));
            }
            else
            {
                player.WarpTo(new Vector2(MinStageX + 1.15f, StartY));
            }

            ApplyRoomLook(room);
            EnsureCameraFollow();
            RespawnEnemies(room);

            hud = hud == null ? FindObjectOfType<WitcherHud>() : hud;
            if (hud != null)
            {
                hud.SetRoomName(room.Name);
            }
        }

        private void ApplyRoomLook(RoomDefinition room)
        {
            GameObject background = GameObject.Find("Background");
            if (background != null && background.TryGetComponent(out SpriteRenderer backgroundRenderer))
            {
                backgroundRenderer.color = room.BackgroundTint;
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = Color.Lerp(new Color32(4, 5, 6, 255), room.BackgroundTint, 0.18f);
            }
        }

        private void RespawnEnemies(RoomDefinition room)
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null)
                {
                    Destroy(spawnedEnemies[i]);
                }
            }

            spawnedEnemies.Clear();

            for (int i = 0; i < room.Enemies.Length; i++)
            {
                EnemySpawn spawn = room.Enemies[i];
                GameObject enemyObject = new GameObject($"{spawn.Kind} Enemy");
                enemyObject.transform.position = new Vector3(spawn.X, spawn.Y, 0f);

                SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 2;

                enemyObject.AddComponent<BoxCollider2D>();
                MonsterPatrol enemy = enemyObject.AddComponent<MonsterPatrol>();
                Vector2 patrol = spawn.Kind == MonsterKind.Drowner ? new Vector2(1.5f, 0.38f) : new Vector2(2.4f, 0.28f);
                enemy.Configure(spawn.Kind, patrol);
                spawnedEnemies.Add(enemyObject);
            }
        }

        private void EnsureCameraFollow()
        {
            Camera camera = Camera.main;
            if (camera == null || player == null)
            {
                return;
            }

            camera.orthographicSize = 3.8f;
            WitcherCameraFollow follow = camera.GetComponent<WitcherCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<WitcherCameraFollow>();
            }

            follow.SetTarget(player.transform);
            follow.ConfigureBounds(MinStageX, MaxStageX, MinStageY + 1.25f, MaxStageY);
        }

        private readonly struct RoomDefinition
        {
            public RoomDefinition(string name, Color32 backgroundTint, params EnemySpawn[] enemies)
            {
                Name = name;
                BackgroundTint = backgroundTint;
                Enemies = enemies;
            }

            public string Name { get; }
            public Color32 BackgroundTint { get; }
            public EnemySpawn[] Enemies { get; }
        }

        private readonly struct EnemySpawn
        {
            public EnemySpawn(MonsterKind kind, float x, float y)
            {
                Kind = kind;
                X = x;
                Y = y;
            }

            public MonsterKind Kind { get; }
            public float X { get; }
            public float Y { get; }
        }
    }
}
