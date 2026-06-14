namespace WitcherGame
{
    // 中文说明：定义回合制技能系统会用到的技能、目标、动画和状态枚举。
    public enum BattleSkillId
    {
        BasicAttack,
        FlameSign,
        Defend,
        Potion,
        ExecuteSlash,
        Fireball,
        ThunderSign,
        ArcaneBurst,
        HunterFocus,
        CorruptedBite,
        PlagueHowl,
        BloodDrain,
        Moonbreaker,
        TrissFirebolt,
        TrissMeltingSigil,
        TrissFlameWard,
        TrissMeteorFlare,
        YenneferArcaneBolt,
        YenneferCursePulse,
        YenneferAegis,
        YenneferObsidianStorm
    }

    // 中文说明：定义技能会选择自己、单体敌人或全体敌人作为目标。
    public enum BattleSkillTargetKind
    {
        Self,
        FirstLivingEnemy,
        AllLivingEnemies,
        SingleEnemy
    }

    // 中文说明：定义技能释放时使用的基础表现动画类型。
    public enum BattleSkillAnimationKind
    {
        None,
        Slash,
        Cast,
        Flame,
        Defend,
        Item
    }

    // 中文说明：定义技能造成伤害时用于弱点和抗性判断的伤害类型。
    public enum BattleDamageType
    {
        Physical,
        Fire,
        Lightning,
        Arcane,
        Pure
    }

    // 中文说明：定义战斗中可以附加到单位身上的状态效果类型。
    public enum BattleStatusKind
    {
        Guard,
        AttackUp,
        DefenseUp,
        Burning,
        Shield,
        Corruption,
        Fear
    }
}
