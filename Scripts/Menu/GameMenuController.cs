using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    public class GameMenuController : MonoBehaviour
    {
        private const string ControllerName = "Game Menu Controller";
        private static GameMenuController instance;

        [SerializeField] private GeraltController player;

        private readonly List<MenuCommand> commands = new List<MenuCommand>();
        private readonly List<Text> commandTexts = new List<Text>();
        private GameObject root;
        private GameObject menuPanel;
        private Text titleText;
        private Text detailText;
        private int selectedIndex;
        private bool isOpen;

        public bool IsOpen => isOpen;

        private class MenuCommand
        {
            public string Label;
            public string Description;
            public Action Execute;

            public MenuCommand(string label, string description, Action execute)
            {
                Label = label;
                Description = description;
                Execute = execute;
            }
        }

        public static GameMenuController CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            GameMenuController existing = FindObjectOfType<GameMenuController>();
            if (existing != null)
            {
                instance = existing;
                existing.SetPlayer(target);
                return existing;
            }

            GameMenuController controller = new GameObject(ControllerName).AddComponent<GameMenuController>();
            controller.SetPlayer(target);
            return controller;
        }

        private void Awake()
        {
            instance = this;
            BuildCommands();
            EnsureUi();
            root.SetActive(false);
        }

        private void Update()
        {
            if (!isOpen || root == null || !root.activeSelf || DialogueManager.IsDialogueActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                MoveSelection(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                MoveSelection(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                commands[selectedIndex].Execute?.Invoke();
            }
            else if (IsPartyCommandSelected() && Input.GetKeyDown(KeyCode.J))
            {
                TogglePartyMember("叶奈法");
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void SetPlayer(GeraltController target)
        {
            player = target == null ? FindObjectOfType<GeraltController>() : target;
        }

        public void Open()
        {
            if (EquipmentUIController.IsAnyOpen || DialogueManager.IsDialogueActive)
            {
                return;
            }

            EnsureUi();
            isOpen = true;
            root.SetActive(true);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, commands.Count - 1);
            RefreshSelection();
            PlayerInputController.RefreshPlayerControl();
        }

        public void Close()
        {
            isOpen = false;
            if (root != null)
            {
                root.SetActive(false);
            }

            PlayerInputController.RefreshPlayerControl();
        }

        private void BuildCommands()
        {
            if (commands.Count > 0)
            {
                return;
            }

            commands.Add(new MenuCommand("状态", "查看队伍生命、魔力、基础属性与当前任务。", ShowStatus));
            commands.Add(new MenuCommand("装备", "调整武器、护甲、饰品与圣物核心。", OpenEquipment));
            commands.Add(new MenuCommand("道具", "背包系统预留：药剂、委托物品和战利品将从这里使用。", () => ShowDetail("道具袋里只有几瓶药剂和怪物牙。真实背包接口已预留。")));
            commands.Add(new MenuCommand("技能", "查看猎魔法印、女术士法术、血魔法和怪物伙伴技能。", ShowSkills));
            commands.Add(new MenuCommand("队伍", "查看当前队伍成员与怪物伙伴预留位。", ShowParty));
            commands.Add(new MenuCommand("对话", "与队友闲聊，获取当前区域的线索。", PlayPartyTalk));
            commands.Add(new MenuCommand("系统", "系统设置预留：存档、读档、选项和返回标题。", () => ShowDetail("系统菜单预留中。之后可接入存档、音量和按键设置。")));
            commands.Add(new MenuCommand("关闭", "关闭命令菜单，返回探索。", Close));
        }

        private void MoveSelection(int delta)
        {
            selectedIndex = (selectedIndex + delta + commands.Count) % commands.Count;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < commandTexts.Count; i++)
            {
                bool selected = i == selectedIndex;
                commandTexts[i].text = selected ? $"< {commands[i].Label} >" : commands[i].Label;
                commandTexts[i].color = selected ? new Color32(255, 212, 133, 255) : new Color32(219, 216, 200, 255);
                commandTexts[i].fontSize = selected ? 25 : 23;
            }

            if (detailText != null && selectedIndex >= 0 && selectedIndex < commands.Count)
            {
                detailText.text = commands[selectedIndex].Description;
            }
        }

        private void OpenEquipment()
        {
            root.SetActive(false);
            isOpen = false;
            EquipmentUIController.CreateIfMissing(player).Open();
        }

        private void ShowStatus()
        {
            PartyManager party = PartyManager.CreateIfMissing();
            QuestManager quest = QuestManager.CreateIfMissing();
            string questText = quest.ActiveQuest == null ? "暂无进行中的委托。" : $"{quest.ActiveQuest.title}\n{quest.ActiveQuest.description}";
            string text = $"当前任务\n{questText}\n\n队伍状态";
            foreach (PartyMember member in party.ActiveParty)
            {
                text += $"\n{member.Name} Lv {member.Level}  HP {member.HP}/{member.TotalMaxHP}  MP {member.MP}/{member.TotalMaxMP}";
            }

            ShowDetail(text);
        }

        private void ShowSkills()
        {
            PartyManager party = PartyManager.CreateIfMissing();
            string text = "技能";
            foreach (PartyMember member in party.ActiveParty)
            {
                text += $"\n\n{member.Name}";
                if (member.SkillDetails.Count == 0)
                {
                    text += $"\n{string.Join(" / ", member.Skills)}";
                    continue;
                }

                for (int i = 0; i < member.SkillDetails.Count; i++)
                {
                    PartySkill skill = member.SkillDetails[i];
                    text += $"\n- {skill.Name}  {skill.Role}  MP {skill.MpCost}\n  {skill.Description}";
                }
            }

            ShowDetail(text);
        }

        private void ShowParty()
        {
            PartyManager party = PartyManager.CreateIfMissing();
            string text = "队伍管理";
            foreach (PartyMember member in party.AllMembers)
            {
                if (member == null)
                {
                    continue;
                }

                string state = member.IsJoined ? "已上阵" : "待命";
                string locked = member.Name == "猎魔人" ? " 固定" : " 上阵锁定";
                text += $"\n- {member.Name} Lv {member.Level}  {state}{locked}";
            }

            text += "\n\n当前队伍规则：只有猎魔人一个人上阵；其他角色保留为剧情/装备成员，后续版本再开放参战。";
            ShowDetail(text);
        }

        private void TogglePartyMember(string memberName)
        {
            PartyManager party = PartyManager.CreateIfMissing();
            if (party.ToggleMember(memberName))
            {
                ShowParty();
            }
        }

        private bool IsPartyCommandSelected()
        {
            return selectedIndex >= 0 && selectedIndex < commands.Count && commands[selectedIndex].Label == "队伍";
        }

        private void PlayPartyTalk()
        {
            root.SetActive(false);
            DialogueLine[] lines =
            {
                new DialogueLine("特莉丝", "这里残留着火焰都烧不干净的诅咒痕迹。"),
                new DialogueLine("猎魔人", "先找到源头，再决定杀谁。")
            };

            DialogueManager.CreateIfMissing().StartDialogue(lines, () =>
            {
                root.SetActive(true);
                isOpen = true;
                RefreshSelection();
                PlayerInputController.RefreshPlayerControl();
            }, false);
            PlayerInputController.RefreshPlayerControl();
        }

        private void ShowDetail(string text)
        {
            if (detailText != null)
            {
                detailText.text = text;
            }
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = GothicUiFactory.EnsureCanvas("Command Menu Canvas", 150);
            root = new GameObject("Gothic Command Menu");
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            menuPanel = GothicUiFactory.CreatePanel("Command Panel", root.transform, new Vector2(300f, 430f), new Vector2(44f, -60f), new Vector2(0f, 1f), new Color32(5, 7, 10, 232));
            GothicUiFactory.AddOutline(menuPanel, new Color32(112, 89, 48, 255), new Vector2(2f, -2f));
            GothicUiFactory.CreatePanel("Command Blood Rule", menuPanel.transform, new Vector2(248f, 2f), new Vector2(26f, -60f), new Vector2(0f, 1f), new Color32(133, 18, 27, 210));
            titleText = GothicUiFactory.CreateText("Command Title", menuPanel.transform, "猎魔命令", 28, TextAnchor.MiddleCenter, new Vector2(24f, -18f), new Vector2(252f, 34f), new Color32(238, 205, 130, 255));

            for (int i = 0; i < commands.Count; i++)
            {
                Text label = GothicUiFactory.CreateText($"Command {i + 1}", menuPanel.transform, commands[i].Label, 23, TextAnchor.MiddleLeft, new Vector2(42f, -82f - i * 39f), new Vector2(214f, 30f), new Color32(219, 216, 200, 255));
                commandTexts.Add(label);
            }

            GameObject detailPanel = GothicUiFactory.CreatePanel("Command Detail Panel", root.transform, new Vector2(640f, 430f), new Vector2(372f, -60f), new Vector2(0f, 1f), new Color32(6, 8, 12, 215));
            GothicUiFactory.AddOutline(detailPanel, new Color32(79, 64, 41, 255), new Vector2(1f, -1f));
            detailText = GothicUiFactory.CreateText("Command Detail", detailPanel.transform, string.Empty, 18, TextAnchor.UpperLeft, new Vector2(24f, -20f), new Vector2(592f, 386f), new Color32(220, 216, 199, 255));
        }
    }
}
