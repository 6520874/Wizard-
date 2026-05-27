using UnityEngine;

namespace WitcherGame
{
    // 中文说明：组织当前地图房间、主角出生点、边界和地图外观刷新。
    public class WitcherWorldDirector : MonoBehaviour
    {
        private const float StartY = -1.65f;
        private const float MinStageX = -7.6f;
        private const float MaxStageX = 55.6f;
        private const float MinStageY = -2.55f;
        private const float MaxStageY = 5f;
        private const float VillageStartX = -0.35f;
        private const float VillageStartY = -1.15f;
        private const float VillageMinStageX = -12.35f;
        private const float VillageMaxStageX = 12.35f;
        private const float VillageMinStageY = -6.35f;
        private const float VillageMaxStageY = 5.85f;

        private GeraltController player;
        private WitcherHud hud;
        private int roomIndex;

        private readonly RoomDefinition[] rooms =
        {
            new RoomDefinition(
                "威伦荒村长路",
                new Color32(106, 92, 86, 255),
                new Color32(42, 19, 17, 104),
                new Color32(120, 112, 100, 82),
                new Color32(224, 82, 36, 148),
                0.48f,
                0.88f,
                0.78f)
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
            StageBounds stageBounds = GetActiveStageBounds();

            player.ConfigureStage(stageBounds.MinX, stageBounds.MaxX, stageBounds.MinY, stageBounds.MaxY);
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
            WitcherEquipmentShopTrigger.CreateIfMissing();
            WitcherStoryNpcDirector.CreateIfMissing();
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
                EnsureVillageWalkableMap(background, runtimeBackground != null && runtimeBackground.IsUsingPreferredMap);

                // WitcherAtmosphereLayer atmosphere = background.GetComponent<WitcherAtmosphereLayer>();
                // if (atmosphere == null)
                // {
                //     atmosphere = background.AddComponent<WitcherAtmosphereLayer>();
                // }
                //
                // atmosphere.ApplyCursedEmbers(
                //     room.AtmosphereTint,
                //     room.FogColor,
                //     room.EmberColor,
                //     room.FogStrength,
                //     room.VignetteStrength,
                //     room.EmberStrength);
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
            if (runtimeBackground != null && runtimeBackground.IsUsingPreferredMap)
            {
                return new StageBounds(
                    VillageStartX,
                    VillageStartY,
                    VillageMinStageX,
                    VillageMaxStageX,
                    VillageMinStageY,
                    VillageMaxStageY);
            }

            return new StageBounds(
                MinStageX + 1.15f,
                StartY,
                MinStageX,
                MaxStageX,
                MinStageY,
                MaxStageY);
        }

        private void EnsureCameraFollow(StageBounds stageBounds)
        {
            Camera camera = Camera.main;
            if (camera == null || player == null)
            {
                return;
            }

            camera.orthographicSize = stageBounds.MaxX - stageBounds.MinX > 30f ? 3.8f : 4.6f;
            WitcherCameraFollow follow = camera.GetComponent<WitcherCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<WitcherCameraFollow>();
            }

            follow.SetTarget(player.transform);
            follow.ConfigureBounds(stageBounds.MinX, stageBounds.MaxX, stageBounds.MinY + 1.1f, stageBounds.MaxY);
        }

        private readonly struct StageBounds
        {
            public StageBounds(float startX, float startY, float minX, float maxX, float minY, float maxY)
            {
                StartX = startX;
                StartY = startY;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
            }

            public float StartX { get; }
            public float StartY { get; }
            public float MinX { get; }
            public float MaxX { get; }
            public float MinY { get; }
            public float MaxY { get; }
        }

        private readonly struct RoomDefinition
        {
            public RoomDefinition(
                string name,
                Color32 backgroundTint,
                Color32 atmosphereTint,
                Color32 fogColor,
                Color32 emberColor,
                float fogStrength,
                float vignetteStrength,
                float emberStrength)
            {
                Name = name;
                BackgroundTint = backgroundTint;
                AtmosphereTint = atmosphereTint;
                FogColor = fogColor;
                EmberColor = emberColor;
                FogStrength = fogStrength;
                VignetteStrength = vignetteStrength;
                EmberStrength = emberStrength;
            }

            public string Name { get; }
            public Color32 BackgroundTint { get; }
            public Color32 AtmosphereTint { get; }
            public Color32 FogColor { get; }
            public Color32 EmberColor { get; }
            public float FogStrength { get; }
            public float VignetteStrength { get; }
            public float EmberStrength { get; }
        }
    }
}
