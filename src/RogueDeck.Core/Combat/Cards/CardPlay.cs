namespace RogueDeck.Core.Combat;

public sealed record CardPlayRequest(
    CardDefinitionId CardDefinitionId,
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
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(request);

        if (combat.Result != CombatResult.Ongoing)
            throw new InvalidOperationException(
                $"Cannot play card because combat result is '{combat.Result}'.");

        var source = combat.GetCombatant(request.SourceCombatantId);

        if (!source.IsAlive)
            throw new InvalidOperationException(
                $"Combatant '{request.SourceCombatantId}' cannot play cards because it is not alive.");

        var card = registry.GetCard(request.CardDefinitionId);

        var effectRequests = BuildEffectRequests(
            combat,
            card,
            source,
            request.TargetCombatantId);

        EnsureCostsCanBePaid(source, card);
        PayCosts(source, card);

        combat.AddLogEntry(
            StandardCombatLogTypes.CardPlayed,
            $"Combatant '{request.SourceCombatantId}' played card '{request.CardDefinitionId}'.");

        combat.EnqueueEvent(
            new CardPlayedCombatEvent(
                CardDefinitionId: request.CardDefinitionId,
                SourceCombatantId: request.SourceCombatantId,
                TargetCombatantId: request.TargetCombatantId));

        foreach (var effectRequest in effectRequests)
            combat.EnqueueEffect(effectRequest);

        _queueProcessor.ResolvePendingQueues(
            combat,
            registry,
            maxQueueCyclesToResolve);
    }

    private static List<IEffectRequest> BuildEffectRequests(
        CombatState combat,
        CardDefinition card,
        CombatantState source,
        CombatantId? requestedTargetId)
    {
        var requests = new List<IEffectRequest>();

        foreach (var effect in card.Effects)
        {
            switch (effect)
            {
                case DealDamageCardEffectDefinition dealDamage:
                {
                    var targetId = ResolveTarget(
                        combat,
                        source,
                        requestedTargetId,
                        dealDamage.TargetMode);

                    requests.Add(
                        new DealDamageEffectRequest(
                            TargetCombatantId: targetId,
                            Amount: dealDamage.Amount,
                            SourceCombatantId: source.Id,
                            SourceCardId: card.Id,
                            Kind: DamageKind.Direct));

                    break;
                }

                case GainBlockCardEffectDefinition gainBlock:
                {
                    var targetId = ResolveTarget(
                        combat,
                        source,
                        requestedTargetId,
                        gainBlock.TargetMode);

                    requests.Add(
                        new GainBlockEffectRequest(
                            TargetCombatantId: targetId,
                            Amount: gainBlock.Amount,
                            SourceCombatantId: source.Id,
                            SourceCardId: card.Id));

                    break;
                }

                default:
                    throw new InvalidOperationException(
                        $"Unsupported card effect definition type '{effect.GetType().Name}'.");
            }
        }

        return requests;
    }

    private static CombatantId ResolveTarget(
        CombatState combat,
        CombatantState source,
        CombatantId? requestedTargetId,
        CardTargetMode targetMode)
    {
        switch (targetMode)
        {
            case CardTargetMode.Self:
                return source.Id;

            case CardTargetMode.SingleEnemy:
            {
                if (requestedTargetId is null)
                    throw new InvalidOperationException("Card requires a single enemy target.");

                var target = combat.GetCombatant(requestedTargetId.Value);

                if (!target.IsAlive)
                    throw new InvalidOperationException(
                        $"Target combatant '{requestedTargetId}' is not alive.");

                if (target.TeamId == source.TeamId)
                    throw new InvalidOperationException(
                        $"Target combatant '{requestedTargetId}' is not an enemy of '{source.Id}'.");

                return target.Id;
            }

            default:
                throw new InvalidOperationException(
                    $"Unsupported card target mode '{targetMode}'.");
        }
    }

    private static void EnsureCostsCanBePaid(
        CombatantState source,
        CardDefinition card)
    {
        foreach (var cost in card.Costs)
        {
            if (cost.Amount < 0)
                throw new InvalidOperationException(
                    $"Card '{card.Id}' has a negative cost for resource '{cost.ResourceId}'.");

            if (!source.Resources.TryGetValue(cost.ResourceId, out var resource))
                throw new InvalidOperationException(
                    $"Combatant '{source.Id}' does not have resource '{cost.ResourceId}'.");

            if (resource.Current < cost.Amount)
                throw new InvalidOperationException(
                    $"Combatant '{source.Id}' cannot pay {cost.Amount} '{cost.ResourceId}'.");
        }
    }

    private static void PayCosts(
        CombatantState source,
        CardDefinition card)
    {
        foreach (var cost in card.Costs)
        {
            var resource = source.Resources[cost.ResourceId];

            resource.SetCurrent(resource.Current - cost.Amount);
        }
    }
}
