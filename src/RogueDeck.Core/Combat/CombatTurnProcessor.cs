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
        EnsureActiveCombatantCanStartTurn(combat);

        combat.MarkTurnStarted();

        combat.AddLogEntry(
            StandardCombatLogTypes.TurnStarted,
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
        EnsureActiveCombatantExistsInTurnOrder(combat);

        combat.MarkTurnEnded();

        combat.AddLogEntry(
            StandardCombatLogTypes.TurnEnded,
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

        if (combat.Result != CombatResult.Ongoing)
            return;

        AdvanceToNextCombatant(combat);

        _queueProcessor.ResolvePendingQueues(
            combat,
            registry,
            maxEffectsToResolve);
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

    private static void EnsureActiveCombatantCanStartTurn(CombatState combat)
    {
        EnsureActiveCombatantExistsInTurnOrder(combat);

        var activeCombatantId = combat.ActiveCombatantId!.Value;
        var activeCombatant = combat.GetCombatant(activeCombatantId);

        if (!activeCombatant.IsAlive)
            throw new InvalidOperationException(
                $"Cannot start turn because active combatant '{activeCombatantId}' is not alive.");
    }

    private static void EnsureActiveCombatantExistsInTurnOrder(CombatState combat)
    {
        if (combat.TurnOrder.Count == 0)
            throw new InvalidOperationException("Cannot use turn processor because the combat has no turn order.");

        if (combat.ActiveCombatantId is null)
        {
            foreach (var combatantId in combat.TurnOrder)
            {
                if (!combat.TryGetCombatant(combatantId, out var combatant))
                    continue;

                if (!combatant!.IsAlive)
                    continue;

                combat.SetActiveCombatant(combatantId);
                return;
            }

            throw new InvalidOperationException("Cannot use turn processor because there are no living combatants in the turn order.");
        }

        if (!combat.TurnOrder.Contains(combat.ActiveCombatantId.Value))
            throw new InvalidOperationException(
                $"Active combatant '{combat.ActiveCombatantId}' is not part of the turn order.");

        if (!combat.TryGetCombatant(combat.ActiveCombatantId.Value, out _))
            throw new InvalidOperationException(
                $"Active combatant '{combat.ActiveCombatantId}' does not exist.");
    }

    private static void AdvanceToNextCombatant(CombatState combat)
    {
        EnsureActiveCombatantExistsInTurnOrder(combat);

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
                combat.EnqueueEvent(
                    new RoundEndedCombatEvent(
                    combat.CurrentRound,
                    combat.ActiveCombatantId));

                combat.AdvanceRound();

                combat.EnqueueEvent(
                    new RoundStartedCombatEvent(combat.CurrentRound));
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
