using UnityEngine;

namespace WitcherGame
{
    // 中文说明：组织当前地图房间、主角出生点、边界和地图外观刷新。
    public class WitcherWorldDirector : MonoBehaviour
    {
        private const string SecondNightMusicPath = "Music/Witcher_SecondNight_BlackNailForge";
        private const string ThirdNightMusicPath = "Music/Witcher_ThirdNight_BlackWaxCrypt";
        private const float StartY = -1.65f;
        private const float MinStageX = -7.6f;
        private const float MaxStageX = 55.6f;
        private const float MinStageY = -2.55f;
        private const float MaxStageY = 5f;
        private const float VillageStartX = -0.2f;
        private const float VillageStartY = -1.6f;
        private const float VillageMinStageX = -10.8f;
        private const float VillageMaxStageX = 10.8f;
        private const float VillageMinStageY = -5.75f;
        private const float VillageMaxStageY = 4.75f;
        private const float SecondNightStartX = -7.2f;
        private const float SecondNightStartY = -2.45f;
        private const float SecondNightMinStageX = -10.8f;
        private const float SecondNightMaxStageX = 10.8f;
        private const float SecondNightMinStageY = -5.2f;
        private const float SecondNightMaxStageY = 4.25f;
        private const float ThirdNightStartX = -7.2f;
        private const float ThirdNightStartY = -2.45f;
        private const float ThirdNightMinStageX = -10.8f;
        private const float ThirdNightMaxStageX = 10.8f;
        private const float ThirdNightMinStageY = -5.2f;
        private const float ThirdNightMaxStageY = 4.25f;

        private GeraltController player;
        private WitcherHud hud;
        private int roomIndex;

        private readonly RoomDefinition[] rooms =
        {
            new RoomDefinition(
                "威伦荒村长路",
                new Color32(168, 196, 214, 255))
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
            WitcherMusicPlayer.CreateIfMissing();
            WitcherSfxPlayer.CreateIfMissing();
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
            StageBounds stageBounds = GetActiveStageBounds();

            player.ConfigureStage(stageBounds.MinX, stageBounds.MaxX, stageBounds.MinY, stageBounds.MaxY);
            player.ConfigureExplorationView(stageBounds.IsIsometricVillage ? 0.56f : 0.76f, stageBounds.IsIsometricVillage ? 3.45f : 5f, stageBounds.IsIsometricVillage ? 1.82f : 3.25f);
            if (preserveDirection)
            {
                float spawnX = requestedRoomIndex > previousRoom ? stageBounds.MinX + 0.45f : stageBounds.MaxX - 0.45f;
                player.WarpTo(new Vector2(spawnX, stageBounds.StartY));
            }
            else
            {
                player.WarpTo(new Vector2(stageBounds.StartX, stageBounds.StartY));
            }

            ApplyRoomLook(room);
            NightContractManager.CreateIfMissing(player);
            EnsureCameraFollow(stageBounds);

            hud = hud == null ? FindObjectOfType<WitcherHud>() : hud;
            if (hud != null)
            {
                hud.SetRoomName(room.Name);
            }
        }

        public void EnterFirstNightMap()
        {
            GameObject background = GameObject.Find("Background");
            if (background != null && background.TryGetComponent(out WitcherRuntimeBackground runtimeBackground))
            {
                runtimeBackground.ApplyFirstNightMap();
            }

            EnterRoom(0, false);
        }

        public void EnterSecondNightMap()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null)
            {
                return;
            }

            GameObject background = GameObject.Find("Background");
            if (background != null && background.TryGetComponent(out WitcherRuntimeBackground runtimeBackground))
            {
                runtimeBackground.ApplySecondNightMap();
            }

            WitcherMusicPlayer.PlayMusic(SecondNightMusicPath);

            RoomDefinition room = new RoomDefinition("第二晚：铁匠铺与教堂", new Color32(122, 152, 184, 255));
            StageBounds stageBounds = GetActiveStageBounds();
            player.ConfigureStage(stageBounds.MinX, stageBounds.MaxX, stageBounds.MinY, stageBounds.MaxY);
            player.ConfigureExplorationView(0.56f, 3.45f, 1.82f);
            player.WarpTo(new Vector2(stageBounds.StartX, stageBounds.StartY));

            ApplyRoomLook(room);
            EnsureCameraFollow(stageBounds);

            hud = hud == null ? FindObjectOfType<WitcherHud>() : hud;
            if (hud != null)
            {
                hud.SetRoomName(room.Name);
            }
        }

        public void EnterThirdNightMap()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null)
            {
                return;
            }

            GameObject background = GameObject.Find("Background");
            if (background != null && background.TryGetComponent(out WitcherRuntimeBackground runtimeBackground))
            {
                runtimeBackground.ApplyThirdNightMap();
            }

            WitcherMusicPlayer.PlayMusic(ThirdNightMusicPath);

            RoomDefinition room = new RoomDefinition("第三晚：黑蜡地下教堂", new Color32(132, 112, 152, 255));
            StageBounds stageBounds = GetActiveStageBounds();
            player.ConfigureStage(stageBounds.MinX, stageBounds.MaxX, stageBounds.MinY, stageBounds.MaxY);
            player.ConfigureExplorationView(0.56f, 3.45f, 1.82f);
            player.WarpTo(new Vector2(stageBounds.StartX, stageBounds.StartY));

            ApplyRoomLook(room);
            EnsureCameraFollow(stageBounds);

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
                WitcherRuntimeBackground runtimeBackground = background.GetComponent<WitcherRuntimeBackground>();
                EnsureVillageWalkableMap(background, runtimeBackground != null && (runtimeBackground.IsUsingPreferredMap || runtimeBackground.IsUsingSecondNightMap || runtimeBackground.IsUsingThirdNightMap));
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = Color.Lerp(new Color32(4, 5, 6, 255), room.BackgroundTint, 0.18f);
            }
        }

        private void EnsureVillageWalkableMap(GameObject background, bool isEnabled)
        {
            WitcherVillageWalkableMap walkableMap = background.GetComponent<WitcherVillageWalkableMap>();
            if (walkableMap == null && isEnabled)
            {
                walkableMap = background.AddComponent<WitcherVillageWalkableMap>();
            }

            if (walkableMap != null)
            {
                walkableMap.enabled = isEnabled;
            }
        }

        private StageBounds GetActiveStageBounds()
        {
            WitcherRuntimeBackground runtimeBackground = FindObjectOfType<WitcherRuntimeBackground>();
            if (runtimeBackground != null && runtimeBackground.IsUsingThirdNightMap)
            {
                return new StageBounds(
                    ThirdNightStartX,
                    ThirdNightStartY,
                    ThirdNightMinStageX,
                    ThirdNightMaxStageX,
                    ThirdNightMinStageY,
                    ThirdNightMaxStageY,
                    true);
            }

            if (runtimeBackground != null && runtimeBackground.IsUsingSecondNightMap)
            {
                return new StageBounds(
                    SecondNightStartX,
                    SecondNightStartY,
                    SecondNightMinStageX,
                    SecondNightMaxStageX,
                    SecondNightMinStageY,
                    SecondNightMaxStageY,
                    true);
            }

            if (runtimeBackground != null && runtimeBackground.IsUsingPreferredMap)
            {
                return new StageBounds(
                    VillageStartX,
                    VillageStartY,
                    VillageMinStageX,
                    VillageMaxStageX,
                    VillageMinStageY,
                    VillageMaxStageY,
                    true);
            }

            return new StageBounds(
                MinStageX + 1.15f,
                StartY,
                MinStageX,
                MaxStageX,
                MinStageY,
                MaxStageY,
                false);
        }

        private void EnsureCameraFollow(StageBounds stageBounds)
        {
            Camera camera = Camera.main;
            if (camera == null || player == null)
            {
                return;
            }

            camera.orthographicSize = stageBounds.IsIsometricVillage ? 5.35f : 3.8f;
            WitcherCameraFollow follow = camera.GetComponent<WitcherCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<WitcherCameraFollow>();
            }

            follow.SetTarget(player.transform);
            follow.ConfigureBounds(stageBounds.MinX, stageBounds.MaxX, stageBounds.MinY + 0.75f, stageBounds.MaxY);
            follow.ConfigureView(stageBounds.IsIsometricVillage ? new Vector3(0.15f, 1.45f, -10f) : new Vector3(2.4f, 1.25f, -10f), stageBounds.IsIsometricVillage ? 0.24f : 0.18f);
        }

        // 中文说明：保存当前地图的出生点、相机边界和是否为等距村庄地图。
        private readonly struct StageBounds
        {
            public StageBounds(float startX, float startY, float minX, float maxX, float minY, float maxY, bool isIsometricVillage)
            {
                StartX = startX;
                StartY = startY;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                IsIsometricVillage = isIsometricVillage;
            }

            public float StartX { get; }
            public float StartY { get; }
            public float MinX { get; }
            public float MaxX { get; }
            public float MinY { get; }
            public float MaxY { get; }
            public bool IsIsometricVillage { get; }
        }

        // 中文说明：保存一个房间/地图段的名字和背景色配置。
        private readonly struct RoomDefinition
        {
            public RoomDefinition(
                string name,
                Color32 backgroundTint)
            {
                Name = name;
                BackgroundTint = backgroundTint;
            }

            public string Name { get; }
            public Color32 BackgroundTint { get; }
        }
    }
}
