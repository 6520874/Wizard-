using System.Collections.Generic;

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
                case BattleSkillId.TrissFirebolt:
                    return CreateTrissFirebolt();
                case BattleSkillId.TrissMeltingSigil:
                    return CreateTrissMeltingSigil();
                case BattleSkillId.TrissFlameWard:
                    return CreateTrissFlameWard();
                case BattleSkillId.TrissMeteorFlare:
                    return CreateTrissMeteorFlare();
                case BattleSkillId.YenneferArcaneBolt:
                    return CreateYenneferArcaneBolt();
                case BattleSkillId.YenneferCursePulse:
                    return CreateYenneferCursePulse();
                case BattleSkillId.YenneferAegis:
                    return CreateYenneferAegis();
                case BattleSkillId.YenneferObsidianStorm:
                    return CreateYenneferObsidianStorm();
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

        public static IReadOnlyList<SkillDefinition> GetFriendlySkills(PartyMember member)
        {
            List<SkillDefinition> skills = new List<SkillDefinition>();
            if (member != null && member.Name == "特莉丝")
            {
                skills.Add(CreateTrissFirebolt());
                skills.Add(CreateTrissMeltingSigil());
                skills.Add(CreateTrissFlameWard());
                skills.Add(CreateTrissMeteorFlare());
                return skills;
            }

            if (member != null && member.Name == "叶奈法")
            {
                skills.Add(CreateYenneferArcaneBolt());
                skills.Add(CreateYenneferCursePulse());
                skills.Add(CreateYenneferAegis());
                skills.Add(CreateYenneferObsidianStorm());
                return skills;
            }

            skills.Add(CreateExecuteSlash());
            skills.Add(CreateFlameSign());
            skills.Add(CreateThunderSign());
            skills.Add(CreateHunterFocus());
            return skills;
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

        public static SkillDefinition CreateTrissFirebolt()
        {
            return new SkillDefinition(
                BattleSkillId.TrissFirebolt,
                "火焰术",
                12,
                BattleSkillTargetKind.FirstLivingEnemy,
                BattleSkillAnimationKind.Flame,
                "特莉丝投出压缩火球，砸向首个敌人！",
                new DamageSkillEffect(28, 0f, 0.25f, 1, BattleDamageType.Fire));
        }

        public static SkillDefinition CreateTrissMeltingSigil()
        {
            return new SkillDefinition(
                BattleSkillId.TrissMeltingSigil,
                "熔甲火印",
                18,
                BattleSkillTargetKind.AllLivingEnemies,
                BattleSkillAnimationKind.Flame,
                "特莉丝点燃敌群护甲的缝隙，降低怪物防御！",
                new DamageSkillEffect(16, 0f, 0.2f, 1, BattleDamageType.Fire),
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Burning, "熔甲", 2, defenseBonus: -3)));
        }

        public static SkillDefinition CreateTrissFlameWard()
        {
            return new SkillDefinition(
                BattleSkillId.TrissFlameWard,
                "灼热结界",
                16,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Defend,
                "特莉丝在前排展开灼热结界。",
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Shield, "灼热结界", 3, incomingDamageMultiplier: 0.72f, shieldAmount: 22)));
        }

        public static SkillDefinition CreateTrissMeteorFlare()
        {
            return new SkillDefinition(
                BattleSkillId.TrissMeteorFlare,
                "流星火雨",
                28,
                BattleSkillTargetKind.AllLivingEnemies,
                BattleSkillAnimationKind.Flame,
                "特莉丝召下流星火雨，席卷敌群！",
                new DamageSkillEffect(34, 0f, 0.18f, 1, BattleDamageType.Fire));
        }

        public static SkillDefinition CreateYenneferArcaneBolt()
        {
            return new SkillDefinition(
                BattleSkillId.YenneferArcaneBolt,
                "紫晶箭",
                12,
                BattleSkillTargetKind.FirstLivingEnemy,
                BattleSkillAnimationKind.Cast,
                "叶奈法凝出紫晶箭，贯穿首个敌人！",
                new DamageSkillEffect(24, 0f, 0.25f, 1, BattleDamageType.Arcane));
        }

        public static SkillDefinition CreateYenneferCursePulse()
        {
            return new SkillDefinition(
                BattleSkillId.YenneferCursePulse,
                "诅咒脉冲",
                18,
                BattleSkillTargetKind.AllLivingEnemies,
                BattleSkillAnimationKind.Cast,
                "叶奈法释放诅咒脉冲，削弱敌群防御！",
                new DamageSkillEffect(14, 0f, 0.22f, 1, BattleDamageType.Arcane),
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Fear, "诅咒", 2, defenseBonus: -3)));
        }

        public static SkillDefinition CreateYenneferAegis()
        {
            return new SkillDefinition(
                BattleSkillId.YenneferAegis,
                "紫晶护盾",
                16,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Defend,
                "叶奈法为前排展开紫晶护盾。",
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Shield, "紫晶护盾", 3, incomingDamageMultiplier: 0.7f, shieldAmount: 24)));
        }

        public static SkillDefinition CreateYenneferObsidianStorm()
        {
            return new SkillDefinition(
                BattleSkillId.YenneferObsidianStorm,
                "黑曜风暴",
                28,
                BattleSkillTargetKind.AllLivingEnemies,
                BattleSkillAnimationKind.Cast,
                "叶奈法召来黑曜碎光，席卷敌群！",
                new DamageSkillEffect(30, 0f, 0.2f, 1, BattleDamageType.Arcane));
        }

        public static IReadOnlyList<SkillDefinition> GetEnemySkills(TurnBasedEnemyState enemy)
        {
            List<SkillDefinition> skills = new List<SkillDefinition>();
            if (enemy == null)
            {
                skills.Add(CreateCorruptedBite());
                return skills;
            }

            switch (enemy.VisualKind)
            {
                case TurnBasedEnemyVisualKind.BloodWraith:
                    skills.Add(CreateBloodDrain());
                    skills.Add(CreatePlagueHowl());
                    skills.Add(CreateCorruptedBite());
                    break;
                case TurnBasedEnemyVisualKind.BlackMoonKnight:
                    skills.Add(CreateMoonbreaker());
                    skills.Add(CreatePlagueHowl());
                    skills.Add(CreateBloodDrain());
                    break;
                default:
                    skills.Add(CreateCorruptedBite());
                    skills.Add(CreatePlagueHowl());
                    break;
            }

            return skills;
        }

        public static SkillDefinition CreateCorruptedBite()
        {
            return new SkillDefinition(
                BattleSkillId.CorruptedBite,
                "腐毒撕咬",
                0,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Slash,
                "怪物扑咬猎魔人，污血渗入伤口！",
                new DamageSkillEffect(3, 1f, 0.45f),
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Corruption, "腐毒", 2, incomingDamageMultiplier: 1.08f, damageOverTime: 4)));
        }

        public static SkillDefinition CreatePlagueHowl()
        {
            return new SkillDefinition(
                BattleSkillId.PlagueHowl,
                "瘟疫嚎叫",
                0,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Cast,
                "怪物发出刺耳嚎叫，猎魔人的攻势被压住了！",
                new DamageSkillEffect(0, 0.45f, 0.25f),
                new StatusSkillEffect(() => new BattleStatusEffect(BattleStatusKind.Fear, "恐惧", 2, attackBonus: -4, defenseBonus: -2)));
        }

        public static SkillDefinition CreateBloodDrain()
        {
            return new SkillDefinition(
                BattleSkillId.BloodDrain,
                "吸血咒吻",
                0,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Cast,
                "血影缠住猎魔人，怪物从伤口里夺回生命！",
                new DamageSkillEffect(7, 0.85f, 0.35f));
        }

        public static SkillDefinition CreateMoonbreaker()
        {
            return new SkillDefinition(
                BattleSkillId.Moonbreaker,
                "黑月断斩",
                0,
                BattleSkillTargetKind.Self,
                BattleSkillAnimationKind.Slash,
                "月夜骑士拖出黑月般的剑痕！",
                new DamageSkillEffect(14, 1.15f, 0.55f, 2, BattleDamageType.Pure));
        }
    }
}
