using System;
using System.Collections.Generic;

namespace WitcherGame
{
    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Accessory1,
        Accessory2,
        RelicCore
    }

    [Serializable]
    public class EquipmentItem
    {
        public string Name;
        public EquipmentSlot Slot;
        public int HpBonus;
        public int MpBonus;
        public int AttackBonus;
        public int DefenseBonus;
        public int MagicBonus;
        public int SpeedBonus;
        public float CriticalBonus;

        public EquipmentItem(string name, EquipmentSlot slot, int hp, int mp, int attack, int defense, int magic, int speed, float critical)
        {
            Name = name;
            Slot = slot;
            HpBonus = hp;
            MpBonus = mp;
            AttackBonus = attack;
            DefenseBonus = defense;
            MagicBonus = magic;
            SpeedBonus = speed;
            CriticalBonus = critical;
        }
    }

    [Serializable]
    public class PartyEquipment
    {
        public EquipmentItem Weapon;
        public EquipmentItem Armor;
        public EquipmentItem Accessory1;
        public EquipmentItem Accessory2;
        public EquipmentItem RelicCore;

        public EquipmentItem Get(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return Weapon;
                case EquipmentSlot.Armor:
                    return Armor;
                case EquipmentSlot.Accessory1:
                    return Accessory1;
                case EquipmentSlot.Accessory2:
                    return Accessory2;
                case EquipmentSlot.RelicCore:
                    return RelicCore;
                default:
                    return null;
            }
        }

        public void Set(EquipmentSlot slot, EquipmentItem item)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    Weapon = item;
                    break;
                case EquipmentSlot.Armor:
                    Armor = item;
                    break;
                case EquipmentSlot.Accessory1:
                    Accessory1 = item;
                    break;
                case EquipmentSlot.Accessory2:
                    Accessory2 = item;
                    break;
                case EquipmentSlot.RelicCore:
                    RelicCore = item;
                    break;
            }
        }

        public IEnumerable<EquipmentItem> EquippedItems()
        {
            if (Weapon != null) yield return Weapon;
            if (Armor != null) yield return Armor;
            if (Accessory1 != null) yield return Accessory1;
            if (Accessory2 != null) yield return Accessory2;
            if (RelicCore != null) yield return RelicCore;
        }
    }

    [Serializable]
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
        public PartyEquipment CurrentEquipment = new PartyEquipment();
        public List<string> Skills = new List<string>();

        public int TotalMaxHP => MaxHP + Sum(item => item.HpBonus);
        public int TotalMaxMP => MaxMP + Sum(item => item.MpBonus);
        public int TotalAttack => Attack + Sum(item => item.AttackBonus);
        public int TotalDefense => Defense + Sum(item => item.DefenseBonus);
        public int TotalMagic => Magic + Sum(item => item.MagicBonus);
        public int TotalSpeed => Speed + Sum(item => item.SpeedBonus);
        public float TotalCriticalRate => CriticalRate + SumFloat(item => item.CriticalBonus);

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

        private int Sum(Func<EquipmentItem, int> selector)
        {
            int total = 0;
            foreach (EquipmentItem item in CurrentEquipment.EquippedItems())
            {
                total += selector(item);
            }

            return total;
        }

        private float SumFloat(Func<EquipmentItem, float> selector)
        {
            float total = 0f;
            foreach (EquipmentItem item in CurrentEquipment.EquippedItems())
            {
                total += selector(item);
            }

            return total;
        }
    }
}
