namespace RogueDeck.Core.Combat;

public sealed record DamageAmountModificationContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant,
    CardDefinitionId? SourceCardId,
    DamageKind Kind,
    int RequestedAmount);

public interface IDamageAmountModifier
{
    int Priority { get; }

    int ModifyDamageAmount(
        DamageAmountModificationContext context,
        int currentAmount);
}

public sealed class StrengthDamageAmountModifier : IDamageAmountModifier
{
    public int Priority => 100;

    public int ModifyDamageAmount(
        DamageAmountModificationContext context,
        int currentAmount)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (currentAmount <= 0)
            return currentAmount;

        if (context.Kind != DamageKind.Direct)
            return currentAmount;

        if (context.SourceCombatant is null)
            return currentAmount;

        var strengthStacks = context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StandardCombatIds.StrengthStatus)
            .Sum(status => status.Stacks);

        if (strengthStacks <= 0)
            return currentAmount;

        return currentAmount + strengthStacks;
    }
}

public sealed class WeakDamageAmountModifier : IDamageAmountModifier
{
    public int Priority => 200;

    public int ModifyDamageAmount(
        DamageAmountModificationContext context,
        int currentAmount)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (currentAmount <= 0)
            return currentAmount;

        if (context.Kind != DamageKind.Direct)
            return currentAmount;

        if (context.SourceCombatant is null)
            return currentAmount;

        var hasWeak = context.SourceCombatant.Statuses
            .Any(status => status.DefinitionId == StandardCombatIds.WeakStatus);

        if (!hasWeak)
            return currentAmount;

        return currentAmount * 75 / 100;
    }
}
