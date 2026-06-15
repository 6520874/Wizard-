using System;
using System.Collections.Generic;

namespace WitcherGame
{
    // 中文说明：统一执行技能定义，负责扣资源并把效果应用到目标身上。
    public static class SkillExecutor
    {
        public static bool CanUse(SkillDefinition skill, BattleSkillUnit caster)
        {
            return skill != null && caster != null && caster.Mana >= skill.ManaCost;
        }

        public static SkillResult Execute(SkillDefinition skill, BattleSkillUnit caster, IEnumerable<BattleSkillUnit> targets)
        {
            if (skill == null)
            {
                throw new ArgumentNullException(nameof(skill));
            }

            if (caster == null)
            {
                throw new ArgumentNullException(nameof(caster));
            }

            SkillResult result = new SkillResult(skill)
            {
                Message = skill.UseMessage
            };

            if (!caster.TrySpendMana(skill.ManaCost))
            {
                result.Message = GameText.Battle.NotEnoughManaShort;
                return result;
            }

            result.ManaSpent = skill.ManaCost;
            if (targets == null)
            {
                return result;
            }

            foreach (BattleSkillUnit target in targets)
            {
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                for (int i = 0; i < skill.Effects.Count; i++)
                {
                    skill.Effects[i].Apply(caster, target, result);
                }
            }

            return result;
        }
    }
}
