using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：保存玩家金币、经验和战利品，后续可接入正式背包系统。
    public class PlayerInventory : MonoBehaviour
    {
        private const int DefaultStartingGold = 180;

        [SerializeField] private int gold = DefaultStartingGold;
        [SerializeField] private int experience;
        [SerializeField] private List<string> ownedLoot = new List<string>();

        public int Gold => gold;
        public int Experience => experience;
        public IReadOnlyList<string> OwnedLoot => ownedLoot;
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

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gold += amount;
            InventoryChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            experience += amount;
            InventoryChanged?.Invoke();
        }

        public void AddLoot(string lootName)
        {
            if (string.IsNullOrWhiteSpace(lootName))
            {
                return;
            }

            ownedLoot.Add(lootName);
            InventoryChanged?.Invoke();
        }

        public void AddBattleRewards(int goldAmount, int experienceAmount, IReadOnlyList<string> lootNames)
        {
            bool changed = false;
            if (goldAmount > 0)
            {
                gold += goldAmount;
                changed = true;
            }

            if (experienceAmount > 0)
            {
                experience += experienceAmount;
                changed = true;
            }

            if (lootNames != null)
            {
                for (int i = 0; i < lootNames.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(lootNames[i]))
                    {
                        continue;
                    }

                    ownedLoot.Add(lootNames[i]);
                    changed = true;
                }
            }

            if (changed)
            {
                InventoryChanged?.Invoke();
            }
        }
    }
}
