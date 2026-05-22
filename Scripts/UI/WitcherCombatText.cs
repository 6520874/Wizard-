using UnityEngine;

namespace WitcherGame
{
    // 中文说明：生成并驱动伤害、治疗和经验等浮动战斗文字。
    public class WitcherCombatText : MonoBehaviour
    {
        private const float Lifetime = 0.72f;
        private const float RiseSpeed = 0.85f;

        private TextMesh textMesh;
        private float timer;
        private Color baseColor;

        public static void Spawn(string text, Vector3 worldPosition, Color color)
        {
            GameObject textObject = new GameObject("Combat Text");
            textObject.transform.position = worldPosition + new Vector3(0f, 1.45f, -0.2f);

            WitcherCombatText combatText = textObject.AddComponent<WitcherCombatText>();
            combatText.Initialize(text, color);
        }

        private void Initialize(string text, Color color)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 38;
            textMesh.characterSize = 0.055f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            baseColor = color;

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 40;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            transform.position += Vector3.up * RiseSpeed * Time.deltaTime;

            float alpha = Mathf.Clamp01(1f - timer / Lifetime);
            textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            if (timer >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
