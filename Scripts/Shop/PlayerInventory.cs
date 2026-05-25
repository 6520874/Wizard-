using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：保存玩家金币和已购买装备，后续可接入正式背包与属性系统。
    public class PlayerInventory : MonoBehaviour
    {
        private const int DefaultStartingGold = 180;

        [SerializeField] private int gold = DefaultStartingGold;
        [SerializeField] private List<string> ownedEquipment = new List<string>();

        public int Gold => gold;
        public IReadOnlyList<string> OwnedEquipment => ownedEquipment;
        public event Action InventoryChanged;

        public static PlayerInventory CreateIfMissing(GeraltController player)
        {
            if (player == null)
            {
                return null;
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = player.gameObject.AddComponent<PlayerInventory>();
            }

            return inventory;
        }

        public bool HasEquipment(string itemName)
        {
            return !string.IsNullOrEmpty(itemName) && ownedEquipment.Contains(itemName);
        }

        public bool TryPurchase(ShopItemData item, out string message)
        {
            if (item == null)
            {
                message = "商品不存在";
                return false;
            }

            if (HasEquipment(item.ItemName))
            {
                message = "你已经拥有这件装备";
                return false;
            }

            if (gold < item.Price)
            {
                message = "金币不足";
                return false;
            }

            gold -= item.Price;
            ownedEquipment.Add(item.ItemName);
            InventoryChanged?.Invoke();
            message = $"购买成功：{item.ItemName}";
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gold += amount;
            InventoryChanged?.Invoke();
        }
    }
}
