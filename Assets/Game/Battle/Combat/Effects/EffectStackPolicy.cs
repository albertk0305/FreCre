namespace ValorChronicle.Battle.Combat.Effects
{
    public enum EffectStackMagnitudeMode
    {
        MultiplyByStackCount = 0,
        Fixed = 1
    }

    public enum EffectStackPolicy
    {
        RefreshDuration = 0,
        SeparateInstance = 1,
        StackMagnitude = 2,
        ReplaceWithStronger = 3,
        ReplaceWithNewest = 4,
        Unique = 5,
        StackCount = 6
    }
}
