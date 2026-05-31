using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    public class PartyManager : MonoBehaviour
    {
        private const string ManagerName = "Party Manager";
        private const int MaxPartySize = 4;

        private static PartyManager instance;

        private readonly List<PartyMember> allMembers = new List<PartyMember>();
        private readonly List<PartyMember> activeParty = new List<PartyMember>();
        private readonly Dictionary<EquipmentSlot, List<EquipmentItem>> sampleEquipment = new Dictionary<EquipmentSlot, List<EquipmentItem>>();

        public IReadOnlyList<PartyMember> AllMembers => allMembers;
        public IReadOnlyList<PartyMember> ActiveParty => activeParty;

        public static PartyManager Instance => instance == null ? CreateIfMissing() : instance;

        public static PartyManager CreateIfMissing()
        {
            if (instance != null)
            {
                return instance;
            }

            PartyManager existing = FindObjectOfType<PartyManager>();
            if (existing != null)
            {
                instance = existing;
                instance.EnsureDefaults();
                return existing;
            }

            PartyManager manager = new GameObject(ManagerName).AddComponent<PartyManager>();
            manager.EnsureDefaults();
            return manager;
        }

        private void Awake()
        {
            instance = this;
            EnsureDefaults();
        }

        public bool AddMember(PartyMember member)
        {
            if (member == null || activeParty.Contains(member) || activeParty.Count >= MaxPartySize)
            {
                return false;
            }

            if (!allMembers.Contains(member))
            {
                allMembers.Add(member);
            }

            member.IsJoined = true;
            activeParty.Add(member);
            return true;
        }

        public bool RemoveMember(PartyMember member)
        {
            if (member == null || activeParty.Count <= 1 || member.Name == "猎魔人")
            {
                return false;
            }

            member.IsJoined = false;
            return activeParty.Remove(member);
        }

        public bool SwitchMember(int partyIndex, PartyMember replacement)
        {
            if (replacement == null || partyIndex <= 0 || partyIndex >= activeParty.Count || activeParty.Contains(replacement))
            {
                return false;
            }

            activeParty[partyIndex].IsJoined = false;
            replacement.IsJoined = true;
            activeParty[partyIndex] = replacement;
            if (!allMembers.Contains(replacement))
            {
                allMembers.Add(replacement);
            }

            return true;
        }

        public PartyMember GetActiveMember(int index)
        {
            if (activeParty.Count == 0)
            {
                EnsureDefaults();
            }

            return activeParty[Mathf.Clamp(index, 0, activeParty.Count - 1)];
        }

        public IReadOnlyList<EquipmentItem> GetSampleEquipment(EquipmentSlot slot)
        {
            EnsureDefaults();
            return sampleEquipment[slot];
        }

        public EquipmentItem GetNextSampleEquipment(EquipmentSlot slot, EquipmentItem current)
        {
            IReadOnlyList<EquipmentItem> items = GetSampleEquipment(slot);
            if (items.Count == 0)
            {
                return null;
            }

            int index = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == current || items[i].Name == current?.Name)
                {
                    index = (i + 1) % items.Count;
                    break;
                }
            }

            return items[index];
        }

        public void AddMonsterPartnerPlaceholder(string name)
        {
            PartyMember monster = new PartyMember(name, 1, 74, 18, 17, 8, 5, 11, 0.04f, false);
            monster.Skills.Add("撕咬");
            monster.Skills.Add("嗅血追踪");
            allMembers.Add(monster);
        }

        private void EnsureDefaults()
        {
            if (sampleEquipment.Count == 0)
            {
                BuildSampleEquipment();
            }

            if (allMembers.Count > 0)
            {
                return;
            }

            PartyMember hunter = new PartyMember("猎魔人", 1, 120, 100, 18, 7, 6, 13, 0.06f, true);
            hunter.Skills.Add("银剑斩");
            hunter.Skills.Add("焚焰法印");
            hunter.CurrentEquipment.Weapon = sampleEquipment[EquipmentSlot.Weapon][0];
            hunter.CurrentEquipment.Armor = sampleEquipment[EquipmentSlot.Armor][0];
            hunter.CurrentEquipment.Accessory1 = sampleEquipment[EquipmentSlot.Accessory1][0];
            hunter.CurrentEquipment.RelicCore = sampleEquipment[EquipmentSlot.RelicCore][0];

            PartyMember yennefer = new PartyMember("叶奈法", 1, 88, 142, 8, 5, 24, 12, 0.04f, true);
            yennefer.Skills.Add("紫晶护盾");
            yennefer.Skills.Add("诅咒脉冲");
            yennefer.CurrentEquipment.Weapon = sampleEquipment[EquipmentSlot.Weapon][1];
            yennefer.CurrentEquipment.Armor = sampleEquipment[EquipmentSlot.Armor][1];
            yennefer.CurrentEquipment.Accessory1 = sampleEquipment[EquipmentSlot.Accessory1][1];
            yennefer.CurrentEquipment.RelicCore = sampleEquipment[EquipmentSlot.RelicCore][1];

            PartyMember lilith = new PartyMember("莉莉丝", 1, 104, 118, 14, 6, 21, 15, 0.08f, true);
            lilith.Skills.Add("血焰");
            lilith.Skills.Add("魅惑低语");
            lilith.CurrentEquipment.Weapon = sampleEquipment[EquipmentSlot.Weapon][2];
            lilith.CurrentEquipment.Armor = sampleEquipment[EquipmentSlot.Armor][2];
            lilith.CurrentEquipment.Accessory1 = sampleEquipment[EquipmentSlot.Accessory1][2];
            lilith.CurrentEquipment.RelicCore = sampleEquipment[EquipmentSlot.RelicCore][2];

            allMembers.Add(hunter);
            allMembers.Add(yennefer);
            allMembers.Add(lilith);
            AddMonsterPartnerPlaceholder("狼魔伙伴");

            activeParty.Add(hunter);
            activeParty.Add(yennefer);
            activeParty.Add(lilith);
        }

        private void BuildSampleEquipment()
        {
            sampleEquipment[EquipmentSlot.Weapon] = new List<EquipmentItem>
            {
                new EquipmentItem("银鸦长剑", EquipmentSlot.Weapon, 0, 0, 7, 0, 0, 1, 0.02f),
                new EquipmentItem("紫晶法杖", EquipmentSlot.Weapon, 0, 12, 1, 0, 8, 0, 0.01f),
                new EquipmentItem("血契短刃", EquipmentSlot.Weapon, 8, 0, 5, 0, 5, 2, 0.04f)
            };
            sampleEquipment[EquipmentSlot.Armor] = new List<EquipmentItem>
            {
                new EquipmentItem("猎魔皮甲", EquipmentSlot.Armor, 16, 0, 0, 6, 0, 1, 0f),
                new EquipmentItem("黑绒法袍", EquipmentSlot.Armor, 8, 18, 0, 3, 6, 0, 0f),
                new EquipmentItem("赤夜礼装", EquipmentSlot.Armor, 12, 10, 1, 4, 4, 2, 0.01f)
            };
            sampleEquipment[EquipmentSlot.Accessory1] = new List<EquipmentItem>
            {
                new EquipmentItem("黑月戒指", EquipmentSlot.Accessory1, 0, 8, 1, 0, 2, 0, 0.02f),
                new EquipmentItem("女术士护符", EquipmentSlot.Accessory1, 0, 14, 0, 0, 5, 1, 0f),
                new EquipmentItem("血蔷薇耳坠", EquipmentSlot.Accessory1, 6, 0, 2, 0, 3, 1, 0.03f)
            };
            sampleEquipment[EquipmentSlot.Accessory2] = new List<EquipmentItem>
            {
                new EquipmentItem("空槽", EquipmentSlot.Accessory2, 0, 0, 0, 0, 0, 0, 0f),
                new EquipmentItem("狼牙坠饰", EquipmentSlot.Accessory2, 8, 0, 2, 1, 0, 1, 0.01f),
                new EquipmentItem("腐银指环", EquipmentSlot.Accessory2, 0, 10, 0, 1, 4, 0, 0.02f)
            };
            sampleEquipment[EquipmentSlot.RelicCore] = new List<EquipmentItem>
            {
                new EquipmentItem("圣物碎片", EquipmentSlot.RelicCore, 10, 6, 1, 1, 1, 0, 0f),
                new EquipmentItem("紫曜魔核", EquipmentSlot.RelicCore, 0, 20, 0, 0, 8, 1, 0.02f),
                new EquipmentItem("恶魔血核", EquipmentSlot.RelicCore, 18, 0, 3, 0, 5, 1, 0.03f)
            };
        }
    }
}
