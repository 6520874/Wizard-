namespace WitcherGame
{
    // 中文说明：集中创建当前猎魔人和怪物可用的默认技能定义。
    public static class WitcherSkillBook
    {
        public static SkillDefinition GetPlayerSkill(TurnBattleAction action)
        {
            switch (action)
            {
                case TurnBattleAction.FlameSign:
                    return CreateFlameSign();
                case TurnBattleAction.Defend:
                    return CreateDefend();
                case TurnBattleAction.Item:
                    return CreatePotion();
                default:
                    return CreateBasicAttack();
            }
        }

        public static SkillDefinition GetPlayerSkill(BattleSkillId skillId)
        {
            switch (skillId)
            {
                case BattleSkillId.ExecuteSlash:
                    return CreateExecuteSlash();
                case BattleSkillId.FlameSign:
                    return CreateFlameSign();
                case BattleSkillId.ThunderSign:
                    return CreateThunderSign();
                case BattleSkillId.HunterFocus:
                    return CreateHunterFocus();
                case BattleSkillId.Fireball:
                    return CreateFireball();
                case BattleSkillId.ArcaneBurst:
                    return CreateArcaneBurst();
                case BattleSkillId.Potion:
                    return CreatePotion();
                default:
                    return CreateBasicAttack();
            }
        }

        public static SkillDefinition CreateBasicAttack()
        {
            return new SkillDefinition(
                BattleSkillId.BasicAttack,
                "银剑斩击",
                0,
                BattleSkillTargetKind.FirstLivingEnemy,
                BattleSkillAnimationKind.Slash,
                "猎魔人挥出银剑。",
                new DamageSkillEffect(0, 1f, 1f));
        }

        public static SkillDefinition CreateFlameSign()
        {
            return new SkillDefinition(
                BattleSkillId.FlameSign,
                "火焰法印",
                18,
                BattleSkillTargetKind.AllLivingEnemies,
                BattleSkillAnimationKind.Flame,
                "火焰法印横扫敌群！",
                new DamageSkillEffect(26, 0f, 0.5f, 1, BattleDamageType.Fire));
        }

        public static SkillDefinition CreateDefend()
        {
            return new SkillDefinition(
                BattleSkillId.Defend,
                "防御",
                0,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Defend,
                "猎魔人架起银剑，准备承受攻击。",
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Guard, "防御", 1, incomingDamageMultiplier: 0.45f)));
        }

        public static SkillDefinition CreatePotion()
        {
            return CreatePotion(32);
        }

        public static SkillDefinition CreatePotion(int healthAmount)
        {
            return new SkillDefinition(
                BattleSkillId.Potion,
                "燕子药剂",
                0,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Item,
                "喝下燕子药剂。",
                new HealSkillEffect(healthAmount));
        }

        public static SkillDefinition CreateExecuteSlash()
        {
            return new SkillDefinition(
                BattleSkillId.ExecuteSlash,
                "连续斩杀",
                12,
                BattleSkillTargetKind.FirstLivingEnemy,
                BattleSkillAnimationKind.Slash,
                "猎魔人连续压制目标！",
                new DamageSkillEffect(2, 0.72f, 0.8f, 3));
        }

        public static SkillDefinition CreateFireball()
        {
            return new SkillDefinition(
                BattleSkillId.Fireball,
                "火球术",
                16,
                BattleSkillTargetKind.FirstLivingEnemy,
                BattleSkillAnimationKind.Flame,
                "火球砸向怪物！",
                new DamageSkillEffect(34, 0f, 0.35f, 1, BattleDamageType.Fire));
        }

        public static SkillDefinition CreateThunderSign()
        {
            return new SkillDefinition(
                BattleSkillId.ThunderSign,
                "雷霆法印",
                20,
                BattleSkillTargetKind.FirstLivingEnemy,
                BattleSkillAnimationKind.Cast,
                "雷霆法印劈向首个敌人！",
                new DamageSkillEffect(30, 0f, 0.25f, 2, BattleDamageType.Lightning));
        }

        public static SkillDefinition CreateArcaneBurst()
        {
            return new SkillDefinition(
                BattleSkillId.ArcaneBurst,
                "远程魔能爆发",
                24,
                BattleSkillTargetKind.AllLivingEnemies,
                BattleSkillAnimationKind.Cast,
                "魔能在敌群中炸裂！",
                new DamageSkillEffect(20, 0f, 0.25f, 1, BattleDamageType.Arcane));
        }

        public static SkillDefinition CreateHunterFocus()
        {
            return new SkillDefinition(
                BattleSkillId.HunterFocus,
                "猎魔专注",
                10,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Cast,
                "猎魔人进入专注状态。",
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.AttackUp, "专注", 3, attackBonus: 6)));
        }
    }
}
