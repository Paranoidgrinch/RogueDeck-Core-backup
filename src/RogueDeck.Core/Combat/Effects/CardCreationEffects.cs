namespace RogueDeck.Core.Combat;

public sealed record CreateCardInstanceEffectRequest(
    CombatantId CombatantId,
    CardDefinitionId CardDefinitionId,
    CardZone ToZone,
    int Count = 1
) : IEffectRequest;

public sealed class CreateCardInstanceEffectHandler : EffectRequestHandler<CreateCardInstanceEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CreateCardInstanceEffectRequest request)
    {
        if (request.Count < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Count), "Card instance count cannot be negative.");

        if (request.Count == 0)
            return;

        registry.GetCard(request.CardDefinitionId);

        var zones = combat.GetCardZones(request.CombatantId);
        var createdCardIds = new List<CardInstanceId>();

        for (var i = 0; i < request.Count; i++)
        {
            var card = new CardInstance(
                combat.CreateNextCardInstanceId(),
                request.CardDefinitionId,
                request.CombatantId,
                request.ToZone);

            zones.AddCard(card);
            createdCardIds.Add(card.Id);
        }

        combat.AddLogEntry(
            StandardCombatLogTypes.CardInstanceCreated,
            $"Created {createdCardIds.Count} instance(s) of card '{request.CardDefinitionId}' for combatant '{request.CombatantId}' in zone '{request.ToZone}'.");

        combat.EnqueueEvent(new CardInstanceCreatedCombatEvent(
            request.CombatantId,
            request.CardDefinitionId,
            createdCardIds,
            request.ToZone));
    }
}
