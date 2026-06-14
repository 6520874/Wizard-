using System.Collections.Generic;

namespace WitcherGame
{
    // 中文说明：记录一次技能结算产生的伤害、治疗、状态和提示文本。
    public class SkillResult
    {
        public SkillResult(SkillDefinition skill)
        {
            Skill = skill;
        }

        public SkillDefinition Skill { get; }
        public int ManaSpent { get; set; }
        public string Message { get; set; }
        public List<SkillTargetResult> TargetResults { get; } = new List<SkillTargetResult>();

        public SkillTargetResult GetOrCreateTarget(BattleSkillUnit target)
        {
            for (int i = 0; i < TargetResults.Count; i++)
            {
                if (TargetResults[i].EnemyIndex == target.EnemyIndex && TargetResults[i].IsPlayerTarget == target.IsPlayer)
                {
                    return TargetResults[i];
                }
            }

            SkillTargetResult result = new SkillTargetResult(target);
            TargetResults.Add(result);
            return result;
        }
    }

    // 中文说明：记录技能对单个目标造成的伤害、治疗、状态和最终生命值。
    public class SkillTargetResult
    {
        public SkillTargetResult(BattleSkillUnit target)
        {
            TargetName = target.Name;
            IsPlayerTarget = target.IsPlayer;
            EnemyIndex = target.EnemyIndex;
            StartingHealth = target.Health;
        }

        public string TargetName { get; }
        public bool IsPlayerTarget { get; }
        public int EnemyIndex { get; }
        public int StartingHealth { get; }
        public int FinalHealth { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaRestored { get; set; }
        public BattleStatusKind? AddedStatus { get; set; }
        public bool Defeated => FinalHealth <= 0 && Damage > 0;
    }
}
