namespace RogueDeck.Core.Combat;

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
        {
            return;
        }

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
