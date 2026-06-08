namespace RogueDeck.Core.Combat;

public sealed class DrawCardsOnTurnStartedHandler : CombatEventHandler<TurnStartedCombatEvent>
{
    private readonly int _cardsToDraw;

    public DrawCardsOnTurnStartedHandler(int cardsToDraw = 5)
    {
        if (cardsToDraw < 0)
            throw new ArgumentOutOfRangeException(nameof(cardsToDraw), "Cards to draw cannot be negative.");

        _cardsToDraw = cardsToDraw;
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnStartedCombatEvent combatEvent)
    {
        if (_cardsToDraw == 0)
            return;

        combat.EnqueueEffect(new DrawCardsEffectRequest(
            combatEvent.CombatantId,
            _cardsToDraw));
    }
}

public sealed class DiscardHandOnTurnEndedHandler : CombatEventHandler<TurnEndedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnEndedCombatEvent combatEvent)
    {
        combat.EnqueueEffect(new DiscardHandEffectRequest(
            combatEvent.CombatantId));
    }
}
