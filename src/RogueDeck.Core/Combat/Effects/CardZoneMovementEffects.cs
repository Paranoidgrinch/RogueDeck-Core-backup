namespace RogueDeck.Core.Combat;

public sealed record MoveAllCardsFromZoneEffectRequest(
    CombatantId CombatantId,
    CardZone FromZone,
    CardZone ToZone
) : IEffectRequest;

public sealed class MoveAllCardsFromZoneEffectHandler
    : EffectRequestHandler<MoveAllCardsFromZoneEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        MoveAllCardsFromZoneEffectRequest request)
    {
        if (request.FromZone == request.ToZone)
            return;

        var zones = combat.GetCardZones(request.CombatantId);
        var movedCards = zones.MoveAllCardsFromZone(
            request.FromZone,
            request.ToZone);

        if (movedCards.Count == 0)
            return;

        var movedCardIds = movedCards
            .Select(card => card.Id)
            .ToArray();

        combat.AddLogEntry(
            StandardCombatLogTypes.CardsMovedBetweenZones,
            $"Moved {movedCardIds.Length} card(s) for combatant '{request.CombatantId}' from '{request.FromZone}' to '{request.ToZone}'.");

        combat.EnqueueEvent(new CardsMovedBetweenZonesCombatEvent(
            request.CombatantId,
            movedCardIds,
            request.FromZone,
            request.ToZone));
    }
}
