using System;

namespace WitcherGame
{
    // 中文说明：技能效果基类，派生类实现伤害、治疗和状态附加。
    public abstract class SkillEffect
    {
        public abstract void Apply(BattleSkillUnit caster, BattleSkillUnit target, SkillResult result);
    }

    // 中文说明：结算技能伤害，支持攻击、防御系数、伤害类型和多段命中。
    public class DamageSkillEffect : SkillEffect
    {
        public DamageSkillEffect(int power, float attackScale, float defenseScale, int hitCount = 1, BattleDamageType damageType = BattleDamageType.Physical)
        {
            Power = Math.Max(0, power);
            AttackScale = Math.Max(0f, attackScale);
            DefenseScale = Math.Max(0f, defenseScale);
            HitCount = Math.Max(1, hitCount);
            DamageType = damageType;
        }

        public int Power { get; }
        public float AttackScale { get; }
        public float DefenseScale { get; }
        public int HitCount { get; }
        public BattleDamageType DamageType { get; }

        public override void Apply(BattleSkillUnit caster, BattleSkillUnit target, SkillResult result)
        {
            SkillTargetResult targetResult = result.GetOrCreateTarget(target);
            int totalDamage = 0;
            for (int i = 0; i < HitCount; i++)
            {
                int rawDamage = Math.Max(1, Power + (int)Math.Round(caster.EffectiveAttack * AttackScale) - (int)Math.Round(target.EffectiveDefense * DefenseScale));
                totalDamage += target.ApplyDamage(rawDamage);
            }

            targetResult.Damage += totalDamage;
            targetResult.FinalHealth = target.Health;
        }
    }

    // 中文说明：结算治疗和回蓝效果，用于药剂、护盾回复等技能。
    public class HealSkillEffect : SkillEffect
    {
        public HealSkillEffect(int healthAmount, int manaAmount = 0)
        {
            HealthAmount = Math.Max(0, healthAmount);
            ManaAmount = Math.Max(0, manaAmount);
        }

        public int HealthAmount { get; }
        public int ManaAmount { get; }

        public override void Apply(BattleSkillUnit caster, BattleSkillUnit target, SkillResult result)
        {
            SkillTargetResult targetResult = result.GetOrCreateTarget(target);
            targetResult.Healing += target.RestoreHealth(HealthAmount);
            targetResult.ManaRestored += target.RestoreMana(ManaAmount);
            targetResult.FinalHealth = target.Health;
        }
    }

    // 中文说明：给目标附加持续状态，例如防御、燃烧、恐惧或腐化。
    public class StatusSkillEffect : SkillEffect
    {
        private readonly Func<BattleStatusEffect> createStatus;

        public StatusSkillEffect(Func<BattleStatusEffect> createStatus)
        {
            this.createStatus = createStatus;
        }

        public override void Apply(BattleSkillUnit caster, BattleSkillUnit target, SkillResult result)
        {
            BattleStatusEffect status = createStatus?.Invoke();
            if (status == null)
            {
                return;
            }

            target.AddStatus(status);
            SkillTargetResult targetResult = result.GetOrCreateTarget(target);
            targetResult.AddedStatus = status.Kind;
            targetResult.FinalHealth = target.Health;
        }
    }
}
