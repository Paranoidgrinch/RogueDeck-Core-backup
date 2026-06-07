namespace RogueDeck.Core.Combat;

public sealed class ApplyStatusEffectHandler : IEffectRequestHandler
{
    public Type RequestType => typeof(ApplyStatusEffectRequest);

    public void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        IEffectRequest request)
    {
        var applyStatus = request as ApplyStatusEffectRequest
            ?? throw new ArgumentException(
                $"Expected request type '{nameof(ApplyStatusEffectRequest)}'.",
                nameof(request));

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