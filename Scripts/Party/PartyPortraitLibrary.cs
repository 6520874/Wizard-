using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
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
            if (memberName == "猎魔人")
            {
                return "HunterPortrait";
            }

            if (memberName == "叶奈法")
            {
                return "YenneferPortrait";
            }

            if (memberName == "特莉丝")
            {
                return "TrissPortrait";
            }

            if (memberName == "莉莉丝")
            {
                return "LilithPortrait";
            }

            return string.Empty;
        }
    }
}
