namespace RogueDeck.Core.Combat;

internal static class TriggeredEffectDefinitionRunner
{
    public static void EnqueueEffectRequests<TDefinition>(
        CombatState combat,
        IEnumerable<TDefinition> definitions,
        Func<TDefinition, bool> matches,
        Func<TDefinition, IEnumerable<IEffectRequest>> buildEffectRequests)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(buildEffectRequests);

        foreach (var definition in definitions)
        {
            if (!matches(definition))
                continue;

            foreach (var effectRequest in buildEffectRequests(definition))
                combat.EnqueueEffect(effectRequest);
        }
    }
}
