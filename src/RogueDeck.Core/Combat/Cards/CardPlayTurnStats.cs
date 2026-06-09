namespace RogueDeck.Core.Combat;

public sealed class CombatantCardPlayTurnStats
{
    private readonly Dictionary<CardDefinitionId, int> _cardsPlayedByDefinitionThisTurn = new();
    private readonly Dictionary<TagId, int> _cardsPlayedByTagThisTurn = new();

    public int CardsPlayedThisTurn { get; private set; }

    public IReadOnlyDictionary<CardDefinitionId, int> CardsPlayedByDefinitionThisTurn =>
        _cardsPlayedByDefinitionThisTurn;

    public IReadOnlyDictionary<TagId, int> CardsPlayedByTagThisTurn =>
        _cardsPlayedByTagThisTurn;

    public int GetCardsPlayedWithDefinitionThisTurn(CardDefinitionId cardDefinitionId)
    {
        return _cardsPlayedByDefinitionThisTurn.TryGetValue(cardDefinitionId, out var count)
            ? count
            : 0;
    }

    public int GetCardsPlayedWithTagThisTurn(TagId tagId)
    {
        return _cardsPlayedByTagThisTurn.TryGetValue(tagId, out var count)
            ? count
            : 0;
    }

    public void RecordCardPlayed(CardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);

        CardsPlayedThisTurn++;

        if (!_cardsPlayedByDefinitionThisTurn.TryAdd(card.Id, 1))
            _cardsPlayedByDefinitionThisTurn[card.Id]++;

        foreach (var tag in card.Tags)
        {
            if (!_cardsPlayedByTagThisTurn.TryAdd(tag, 1))
                _cardsPlayedByTagThisTurn[tag]++;
        }
    }

    public void Reset()
    {
        CardsPlayedThisTurn = 0;
        _cardsPlayedByDefinitionThisTurn.Clear();
        _cardsPlayedByTagThisTurn.Clear();
    }
}

public sealed class TrackCardsPlayedThisTurnHandler
    : CombatEventHandler<CardPlayedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardPlayedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.SourceCombatantId, out _))
            return;

        var card = registry.GetCard(combatEvent.CardDefinitionId);
        var stats = combat.GetCardPlayTurnStats(combatEvent.SourceCombatantId);

        stats.RecordCardPlayed(card);
    }
}

public sealed class ResetCardPlayTurnStatsOnTurnStartedHandler
    : CombatEventHandler<TurnStartedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnStartedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out _))
            return;

        combat.GetCardPlayTurnStats(combatEvent.CombatantId).Reset();
    }
}
