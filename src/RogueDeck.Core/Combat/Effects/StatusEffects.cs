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
                    "StatusMerged",
                    $"Merged status '{applyStatus.StatusDefinitionId}' on '{applyStatus.TargetCombatantId}'.");

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
            "StatusApplied",
            $"Applied status '{applyStatus.StatusDefinitionId}' to '{applyStatus.TargetCombatantId}'.");
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
            "StatusDurationReduced",
            $"Reduced status '{status.DefinitionId}' duration on '{target.Id}' from {oldDuration} to {newDuration}.");

        if (newDuration > 0)
            return;

        target.Statuses.Remove(status);

        combat.AddLogEntry(
            "StatusExpired",
            $"Status '{status.DefinitionId}' expired on '{target.Id}'.");
    }
}

public sealed class DecreaseTimedStatusDurationsOnTurnEndedHandler : CombatEventHandler<TurnEndedCombatEvent>
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
