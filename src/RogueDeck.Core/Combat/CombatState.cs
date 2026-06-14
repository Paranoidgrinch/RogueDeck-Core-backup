namespace RogueDeck.Core.Combat;

public sealed class CombatState
{
    private readonly Queue<IEffectRequest> _pendingEffects = new();
    private readonly Queue<CombatEffectChainContext> _pendingEffectChains = new();
    private readonly Queue<ICombatEvent> _pendingEvents = new();
    private readonly Queue<CombatEffectChainContext> _pendingEventChains = new();
    private long _nextEffectChainNumber = 1;

    public CombatId Id { get; }

    public int CurrentRound { get; private set; }

    public int CurrentTurn { get; private set; }

    public CombatTurnPhase TurnPhase { get; private set; }

    public CombatantId? ActiveCombatantId { get; private set; }

    public CombatResult Result { get; private set; }

    public int RandomSeed { get; }

    public int MaximumTriggerDepth { get; }

    public int RandomStep { get; private set; }

    public int NextStatusInstanceNumber { get; private set; } = 1;

    public int NextCardInstanceNumber { get; private set; } = 1;

    public List<CombatantState> Combatants { get; } = new();

    public List<CombatantId> TurnOrder { get; } = new();

    public List<StatusInstance> GlobalStatuses { get; } = new();

    public Dictionary<CombatantId, CombatantCardZones> CardZonesByCombatant { get; } = new();
    public Dictionary<CombatantId, CombatantCardPlayTurnStats> CardPlayTurnStatsByCombatant { get; } = new();

    public IReadOnlyCollection<IEffectRequest> PendingEffects => _pendingEffects;

    public IReadOnlyCollection<ICombatEvent> PendingEvents => _pendingEvents;

    public CombatEffectChainContext? CurrentEffectChain { get; private set; }

    public List<CombatLogEntry> CombatLog { get; } = new();

    public bool HasPendingEffects => _pendingEffects.Count > 0;

    public int PendingEffectCount => _pendingEffects.Count;

    public bool HasPendingEvents => _pendingEvents.Count > 0;

    public int PendingEventCount => _pendingEvents.Count;

    public CombatState(
        CombatId id,
        int randomSeed,
        int maximumTriggerDepth =
            CombatEffectChainContext.DefaultMaximumTriggerDepth)
    {
        if (maximumTriggerDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumTriggerDepth),
                "Maximum trigger depth must be greater than zero.");
        }

        Id = id;
        RandomSeed = randomSeed;
        MaximumTriggerDepth = maximumTriggerDepth;
        CurrentRound = 1;
        CurrentTurn = 1;
        TurnPhase = CombatTurnPhase.WaitingToStartTurn;
        Result = CombatResult.Ongoing;
    }

    public void AddCombatant(CombatantState combatant)
    {
        ArgumentNullException.ThrowIfNull(combatant);

        if (Combatants.Any(existing => existing.Id == combatant.Id))
            throw new InvalidOperationException($"Combatant with id '{combatant.Id}' already exists.");

        Combatants.Add(combatant);
        TurnOrder.Add(combatant.Id);
        CardZonesByCombatant.Add(combatant.Id, new CombatantCardZones());
        CardPlayTurnStatsByCombatant.Add(combatant.Id, new CombatantCardPlayTurnStats());

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

    public CombatantCardZones GetCardZones(CombatantId combatantId)
    {
        if (!TryGetCombatant(combatantId, out _))
            throw new InvalidOperationException($"Combatant with id '{combatantId}' does not exist.");

        return CardZonesByCombatant[combatantId];
    }    public CombatantCardPlayTurnStats GetCardPlayTurnStats(CombatantId combatantId)
    {
        if (!TryGetCombatant(combatantId, out _))
            throw new InvalidOperationException($"Combatant with id '{combatantId}' does not exist.");

        if (!CardPlayTurnStatsByCombatant.TryGetValue(combatantId, out var stats))
        {
            stats = new CombatantCardPlayTurnStats();
            CardPlayTurnStatsByCombatant.Add(combatantId, stats);
        }

        return stats;
    }


    public IReadOnlyCollection<CombatantState> GetLivingCombatantsOnTeam(TeamId teamId)
    {
        return Combatants
            .Where(combatant => combatant.TeamId == teamId && combatant.IsAlive)
            .ToArray();
    }

    public bool HasLivingCombatantsOnTeam(TeamId teamId)
    {
        return Combatants.Any(combatant => combatant.TeamId == teamId && combatant.IsAlive);
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

        EnqueueEffect(
            request,
            CurrentEffectChain ?? CreateRootEffectChain());
    }

    internal void EnqueueEffect(
        IEffectRequest request,
        CombatEffectChainContext effectChain)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(effectChain);

        _pendingEffects.Enqueue(request);
        _pendingEffectChains.Enqueue(effectChain);
    }

    public IEffectRequest DequeueNextEffect()
    {
        return DequeueNextEffectEntry().Request;
    }

    internal PendingEffectQueueEntry DequeueNextEffectEntry()
    {
        EnsureEffectQueueMetadataAligned();

        if (_pendingEffects.Count == 0)
            throw new InvalidOperationException("Cannot dequeue an effect because there are no pending effects.");

        return new PendingEffectQueueEntry(
            _pendingEffects.Dequeue(),
            _pendingEffectChains.Dequeue());
    }

    public void EnqueueEvent(ICombatEvent combatEvent)
    {
        ArgumentNullException.ThrowIfNull(combatEvent);

        EnqueueEvent(
            combatEvent,
            CurrentEffectChain ?? CreateRootEffectChain());
    }

    internal void EnqueueEvent(
        ICombatEvent combatEvent,
        CombatEffectChainContext effectChain)
    {
        ArgumentNullException.ThrowIfNull(combatEvent);
        ArgumentNullException.ThrowIfNull(effectChain);

        _pendingEvents.Enqueue(combatEvent);
        _pendingEventChains.Enqueue(effectChain);
    }

    public ICombatEvent DequeueNextEvent()
    {
        return DequeueNextEventEntry().CombatEvent;
    }

    internal PendingEventQueueEntry DequeueNextEventEntry()
    {
        EnsureEventQueueMetadataAligned();

        if (_pendingEvents.Count == 0)
            throw new InvalidOperationException("Cannot dequeue an event because there are no pending events.");

        return new PendingEventQueueEntry(
            _pendingEvents.Dequeue(),
            _pendingEventChains.Dequeue());
    }

    internal CombatEffectChainContext CreateTriggeredEffectChain(
        TriggeredEffectDefinitionId definitionId)
    {
        var parentChain = CurrentEffectChain ?? CreateRootEffectChain();

        return parentChain.AppendTriggeredEffectDefinition(definitionId);
    }

    internal IDisposable EnterEffectChain(
        CombatEffectChainContext effectChain)
    {
        ArgumentNullException.ThrowIfNull(effectChain);

        var previousEffectChain = CurrentEffectChain;
        CurrentEffectChain = effectChain;

        return new EffectChainScope(
            this,
            previousEffectChain);
    }

    private CombatEffectChainContext CreateRootEffectChain()
    {
        var effectChain = new CombatEffectChainContext(
            new CombatEffectChainId(_nextEffectChainNumber),
            Array.Empty<TriggeredEffectDefinitionId>(),
            MaximumTriggerDepth);

        _nextEffectChainNumber++;

        return effectChain;
    }

    private void EnsureEffectQueueMetadataAligned()
    {
        if (_pendingEffects.Count != _pendingEffectChains.Count)
            throw new InvalidOperationException(
                "Pending effect requests and their effect-chain metadata are out of sync.");
    }

    private void EnsureEventQueueMetadataAligned()
    {
        if (_pendingEvents.Count != _pendingEventChains.Count)
            throw new InvalidOperationException(
                "Pending combat events and their effect-chain metadata are out of sync.");
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

    public CardInstanceId CreateNextCardInstanceId()
    {
        var id = new CardInstanceId($"card_{NextCardInstanceNumber:000000}");

        NextCardInstanceNumber++;

        return id;
    }

    public void AddLogEntry(string type, string message)
    {
        CombatLog.Add(new CombatLogEntry(CurrentRound, CurrentTurn, type, message));
    }

    private sealed class EffectChainScope : IDisposable
    {
        private CombatState? _combat;
        private readonly CombatEffectChainContext? _previousEffectChain;

        public EffectChainScope(
            CombatState combat,
            CombatEffectChainContext? previousEffectChain)
        {
            _combat = combat;
            _previousEffectChain = previousEffectChain;
        }

        public void Dispose()
        {
            if (_combat is null)
                return;

            _combat.CurrentEffectChain = _previousEffectChain;
            _combat = null;
        }
    }
}

