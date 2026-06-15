# 剧情数据编辑说明

这个目录里的 `story_database.json` 是游戏主线剧情文本入口。你以后想改剧情，优先改这个 JSON，不需要改 C#。

## 可以安全修改

- `openingNarration`：游戏开头黑屏旁白。
- `dialogues[].lines[].speaker`：对话框显示的说话人名字。
- `dialogues[].lines[].text`：对白内容。
- `quests[].title`：任务标题。
- `quests[].description`：任务描述。
- `objectives[].text`：任务面板显示的当前目标。
- `choices[].summary`：真相选择题大文本。
- `choices[].labels`：三个选项按钮文本。
- `choices[].correctIndex`：正确答案下标，`0` 是 A，`1` 是 B，`2` 是 C。

## 不建议随便改

- `id`：这是代码用来找到剧情段落、任务和目标的稳定键。改了以后，对应剧情可能加载不到。
- `quests[].objectiveIds`：这里引用的是 `objectives[].id`，如果要新增一晚委托，建议先让我一起接入流程。

## 对话格式例子

```json
{
  "id": "firstNight.oldWell",
  "lines": [
    { "speaker": "老井", "text": "井沿凝着黑色血迹。" },
    { "speaker": "猎魔人", "text": "不是普通亡魂。银剑能伤它。" }
  ]
}
```

## 注意

JSON 里不能写注释，也不能漏逗号。改完后如果游戏读不到，会自动使用 C# 里的兜底文本，并在 Unity Console 打 warning。
