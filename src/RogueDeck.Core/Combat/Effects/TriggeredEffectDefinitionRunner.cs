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
            if (definition is ITriggeredEffectDefinition reentryDefinition
                && combat.CurrentEffectChain is { } currentEffectChain)
            {
                if (!currentEffectChain.CanEnterTriggeredEffectDefinition(reentryDefinition))
                    continue;

                currentEffectChain.EnsureCanAppendTriggeredEffectDefinition(
                    reentryDefinition.Id);
            }

            if (!matches(definition))
                continue;

            CombatEffectChainContext? triggeredEffectChain = null;

            foreach (var effectRequest in buildEffectRequests(definition))
            {
                if (definition is ITriggeredEffectDefinition triggeredDefinition)
                {
                    triggeredEffectChain ??=
                        combat.CreateTriggeredEffectChain(triggeredDefinition.Id);

                    combat.EnqueueEffect(
                        effectRequest,
                        triggeredEffectChain);

                    continue;
                }

                combat.EnqueueEffect(effectRequest);
            }
        }
    }
}
