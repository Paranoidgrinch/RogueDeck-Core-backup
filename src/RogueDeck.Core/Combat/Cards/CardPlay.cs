namespace RogueDeck.Core.Combat;

public sealed record CardPlayRequest(
    CardDefinitionId CardDefinitionId,
    CombatantId SourceCombatantId,
    CombatantId? TargetCombatantId = null);

public sealed record CardInstancePlayRequest(
    CardInstanceId CardInstanceId,
    CombatantId SourceCombatantId,
    CombatantId? TargetCombatantId = null);

public sealed class CombatCardPlayProcessor
{
    private readonly CombatQueueProcessor _queueProcessor = new();

    public void PlayCard(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardPlayRequest request,
        int maxQueueCyclesToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(request);

        PlayCardCore(
            combat,
            registry,
            request.CardDefinitionId,
            request.SourceCombatantId,
            request.TargetCombatantId,
            cardInstanceId: null,
            maxQueueCyclesToResolve);
    }

    public void PlayCardInstance(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardInstancePlayRequest request,
        int maxQueueCyclesToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(request);

        var zones = combat.GetCardZones(request.SourceCombatantId);
        var cardInstance = zones.GetCard(request.CardInstanceId);

        if (cardInstance.OwnerId != request.SourceCombatantId)
        {
            throw new InvalidOperationException(
                $"Card instance '{request.CardInstanceId}' is not owned by combatant '{request.SourceCombatantId}'.");
        }

        if (cardInstance.Zone != CardZone.Hand)
        {
            throw new InvalidOperationException(
                $"Card instance '{request.CardInstanceId}' cannot be played because it is in zone '{cardInstance.Zone}'.");
        }

        PlayCardCore(
            combat,
            registry,
            cardInstance.DefinitionId,
            request.SourceCombatantId,
            request.TargetCombatantId,
            cardInstance.Id,
            maxQueueCyclesToResolve);
    }

    private void PlayCardCore(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinitionId cardDefinitionId,
        CombatantId sourceCombatantId,
        CombatantId? targetCombatantId,
        CardInstanceId? cardInstanceId,
        int maxQueueCyclesToResolve)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);

        if (combat.Result != CombatResult.Ongoing)
        {
            throw new InvalidOperationException(
                $"Cannot play card because combat result is '{combat.Result}'.");
        }

        var source = combat.GetCombatant(sourceCombatantId);

        if (!source.IsAlive)
        {
            throw new InvalidOperationException(
                $"Combatant '{sourceCombatantId}' cannot play cards because it is not alive.");
        }

        var card = registry.GetCard(cardDefinitionId);

        ValidateCardPlay(
            combat,
            registry,
            card,
            source,
            targetCombatantId,
            cardInstanceId);

        var effectRequests = BuildEffectRequests(
            combat,
            registry,
            card,
            source,
            targetCombatantId);

        var costsToPay = CalculateCosts(
            combat,
            registry,
            card,
            source,
            targetCombatantId,
            cardInstanceId);

        EnsureCostsCanBePaid(source, card, costsToPay);
        PayCosts(source, costsToPay);

        combat.AddLogEntry(
            StandardCombatLogTypes.CardPlayed,
            $"Combatant '{sourceCombatantId}' played card '{cardDefinitionId}'.");

        combat.EnqueueEvent(
            new CardPlayedCombatEvent(
                CardDefinitionId: cardDefinitionId,
                SourceCombatantId: sourceCombatantId,
                TargetCombatantId: targetCombatantId,
                CardInstanceId: cardInstanceId));

        foreach (var effectRequest in effectRequests)
            combat.EnqueueEffect(effectRequest);

        if (cardInstanceId is not null)
        {
            combat.EnqueueEffect(new MoveCardToZoneEffectRequest(
                sourceCombatantId,
                cardInstanceId.Value,
                card.PlayedCardDestinationZone));
        }

        _queueProcessor.ResolvePendingQueues(
            combat,
            registry,
            maxQueueCyclesToResolve);
    }

    private static void ValidateCardPlay(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinition card,
        CombatantState source,
        CombatantId? requestedTargetId,
        CardInstanceId? cardInstanceId)
    {
        var context = new CardPlayValidationContext(
            Combat: combat,
            Registry: registry,
            Card: card,
            Source: source,
            RequestedTargetId: requestedTargetId,
            CardInstanceId: cardInstanceId);

        foreach (var validator in registry.GetCardPlayValidators())
            validator.Validate(context);
    }

    private static List<IEffectRequest> BuildEffectRequests(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinition card,
        CombatantState source,
        CombatantId? requestedTargetId)
    {
        var requests = new List<IEffectRequest>();
        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: source,
            RequestedTargetId: requestedTargetId);

        foreach (var effect in card.Effects)
        {
            var handler = registry.GetCardEffectDefinitionHandler(effect.GetType());
            requests.AddRange(handler.BuildEffectRequests(context, effect));
        }

        return requests;
    }

    private static IReadOnlyList<CalculatedResourceCost> CalculateCosts(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinition card,
        CombatantState source,
        CombatantId? requestedTargetId,
        CardInstanceId? cardInstanceId)
    {
        var totalCosts = new Dictionary<ResourceId, int>();

        foreach (var cost in card.Costs)
        {
            if (cost.Amount < 0)
            {
                throw new InvalidOperationException(
                    $"Card '{card.Id}' has a negative cost for resource '{cost.ResourceId}'.");
            }

            var context = new CardCostModificationContext(
                Combat: combat,
                Registry: registry,
                Card: card,
                Source: source,
                Cost: cost,
                RequestedTargetId: requestedTargetId,
                CardInstanceId: cardInstanceId);

            var modifiedAmount = cost.Amount;

            foreach (var modifier in registry.GetCardCostModifiers())
                modifiedAmount = Math.Max(0, modifier.ModifyCostAmount(context, modifiedAmount));

            if (modifiedAmount == 0)
                continue;

            if (!totalCosts.TryAdd(cost.ResourceId, modifiedAmount))
                totalCosts[cost.ResourceId] = checked(totalCosts[cost.ResourceId] + modifiedAmount);
        }

        return totalCosts
            .Select(pair => new CalculatedResourceCost(pair.Key, pair.Value))
            .ToArray();
    }

    private static void EnsureCostsCanBePaid(
        CombatantState source,
        CardDefinition card,
        IReadOnlyCollection<CalculatedResourceCost> costsToPay)
    {
        foreach (var cost in costsToPay)
        {
            if (!source.Resources.TryGetValue(cost.ResourceId, out var resource))
            {
                throw new InvalidOperationException(
                    $"Combatant '{source.Id}' does not have resource '{cost.ResourceId}'.");
            }

            if (resource.Current < cost.Amount)
            {
                throw new InvalidOperationException(
                    $"Combatant '{source.Id}' cannot pay {cost.Amount} '{cost.ResourceId}' for card '{card.Id}'.");
            }
        }
    }

    private static void PayCosts(
        CombatantState source,
        IReadOnlyCollection<CalculatedResourceCost> costsToPay)
    {
        foreach (var cost in costsToPay)
        {
            var resource = source.Resources[cost.ResourceId];
            resource.SetCurrent(resource.Current - cost.Amount);
        }
    }
}
