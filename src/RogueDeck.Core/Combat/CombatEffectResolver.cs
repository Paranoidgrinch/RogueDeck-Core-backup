namespace RogueDeck.Core.Combat;

public sealed class CombatEffectResolver
{
    public void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        IEffectRequest request)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(request);

        switch (request)
        {
            case ApplyStatusEffectRequest applyStatus:
                ResolveApplyStatus(combat, registry, applyStatus);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported effect request type '{request.GetType().Name}'.");
        }
    }

    private void ResolveApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ApplyStatusEffectRequest request)
    {
        if (request.Stacks < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Stacks), "Stacks cannot be negative.");

        if (request.DurationTurns < 0)
            throw new ArgumentOutOfRangeException(nameof(request.DurationTurns), "Duration cannot be negative.");

        if (request.Charges < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Charges), "Charges cannot be negative.");

        var target = combat.GetCombatant(request.TargetCombatantId);
        var definition = registry.GetStatus(request.StatusDefinitionId);

        if (definition.StackingBehavior == StatusStackingBehavior.MergeWithExistingInstance)
        {
            var existingStatus = target.Statuses.FirstOrDefault(
                status => status.DefinitionId == request.StatusDefinitionId);

            if (existingStatus is not null)
            {
                existingStatus.SetStacks(existingStatus.Stacks + request.Stacks);
                existingStatus.SetDurationTurns(Math.Max(existingStatus.DurationTurns, request.DurationTurns));
                existingStatus.SetCharges(existingStatus.Charges + request.Charges);

                combat.AddLogEntry(
                    "StatusMerged",
                    $"Merged status '{request.StatusDefinitionId}' on '{request.TargetCombatantId}'.");

                return;
            }
        }

        var newStatus = new StatusInstance(
            combat.CreateNextStatusInstanceId(),
            request.StatusDefinitionId,
            request.TargetCombatantId,
            request.SourceCombatantId,
            request.SourceCardId,
            request.Stacks,
            request.DurationTurns,
            request.Charges,
            combat.CurrentRound,
            combat.CurrentTurn,
            definition.DefaultVisibility,
            definition.Polarity);

        foreach (var tag in definition.Tags)
            newStatus.Tags.Add(tag);

        target.Statuses.Add(newStatus);

        combat.AddLogEntry(
            "StatusApplied",
            $"Applied status '{request.StatusDefinitionId}' to '{request.TargetCombatantId}'.");
    }
}