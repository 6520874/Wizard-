using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WitcherGame.Tests
{
    public class TurnBasedSkillFrameworkTests
    {
        [Test]
        public void Execute_AllEnemyFireSkill_DamagesEveryLivingTarget()
        {
            BattleSkillUnit caster = BattleSkillUnit.CreatePlayer("猎魔人", 120, 100, 18, 4);
            BattleSkillUnit wolf = BattleSkillUnit.CreateEnemy("腐化狼", 0, 30, 8, 2);
            BattleSkillUnit wraith = BattleSkillUnit.CreateEnemy("吸血女妖", 1, 34, 10, 3);
            SkillDefinition skill = WitcherSkillBook.CreateFlameSign();

            SkillResult result = SkillExecutor.Execute(skill, caster, new[] { wolf, wraith });

            Assert.AreEqual(2, result.TargetResults.Count);
            Assert.Less(wolf.Health, wolf.MaxHealth);
            Assert.Less(wraith.Health, wraith.MaxHealth);
            Assert.AreEqual(82, caster.Mana);
        }

        [Test]
        public void Execute_DefenseSkill_AddsGuardStatusThatReducesIncomingDamage()
        {
            BattleSkillUnit caster = BattleSkillUnit.CreatePlayer("猎魔人", 120, 100, 18, 4);
            SkillDefinition skill = WitcherSkillBook.CreateDefend();

            SkillExecutor.Execute(skill, caster, new[] { caster });

            Assert.AreEqual(1, caster.Statuses.Count);
            Assert.AreEqual(BattleStatusKind.Guard, caster.Statuses[0].Kind);
            Assert.AreEqual(5, caster.CalculateIncomingDamage(12));
        }

        [Test]
        public void Inventory_PurchaseEquipment_AddsCombatBonuses()
        {
            GameObject inventoryObject = new GameObject("Inventory Test");
            PlayerInventory inventory = inventoryObject.AddComponent<PlayerInventory>();
            ShopItemData sword = new ShopItemData("猎人长剑", 120, 7, 0);

            bool purchased = inventory.TryPurchase(sword, out string message);

            Assert.IsTrue(purchased, message);
            Assert.AreEqual(60, inventory.Gold);
            Assert.AreEqual(7, inventory.AttackBonus);
            Assert.AreEqual(0, inventory.DefenseBonus);
            Assert.IsTrue(inventory.HasEquipment("猎人长剑"));
            Object.DestroyImmediate(inventoryObject);
        }

        [Test]
        public void Inventory_AddBattleRewards_StoresGoldExperienceAndLoot()
        {
            GameObject inventoryObject = new GameObject("Reward Test");
            PlayerInventory inventory = inventoryObject.AddComponent<PlayerInventory>();
            List<string> loot = new List<string> { "腐化狼牙", "女妖残纱" };

            inventory.AddBattleRewards(21, 7, loot);

            Assert.AreEqual(201, inventory.Gold);
            Assert.AreEqual(7, inventory.Experience);
            Assert.AreEqual(2, inventory.OwnedLoot.Count);
            Assert.AreEqual("腐化狼牙", inventory.OwnedLoot[0]);
            Object.DestroyImmediate(inventoryObject);
        }

        [Test]
        public void Hud_EnemyWeaknessRowsForThreeEnemies_StayAboveCommandPanel()
        {
            Vector2[] stagePositions =
            {
                new Vector2(-322f, 42f),
                new Vector2(-184f, -32f),
                new Vector2(-228f, 112f)
            };

            for (int i = 0; i < stagePositions.Length; i++)
            {
                Vector2 rowPosition = EnemyWeaknessItem.CalculateAnchoredPosition(stagePositions[i], i);

                Assert.GreaterOrEqual(rowPosition.y, -96f, $"Enemy weakness row {i + 1} should not overlap the bottom command panel.");
            }
        }

        [Test]
        public void Hud_EnemyWeaknessRowsForThreeEnemies_DoNotOverlapEnemySprites()
        {
            Vector2[] stagePositions =
            {
                new Vector2(-322f, 42f),
                new Vector2(-184f, -32f),
                new Vector2(-228f, 112f)
            };

            for (int i = 0; i < stagePositions.Length; i++)
            {
                Rect enemyRect = Rect.MinMaxRect(
                    stagePositions[i].x - 92f,
                    stagePositions[i].y - 92f,
                    stagePositions[i].x + 92f,
                    stagePositions[i].y + 92f);
                Vector2 rowPosition = EnemyWeaknessItem.CalculateAnchoredPosition(stagePositions[i], i);
                Rect rowRect = Rect.MinMaxRect(
                    rowPosition.x - 98f,
                    rowPosition.y - 17f,
                    rowPosition.x + 98f,
                    rowPosition.y + 17f);

                Assert.IsFalse(rowRect.Overlaps(enemyRect), $"Enemy weakness row {i + 1} should not cover the monster sprite.");
            }
        }
    }
}
