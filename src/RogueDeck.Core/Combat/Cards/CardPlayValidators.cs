namespace RogueDeck.Core.Combat;

public sealed record CardPlayValidationContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardDefinition Card,
    CombatantState Source,
    CombatantId? RequestedTargetId,
    CardInstanceId? CardInstanceId);

public interface ICardPlayValidator
{
    int Priority { get; }

    void Validate(CardPlayValidationContext context);
}

public sealed class StunCardPlayValidator : ICardPlayValidator
{
    public int Priority => 100;

    public void Validate(CardPlayValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var isStunned = context.Source.Statuses
            .Any(status => status.DefinitionId == StandardCombatIds.StunStatus);

        if (!isStunned)
            return;

        throw new InvalidOperationException(
            $"Combatant '{context.Source.Id}' cannot play cards while stunned.");
    }
}
