namespace RogueDeck.Core.Combat;

public sealed record DrawCardsEffectRequest(
    CombatantId CombatantId,
    int Count
) : IEffectRequest;

public sealed class DrawCardsEffectHandler : EffectRequestHandler<DrawCardsEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DrawCardsEffectRequest request)
    {
        if (request.Count < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Count), "Draw count cannot be negative.");

        if (request.Count == 0)
            return;

        var zones = combat.GetCardZones(request.CombatantId);
        var drawnCards = new List<CardInstance>();

        while (drawnCards.Count < request.Count)
        {
            var remainingCardsToDraw = request.Count - drawnCards.Count;
            var newlyDrawnCards = zones.DrawCards(remainingCardsToDraw);

            drawnCards.AddRange(newlyDrawnCards);

            if (drawnCards.Count >= request.Count)
                break;

            if (zones.DiscardPile.Count == 0)
                break;

            var shuffleOrder = CombatRandom.CreateShuffledIndexes(
                zones.DiscardPile.Count,
                combat.RandomSeed,
                combat.RandomStep);

            var shuffledCards = zones.MoveDiscardPileToDrawPile(shuffleOrder);

            combat.AdvanceRandomStep();

            var shuffledCardIds = shuffledCards
                .Select(card => card.Id)
                .ToArray();

            combat.AddLogEntry(
                StandardCombatLogTypes.DiscardPileShuffledIntoDrawPile,
                $"Combatant '{request.CombatantId}' shuffled {shuffledCardIds.Length} card(s) from discard pile into draw pile.");

            combat.EnqueueEvent(new DiscardPileShuffledIntoDrawPileCombatEvent(
                request.CombatantId,
                shuffledCardIds));
        }

        if (drawnCards.Count == 0)
            return;

        var drawnCardIds = drawnCards
            .Select(card => card.Id)
            .ToArray();

        combat.AddLogEntry(
            StandardCombatLogTypes.CardsDrawn,
            $"Combatant '{request.CombatantId}' drew {drawnCardIds.Length} card(s).");

        combat.EnqueueEvent(new CardsDrawnCombatEvent(
            request.CombatantId,
            drawnCardIds));
    }
}

public sealed record DiscardHandEffectRequest(
    CombatantId CombatantId
) : IEffectRequest;

public sealed class DiscardHandEffectHandler : EffectRequestHandler<DiscardHandEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DiscardHandEffectRequest request)
    {
        var zones = combat.GetCardZones(request.CombatantId);
        var discardedCardIds = zones.Hand
            .Select(card => card.Id)
            .ToArray();

        if (discardedCardIds.Length == 0)
            return;

        zones.DiscardHand();

        combat.AddLogEntry(
            StandardCombatLogTypes.HandDiscarded,
            $"Combatant '{request.CombatantId}' discarded {discardedCardIds.Length} card(s).");

        combat.EnqueueEvent(new HandDiscardedCombatEvent(
            request.CombatantId,
            discardedCardIds));
    }
}

public sealed record MoveCardToZoneEffectRequest(
    CombatantId CombatantId,
    CardInstanceId CardInstanceId,
    CardZone ToZone
) : IEffectRequest;

public sealed class MoveCardToZoneEffectHandler : EffectRequestHandler<MoveCardToZoneEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        MoveCardToZoneEffectRequest request)
    {
        var zones = combat.GetCardZones(request.CombatantId);
        var card = zones.GetCard(request.CardInstanceId);

        if (card.OwnerId != request.CombatantId)
        {
            throw new InvalidOperationException(
                $"Card instance '{request.CardInstanceId}' is not owned by combatant '{request.CombatantId}'.");
        }

        var fromZone = card.Zone;

        if (fromZone == request.ToZone)
            return;

        zones.MoveCardToZone(request.CardInstanceId, request.ToZone);

        combat.AddLogEntry(
            StandardCombatLogTypes.CardMovedToZone,
            $"Moved card instance '{request.CardInstanceId}' for combatant '{request.CombatantId}' from '{fromZone}' to '{request.ToZone}'.");

        combat.EnqueueEvent(new CardMovedToZoneCombatEvent(
            request.CombatantId,
            request.CardInstanceId,
            fromZone,
            request.ToZone));
    }
}
