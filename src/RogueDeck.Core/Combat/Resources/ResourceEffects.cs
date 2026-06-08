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

public sealed record GainResourceEffectRequest(
    CombatantId CombatantId,
    ResourceId ResourceId,
    int Amount,
    int? DefaultMax = null
) : IEffectRequest;

public sealed class GainResourceEffectHandler : EffectRequestHandler<GainResourceEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        GainResourceEffectRequest request)
    {
        if (request.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Resource gain amount cannot be negative.");

        if (request.DefaultMax is < 0)
            throw new ArgumentOutOfRangeException(nameof(request.DefaultMax), "Default max cannot be negative.");

        if (request.Amount == 0)
            return;

        var combatant = combat.GetCombatant(request.CombatantId);

        if (!combatant.Resources.TryGetValue(request.ResourceId, out var resource))
        {
            var createdCurrent = request.Amount;

            if (request.DefaultMax.HasValue)
                createdCurrent = Math.Min(createdCurrent, request.DefaultMax.Value);

            combatant.Resources.Add(
                request.ResourceId,
                new ValuePoolState(
                    current: createdCurrent,
                    max: request.DefaultMax));

            if (createdCurrent == 0)
                return;

            combat.AddLogEntry(
                StandardCombatLogTypes.ResourceGained,
                $"Created resource '{request.ResourceId}' on '{request.CombatantId}' and gained {createdCurrent}.");

            combat.EnqueueEvent(new ResourceGainedCombatEvent(
                request.CombatantId,
                request.ResourceId,
                PreviousCurrent: 0,
                NewCurrent: createdCurrent,
                GainedAmount: createdCurrent,
                Max: request.DefaultMax));

            return;
        }

        var previousCurrent = resource.Current;
        var requestedCurrent = (long)previousCurrent + request.Amount;

        if (resource.Max.HasValue && !resource.CanExceedMax)
            requestedCurrent = Math.Min(requestedCurrent, resource.Max.Value);

        if (requestedCurrent > int.MaxValue)
            throw new OverflowException("Resource value exceeded Int32.MaxValue.");

        var newCurrent = (int)requestedCurrent;
        var gainedAmount = newCurrent - previousCurrent;

        if (gainedAmount <= 0)
            return;

        resource.SetCurrent(newCurrent);

        combat.AddLogEntry(
            StandardCombatLogTypes.ResourceGained,
            $"Combatant '{request.CombatantId}' gained {gainedAmount} resource '{request.ResourceId}'.");

        combat.EnqueueEvent(new ResourceGainedCombatEvent(
            request.CombatantId,
            request.ResourceId,
            PreviousCurrent: previousCurrent,
            NewCurrent: newCurrent,
            GainedAmount: gainedAmount,
            Max: resource.Max));
    }
}

