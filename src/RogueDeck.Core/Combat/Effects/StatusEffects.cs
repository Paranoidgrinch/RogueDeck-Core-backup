namespace RogueDeck.Core.Combat;

public sealed record ApplyStatusEffectRequest(
    CombatantId TargetCombatantId,
    StatusDefinitionId StatusDefinitionId,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null,
    int Stacks = 0,
    int DurationTurns = 0,
    int Charges = 0
) : IEffectRequest;

public sealed class ApplyStatusEffectHandler : EffectRequestHandler<ApplyStatusEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ApplyStatusEffectRequest applyStatus)
    {
        if (applyStatus.Stacks < 0)
            throw new ArgumentOutOfRangeException(nameof(applyStatus.Stacks), "Stacks cannot be negative.");

        if (applyStatus.DurationTurns < 0)
            throw new ArgumentOutOfRangeException(nameof(applyStatus.DurationTurns), "Duration cannot be negative.");

        if (applyStatus.Charges < 0)
            throw new ArgumentOutOfRangeException(nameof(applyStatus.Charges), "Charges cannot be negative.");

        var target = combat.GetCombatant(applyStatus.TargetCombatantId);
        var definition = registry.GetStatus(applyStatus.StatusDefinitionId);

        if (IsStatusApplicationIntercepted(
                combat,
                registry,
                applyStatus,
                target,
                definition))
        {
            return;
        }

        if (definition.StackingBehavior == StatusStackingBehavior.MergeWithExistingInstance)
        {
            var existingStatus = target.Statuses.FirstOrDefault(
                status => status.DefinitionId == applyStatus.StatusDefinitionId);

            if (existingStatus is not null)
            {
                existingStatus.SetStacks(existingStatus.Stacks + applyStatus.Stacks);
                existingStatus.SetDurationTurns(Math.Max(existingStatus.DurationTurns, applyStatus.DurationTurns));
                existingStatus.SetCharges(existingStatus.Charges + applyStatus.Charges);

                combat.AddLogEntry(
                    StandardCombatLogTypes.StatusMerged,
                    $"Merged status '{applyStatus.StatusDefinitionId}' on '{applyStatus.TargetCombatantId}'.");

                combat.EnqueueEvent(
                    new StatusMergedCombatEvent(
                        TargetCombatantId: applyStatus.TargetCombatantId,
                        StatusInstanceId: existingStatus.Id,
                        StatusDefinitionId: existingStatus.DefinitionId,
                        Stacks: existingStatus.Stacks,
                        DurationTurns: existingStatus.DurationTurns,
                        Charges: existingStatus.Charges,
                        SourceCombatantId: existingStatus.SourceCombatantId,
                        SourceCardId: existingStatus.SourceCardId));

                return;
            }
        }

        var newStatus = new StatusInstance(
            combat.CreateNextStatusInstanceId(),
            applyStatus.StatusDefinitionId,
            applyStatus.TargetCombatantId,
            applyStatus.SourceCombatantId,
            applyStatus.SourceCardId,
            applyStatus.Stacks,
            applyStatus.DurationTurns,
            applyStatus.Charges,
            combat.CurrentRound,
            combat.CurrentTurn,
            definition.DefaultVisibility,
            definition.Polarity);

        foreach (var tag in definition.Tags)
            newStatus.Tags.Add(tag);

        target.Statuses.Add(newStatus);

        combat.AddLogEntry(
            StandardCombatLogTypes.StatusApplied,
            $"Applied status '{applyStatus.StatusDefinitionId}' to '{applyStatus.TargetCombatantId}'.");

        combat.EnqueueEvent(
            new StatusAppliedCombatEvent(
                TargetCombatantId: applyStatus.TargetCombatantId,
                StatusInstanceId: newStatus.Id,
                StatusDefinitionId: newStatus.DefinitionId,
                Stacks: newStatus.Stacks,
                DurationTurns: newStatus.DurationTurns,
                Charges: newStatus.Charges,
                SourceCombatantId: newStatus.SourceCombatantId,
                SourceCardId: newStatus.SourceCardId));
    }

    private static bool IsStatusApplicationIntercepted(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ApplyStatusEffectRequest applyStatus,
        CombatantState target,
        StatusDefinition definition)
    {
        var context = new StatusApplicationInterceptionContext(
            Combat: combat,
            Registry: registry,
            TargetCombatant: target,
            StatusDefinition: definition,
            Request: applyStatus);

        foreach (var interceptor in registry.GetStatusApplicationInterceptors())
        {
            if (interceptor.TryIntercept(context))
                return true;
        }

        return false;
    }
}

public sealed record DecreaseStatusDurationEffectRequest(
    CombatantId TargetCombatantId,
    StatusInstanceId StatusInstanceId,
    int Amount = 1
) : IEffectRequest;

public sealed class DecreaseStatusDurationEffectHandler : EffectRequestHandler<DecreaseStatusDurationEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DecreaseStatusDurationEffectRequest decreaseDuration)
    {
        if (decreaseDuration.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(decreaseDuration.Amount), "Duration decrease amount must be greater than zero.");

        var target = combat.GetCombatant(decreaseDuration.TargetCombatantId);
        var status = target.Statuses.FirstOrDefault(
            existing => existing.Id == decreaseDuration.StatusInstanceId);

        if (status is null)
            return;

        if (status.DurationTurns <= 0)
            return;

        var oldDuration = status.DurationTurns;
        var newDuration = Math.Max(0, oldDuration - decreaseDuration.Amount);

        status.SetDurationTurns(newDuration);

        combat.AddLogEntry(
            StandardCombatLogTypes.StatusDurationReduced,
            $"Reduced status '{status.DefinitionId}' duration on '{target.Id}' from {oldDuration} to {newDuration}.");

        if (newDuration > 0)
            return;

        target.Statuses.Remove(status);

        combat.AddLogEntry(
            StandardCombatLogTypes.StatusExpired,
            $"Status '{status.DefinitionId}' expired on '{target.Id}'.");

        combat.EnqueueEvent(
            new StatusExpiredCombatEvent(
                TargetCombatantId: target.Id,
                StatusInstanceId: status.Id,
                StatusDefinitionId: status.DefinitionId));
    }
}

public sealed class DecreaseTimedStatusDurationsOnTurnEndedHandler
    : CombatEventHandler<TurnEndedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnEndedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var combatant))
            return;

        foreach (var status in combatant!.Statuses.ToArray())
        {
            var definition = registry.GetStatus(status.DefinitionId);

            if (!definition.UsesDuration)
                continue;

            if (status.DurationTurns <= 0)
                continue;

            combat.EnqueueEffect(
                new DecreaseStatusDurationEffectRequest(
                    TargetCombatantId: combatEvent.CombatantId,
                    StatusInstanceId: status.Id));
        }
    }
}

public sealed class DamageOverTimeOnTurnStartedHandler
    : CombatEventHandler<TurnStartedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnStartedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var combatant))
            return;

        foreach (var status in combatant!.Statuses.ToArray())
        {
            if (!status.Tags.Contains(StandardCombatIds.DamageOverTimeTag))
                continue;

            if (status.Stacks <= 0)
                continue;

            combat.EnqueueEffect(
                new DealDamageEffectRequest(
                    TargetCombatantId: combatEvent.CombatantId,
                    Amount: status.Stacks,
                    SourceCombatantId: status.SourceCombatantId,
                    SourceCardId: status.SourceCardId,
                    Kind: DamageKind.DamageOverTime));
        }
    }
}

public sealed class TriggeredDamageOnDamageDealtHandler
    : CombatEventHandler<DamageDealtCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DamageDealtCombatEvent combatEvent)
    {
        if (combatEvent.Kind == DamageKind.Reflected)
            return;

        if (combatEvent.HealthDamage <= 0)
            return;

        if (combatEvent.SourceCombatantId is null)
            return;

        if (combatEvent.SourceCombatantId.Value == combatEvent.TargetCombatantId)
            return;

        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var damagedCombatant))
            return;

        foreach (var status in damagedCombatant!.Statuses.ToArray())
        {
            if (!status.Tags.Contains(StandardCombatIds.TriggeredDamageTag))
                continue;

            if (status.Stacks <= 0)
                continue;

            combat.EnqueueEffect(
                new DealDamageEffectRequest(
                    TargetCombatantId: combatEvent.SourceCombatantId.Value,
                    Amount: status.Stacks,
                    SourceCombatantId: combatEvent.TargetCombatantId,
                    SourceCardId: status.SourceCardId,
                    Kind: DamageKind.Reflected));
        }
    }
}

public sealed record RemoveStatusEffectRequest(
    CombatantId TargetCombatantId,
    StatusDefinitionId StatusDefinitionId
) : IEffectRequest;

public sealed class RemoveStatusEffectHandler : EffectRequestHandler<RemoveStatusEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        RemoveStatusEffectRequest request)
    {
        registry.GetStatus(request.StatusDefinitionId);

        var target = combat.GetCombatant(request.TargetCombatantId);
        var statusesToRemove = target.Statuses
            .Where(status => status.DefinitionId == request.StatusDefinitionId)
            .ToArray();

        if (statusesToRemove.Length == 0)
            return;

        foreach (var status in statusesToRemove)
            target.Statuses.Remove(status);

        var removedStatusIds = statusesToRemove
            .Select(status => status.Id)
            .ToArray();

        combat.AddLogEntry(
            StandardCombatLogTypes.StatusRemoved,
            $"Removed status '{request.StatusDefinitionId}' from '{request.TargetCombatantId}'.");

        combat.EnqueueEvent(new StatusRemovedCombatEvent(
            request.TargetCombatantId,
            removedStatusIds,
            request.StatusDefinitionId));
    }
}

public sealed record DecreaseStatusChargesEffectRequest(
    CombatantId TargetCombatantId,
    StatusInstanceId StatusInstanceId,
    int Amount = 1
) : IEffectRequest;

public sealed class DecreaseStatusChargesEffectHandler
    : EffectRequestHandler<DecreaseStatusChargesEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DecreaseStatusChargesEffectRequest decreaseCharges)
    {
        if (decreaseCharges.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(decreaseCharges.Amount), "Charge decrease amount must be greater than zero.");

        var target = combat.GetCombatant(decreaseCharges.TargetCombatantId);
        var status = target.Statuses.FirstOrDefault(
            existing => existing.Id == decreaseCharges.StatusInstanceId);

        if (status is null)
            return;

        if (status.Charges <= 0)
            return;

        var oldCharges = status.Charges;
        var newCharges = Math.Max(0, oldCharges - decreaseCharges.Amount);

        status.SetCharges(newCharges);

        combat.AddLogEntry(
            StandardCombatLogTypes.StatusChargesReduced,
            $"Reduced status '{status.DefinitionId}' charges on '{target.Id}' from {oldCharges} to {newCharges}.");

        combat.EnqueueEvent(new StatusChargesReducedCombatEvent(
            TargetCombatantId: target.Id,
            StatusInstanceId: status.Id,
            StatusDefinitionId: status.DefinitionId,
            OldCharges: oldCharges,
            NewCharges: newCharges));

        if (newCharges > 0)
            return;

        target.Statuses.Remove(status);

        combat.AddLogEntry(
            StandardCombatLogTypes.StatusExpired,
            $"Status '{status.DefinitionId}' expired on '{target.Id}'.");

        combat.EnqueueEvent(new StatusExpiredCombatEvent(
            TargetCombatantId: target.Id,
            StatusInstanceId: status.Id,
            StatusDefinitionId: status.DefinitionId));
    }
}
