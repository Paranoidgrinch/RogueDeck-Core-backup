namespace RogueDeck.Core.Combat;

public sealed class CombatEffectQueueProcessor
{
    private readonly CombatEffectResolver _resolver = new();

    public void ResolvePendingEffects(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int maxEffectsToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);

        if (maxEffectsToResolve <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxEffectsToResolve),
                "Maximum effects to resolve must be greater than zero.");

        var resolvedEffects = 0;

        while (combat.HasPendingEffects && combat.Result == CombatResult.Ongoing)
        {
            if (resolvedEffects >= maxEffectsToResolve)
                throw new InvalidOperationException(
                    $"Stopped resolving pending effects after reaching the limit of {maxEffectsToResolve} effects.");

            var request = combat.DequeueNextEffect();

            _resolver.Resolve(combat, registry, request);

            resolvedEffects++;
        }
    }
}
