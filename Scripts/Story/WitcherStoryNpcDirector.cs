using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：在大地图上生成主线剧情 NPC，并把他们吸附到玩家可走区域。
    public class WitcherStoryNpcDirector : MonoBehaviour
    {
        private const string RootName = "Grey Raven Story NPCs";
        private const float NpcPixelsPerUnit = 820f;
        private static readonly Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

        private readonly StoryNpcDefinition[] npcDefinitions =
        {
            new StoryNpcDefinition(
                "OldVillageChief",
                "老村长",
                "Art/Story/Npcs/OldVillageChief.png",
                new Vector2(-0.8f, -0.92f),
                0.42f,
                0,
                new[]
                {
                    new DialogueManager.DialogueLine("老村长", "你看见村口的乌鸦了吗？它们这几天不吃腐肉，只盯着活人。", "Art/Story/Npcs/OldVillageChief.png"),
                    new DialogueManager.DialogueLine("猎魔人", "你说矿洞哭声像你的女儿。你还有什么没告诉我？", "Art/UI/GeraltPortrait.png"),
                    new DialogueManager.DialogueLine("老村长", "有些罪埋在地下太久，连祷告都挖不出来。先找到孩子……之后我会说。", "Art/Story/Npcs/OldVillageChief.png")
                }),
            new StoryNpcDefinition(
                "Blacksmith",
                "铁匠",
                "Art/Story/Npcs/Blacksmith.png",
                new Vector2(5.08f, -1.42f),
                0.4f,
                1,
                new[]
                {
                    new DialogueManager.DialogueLine("铁匠", "别盯着我的手。少了一只，总比整个人被矿洞吞掉强。", "Art/Story/Npcs/Blacksmith.png"),
                    new DialogueManager.DialogueLine("猎魔人", "银钉是谁让你打的？", "Art/UI/GeraltPortrait.png"),
                    new DialogueManager.DialogueLine("铁匠", "村里人说那是封矿的钉子……可钉子上刻的是献祭纹。那晚以后，我再没睡踏实过。", "Art/Story/Npcs/Blacksmith.png")
                }),
            new StoryNpcDefinition(
                "Priest",
                "神父",
                "Art/Story/Npcs/Priest.png",
                new Vector2(1.62f, 3.02f),
                0.42f,
                2,
                new[]
                {
                    new DialogueManager.DialogueLine("神父", "哭声来自不洁之物。火、盐与忏悔，会让她沉默。", "Art/Story/Npcs/Priest.png"),
                    new DialogueManager.DialogueLine("猎魔人", "如果只是女妖，你为什么封住教堂地下室？", "Art/UI/GeraltPortrait.png"),
                    new DialogueManager.DialogueLine("神父", "有些门不是为了挡住怪物，而是为了挡住软弱的人心。", "Art/Story/Npcs/Priest.png")
                }),
            new StoryNpcDefinition(
                "NobleEnvoy",
                "贵族使者",
                "Art/Story/Npcs/NobleEnvoy.png",
                new Vector2(9.78f, 0.12f),
                0.42f,
                3,
                new[]
                {
                    new DialogueManager.DialogueLine("贵族使者", "矿洞事故由领主接管。村民不得离村，外人不得入矿。", "Art/Story/Npcs/NobleEnvoy.png"),
                    new DialogueManager.DialogueLine("猎魔人", "孩子失踪，你关心的却是封锁消息。", "Art/UI/GeraltPortrait.png"),
                    new DialogueManager.DialogueLine("贵族使者", "北境需要银矿，不需要真相。猎魔人，别把自己也埋进去。", "Art/Story/Npcs/NobleEnvoy.png")
                }),
            new StoryNpcDefinition(
                "MissingChild",
                "失踪男孩",
                "Art/Story/Npcs/MissingChild.png",
                new Vector2(-6.9f, -3.42f),
                0.32f,
                4,
                new[]
                {
                    new DialogueManager.DialogueLine("失踪男孩", "我听见妈妈在矿洞里喊我……可我没有妈妈了。", "Art/Story/Npcs/MissingChild.png"),
                    new DialogueManager.DialogueLine("猎魔人", "别再靠近矿洞。那声音不是给孩子听的。", "Art/UI/GeraltPortrait.png"),
                    new DialogueManager.DialogueLine("失踪男孩", "他们拿走了我的木马，说灰母会喜欢听孩子哭。", "Art/Story/Npcs/MissingChild.png")
                })
        };

        public static WitcherStoryNpcDirector CreateIfMissing()
        {
            WitcherStoryNpcDirector existing = FindObjectOfType<WitcherStoryNpcDirector>();
            if (existing != null)
            {
                return existing;
            }

            GameObject directorObject = new GameObject("Witcher Story NPC Director");
            return directorObject.AddComponent<WitcherStoryNpcDirector>();
        }

        private void Start()
        {
            StartCoroutine(SpawnAfterWorldReady());
        }

        private IEnumerator SpawnAfterWorldReady()
        {
            yield return null;
            yield return null;
            SpawnStoryNpcs();
        }

        private void SpawnStoryNpcs()
        {
            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Destroy(oldRoot);
            }

            GameObject root = new GameObject(RootName);
            WitcherVillageWalkableMap walkableMap = WitcherVillageWalkableMap.Current;

            foreach (StoryNpcDefinition definition in npcDefinitions)
            {
                Vector2 position = definition.Position;
                if (walkableMap != null && !walkableMap.TryGetNearestWalkablePoint(position, out position))
                {
                    Debug.LogWarning($"剧情人物 {definition.DisplayName} 的预设位置不可达，已保留原位置。");
                    position = definition.Position;
                }

                CreateNpc(root.transform, definition, position);
            }
        }

        private static void CreateNpc(Transform root, StoryNpcDefinition definition, Vector2 position)
        {
            GameObject npc = new GameObject("Story NPC - " + definition.DisplayName);
            npc.transform.SetParent(root, false);
            npc.transform.position = new Vector3(position.x, position.y, 0f);
            npc.transform.localScale = Vector3.one * definition.Scale;

            CreateShadow(npc.transform);

            SpriteRenderer renderer = npc.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadNpcSprite(definition.SpritePath);
            renderer.color = Color.white;
            renderer.sortingOrder = Mathf.RoundToInt((6.2f - position.y) * 100f) + 18;
            if (renderer.sprite == null)
            {
                renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(54, 42, 36, 255));
                renderer.transform.localScale = new Vector3(0.65f, 1.35f, 1f);
            }

            BoxCollider2D collider = npc.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            // Trigger range is compensated for the visual scale so small NPCs are still easy to talk to.
            collider.size = new Vector2(1.35f / definition.Scale, 1.45f / definition.Scale);
            collider.offset = new Vector2(0f, 0.5f / definition.Scale);

            Rigidbody2D body = npc.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;

            WitcherStoryNpcTrigger trigger = npc.AddComponent<WitcherStoryNpcTrigger>();
            trigger.Configure(definition.DisplayName, definition.QuestObjectiveIndex, definition.DialogueLines);
        }

        private static void CreateShadow(Transform parent)
        {
            GameObject shadow = new GameObject("NPC Ground Shadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, -0.03f, 0.04f);
            shadow.transform.localScale = new Vector3(0.92f, 0.18f, 1f);
            SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 90));
            renderer.color = new Color32(0, 0, 0, 90);
            renderer.sortingOrder = 16;
        }

        private static Sprite LoadNpcSprite(string relativePath)
        {
            if (CachedSprites.TryGetValue(relativePath, out Sprite cached))
            {
                return cached;
            }

            string absolutePath = Path.Combine(Application.dataPath, relativePath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"缺少剧情人物图片：{relativePath}，请把角色图放到对应目录。");
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Debug.LogWarning($"剧情人物图片读取失败：{relativePath}");
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), NpcPixelsPerUnit);
            sprite.name = Path.GetFileNameWithoutExtension(relativePath) + "_StoryNpc_Runtime";
            CachedSprites[relativePath] = sprite;
            return sprite;
        }

        private readonly struct StoryNpcDefinition
        {
            public StoryNpcDefinition(string id, string displayName, string spritePath, Vector2 position, float scale, int questObjectiveIndex, DialogueManager.DialogueLine[] dialogueLines)
            {
                Id = id;
                DisplayName = displayName;
                SpritePath = spritePath;
                Position = position;
                Scale = scale;
                QuestObjectiveIndex = questObjectiveIndex;
                DialogueLines = dialogueLines;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string SpritePath { get; }
            public Vector2 Position { get; }
            public float Scale { get; }
            public int QuestObjectiveIndex { get; }
            public DialogueManager.DialogueLine[] DialogueLines { get; }
        }
    }
}
