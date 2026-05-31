using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitcherGame
{
    public class EquipmentUIController : MonoBehaviour
    {
        private const string ControllerName = "Equipment UI Controller";
        private static EquipmentUIController instance;

        [SerializeField] private GeraltController player;

        private readonly EquipmentSlot[] slotOrder =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory1,
            EquipmentSlot.Accessory2,
            EquipmentSlot.RelicCore
        };

        private readonly List<Text> memberTexts = new List<Text>();
        private readonly List<Text> slotTexts = new List<Text>();
        private GameObject root;
        private Image portraitImage;
        private Text titleText;
        private Text statsText;
        private Text helpText;
        private int selectedMemberIndex;
        private int selectedSlotIndex;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public static bool IsAnyOpen => instance != null && instance.isOpen;

        public static EquipmentUIController CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            EquipmentUIController existing = FindObjectOfType<EquipmentUIController>();
            if (existing != null)
            {
                instance = existing;
                existing.SetPlayer(target);
                return existing;
            }

            EquipmentUIController controller = new GameObject(ControllerName).AddComponent<EquipmentUIController>();
            controller.SetPlayer(target);
            return controller;
        }

        private void Awake()
        {
            instance = this;
            EnsureUi();
            root.SetActive(false);
        }

        private void Update()
        {
            if (!isOpen || DialogueManager.IsDialogueActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                MoveMember(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                MoveMember(1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                MoveSlot(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                MoveSlot(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                CycleSelectedEquipment();
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
            if (DialogueManager.IsDialogueActive || (GameMenuController.CreateIfMissing(player).IsOpen && !isOpen))
            {
                return;
            }

            EnsureUi();
            isOpen = true;
            root.SetActive(true);
            Refresh();
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

        private void MoveMember(int delta)
        {
            IReadOnlyList<PartyMember> members = PartyManager.CreateIfMissing().ActiveParty;
            if (members.Count == 0)
            {
                return;
            }

            selectedMemberIndex = (selectedMemberIndex + delta + members.Count) % members.Count;
            Refresh();
        }

        private void MoveSlot(int delta)
        {
            selectedSlotIndex = (selectedSlotIndex + delta + slotOrder.Length) % slotOrder.Length;
            Refresh();
        }

        private void CycleSelectedEquipment()
        {
            PartyManager party = PartyManager.CreateIfMissing();
            PartyMember member = party.GetActiveMember(selectedMemberIndex);
            EquipmentSlot slot = slotOrder[selectedSlotIndex];
            EquipmentItem current = member.CurrentEquipment.Get(slot);
            member.CurrentEquipment.Set(slot, party.GetNextSampleEquipment(slot, current));
            Refresh();
        }

        private void Refresh()
        {
            PartyManager party = PartyManager.CreateIfMissing();
            IReadOnlyList<PartyMember> members = party.ActiveParty;
            selectedMemberIndex = Mathf.Clamp(selectedMemberIndex, 0, Mathf.Max(0, members.Count - 1));
            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slotOrder.Length - 1);

            for (int i = 0; i < memberTexts.Count; i++)
            {
                if (i >= members.Count)
                {
                    memberTexts[i].gameObject.SetActive(false);
                    continue;
                }

                memberTexts[i].gameObject.SetActive(true);
                bool isSelectedMember = i == selectedMemberIndex;
                PartyMember member = members[i];
                memberTexts[i].text = isSelectedMember ? $"> {member.Name}  Lv {member.Level}" : $"  {member.Name}  Lv {member.Level}";
                memberTexts[i].color = isSelectedMember ? new Color32(255, 214, 139, 255) : new Color32(218, 215, 198, 255);
            }

            PartyMember selectedMember = party.GetActiveMember(selectedMemberIndex);
            titleText.text = $"{selectedMember.Name} 装备";
            if (portraitImage != null)
            {
                portraitImage.sprite = PartyPortraitLibrary.GetPortrait(selectedMember);
                portraitImage.color = Color.white;
            }

            for (int i = 0; i < slotTexts.Count; i++)
            {
                EquipmentSlot slot = slotOrder[i];
                EquipmentItem item = selectedMember.CurrentEquipment.Get(slot);
                bool selectedSlot = i == selectedSlotIndex;
                string slotName = GetSlotDisplayName(slot);
                string itemName = item == null ? "空" : item.Name;
                slotTexts[i].text = selectedSlot ? $"< {slotName} >  {itemName}" : $"{slotName}  {itemName}";
                slotTexts[i].color = selectedSlot ? new Color32(255, 201, 117, 255) : new Color32(220, 218, 203, 255);
            }

            statsText.text =
                $"HP    {selectedMember.HP}/{selectedMember.TotalMaxHP}\n" +
                $"MP    {selectedMember.MP}/{selectedMember.TotalMaxMP}\n" +
                $"攻击  {selectedMember.TotalAttack}\n" +
                $"防御  {selectedMember.TotalDefense}\n" +
                $"魔力  {selectedMember.TotalMagic}\n" +
                $"速度  {selectedMember.TotalSpeed}\n" +
                $"暴击  {Mathf.RoundToInt(selectedMember.TotalCriticalRate * 100f)}%";
            helpText.text = "↑↓ 选择成员    ←→ 选择装备槽    Enter 更换假数据装备    Esc 关闭";
        }

        private static string GetSlotDisplayName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return "武器";
                case EquipmentSlot.Armor:
                    return "护甲";
                case EquipmentSlot.Accessory1:
                    return "饰品 1";
                case EquipmentSlot.Accessory2:
                    return "饰品 2";
                case EquipmentSlot.RelicCore:
                    return "圣物 / 魔法核心";
                default:
                    return slot.ToString();
            }
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = GothicUiFactory.EnsureCanvas("Equipment Menu Canvas", 155);
            root = new GameObject("Gothic Equipment Menu");
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject dim = GothicUiFactory.CreatePanel("Equipment Dim", root.transform, new Vector2(2400f, 1400f), Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(0, 0, 0, 112));
            dim.transform.SetAsFirstSibling();

            GameObject leftPanel = GothicUiFactory.CreatePanel("Equipment Party Panel", root.transform, new Vector2(324f, 422f), new Vector2(54f, -62f), new Vector2(0f, 1f), new Color32(5, 7, 10, 232));
            GothicUiFactory.AddOutline(leftPanel, new Color32(105, 82, 49, 255), new Vector2(2f, -2f));
            GothicUiFactory.CreateText("Equipment Party Title", leftPanel.transform, "队伍", 28, TextAnchor.MiddleCenter, new Vector2(30f, -18f), new Vector2(264f, 36f), new Color32(238, 205, 130, 255));
            GothicUiFactory.CreatePanel("Equipment Party Rule", leftPanel.transform, new Vector2(260f, 2f), new Vector2(32f, -62f), new Vector2(0f, 1f), new Color32(128, 20, 28, 210));
            for (int i = 0; i < 4; i++)
            {
                memberTexts.Add(GothicUiFactory.CreateText($"Party Member {i + 1}", leftPanel.transform, string.Empty, 23, TextAnchor.MiddleLeft, new Vector2(34f, -92f - i * 52f), new Vector2(254f, 34f), new Color32(218, 215, 198, 255)));
            }

            GameObject rightPanel = GothicUiFactory.CreatePanel("Equipment Detail Panel", root.transform, new Vector2(690f, 422f), new Vector2(404f, -62f), new Vector2(0f, 1f), new Color32(5, 8, 12, 234));
            GothicUiFactory.AddOutline(rightPanel, new Color32(105, 82, 49, 255), new Vector2(2f, -2f));
            titleText = GothicUiFactory.CreateText("Equipment Detail Title", rightPanel.transform, "装备", 28, TextAnchor.MiddleLeft, new Vector2(32f, -18f), new Vector2(330f, 36f), new Color32(238, 205, 130, 255));
            GothicUiFactory.CreatePanel("Equipment Detail Rule", rightPanel.transform, new Vector2(626f, 2f), new Vector2(32f, -62f), new Vector2(0f, 1f), new Color32(128, 20, 28, 210));
            GameObject portraitFrame = GothicUiFactory.CreatePanel("Equipment Portrait Frame", rightPanel.transform, new Vector2(116f, 132f), new Vector2(36f, -88f), new Vector2(0f, 1f), new Color32(8, 10, 13, 244));
            GothicUiFactory.AddOutline(portraitFrame, new Color32(90, 72, 47, 255), new Vector2(1f, -1f));
            GameObject portraitObject = new GameObject("Equipment Portrait");
            portraitObject.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitRect = portraitObject.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.sizeDelta = new Vector2(104f, 118f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            for (int i = 0; i < slotOrder.Length; i++)
            {
                slotTexts.Add(GothicUiFactory.CreateText($"Equipment Slot {i + 1}", rightPanel.transform, string.Empty, 22, TextAnchor.MiddleLeft, new Vector2(176f, -92f - i * 42f), new Vector2(250f, 32f), new Color32(220, 218, 203, 255)));
            }

            statsText = GothicUiFactory.CreateText("Equipment Stats", rightPanel.transform, string.Empty, 21, TextAnchor.UpperLeft, new Vector2(466f, -92f), new Vector2(178f, 236f), new Color32(224, 223, 208, 255));
            helpText = GothicUiFactory.CreateText("Equipment Help", rightPanel.transform, string.Empty, 15, TextAnchor.MiddleLeft, new Vector2(36f, -368f), new Vector2(620f, 26f), new Color32(153, 164, 162, 255));
        }
    }
}
