using System;
using System.Collections.Generic;

namespace WitcherGame
{
    [Serializable]
    // 中文说明：保存队伍成员已学会技能的名称、定位、消耗和说明。
    public class PartySkill
    {
        public string Name;
        public string Role;
        public int MpCost;
        public string Description;

        public PartySkill(string name, string role, int mpCost, string description)
        {
            Name = name;
            Role = role;
            MpCost = Math.Max(0, mpCost);
            Description = description;
        }

        public override string ToString()
        {
            return MpCost > 0 ? $"{Name}({MpCost}MP)" : Name;
        }
    }

    [Serializable]
    // 中文说明：保存一个队伍成员的基础属性、技能和入队状态。
    public class PartyMember
    {
        public string Name;
        public int Level;
        public int HP;
        public int MaxHP;
        public int MP;
        public int MaxMP;
        public int Attack;
        public int Defense;
        public int Magic;
        public int Speed;
        public float CriticalRate;
        public bool IsJoined;
        public List<string> Skills = new List<string>();
        public List<PartySkill> SkillDetails = new List<PartySkill>();

        public int TotalMaxHP => MaxHP;
        public int TotalMaxMP => MaxMP;
        public int TotalAttack => Attack;
        public int TotalDefense => Defense;
        public int TotalMagic => Magic;
        public int TotalSpeed => Speed;
        public float TotalCriticalRate => CriticalRate;

        public PartyMember(string name, int level, int maxHp, int maxMp, int attack, int defense, int magic, int speed, float criticalRate, bool isJoined)
        {
            Name = name;
            Level = level;
            HP = maxHp;
            MaxHP = maxHp;
            MP = maxMp;
            MaxMP = maxMp;
            Attack = attack;
            Defense = defense;
            Magic = magic;
            Speed = speed;
            CriticalRate = criticalRate;
            IsJoined = isJoined;
        }

        public void LearnSkill(string name, string role, int mpCost, string description)
        {
            if (!Skills.Contains(name))
            {
                Skills.Add(name);
            }

            SkillDetails.Add(new PartySkill(name, role, mpCost, description));
        }
    }
}
