namespace RogueDeck.Core.Combat;

public sealed class CombatTurnProcessor
{
    private readonly CombatEffectQueueProcessor _effectQueueProcessor = new();

    public void EndCurrentTurn(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int maxEffectsToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);

        _effectQueueProcessor.ResolvePendingEffects(
            combat,
            registry,
            maxEffectsToResolve);

        AdvanceToNextCombatant(combat);
    }

    private static void AdvanceToNextCombatant(CombatState combat)
    {
        if (combat.TurnOrder.Count == 0)
            throw new InvalidOperationException("Cannot advance turn because the combat has no turn order.");

        if (combat.ActiveCombatantId is null)
        {
            combat.SetActiveCombatant(combat.TurnOrder[0]);
            return;
        }

        var currentIndex = combat.TurnOrder.IndexOf(combat.ActiveCombatantId.Value);

        if (currentIndex < 0)
            throw new InvalidOperationException(
                $"Active combatant '{combat.ActiveCombatantId}' is not part of the turn order.");

        var nextIndex = currentIndex + 1;

        if (nextIndex >= combat.TurnOrder.Count)
        {
            combat.AdvanceRound();
            combat.SetActiveCombatant(combat.TurnOrder[0]);
            return;
        }

        combat.AdvanceTurn();
        combat.SetActiveCombatant(combat.TurnOrder[nextIndex]);
    }
}
