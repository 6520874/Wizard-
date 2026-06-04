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
        YenneferArcaneBolt,
        YenneferCursePulse,
        YenneferAegis,
        YenneferObsidianStorm
    }

    public enum BattleSkillTargetKind
    {
        Self,
        FirstLivingEnemy,
        AllLivingEnemies,
        SingleEnemy
    }

    public enum BattleSkillAnimationKind
    {
        None,
        Slash,
        Cast,
        Flame,
        Defend,
        Item
    }

    public enum BattleDamageType
    {
        Physical,
        Fire,
        Lightning,
        Arcane,
        Pure
    }

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
