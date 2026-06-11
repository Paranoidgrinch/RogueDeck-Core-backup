namespace RogueDeck.Core.Combat;

internal static class TriggeredEffectTargetSelectorGuards
{
    public static bool ShouldSuppressMissingSourceTarget(
        CombatantState? sourceCombatant,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(targetSelector);

        return sourceCombatant is null &&
            targetSelector is SourceCombatantTargetSelector;
    }
}
