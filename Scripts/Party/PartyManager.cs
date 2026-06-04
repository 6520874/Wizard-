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
        public System.Action PartyChanged;

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
            PartyChanged?.Invoke();
            return true;
        }

        public bool RemoveMember(PartyMember member)
        {
            if (member == null || activeParty.Count <= 1 || member.Name == "猎魔人")
            {
                return false;
            }

            member.IsJoined = false;
            bool removed = activeParty.Remove(member);
            if (removed)
            {
                PartyChanged?.Invoke();
            }

            return removed;
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

            PartyChanged?.Invoke();
            return true;
        }

        public PartyMember FindMember(string memberName)
        {
            EnsureDefaults();
            for (int i = 0; i < allMembers.Count; i++)
            {
                if (allMembers[i] != null && allMembers[i].Name == memberName)
                {
                    return allMembers[i];
                }
            }

            return null;
        }

        public bool ToggleMember(string memberName)
        {
            PartyMember member = FindMember(memberName);
            if (member == null || member.Name == "猎魔人")
            {
                return false;
            }

            return activeParty.Contains(member) ? RemoveMember(member) : AddMember(member);
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
            PartyMember monster = new PartyMember(name, 1, 98, 74, 18, 8, 19, 12, 0.06f, false);
            monster.LearnSkill("血焰", "火焰 / 伤害", 18, "以恶魔血火灼烧单个怪物。");
            monster.LearnSkill("魅惑低语", "控制 / 弱化", 14, "短暂扰乱敌人的攻击欲望。");
            monster.LearnSkill("恶魔召唤", "召唤 / 爆发", 28, "召出低阶恶魔影子撕裂敌群。");
            monster.LearnSkill("血契吸取", "血魔法 / 回复", 20, "撕开血契，从敌人生命里抽回自身血量。");
            monster.LearnSkill("地狱烙印", "诅咒 / 持续伤害", 24, "给目标刻下恶魔烙印，持续灼烧其灵魂。");
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
                RemoveHiddenMainPartyMembers();
                return;
            }

            PartyMember hunter = new PartyMember("猎魔人", 1, 120, 100, 18, 7, 6, 13, 0.06f, true);
            hunter.LearnSkill("银剑斩", "物理 / 单体", 0, "可靠的猎魔人基础攻击。");
            hunter.LearnSkill("焚焰法印", "火焰 / 群体", 18, "用法印横扫敌群。");
            hunter.CurrentEquipment.Weapon = sampleEquipment[EquipmentSlot.Weapon][0];
            hunter.CurrentEquipment.Armor = sampleEquipment[EquipmentSlot.Armor][0];
            hunter.CurrentEquipment.Accessory1 = sampleEquipment[EquipmentSlot.Accessory1][0];
            hunter.CurrentEquipment.RelicCore = sampleEquipment[EquipmentSlot.RelicCore][0];

            PartyMember yennefer = new PartyMember("叶奈法", 1, 88, 142, 8, 5, 24, 12, 0.04f, true);
            yennefer.LearnSkill("紫晶护盾", "防护 / 护盾", 16, "为队伍展开紫色魔法护盾。");
            yennefer.LearnSkill("诅咒脉冲", "奥术 / 弱化", 20, "释放扭曲脉冲削弱敌人的防御。");
            yennefer.CurrentEquipment.Weapon = sampleEquipment[EquipmentSlot.Weapon][1];
            yennefer.CurrentEquipment.Armor = sampleEquipment[EquipmentSlot.Armor][1];
            yennefer.CurrentEquipment.Accessory1 = sampleEquipment[EquipmentSlot.Accessory1][1];
            yennefer.CurrentEquipment.RelicCore = sampleEquipment[EquipmentSlot.RelicCore][1];

            PartyMember triss = new PartyMember("特莉丝", 1, 102, 126, 11, 6, 22, 14, 0.06f, false);
            triss.LearnSkill("火焰术", "火焰 / 单体", 14, "向目标投出压缩火球。");
            triss.LearnSkill("灼热结界", "火焰 / 防护", 22, "以火焰结界保护队伍并反制近身敌人。");
            triss.CurrentEquipment.Weapon = sampleEquipment[EquipmentSlot.Weapon][2];
            triss.CurrentEquipment.Armor = sampleEquipment[EquipmentSlot.Armor][2];
            triss.CurrentEquipment.Accessory1 = sampleEquipment[EquipmentSlot.Accessory1][2];
            triss.CurrentEquipment.RelicCore = sampleEquipment[EquipmentSlot.RelicCore][2];

            allMembers.Add(hunter);
            allMembers.Add(yennefer);
            allMembers.Add(triss);
            AddMonsterPartnerPlaceholder("莉莉丝");

            activeParty.Add(hunter);
            activeParty.Add(yennefer);
            RemoveHiddenMainPartyMembers();
            PartyChanged?.Invoke();
        }

        private void RemoveHiddenMainPartyMembers()
        {
            for (int i = activeParty.Count - 1; i >= 0; i--)
            {
                PartyMember member = activeParty[i];
                if (member != null && member.Name == "特莉丝")
                {
                    member.IsJoined = false;
                    activeParty.RemoveAt(i);
                }
            }
        }

        private void BuildSampleEquipment()
        {
            sampleEquipment[EquipmentSlot.Weapon] = new List<EquipmentItem>
            {
                new EquipmentItem("银鸦长剑", EquipmentSlot.Weapon, 0, 0, 7, 0, 0, 1, 0.02f),
                new EquipmentItem("紫晶法杖", EquipmentSlot.Weapon, 0, 12, 1, 0, 8, 0, 0.01f),
                new EquipmentItem("赤焰法器", EquipmentSlot.Weapon, 4, 8, 2, 0, 7, 2, 0.03f)
            };
            sampleEquipment[EquipmentSlot.Armor] = new List<EquipmentItem>
            {
                new EquipmentItem("猎魔皮甲", EquipmentSlot.Armor, 16, 0, 0, 6, 0, 1, 0f),
                new EquipmentItem("黑绒法袍", EquipmentSlot.Armor, 8, 18, 0, 3, 6, 0, 0f),
                new EquipmentItem("红羽术袍", EquipmentSlot.Armor, 12, 12, 0, 4, 5, 2, 0.01f)
            };
            sampleEquipment[EquipmentSlot.Accessory1] = new List<EquipmentItem>
            {
                new EquipmentItem("黑月戒指", EquipmentSlot.Accessory1, 0, 8, 1, 0, 2, 0, 0.02f),
                new EquipmentItem("女术士护符", EquipmentSlot.Accessory1, 0, 14, 0, 0, 5, 1, 0f),
                new EquipmentItem("赤蔷薇耳坠", EquipmentSlot.Accessory1, 4, 6, 1, 0, 4, 1, 0.02f)
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
                new EquipmentItem("火纹魔核", EquipmentSlot.RelicCore, 10, 10, 1, 0, 7, 1, 0.02f)
            };
        }
    }
}
