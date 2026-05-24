using System.Collections.Generic;
using NUnit.Framework;

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
    }
}
