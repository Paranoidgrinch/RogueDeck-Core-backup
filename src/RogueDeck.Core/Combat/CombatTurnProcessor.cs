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

        EnsureCombatIsOngoing(combat);
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

        EnsureCombatIsOngoing(combat);
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

    private static void EnsureCombatIsOngoing(CombatState combat)
    {
        if (combat.Result != CombatResult.Ongoing)
            throw new InvalidOperationException(
                $"Cannot process turns because combat result is '{combat.Result}'.");
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

        for (var offset = 1; offset <= combat.TurnOrder.Count; offset++)
        {
            var nextIndex = (currentIndex + offset) % combat.TurnOrder.Count;
            var nextCombatantId = combat.TurnOrder[nextIndex];

            if (!combat.TryGetCombatant(nextCombatantId, out var nextCombatant))
                continue;

            if (!nextCombatant!.IsAlive)
                continue;

            if (nextIndex <= currentIndex)
            {
                combat.AdvanceRound();
            }
            else
            {
                combat.AdvanceTurn();
            }

            combat.SetActiveCombatant(nextCombatantId);
            return;
        }

        throw new InvalidOperationException("Cannot advance turn because there are no living combatants in the turn order.");
    }
}


