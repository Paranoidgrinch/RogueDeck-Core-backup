namespace RogueDeck.Core.Combat;

public sealed class CombatTurnProcessor
{
    private readonly CombatQueueProcessor _queueProcessor = new();

    public void StartCurrentTurn(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int maxEffectsToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);

        EnsureActiveCombatant(combat);

        combat.MarkTurnStarted();

        combat.AddLogEntry(
            "TurnStarted",
            $"Started turn for '{combat.ActiveCombatantId}'.");

        combat.EnqueueEvent(
            new TurnStartedCombatEvent(
                combat.ActiveCombatantId!.Value,
                combat.CurrentRound,
                combat.CurrentTurn));

        _queueProcessor.ResolvePendingQueues(
            combat,
            registry,
            maxEffectsToResolve);
    }

    public void EndCurrentTurn(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int maxEffectsToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);

        EnsureActiveCombatant(combat);

        combat.MarkTurnEnded();

        combat.AddLogEntry(
            "TurnEnded",
            $"Ended turn for '{combat.ActiveCombatantId}'.");

        combat.EnqueueEvent(
            new TurnEndedCombatEvent(
                combat.ActiveCombatantId!.Value,
                combat.CurrentRound,
                combat.CurrentTurn));

        _queueProcessor.ResolvePendingQueues(
            combat,
            registry,
            maxEffectsToResolve);

        AdvanceToNextCombatant(combat);
    }

    public void EndCurrentTurnAndStartNextTurn(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int maxEffectsToResolve = 1024)
    {
        EndCurrentTurn(
            combat,
            registry,
            maxEffectsToResolve);

        StartCurrentTurn(
            combat,
            registry,
            maxEffectsToResolve);
    }

    private static void EnsureActiveCombatant(CombatState combat)
    {
        if (combat.TurnOrder.Count == 0)
            throw new InvalidOperationException("Cannot use turn processor because the combat has no turn order.");

        if (combat.ActiveCombatantId is null)
        {
            combat.SetActiveCombatant(combat.TurnOrder[0]);
            return;
        }

        if (!combat.TurnOrder.Contains(combat.ActiveCombatantId.Value))
            throw new InvalidOperationException(
                $"Active combatant '{combat.ActiveCombatantId}' is not part of the turn order.");
    }

    private static void AdvanceToNextCombatant(CombatState combat)
    {
        EnsureActiveCombatant(combat);

        var currentIndex = combat.TurnOrder.IndexOf(combat.ActiveCombatantId!.Value);
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
