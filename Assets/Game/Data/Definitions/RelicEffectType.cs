namespace ValorChronicle.Data.Definitions
{
    public enum RelicEffectType
    {
        None = 0,
        AttackIncrease = 1,
        MaxHpIncrease = 2,
        CriticalChanceIncrease = 3,
        CriticalDamageIncrease = 4,
        ElementDamageIncrease = 5,
        HealingIncrease = 6,
        ShieldAmountIncrease = 7,
        AttackTypeDamageIncrease = 8,
        DealtDamageIncrease = 9,
        PartyDamageReduction = 10,
        AttackReduction = 11,
        MaxHpReduction = 12
    }

    public enum RelicEffectConditionType
    {
        None = 0,
        PartyHpAtOrBelow = 1
    }
}
