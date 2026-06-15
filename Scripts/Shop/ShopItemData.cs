using System;

namespace WitcherGame
{
    // 中文说明：定义装备店商品的名称、价格和基础属性加成。
    [Serializable]
    public class ShopItemData
    {
        public string ItemName;
        public int Price;
        public int AttackBonus;
        public int DefenseBonus;

        public ShopItemData(string itemName, int price, int attackBonus, int defenseBonus)
        {
            ItemName = itemName;
            Price = price;
            AttackBonus = attackBonus;
            DefenseBonus = defenseBonus;
        }

        public string GetBonusText()
        {
            if (AttackBonus > 0 && DefenseBonus > 0)
            {
                return GameText.Stats.AttackDefenseBonus(AttackBonus, DefenseBonus);
            }

            if (AttackBonus > 0)
            {
                return GameText.Stats.AttackBonus(AttackBonus);
            }

            if (DefenseBonus > 0)
            {
                return GameText.Stats.DefenseBonus(DefenseBonus);
            }

            return GameText.Stats.NoBonus;
        }
    }
}
