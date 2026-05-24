using System.Collections.Generic;

namespace WitcherGame
{
    // 中文说明：定义一个回合制技能的消耗、目标、动画和效果列表。
    public class SkillDefinition
    {
        public SkillDefinition(
            BattleSkillId id,
            string displayName,
            int manaCost,
            BattleSkillTargetKind targetKind,
            BattleSkillAnimationKind animationKind,
            string useMessage,
            params SkillEffect[] effects)
        {
            Id = id;
            DisplayName = displayName;
            ManaCost = System.Math.Max(0, manaCost);
            TargetKind = targetKind;
            AnimationKind = animationKind;
            UseMessage = useMessage;
            Effects = new List<SkillEffect>(effects ?? new SkillEffect[0]);
        }

        public BattleSkillId Id { get; }
        public string DisplayName { get; }
        public int ManaCost { get; }
        public BattleSkillTargetKind TargetKind { get; }
        public BattleSkillAnimationKind AnimationKind { get; }
        public string UseMessage { get; }
        public IReadOnlyList<SkillEffect> Effects { get; }
    }
}
