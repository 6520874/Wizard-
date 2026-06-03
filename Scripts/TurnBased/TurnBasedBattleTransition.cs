using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：负责从地图遭遇切入回合制战斗时的暗幕、扫光和标题转场。
    public class TurnBasedBattleTransition : MonoBehaviour
    {
        private const string TransitionName = "Turn Based Battle Transition";
        private static TurnBasedBattleTransition instance;

        private Canvas canvas;
        private GameObject root;
        private Image dim;
        private Image topBlade;
        private Image bottomBlade;
        private Image topFade;
        private Image bottomFade;
        private Image redSweep;
        private Image blueSweep;
        private Text titleText;
        private Text subtitleText;

        public static TurnBasedBattleTransition CreateIfMissing()
        {
            if (instance != null)
            {
                return instance;
            }

            TurnBasedBattleTransition existing = FindObjectOfType<TurnBasedBattleTransition>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject transitionObject = new GameObject(TransitionName);
            instance = transitionObject.AddComponent<TurnBasedBattleTransition>();
            return instance;
        }

        private void Awake()
        {
            instance = this;
            EnsureUi();
            HideImmediate();
        }

        public IEnumerator PlayEncounterTransition(string encounterTitle, Action onBlackout)
        {
            EnsureUi();
            root.SetActive(true);
            SetAlpha(dim, 0f);
            SetAlpha(topBlade, 0f);
            SetAlpha(bottomBlade, 0f);
            SetAlpha(topFade, 0f);
            SetAlpha(bottomFade, 0f);
            SetAlpha(redSweep, 0f);
            SetAlpha(blueSweep, 0f);
            SetTextAlpha(titleText, 0f);
            SetTextAlpha(subtitleText, 0f);

            titleText.text = "遭遇战";
            subtitleText.text = string.IsNullOrWhiteSpace(encounterTitle) ? "黑暗中的怪物逼近" : encounterTitle;

            RectTransform topRect = topBlade.rectTransform;
            RectTransform bottomRect = bottomBlade.rectTransform;
            RectTransform topFadeRect = topFade.rectTransform;
            RectTransform bottomFadeRect = bottomFade.rectTransform;
            RectTransform redRect = redSweep.rectTransform;
            RectTransform blueRect = blueSweep.rectTransform;
            Vector2 topHome = Vector2.zero;
            Vector2 bottomHome = Vector2.zero;
            Vector2 topFadeHome = new Vector2(0f, -156f);
            Vector2 bottomFadeHome = new Vector2(0f, 156f);

            yield return Animate(0.34f, t =>
            {
                float eased = EaseOutCubic(t);
                SetAlpha(dim, Mathf.Lerp(0f, 0.42f, eased));
                SetAlpha(topBlade, eased);
                SetAlpha(bottomBlade, eased);
                SetAlpha(topFade, Mathf.Lerp(0f, 0.76f, eased));
                SetAlpha(bottomFade, Mathf.Lerp(0f, 0.76f, eased));
                topRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, 176f), topHome, eased);
                bottomRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, -176f), bottomHome, eased);
                topFadeRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, 12f), topFadeHome, eased);
                bottomFadeRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, -12f), bottomFadeHome, eased);
            });

            yield return Animate(0.46f, t =>
            {
                float eased = EaseInOut(t);
                SetTextAlpha(titleText, Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.25f));
                SetTextAlpha(subtitleText, Mathf.Clamp01((t - 0.18f) / 0.55f));
                SetAlpha(redSweep, Mathf.Sin(t * Mathf.PI) * 0.56f);
                SetAlpha(blueSweep, Mathf.Sin(t * Mathf.PI) * 0.48f);
                redRect.anchoredPosition = Vector2.Lerp(new Vector2(-620f, -82f), new Vector2(620f, -82f), eased);
                blueRect.anchoredPosition = Vector2.Lerp(new Vector2(620f, 82f), new Vector2(-620f, 82f), eased);
            });

            yield return Animate(0.18f, t =>
            {
                float eased = EaseInOut(t);
                SetAlpha(dim, Mathf.Lerp(0.42f, 0.82f, eased));
                SetAlpha(topBlade, 1f);
                SetAlpha(bottomBlade, 1f);
                SetAlpha(topFade, Mathf.Lerp(0.76f, 1f, eased));
                SetAlpha(bottomFade, Mathf.Lerp(0.76f, 1f, eased));
                SetTextAlpha(titleText, Mathf.Lerp(0.72f, 0f, eased));
                SetTextAlpha(subtitleText, Mathf.Lerp(1f, 0f, eased));
            });

            onBlackout?.Invoke();
            yield return new WaitForSeconds(0.12f);

            yield return Animate(0.34f, t =>
            {
                float eased = EaseOutCubic(t);
                SetAlpha(dim, Mathf.Lerp(0.82f, 0f, eased));
                SetAlpha(topBlade, Mathf.Lerp(1f, 0f, eased));
                SetAlpha(bottomBlade, Mathf.Lerp(1f, 0f, eased));
                SetAlpha(topFade, Mathf.Lerp(1f, 0f, eased));
                SetAlpha(bottomFade, Mathf.Lerp(1f, 0f, eased));
                topRect.anchoredPosition = Vector2.Lerp(topHome, new Vector2(0f, 176f), eased);
                bottomRect.anchoredPosition = Vector2.Lerp(bottomHome, new Vector2(0f, -176f), eased);
                topFadeRect.anchoredPosition = Vector2.Lerp(topFadeHome, new Vector2(0f, 12f), eased);
                bottomFadeRect.anchoredPosition = Vector2.Lerp(bottomFadeHome, new Vector2(0f, -12f), eased);
            });

            HideImmediate();
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;

            root = CreateUiObject("Battle Transition Root", transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            StretchToParent(root.GetComponent<RectTransform>());
            dim = CreateImage("Battle Transition Dim", root.transform, new Vector2(2000f, 1200f), Vector2.zero, new Color32(0, 0, 0, 0), new Vector2(0.5f, 0.5f));
            dim.raycastTarget = true;
            topBlade = CreateImage("Battle Transition Top Letterbox", root.transform, new Vector2(2400f, 156f), Vector2.zero, new Color32(0, 0, 0, 0), new Vector2(0.5f, 1f));
            bottomBlade = CreateImage("Battle Transition Bottom Letterbox", root.transform, new Vector2(2400f, 156f), Vector2.zero, new Color32(0, 0, 0, 0), new Vector2(0.5f, 0f));
            topFade = CreateImage("Battle Transition Top Soft Edge", root.transform, new Vector2(2400f, 34f), new Vector2(0f, -156f), new Color32(0, 0, 0, 0), new Vector2(0.5f, 1f));
            bottomFade = CreateImage("Battle Transition Bottom Soft Edge", root.transform, new Vector2(2400f, 34f), new Vector2(0f, 156f), new Color32(0, 0, 0, 0), new Vector2(0.5f, 0f));
            redSweep = CreateImage("Battle Transition Red Sweep", root.transform, new Vector2(320f, 5f), new Vector2(-620f, -82f), new Color32(178, 23, 25, 0), new Vector2(0.5f, 0.5f));
            blueSweep = CreateImage("Battle Transition Blue Sweep", root.transform, new Vector2(320f, 5f), new Vector2(620f, 82f), new Color32(50, 130, 218, 0), new Vector2(0.5f, 0.5f));

            titleText = CreateText("Battle Transition Title", root.transform, "遭遇战", 46, TextAnchor.MiddleCenter, new Vector2(0f, 18f), new Vector2(420f, 66f), new Color32(255, 224, 138, 0));
            AddOutline(titleText, new Color32(0, 0, 0, 220), new Vector2(2f, -2f));
            subtitleText = CreateText("Battle Transition Subtitle", root.transform, string.Empty, 22, TextAnchor.MiddleCenter, new Vector2(0f, -42f), new Vector2(520f, 42f), new Color32(226, 233, 238, 0));
            AddOutline(subtitleText, new Color32(0, 0, 0, 230), new Vector2(1f, -1f));
        }

        private IEnumerator Animate(float duration, Action<float> tick)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                tick?.Invoke(Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            tick?.Invoke(1f);
        }

        private void HideImmediate()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private static float EaseOutCubic(float t)
        {
            float inverted = 1f - Mathf.Clamp01(t);
            return 1f - inverted * inverted * inverted;
        }

        private static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        private static void SetTextAlpha(Text text, float alpha)
        {
            Color color = text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color32 color)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, new Vector2(0.5f, 0.5f));
            Text textComponent = obj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = anchor;
            textComponent.color = color;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            return textComponent;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color, Vector2 anchor)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, anchor);
            Image image = obj.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(color.r, color.g, color.b, 255));
            image.color = color;
            return image;
        }

        private static GameObject CreateUiObject(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return obj;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
