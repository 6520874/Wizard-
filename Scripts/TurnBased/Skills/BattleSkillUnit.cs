using System;
using System.Collections.Generic;

namespace WitcherGame
{
    // 中文说明：统一表示玩家或敌人在技能结算中的战斗属性和状态。
    public class BattleSkillUnit
    {
        private readonly List<BattleStatusEffect> statuses = new List<BattleStatusEffect>();

        private BattleSkillUnit(string name, bool isPlayer, int enemyIndex, int maxHealth, int maxMana, int attack, int defense)
        {
            Name = name;
            IsPlayer = isPlayer;
            EnemyIndex = enemyIndex;
            MaxHealth = Math.Max(1, maxHealth);
            Health = MaxHealth;
            MaxMana = Math.Max(0, maxMana);
            Mana = MaxMana;
            Attack = Math.Max(0, attack);
            Defense = Math.Max(0, defense);
        }

        public string Name { get; }
        public bool IsPlayer { get; }
        public int EnemyIndex { get; }
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public int MaxMana { get; }
        public int Mana { get; private set; }
        public int Attack { get; }
        public int Defense { get; }
        public IReadOnlyList<BattleStatusEffect> Statuses => statuses;
        public bool IsAlive => Health > 0;

        public int EffectiveAttack => Math.Max(0, Attack + SumStatusValue(status => status.AttackBonus));
        public int EffectiveDefense => Math.Max(0, Defense + SumStatusValue(status => status.DefenseBonus));

        public static BattleSkillUnit CreatePlayer(string name, int maxHealth, int maxMana, int attack, int defense)
        {
            return new BattleSkillUnit(name, true, -1, maxHealth, maxMana, attack, defense);
        }

        public static BattleSkillUnit CreateEnemy(string name, int enemyIndex, int maxHealth, int attack, int defense)
        {
            return new BattleSkillUnit(name, false, enemyIndex, maxHealth, 0, attack, defense);
        }

        public void SetHealth(int value)
        {
            Health = Math.Max(0, Math.Min(MaxHealth, value));
        }

        public void SetMana(int value)
        {
            Mana = Math.Max(0, Math.Min(MaxMana, value));
        }

        public bool TrySpendMana(int amount)
        {
            int cost = Math.Max(0, amount);
            if (Mana < cost)
            {
                return false;
            }

            Mana -= cost;
            return true;
        }

        public int ApplyDamage(int rawDamage)
        {
            int damage = CalculateIncomingDamage(rawDamage);
            Health = Math.Max(0, Health - damage);
            return damage;
        }

        public int CalculateIncomingDamage(int rawDamage)
        {
            int damage = Math.Max(0, rawDamage);
            for (int i = 0; i < statuses.Count; i++)
            {
                damage = (int)Math.Ceiling(damage * statuses[i].IncomingDamageMultiplier);
                damage = statuses[i].AbsorbDamage(damage);
            }

            return Math.Max(0, damage);
        }

        public int RestoreHealth(int amount)
        {
            int before = Health;
            Health = Math.Min(MaxHealth, Health + Math.Max(0, amount));
            return Health - before;
        }

        public int RestoreMana(int amount)
        {
            int before = Mana;
            Mana = Math.Min(MaxMana, Mana + Math.Max(0, amount));
            return Mana - before;
        }

        public void AddStatus(BattleStatusEffect status)
        {
            if (status == null)
            {
                return;
            }

            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i].Kind == status.Kind)
                {
                    statuses.RemoveAt(i);
                }
            }

            statuses.Add(status);
        }

        public void AddStatusInstance(BattleStatusEffect status)
        {
            AddStatus(status);
        }

        public void ConsumeStatusTurns()
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                statuses[i].ConsumeTurn();
                if (statuses[i].Expired)
                {
                    statuses.RemoveAt(i);
                }
            }
        }

        private int SumStatusValue(Func<BattleStatusEffect, int> selector)
        {
            int value = 0;
            for (int i = 0; i < statuses.Count; i++)
            {
                value += selector(statuses[i]);
            }

            return value;
        }
    }
}
