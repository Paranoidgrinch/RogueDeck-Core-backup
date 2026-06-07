namespace RogueDeck.Core.Combat;

public sealed class CombatState
{
    private readonly Queue<IEffectRequest> _pendingEffects = new();
    private readonly Queue<ICombatEvent> _pendingEvents = new();

    public CombatId Id { get; }

    public int CurrentRound { get; private set; }

    public int CurrentTurn { get; private set; }

    public CombatTurnPhase TurnPhase { get; private set; }

    public CombatantId? ActiveCombatantId { get; private set; }

    public CombatResult Result { get; private set; }

    public int RandomSeed { get; }

    public int RandomStep { get; private set; }

    public int NextStatusInstanceNumber { get; private set; } = 1;

    public List<CombatantState> Combatants { get; } = new();

    public List<CombatantId> TurnOrder { get; } = new();

    public List<StatusInstance> GlobalStatuses { get; } = new();

    public IReadOnlyCollection<IEffectRequest> PendingEffects => _pendingEffects;

    public IReadOnlyCollection<ICombatEvent> PendingEvents => _pendingEvents;

    public List<CombatLogEntry> CombatLog { get; } = new();

    public bool HasPendingEffects => _pendingEffects.Count > 0;

    public int PendingEffectCount => _pendingEffects.Count;

    public bool HasPendingEvents => _pendingEvents.Count > 0;

    public int PendingEventCount => _pendingEvents.Count;

    public CombatState(CombatId id, int randomSeed)
    {
        Id = id;
        RandomSeed = randomSeed;
        CurrentRound = 1;
        CurrentTurn = 1;
        TurnPhase = CombatTurnPhase.WaitingToStartTurn;
        Result = CombatResult.Ongoing;
    }

    public void AddCombatant(CombatantState combatant)
    {
        if (Combatants.Any(existing => existing.Id == combatant.Id))
            throw new InvalidOperationException($"Combatant with id '{combatant.Id}' already exists.");

        Combatants.Add(combatant);
        TurnOrder.Add(combatant.Id);

        if (ActiveCombatantId is null)
            ActiveCombatantId = combatant.Id;
    }

    public CombatantState GetCombatant(CombatantId id)
    {
        return Combatants.First(combatant => combatant.Id == id);
    }

    public bool TryGetCombatant(CombatantId id, out CombatantState? combatant)
    {
        combatant = Combatants.FirstOrDefault(existing => existing.Id == id);

        return combatant is not null;
    }

    public void SetActiveCombatant(CombatantId combatantId)
    {
        if (!Combatants.Any(combatant => combatant.Id == combatantId))
            throw new InvalidOperationException($"Combatant with id '{combatantId}' does not exist.");

        ActiveCombatantId = combatantId;
    }

    public void MarkTurnStarted()
    {
        if (TurnPhase != CombatTurnPhase.WaitingToStartTurn)
            throw new InvalidOperationException("Cannot start the current turn because a turn is already in progress.");

        TurnPhase = CombatTurnPhase.TurnInProgress;
    }

    public void MarkTurnEnded()
    {
        if (TurnPhase != CombatTurnPhase.TurnInProgress)
            throw new InvalidOperationException("Cannot end the current turn because no turn is currently in progress.");

        TurnPhase = CombatTurnPhase.WaitingToStartTurn;
    }

    public void SetResult(CombatResult result)
    {
        Result = result;
    }

    public void EnqueueEffect(IEffectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _pendingEffects.Enqueue(request);
    }

    public IEffectRequest DequeueNextEffect()
    {
        if (_pendingEffects.Count == 0)
            throw new InvalidOperationException("Cannot dequeue an effect because there are no pending effects.");

        return _pendingEffects.Dequeue();
    }

    public void EnqueueEvent(ICombatEvent combatEvent)
    {
        ArgumentNullException.ThrowIfNull(combatEvent);

        _pendingEvents.Enqueue(combatEvent);
    }

    public ICombatEvent DequeueNextEvent()
    {
        if (_pendingEvents.Count == 0)
            throw new InvalidOperationException("Cannot dequeue an event because there are no pending events.");

        return _pendingEvents.Dequeue();
    }

    public void AdvanceTurn()
    {
        CurrentTurn++;
    }

    public void AdvanceRound()
    {
        CurrentRound++;
        CurrentTurn = 1;
    }

    public void AdvanceRandomStep()
    {
        RandomStep++;
    }

    public StatusInstanceId CreateNextStatusInstanceId()
    {
        var id = new StatusInstanceId($"status_{NextStatusInstanceNumber:000000}");

        NextStatusInstanceNumber++;

        return id;
    }

    public void AddLogEntry(string type, string message)
    {
        CombatLog.Add(new CombatLogEntry(CurrentRound, CurrentTurn, type, message));
    }
}
