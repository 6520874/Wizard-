using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：按队伍成员名字加载头像资源，缺失时回退到占位图。
    public static class PartyPortraitLibrary
    {
        private static readonly Dictionary<string, Sprite> CachedPortraits = new Dictionary<string, Sprite>();

        public static Sprite GetPortrait(PartyMember member)
        {
            if (member == null)
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(13, 16, 20, 255));
            }

            string resourceName = GetResourceName(member.Name);
            if (string.IsNullOrEmpty(resourceName))
            {
                return WitcherSpriteLibrary.GetGeraltFrame(GeraltAnimation.Idle, 0);
            }

            if (CachedPortraits.TryGetValue(resourceName, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>($"PartyPortraits/{resourceName}");
            if (texture == null)
            {
                return WitcherSpriteLibrary.GetSolidSprite(new Color32(13, 16, 20, 255));
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = resourceName;
            CachedPortraits[resourceName] = sprite;
            return sprite;
        }

        private static string GetResourceName(string memberName)
        {
            if (memberName == GameText.HunterName)
            {
                return "HunterPortrait";
            }

            if (memberName == GameText.YenneferName)
            {
                return "YenneferPortrait";
            }

            if (memberName == GameText.TrissName)
            {
                return "TrissPortrait";
            }

            return string.Empty;
        }
    }
}
