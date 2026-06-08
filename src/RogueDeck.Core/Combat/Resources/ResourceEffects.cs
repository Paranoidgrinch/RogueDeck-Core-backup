namespace RogueDeck.Core.Combat;

public sealed record RefillResourceEffectRequest(
    CombatantId CombatantId,
    ResourceId ResourceId,
    int DefaultMax
) : IEffectRequest;

public sealed class RefillResourceEffectHandler : EffectRequestHandler<RefillResourceEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        RefillResourceEffectRequest request)
    {
        if (request.DefaultMax < 0)
            throw new ArgumentOutOfRangeException(nameof(request.DefaultMax), "Default max cannot be negative.");

        var combatant = combat.GetCombatant(request.CombatantId);

        if (!combatant.Resources.TryGetValue(request.ResourceId, out var resource))
        {
            combatant.Resources.Add(
                request.ResourceId,
                new ValuePoolState(
                    current: request.DefaultMax,
                    max: request.DefaultMax));

            combat.AddLogEntry(
                StandardCombatLogTypes.ResourceRefilled,
                $"Created and refilled resource '{request.ResourceId}' on '{request.CombatantId}' to {request.DefaultMax}.");

            combat.EnqueueEvent(new ResourceRefilledCombatEvent(
                request.CombatantId,
                request.ResourceId,
                PreviousCurrent: 0,
                NewCurrent: request.DefaultMax,
                Max: request.DefaultMax));

            return;
        }

        var previousCurrent = resource.Current;
        var refillTarget = resource.Max ?? request.DefaultMax;

        resource.SetCurrent(refillTarget);

        combat.AddLogEntry(
            StandardCombatLogTypes.ResourceRefilled,
            $"Refilled resource '{request.ResourceId}' on '{request.CombatantId}' from {previousCurrent} to {refillTarget}.");

        combat.EnqueueEvent(new ResourceRefilledCombatEvent(
            request.CombatantId,
            request.ResourceId,
            PreviousCurrent: previousCurrent,
            NewCurrent: refillTarget,
            Max: resource.Max));
    }
}
