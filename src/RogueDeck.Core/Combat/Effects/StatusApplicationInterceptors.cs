namespace RogueDeck.Core.Combat;

public sealed record StatusApplicationInterceptionContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CombatantState TargetCombatant,
    StatusDefinition StatusDefinition,
    ApplyStatusEffectRequest Request);

public interface IStatusApplicationInterceptor
{
    int Priority { get; }

    bool TryIntercept(StatusApplicationInterceptionContext context);
}

public sealed class ArtifactStatusApplicationInterceptor : IStatusApplicationInterceptor
{
    public int Priority => 100;

    public bool TryIntercept(StatusApplicationInterceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.StatusDefinition.Polarity != StatusPolarity.Debuff)
            return false;

        var artifactStatus = context.TargetCombatant.Statuses.FirstOrDefault(status =>
            status.DefinitionId == StandardCombatIds.ArtifactStatus &&
            status.Charges > 0);

        if (artifactStatus is null)
            return false;

        context.Combat.AddLogEntry(
            StandardCombatLogTypes.StatusApplicationBlocked,
            $"Status '{context.Request.StatusDefinitionId}' was blocked on '{context.TargetCombatant.Id}' by '{artifactStatus.DefinitionId}'.");

        context.Combat.EnqueueEvent(new StatusApplicationBlockedCombatEvent(
            TargetCombatantId: context.TargetCombatant.Id,
            BlockedStatusDefinitionId: context.Request.StatusDefinitionId,
            BlockingStatusInstanceId: artifactStatus.Id,
            BlockingStatusDefinitionId: artifactStatus.DefinitionId));

        context.Combat.EnqueueEffect(new DecreaseStatusChargesEffectRequest(
            TargetCombatantId: context.TargetCombatant.Id,
            StatusInstanceId: artifactStatus.Id));

        return true;
    }
}
