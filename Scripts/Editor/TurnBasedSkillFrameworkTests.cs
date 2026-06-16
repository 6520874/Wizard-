using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WitcherGame.Tests
{
    // 中文说明：验证回合制技能、任务、敌人动画和队伍系统的编辑器单元测试。
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

        [Test]
        public void EnemyAnimation_BlackMoonKnightFolderFrames_UseBilinearFiltering()
        {
            TurnBasedEnemyState enemy = new TurnBasedEnemyState();

            TurnBasedEnemyAnimationLibrary.FillAnimations(enemy, TurnBasedEnemyVisualKind.BlackMoonKnight);

            Assert.NotNull(enemy.IdleFrames);
            Assert.Greater(enemy.IdleFrames.Length, 0);
            Assert.AreEqual(FilterMode.Bilinear, enemy.IdleFrames[0].texture.filterMode);
        }

        [Test]
        public void EnemyAnimation_BlackMoonKnightFolderFrames_UseHighResolutionSourceWithoutChangingWorldSize()
        {
            TurnBasedEnemyState enemy = new TurnBasedEnemyState();

            TurnBasedEnemyAnimationLibrary.FillAnimations(enemy, TurnBasedEnemyVisualKind.BlackMoonKnight);

            Assert.NotNull(enemy.IdleFrames);
            Assert.Greater(enemy.IdleFrames.Length, 0);
            Assert.GreaterOrEqual(enemy.IdleFrames[0].texture.width, 768);
            Assert.GreaterOrEqual(enemy.IdleFrames[0].texture.height, 640);
            Assert.AreEqual(256f, enemy.IdleFrames[0].pixelsPerUnit);
        }

        [Test]
        public void Movement_WalkableMap_DoesNotCreateRuntimePhysicsBlockers()
        {
            GameObject mapObject = new GameObject("Walkable Map Test");

            mapObject.AddComponent<WitcherVillageWalkableMap>();

            Assert.IsNull(GameObject.Find("Village Collision Blockers"));
            Object.DestroyImmediate(mapObject);
        }

        [Test]
        public void PartyManager_StoryUnlock_AddsTrissToActiveParty()
        {
            GameObject partyObject = new GameObject("Party Manager Test");
            PartyManager party = partyObject.AddComponent<PartyManager>();

            Assert.AreEqual(1, party.ActiveParty.Count);
            Assert.AreEqual("猎魔人", party.ActiveParty[0].Name);
            Assert.AreEqual(3, party.AllMembers.Count);

            Assert.IsTrue(party.UnlockStoryAllyForBattle("特莉丝"));
            Assert.AreEqual(2, party.ActiveParty.Count);
            Assert.AreEqual("特莉丝", party.ActiveParty[1].Name);
            Assert.GreaterOrEqual(party.ActiveParty[1].SkillDetails.Count, 2);
            Object.DestroyImmediate(partyObject);
        }

        [Test]
        public void PartyMember_TotalStats_UseBaseStats()
        {
            PartyMember member = new PartyMember("测试猎人", 1, 100, 30, 10, 5, 4, 8, 0.05f, true);

            Assert.AreEqual(100, member.TotalMaxHP);
            Assert.AreEqual(30, member.TotalMaxMP);
            Assert.AreEqual(10, member.TotalAttack);
            Assert.AreEqual(8, member.TotalSpeed);
            Assert.AreEqual(0.05f, member.TotalCriticalRate, 0.001f);
        }

        [Test]
        public void QuestManager_FirstMainQuest_KeepsExistingStory()
        {
            GameObject questObject = new GameObject("Quest Manager Test");
            QuestManager questManager = questObject.AddComponent<QuestManager>();

            questManager.StartFirstMainQuest();

            Assert.AreEqual("灰鸦村的哭声", questManager.ActiveQuest.title);
            Assert.That(questManager.ActiveQuest.description, Does.Contain("矿洞"));
            Object.DestroyImmediate(questObject);
        }

        [TestCase(TurnBasedEnemyVisualKind.CorruptedWolf)]
        [TestCase(TurnBasedEnemyVisualKind.BloodWraith)]
        public void EnemyAnimation_SheetFrames_UseBilinearFiltering(TurnBasedEnemyVisualKind visualKind)
        {
            TurnBasedEnemyState enemy = new TurnBasedEnemyState();

            TurnBasedEnemyAnimationLibrary.FillAnimations(enemy, visualKind);

            Assert.NotNull(enemy.IdleFrames);
            Assert.Greater(enemy.IdleFrames.Length, 0);
            Assert.AreEqual(FilterMode.Bilinear, enemy.IdleFrames[0].texture.filterMode);
        }

        [TestCase(TurnBasedEnemyVisualKind.BlackNailThrall)]
        [TestCase(TurnBasedEnemyVisualKind.BlackWaxAcolyte)]
        public void EnemyAnimation_SecondNightMonsters_LoadBlackNailFrames(TurnBasedEnemyVisualKind visualKind)
        {
            TurnBasedEnemyState enemy = new TurnBasedEnemyState();

            TurnBasedEnemyAnimationLibrary.FillAnimations(enemy, visualKind);

            Assert.NotNull(enemy.IdleFrames);
            Assert.Greater(enemy.IdleFrames.Length, 0);
            Assert.NotNull(enemy.AttackFrames);
            Assert.Greater(enemy.AttackFrames.Length, 0);
        }
    }
}
