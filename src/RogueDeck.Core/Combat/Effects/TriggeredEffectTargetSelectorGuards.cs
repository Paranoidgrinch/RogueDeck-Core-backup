namespace RogueDeck.Core.Combat;

internal static class TriggeredEffectTargetSelectorGuards
{
    public static bool ShouldSuppressMissingSourceTarget(
        CombatantState? sourceCombatant,
        ICombatantTargetSelector? targetSelector)
    {
        return sourceCombatant is null &&
            targetSelector is SourceCombatantTargetSelector;
    }
}
