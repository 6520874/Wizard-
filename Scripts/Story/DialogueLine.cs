using System;
using UnityEngine;

namespace WitcherGame
{
    [Serializable]
    // 中文说明：保存一行剧情对话的说话人和对白文本。
    public struct DialogueLine
    {
        public string SpeakerName;
        [TextArea(2, 4)] public string Text;

        public DialogueLine(string speakerName, string text)
        {
            SpeakerName = speakerName;
            Text = text;
        }
    }
}
