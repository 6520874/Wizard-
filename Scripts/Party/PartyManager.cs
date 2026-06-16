using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：管理队伍成员、上阵名单和队伍变化通知。
    public class PartyManager : MonoBehaviour
    {
        private const string ManagerName = "Party Manager";
        private const int MaxPartySize = 4;
        private const string HunterName = "猎魔人";

        private static PartyManager instance;

        private readonly List<PartyMember> allMembers = new List<PartyMember>();
        private readonly List<PartyMember> activeParty = new List<PartyMember>();
        private readonly HashSet<string> unlockedBattleMembers = new HashSet<string>();

        public IReadOnlyList<PartyMember> AllMembers => allMembers;
        public IReadOnlyList<PartyMember> ActiveParty => activeParty;
        public bool AllowsBattleAllies => unlockedBattleMembers.Count > 0;
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

            if (!IsMemberAllowedInActiveParty(member))
            {
                member.IsJoined = false;
                return false;
            }

            member.IsJoined = true;
            activeParty.Add(member);
            RemoveHiddenMainPartyMembers();
            PartyChanged?.Invoke();
            return true;
        }

        public bool RemoveMember(PartyMember member)
        {
            if (member == null || activeParty.Count <= 1 || member.Name == HunterName)
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

            if (!IsMemberAllowedInActiveParty(replacement))
            {
                replacement.IsJoined = false;
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
            if (member == null || member.Name == HunterName || !IsMemberAllowedInActiveParty(member))
            {
                return false;
            }

            return activeParty.Contains(member) ? RemoveMember(member) : AddMember(member);
        }

        public bool IsBattleMemberUnlocked(string memberName)
        {
            return memberName == HunterName || unlockedBattleMembers.Contains(memberName);
        }

        public bool UnlockStoryAllyForBattle(string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName) || memberName == HunterName)
            {
                return false;
            }

            EnsureDefaults();
            unlockedBattleMembers.Add(memberName);

            PartyMember member = FindMember(memberName);
            if (member == null)
            {
                PartyChanged?.Invoke();
                return false;
            }

            if (activeParty.Contains(member))
            {
                member.IsJoined = true;
                PartyChanged?.Invoke();
                return true;
            }

            return AddMember(member);
        }

        public PartyMember GetActiveMember(int index)
        {
            if (activeParty.Count == 0)
            {
                EnsureDefaults();
            }

            return activeParty[Mathf.Clamp(index, 0, activeParty.Count - 1)];
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
            if (allMembers.Count > 0)
            {
                RemoveHiddenMainPartyMembers();
                return;
            }

            PartyMember hunter = CreateDefaultHunter();
            PartyMember yennefer = CreateDefaultYennefer();
            PartyMember triss = CreateDefaultTriss();

            allMembers.Add(hunter);
            allMembers.Add(yennefer);
            allMembers.Add(triss);

            activeParty.Add(hunter);
            RemoveHiddenMainPartyMembers();
            PartyChanged?.Invoke();
        }

        private PartyMember CreateDefaultHunter()
        {
            PartyMember hunter = new PartyMember("猎魔人", 1, 120, 100, 18, 7, 6, 13, 0.06f, true);
            hunter.LearnSkill("银剑斩", "物理 / 单体", 0, "可靠的猎魔人基础攻击。");
            hunter.LearnSkill("焚焰法印", "火焰 / 群体", 18, "用法印横扫敌群。");
            return hunter;
        }

        private PartyMember CreateDefaultYennefer()
        {
            PartyMember yennefer = new PartyMember("叶奈法", 1, 88, 142, 8, 5, 24, 12, 0.04f, false);
            yennefer.LearnSkill("紫晶护盾", "防护 / 护盾", 16, "为队伍展开紫色魔法护盾。");
            yennefer.LearnSkill("诅咒脉冲", "奥术 / 弱化", 20, "释放扭曲脉冲削弱敌人的防御。");
            yennefer.LearnSkill("紫晶箭", "奥术 / 单体", 12, "凝出紫晶箭贯穿单个怪物。");
            yennefer.LearnSkill("黑曜风暴", "奥术 / 群体", 28, "以黑曜碎光席卷敌群。");
            return yennefer;
        }

        private PartyMember CreateDefaultTriss()
        {
            PartyMember triss = new PartyMember("特莉丝", 1, 102, 126, 11, 6, 22, 14, 0.06f, false);
            triss.LearnSkill("火焰术", "火焰 / 单体", 14, "向目标投出压缩火球。");
            triss.LearnSkill("灼热结界", "火焰 / 防护", 22, "以火焰结界保护队伍并反制近身敌人。");
            triss.LearnSkill("熔甲火印", "火焰 / 弱化", 18, "点燃敌人护甲缝隙，降低怪物防御。");
            triss.LearnSkill("流星火雨", "火焰 / 群体", 28, "召下火雨压制敌群。");
            return triss;
        }

        private void RemoveHiddenMainPartyMembers()
        {
            activeParty.RemoveAll(member => member == null);
            activeParty.RemoveAll(member =>
            {
                bool remove = !IsMemberAllowedInActiveParty(member);
                if (remove && member != null)
                {
                    member.IsJoined = false;
                }

                return remove;
            });

            PartyMember hunter = allMembers.Find(member => member != null && member.Name == HunterName);
            if (hunter != null && !activeParty.Contains(hunter))
            {
                hunter.IsJoined = true;
                activeParty.Insert(0, hunter);
            }
        }

        private bool IsMemberAllowedInActiveParty(PartyMember member)
        {
            return member != null && (member.Name == HunterName || unlockedBattleMembers.Contains(member.Name));
        }
    }
}
