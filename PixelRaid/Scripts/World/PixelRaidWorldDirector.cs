using System.Collections.Generic;
using UnityEngine;

namespace PixelRaid
{
    public class PixelRaidWorldDirector : MonoBehaviour
    {
        private const float RoadY = -1.65f;
        private const float MinRoadX = -7.6f;
        private const float MaxRoadX = 7.6f;

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        private PixelRaidPlayerController player;
        private PixelRaidPlayerHud hud;
        private int roomIndex;

        private readonly RoomDefinition[] rooms =
        {
            new RoomDefinition("霜林边境", new Color32(180, 210, 230, 255)),
            new RoomDefinition("沉没沼泽", new Color32(91, 145, 139, 255)),
            new RoomDefinition("墓园旧道", new Color32(128, 145, 166, 255))
        };

        public static PixelRaidWorldDirector CreateIfMissing(PixelRaidPlayerController target)
        {
            PixelRaidWorldDirector existing = FindObjectOfType<PixelRaidWorldDirector>();
            if (existing != null)
            {
                existing.SetPlayer(target);
                return existing;
            }

            GameObject directorObject = new GameObject("PixelRaid World Director");
            PixelRaidWorldDirector director = directorObject.AddComponent<PixelRaidWorldDirector>();
            director.SetPlayer(target);
            return director;
        }

        public void SetPlayer(PixelRaidPlayerController target)
        {
            player = target;
        }

        private void Start()
        {
            player = player == null ? FindObjectOfType<PixelRaidPlayerController>() : player;
            hud = FindObjectOfType<PixelRaidPlayerHud>();
            EnterRoom(0, false);
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindObjectOfType<PixelRaidPlayerController>();
                return;
            }

            if (player.transform.position.x >= MaxRoadX - 0.05f && Input.GetAxisRaw("Horizontal") > 0f)
            {
                EnterRoom(roomIndex + 1, true);
            }
            else if (player.transform.position.x <= MinRoadX + 0.05f && Input.GetAxisRaw("Horizontal") < 0f)
            {
                EnterRoom(roomIndex - 1, true);
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

            player.ConfigureRoad(RoadY, MinRoadX, MaxRoadX);
            if (preserveDirection)
            {
                float spawnX = requestedRoomIndex > previousRoom ? MinRoadX + 0.45f : MaxRoadX - 0.45f;
                player.WarpTo(new Vector2(spawnX, RoadY));
            }

            ApplyRoomLook(room);
            RespawnEnemies(room);

            hud = hud == null ? FindObjectOfType<PixelRaidPlayerHud>() : hud;
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
                enemyObject.transform.position = new Vector3(spawn.X, RoadY, 0f);

                SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 2;

                enemyObject.AddComponent<BoxCollider2D>();
                PixelRaidEnemyPatrol enemy = enemyObject.AddComponent<PixelRaidEnemyPatrol>();
                float patrolWidth = spawn.Kind == PixelRaidEnemyKind.Drowner ? 1.4f : 2.1f;
                enemy.Configure(spawn.Kind, new Vector2(patrolWidth, 0f));
                spawnedEnemies.Add(enemyObject);
            }
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
            public EnemySpawn(PixelRaidEnemyKind kind, float x)
            {
                Kind = kind;
                X = x;
            }

            public PixelRaidEnemyKind Kind { get; }
            public float X { get; }
        }
    }
}
