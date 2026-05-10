using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitcherGame
{
    public class TurnBasedBattleHud : MonoBehaviour
    {
        private const string HudName = "Turn Based Battle HUD";

        private readonly List<Text> enemyRows = new List<Text>();
        private readonly List<EnemyVisualSlot> enemySlots = new List<EnemyVisualSlot>();
        private readonly List<Button> commandButtons = new List<Button>();

        private TurnBasedBattleManager manager;
        private GameObject root;
        private Text messageText;
        private Text playerText;
        private Text potionText;
        private IReadOnlyList<TurnBasedEnemyState> visibleEnemies;

        private class EnemyVisualSlot
        {
            public Image Image;
            public RectTransform Rect;
            public Vector2 HomePosition;
            public bool Busy;
            public int IdleIndex;
            public float IdleTimer;
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || visibleEnemies == null)
            {
                return;
            }

            for (int i = 0; i < enemySlots.Count && i < visibleEnemies.Count; i++)
            {
                EnemyVisualSlot slot = enemySlots[i];
                TurnBasedEnemyState enemy = visibleEnemies[i];
                if (slot.Busy || !enemy.IsAlive || enemy.IdleFrames == null || enemy.IdleFrames.Length <= 1)
                {
                    continue;
                }

                slot.IdleTimer += Time.deltaTime;
                if (slot.IdleTimer < 0.15f)
                {
                    continue;
                }

                slot.IdleTimer = 0f;
                slot.IdleIndex = (slot.IdleIndex + 1) % enemy.IdleFrames.Length;
                slot.Image.sprite = enemy.IdleFrames[slot.IdleIndex];
            }
        }

        public static TurnBasedBattleHud CreateIfMissing(TurnBasedBattleManager target)
        {
            TurnBasedBattleHud existing = FindObjectOfType<TurnBasedBattleHud>();
            if (existing != null)
            {
                existing.manager = target;
                return existing;
            }

            GameObject hudObject = new GameObject(HudName);
            TurnBasedBattleHud hud = hudObject.AddComponent<TurnBasedBattleHud>();
            hud.manager = target;
            return hud;
        }

        public void Show(IReadOnlyList<TurnBasedEnemyState> enemies, GeraltController player, int potionCount)
        {
            if (root == null)
            {
                BuildHud();
            }

            root.SetActive(true);
            Refresh(enemies, player, potionCount);
            SetCommandsEnabled(true);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void SetCommandsEnabled(bool enabled)
        {
            for (int i = 0; i < commandButtons.Count; i++)
            {
                commandButtons[i].interactable = enabled;
            }
        }

        public void Refresh(IReadOnlyList<TurnBasedEnemyState> enemies, GeraltController player, int potionCount)
        {
            visibleEnemies = enemies;
            if (playerText != null && player != null)
            {
                playerText.text = $"猎魔人  HP {player.CurrentHealth}/{player.MaxHealth}   MP {player.CurrentMana}/{player.MaxMana}";
            }

            if (potionText != null)
            {
                potionText.text = $"药剂 x{potionCount}";
            }

            for (int i = 0; i < enemyRows.Count; i++)
            {
                if (i >= enemies.Count)
                {
                    enemyRows[i].text = string.Empty;
                    if (i < enemySlots.Count)
                    {
                        enemySlots[i].Image.gameObject.SetActive(false);
                    }
                    continue;
                }

                TurnBasedEnemyState enemy = enemies[i];
                string state = enemy.IsAlive ? $"HP {enemy.Health}/{enemy.MaxHealth}" : "已击败";
                enemyRows[i].text = $"{enemy.Name}    {state}";
                enemyRows[i].color = enemy.IsAlive ? new Color32(233, 238, 229, 255) : new Color32(128, 126, 119, 255);
                if (i < enemySlots.Count)
                {
                    EnemyVisualSlot slot = enemySlots[i];
                    slot.Image.gameObject.SetActive(enemy.Sprite != null && enemy.IsAlive);
                    if (!slot.Busy)
                    {
                        slot.Image.sprite = FirstFrame(enemy.IdleFrames, enemy.Sprite);
                        slot.Image.color = enemy.IsAlive ? Color.white : new Color32(120, 120, 120, 150);
                        slot.Rect.anchoredPosition = slot.HomePosition;
                        slot.Rect.localScale = Vector3.one;
                    }
                }
            }
        }

        public IEnumerator PlayEnemyAttack(int enemyIndex)
        {
            if (!TryGetSlot(enemyIndex, out EnemyVisualSlot slot) || visibleEnemies == null || enemyIndex >= visibleEnemies.Count)
            {
                yield break;
            }

            TurnBasedEnemyState enemy = visibleEnemies[enemyIndex];
            yield return PlayEnemyFrames(slot, enemy.AttackFrames, enemy.Sprite, 0.085f, true, false);
        }

        public IEnumerator PlayEnemyHurt(int enemyIndex, float startDelay = 0f)
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            if (!TryGetSlot(enemyIndex, out EnemyVisualSlot slot) || visibleEnemies == null || enemyIndex >= visibleEnemies.Count)
            {
                yield break;
            }

            TurnBasedEnemyState enemy = visibleEnemies[enemyIndex];
            yield return PlayEnemyFrames(slot, enemy.HurtFrames, enemy.Sprite, 0.075f, false, true);
        }

        private void BuildHud()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();

            root = CreateUiObject("Turn Battle Root", transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            StretchToParent(root.GetComponent<RectTransform>());

            Image dim = CreateCenteredImage("Turn Battle Dim", root.transform, new Vector2(2400f, 1400f), Vector2.zero, new Color32(0, 0, 0, 235));
            dim.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(0, 0, 0, 235));

            Image enemyPanel = CreateImage("Enemy Status Panel", root.transform, new Vector2(430f, 182f), new Vector2(30f, -96f), new Color32(8, 12, 16, 218));
            enemyPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(8, 12, 16, 218));
            AddOutline(enemyPanel, new Color32(82, 75, 59, 255), new Vector2(2f, -2f));

            enemySlots.Clear();
            for (int i = 0; i < 4; i++)
            {
                Image enemyImage = CreateCenteredImage($"Battle Enemy Sprite {i + 1}", root.transform, new Vector2(148f, 148f), new Vector2(-210f + i * 140f, 76f), Color.white);
                enemyImage.preserveAspect = true;
                enemyImage.raycastTarget = false;
                enemyImage.gameObject.SetActive(false);
                enemySlots.Add(new EnemyVisualSlot
                {
                    Image = enemyImage,
                    Rect = enemyImage.rectTransform,
                    HomePosition = enemyImage.rectTransform.anchoredPosition
                });
            }

            Text enemyTitle = CreateText("Enemy Title", enemyPanel.transform, "敌群", 25, TextAnchor.MiddleLeft, new Vector2(20f, -14f), new Vector2(200f, 34f));
            enemyTitle.color = new Color32(255, 214, 132, 255);
            AddOutline(enemyTitle, Color.black, new Vector2(1f, -1f));

            enemyRows.Clear();
            for (int i = 0; i < 4; i++)
            {
                Text row = CreateText($"Enemy Row {i + 1}", enemyPanel.transform, string.Empty, 19, TextAnchor.MiddleLeft, new Vector2(26f, -58f - i * 29f), new Vector2(370f, 28f));
                row.color = new Color32(233, 238, 229, 255);
                AddOutline(row, Color.black, new Vector2(1f, -1f));
                enemyRows.Add(row);
            }

            Image commandPanel = CreateImage("Command Panel", root.transform, new Vector2(880f, 206f), new Vector2(40f, -318f), new Color32(10, 13, 18, 238));
            commandPanel.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(10, 13, 18, 238));
            AddOutline(commandPanel, new Color32(112, 91, 58, 255), new Vector2(3f, -3f));

            playerText = CreateText("Battle Player Stats", commandPanel.transform, "猎魔人", 21, TextAnchor.MiddleLeft, new Vector2(24f, -18f), new Vector2(470f, 32f));
            playerText.color = new Color32(226, 241, 238, 255);
            AddOutline(playerText, Color.black, new Vector2(1f, -1f));

            potionText = CreateText("Battle Potion Count", commandPanel.transform, "药剂 x3", 18, TextAnchor.MiddleRight, new Vector2(666f, -20f), new Vector2(180f, 30f));
            potionText.color = new Color32(183, 219, 255, 255);
            AddOutline(potionText, Color.black, new Vector2(1f, -1f));

            messageText = CreateText("Battle Message", commandPanel.transform, "选择行动。", 21, TextAnchor.MiddleLeft, new Vector2(26f, -65f), new Vector2(810f, 42f));
            messageText.color = new Color32(255, 246, 214, 255);
            AddOutline(messageText, Color.black, new Vector2(1f, -1f));

            commandButtons.Clear();
            AddCommandButton(commandPanel.transform, "1 攻击", TurnBattleAction.Attack, new Vector2(26f, -126f));
            AddCommandButton(commandPanel.transform, "2 火焰法印", TurnBattleAction.FlameSign, new Vector2(196f, -126f));
            AddCommandButton(commandPanel.transform, "3 防御", TurnBattleAction.Defend, new Vector2(396f, -126f));
            AddCommandButton(commandPanel.transform, "4 物品", TurnBattleAction.Item, new Vector2(566f, -126f));
            AddCommandButton(commandPanel.transform, "5 逃跑", TurnBattleAction.Escape, new Vector2(706f, -126f));

            root.SetActive(false);
        }

        private IEnumerator PlayEnemyFrames(EnemyVisualSlot slot, Sprite[] frames, Sprite fallback, float frameDuration, bool attackMotion, bool hurtMotion)
        {
            slot.Busy = true;
            Vector2 home = slot.HomePosition;
            Vector2 motion = attackMotion ? new Vector2(-34f, -12f) : new Vector2(18f, 0f);
            Sprite[] safeFrames = frames != null && frames.Length > 0 ? frames : new[] { fallback };

            for (int i = 0; i < safeFrames.Length; i++)
            {
                if (safeFrames[i] != null)
                {
                    slot.Image.sprite = safeFrames[i];
                }

                float t = safeFrames.Length <= 1 ? 1f : (float)i / (safeFrames.Length - 1);
                float pulse = Mathf.Sin(t * Mathf.PI);
                slot.Rect.anchoredPosition = home + motion * pulse;
                slot.Rect.localScale = Vector3.one * (1f + (attackMotion ? 0.08f : 0.04f) * pulse);
                slot.Image.color = hurtMotion && i % 2 == 0 ? new Color32(255, 235, 222, 255) : Color.white;
                yield return new WaitForSeconds(frameDuration);
            }

            slot.Rect.anchoredPosition = home;
            slot.Rect.localScale = Vector3.one;
            slot.Image.color = Color.white;
            slot.Busy = false;
        }

        private bool TryGetSlot(int index, out EnemyVisualSlot slot)
        {
            if (index >= 0 && index < enemySlots.Count)
            {
                slot = enemySlots[index];
                return true;
            }

            slot = null;
            return false;
        }

        private static Sprite FirstFrame(Sprite[] frames, Sprite fallback)
        {
            return frames != null && frames.Length > 0 && frames[0] != null ? frames[0] : fallback;
        }

        private void AddCommandButton(Transform parent, string label, TurnBattleAction action, Vector2 position)
        {
            GameObject buttonObject = CreateUiObject(label + " Button", parent, new Vector2(142f, 52f), position, new Vector2(0f, 1f));
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(33, 39, 46, 246));
            image.color = new Color32(33, 39, 46, 246);
            AddOutline(image, new Color32(118, 101, 72, 255), new Vector2(2f, -2f));

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => manager.SelectAction(action));
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(255, 255, 255, 255);
            colors.highlightedColor = new Color32(255, 232, 169, 255);
            colors.pressedColor = new Color32(208, 143, 74, 255);
            colors.disabledColor = new Color32(92, 92, 92, 160);
            button.colors = colors;

            Text text = CreateText(label + " Text", buttonObject.transform, label, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(142f, 52f));
            text.color = new Color32(246, 241, 220, 255);
            AddOutline(text, Color.black, new Vector2(1f, -1f));
            commandButtons.Add(button);
        }

        private static GameObject CreateUiObject(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return obj;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateCenteredImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0.5f, 0.5f));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0f, 1f));
            Text label = obj.AddComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.raycastTarget = false;
            return label;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
