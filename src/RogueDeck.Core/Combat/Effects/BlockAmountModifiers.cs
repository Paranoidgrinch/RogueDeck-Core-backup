namespace RogueDeck.Core.Combat;

public sealed record BlockAmountModificationContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant,
    CardDefinitionId? SourceCardId,
    int RequestedAmount);

public interface IBlockAmountModifier
{
    int Priority { get; }

    int ModifyBlockAmount(
        BlockAmountModificationContext context,
        int currentAmount);
}

public sealed class DexterityBlockAmountModifier : IBlockAmountModifier
{
    public int Priority => 100;

    public int ModifyBlockAmount(
        BlockAmountModificationContext context,
        int currentAmount)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (currentAmount <= 0)
            return currentAmount;

        var dexterityStacks = context.TargetCombatant.Statuses
            .Where(status => status.DefinitionId == StandardCombatIds.DexterityStatus)
            .Sum(status => status.Stacks);

        if (dexterityStacks <= 0)
            return currentAmount;

        return currentAmount + dexterityStacks;
    }
}

public sealed class FrailBlockAmountModifier : IBlockAmountModifier
{
    public int Priority => 200;

    public int ModifyBlockAmount(
        BlockAmountModificationContext context,
        int currentAmount)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (currentAmount <= 0)
            return currentAmount;

        var hasFrail = context.TargetCombatant.Statuses
            .Any(status => status.DefinitionId == StandardCombatIds.FrailStatus);

        if (!hasFrail)
            return currentAmount;

        return currentAmount * 75 / 100;
    }
}
