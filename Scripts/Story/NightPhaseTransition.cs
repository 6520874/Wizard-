using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：负责第一晚、第二晚进入时的暗幕标题转场。
    public class NightPhaseTransition : MonoBehaviour
    {
        private const string TransitionName = "Night Phase Transition";
        private static NightPhaseTransition instance;

        private GameObject root;
        private Image dim;
        private Image band;
        private Image accentLine;
        private Text titleText;
        private Text subtitleText;

        public static NightPhaseTransition CreateIfMissing()
        {
            if (instance != null)
            {
                return instance;
            }

            NightPhaseTransition existing = FindObjectOfType<NightPhaseTransition>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject transitionObject = new GameObject(TransitionName);
            instance = transitionObject.AddComponent<NightPhaseTransition>();
            return instance;
        }

        private void Awake()
        {
            instance = this;
            EnsureUi();
            HideImmediate();
        }

        public IEnumerator PlayTransition(string title, string subtitle, Action onBlackout)
        {
            EnsureUi();
            root.SetActive(true);
            WitcherSfxPlayer.Play(WitcherSfxCue.NightTransition, 0.82f);

            titleText.text = title;
            subtitleText.text = subtitle;
            SetAlpha(dim, 0f);
            SetAlpha(band, 0f);
            SetAlpha(accentLine, 0f);
            SetTextAlpha(titleText, 0f);
            SetTextAlpha(subtitleText, 0f);

            yield return Animate(0.42f, t =>
            {
                float eased = EaseInOut(t);
                SetAlpha(dim, Mathf.Lerp(0f, 0.92f, eased));
                SetAlpha(band, Mathf.Lerp(0f, 0.88f, eased));
                SetAlpha(accentLine, Mathf.Lerp(0f, 0.72f, eased));
                SetTextAlpha(titleText, Mathf.Clamp01((t - 0.12f) / 0.52f));
                SetTextAlpha(subtitleText, Mathf.Clamp01((t - 0.28f) / 0.48f));
            });

            onBlackout?.Invoke();
            yield return new WaitForSeconds(0.58f);

            yield return Animate(0.4f, t =>
            {
                float eased = EaseOutCubic(t);
                SetAlpha(dim, Mathf.Lerp(0.92f, 0f, eased));
                SetAlpha(band, Mathf.Lerp(0.88f, 0f, eased));
                SetAlpha(accentLine, Mathf.Lerp(0.72f, 0f, eased));
                SetTextAlpha(titleText, Mathf.Lerp(1f, 0f, eased));
                SetTextAlpha(subtitleText, Mathf.Lerp(1f, 0f, eased));
            });

            HideImmediate();
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 515;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;

            root = CreateUiObject("Night Transition Root", transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            StretchToParent(root.GetComponent<RectTransform>());
            dim = CreateImage("Night Transition Dim", root.transform, new Vector2(2000f, 1200f), Vector2.zero, new Color32(0, 0, 0, 0), new Vector2(0.5f, 0.5f));
            dim.raycastTarget = true;
            band = CreateImage("Night Transition Band", root.transform, new Vector2(860f, 126f), Vector2.zero, new Color32(4, 10, 17, 0), new Vector2(0.5f, 0.5f));
            accentLine = CreateImage("Night Transition Accent", band.transform, new Vector2(760f, 2f), new Vector2(0f, -18f), new Color32(126, 34, 46, 0), new Vector2(0.5f, 0.5f));

            titleText = CreateText("Night Transition Title", band.transform, string.Empty, 44, TextAnchor.MiddleCenter, new Vector2(0f, 24f), new Vector2(720f, 54f), new Color32(237, 214, 154, 0));
            AddOutline(titleText, new Color32(0, 0, 0, 230), new Vector2(2f, -2f));
            subtitleText = CreateText("Night Transition Subtitle", band.transform, string.Empty, 21, TextAnchor.MiddleCenter, new Vector2(0f, -42f), new Vector2(740f, 36f), new Color32(199, 213, 224, 0));
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

        private static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float EaseOutCubic(float t)
        {
            float inverted = 1f - Mathf.Clamp01(t);
            return 1f - inverted * inverted * inverted;
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

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Color32 color, Vector2 anchor)
        {
            GameObject obj = CreateUiObject(name, parent, size, position, anchor);
            Image image = obj.AddComponent<Image>();
            image.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(color.r, color.g, color.b, 255));
            image.color = color;
            return image;
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
