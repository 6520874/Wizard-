using System;

namespace WitcherGame
{
    // 中文说明：表示回合制战斗中的持续状态，例如防御、攻击提升、燃烧和护盾。
    [Serializable]
    public class BattleStatusEffect
    {
        public BattleStatusEffect(
            BattleStatusKind kind,
            string displayName,
            int remainingTurns,
            int attackBonus = 0,
            int defenseBonus = 0,
            float incomingDamageMultiplier = 1f,
            int shieldAmount = 0,
            int damageOverTime = 0)
        {
            Kind = kind;
            DisplayName = displayName;
            RemainingTurns = Math.Max(1, remainingTurns);
            AttackBonus = attackBonus;
            DefenseBonus = defenseBonus;
            IncomingDamageMultiplier = Math.Max(0f, incomingDamageMultiplier);
            ShieldAmount = Math.Max(0, shieldAmount);
            DamageOverTime = Math.Max(0, damageOverTime);
        }

        public BattleStatusKind Kind { get; }
        public string DisplayName { get; }
        public int RemainingTurns { get; private set; }
        public int AttackBonus { get; }
        public int DefenseBonus { get; }
        public float IncomingDamageMultiplier { get; }
        public int ShieldAmount { get; private set; }
        public int DamageOverTime { get; }

        public void ConsumeTurn()
        {
            RemainingTurns = Math.Max(0, RemainingTurns - 1);
        }

        public int AbsorbDamage(int damage)
        {
            if (ShieldAmount <= 0 || damage <= 0)
            {
                return damage;
            }

            int absorbed = Math.Min(ShieldAmount, damage);
            ShieldAmount -= absorbed;
            return damage - absorbed;
        }

        public bool Expired => RemainingTurns <= 0 || (Kind == BattleStatusKind.Shield && ShieldAmount <= 0);
    }
}
