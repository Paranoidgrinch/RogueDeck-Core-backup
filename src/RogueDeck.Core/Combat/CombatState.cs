namespace RogueDeck.Core.Combat;

public sealed class CombatState
{
    public CombatId Id { get; }

    public int CurrentRound { get; private set; }
    public int CurrentTurn { get; private set; }

    public CombatantId? ActiveCombatantId { get; private set; }
    public CombatResult Result { get; private set; }

    public int RandomSeed { get; }
    public int RandomStep { get; private set; }

    public List<CombatantState> Combatants { get; } = new();
    public List<CombatantId> TurnOrder { get; } = new();

    public List<StatusInstance> GlobalStatuses { get; } = new();

    public Queue<IEffectRequest> PendingEffects { get; } = new();
    public Queue<ICombatEvent> PendingEvents { get; } = new();

    public List<CombatLogEntry> CombatLog { get; } = new();

    public CombatState(CombatId id, int randomSeed)
    {
        Id = id;
        RandomSeed = randomSeed;
        CurrentRound = 1;
        CurrentTurn = 1;
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

    public void SetResult(CombatResult result)
    {
        Result = result;
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

    public void AddLogEntry(string type, string message)
    {
        CombatLog.Add(new CombatLogEntry(CurrentRound, CurrentTurn, type, message));
    }
}