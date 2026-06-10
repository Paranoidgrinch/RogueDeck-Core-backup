namespace RogueDeck.Core.Combat;

public readonly record struct TriggeredEffectDefinitionId(string value)
{
    public override string ToString() => value;
}

public interface ITriggeredEffectDefinition
{
    TriggeredEffectDefinitionId Id { get; }
    Type EventType { get; }
}

public sealed class CardPlayedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardPlayedCombatEvent);

    public List<ICardPlayedTriggeredEffectFilter> Filters { get; } = new();

    public List<ICardPlayedTriggeredEffectAction> Actions { get; } = new();

    public CardPlayedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CardPlayedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardPlayedCombatEvent CombatEvent,
    CombatantState Source,
    CardDefinition Card);

public interface ICardPlayedTriggeredEffectFilter
{
    bool Matches(CardPlayedTriggeredEffectContext context);
}

public interface ICardPlayedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context);
}

public interface ICardPlayedTriggeredEffectAmountProvider
{
    int ResolveAmount(CardPlayedTriggeredEffectContext context);
}

public sealed record FixedCardPlayedTriggeredEffectAmount(int Amount)
    : ICardPlayedTriggeredEffectAmountProvider
{
    public int ResolveAmount(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed record CardPlayedSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : ICardPlayedTriggeredEffectAmountProvider
{
    public int ResolveAmount(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Source.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}
public sealed record CardPlayedCardHasTagTriggerFilter(TagId TagId)
    : ICardPlayedTriggeredEffectFilter
{
    public bool Matches(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Card.Tags.Contains(TagId);
    }
}

public sealed record CardPlayedEveryNthCardWithTagThisTurnFilter
    : ICardPlayedTriggeredEffectFilter
{
    public TagId TagId { get; }

    public int Interval { get; }

    public CardPlayedEveryNthCardWithTagThisTurnFilter(TagId tagId, int interval)
    {
        if (interval <= 0)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");

        TagId = tagId;
        Interval = interval;
    }

    public bool Matches(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stats = context.Combat.GetCardPlayTurnStats(context.CombatEvent.SourceCombatantId);
        var cardsPlayedWithTagThisTurn = stats.GetCardsPlayedWithTagThisTurn(TagId);

        if (cardsPlayedWithTagThisTurn <= 0)
            return false;

        return cardsPlayedWithTagThisTurn % Interval == 0;
    }
}
public sealed record CardPlayedSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : ICardPlayedTriggeredEffectFilter
{
    public bool Matches(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Source.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public enum CardPlayedTriggeredEffectTarget
{
    Source,
    PlayedCardTarget
}

public sealed record GainBlockOnCardPlayedTriggeredEffectAction(
    CardPlayedTriggeredEffectTarget Target,
    ICardPlayedTriggeredEffectAmountProvider AmountProvider)
    : ICardPlayedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        if (!TryResolveTarget(context, Target, out var targetId))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        return new IEffectRequest[]
        {
            new GainBlockEffectRequest(
                TargetCombatantId: targetId,
                Amount: amount,
                SourceCombatantId: context.CombatEvent.SourceCombatantId,
                SourceCardId: context.CombatEvent.CardDefinitionId)
        };
    }

    private static bool TryResolveTarget(
        CardPlayedTriggeredEffectContext context,
        CardPlayedTriggeredEffectTarget target,
        out CombatantId targetId)
    {
        switch (target)
        {
            case CardPlayedTriggeredEffectTarget.Source:
                targetId = context.CombatEvent.SourceCombatantId;
                return true;

            case CardPlayedTriggeredEffectTarget.PlayedCardTarget:
                if (context.CombatEvent.TargetCombatantId is null)
                {
                    targetId = default;
                    return false;
                }

                targetId = context.CombatEvent.TargetCombatantId.Value;
                return context.Combat.TryGetCombatant(targetId, out var combatant) &&
                    combatant!.IsAlive;

            default:
                throw new InvalidOperationException(
                    $"Unsupported card played triggered effect target '{target}'.");
        }
    }
}

public sealed record DrawCardsOnCardPlayedTriggeredEffectAction(
    CardPlayedTriggeredEffectTarget Target,
    ICardPlayedTriggeredEffectAmountProvider AmountProvider)
    : ICardPlayedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        if (!TryResolveTarget(context, Target, out var targetId))
            return Array.Empty<IEffectRequest>();

        var count = AmountProvider.ResolveAmount(context);

        if (count <= 0)
            return Array.Empty<IEffectRequest>();

        return new IEffectRequest[]
        {
            new DrawCardsEffectRequest(
                CombatantId: targetId,
                Count: count)
        };
    }

    private static bool TryResolveTarget(
        CardPlayedTriggeredEffectContext context,
        CardPlayedTriggeredEffectTarget target,
        out CombatantId targetId)
    {
        switch (target)
        {
            case CardPlayedTriggeredEffectTarget.Source:
                targetId = context.CombatEvent.SourceCombatantId;
                return true;

            case CardPlayedTriggeredEffectTarget.PlayedCardTarget:
                if (context.CombatEvent.TargetCombatantId is null)
                {
                    targetId = default;
                    return false;
                }

                targetId = context.CombatEvent.TargetCombatantId.Value;
                return context.Combat.TryGetCombatant(targetId, out var combatant) &&
                    combatant!.IsAlive;

            default:
                throw new InvalidOperationException(
                    $"Unsupported card played triggered effect target '{target}'.");
        }
    }
}
public sealed class CardPlayedTriggeredEffectHandler
    : CombatEventHandler<CardPlayedCombatEvent>
{
    private readonly IReadOnlyCollection<CardPlayedTriggeredEffectDefinition> _definitions;

    public CardPlayedTriggeredEffectHandler(params CardPlayedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardPlayedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.SourceCombatantId, out var source))
            return;

        var card = registry.GetCard(combatEvent.CardDefinitionId);

        var context = new CardPlayedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            Source: source!,
            Card: card);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardPlayedCombatEvent))
                .OfType<CardPlayedTriggeredEffectDefinition>());

        foreach (var definition in definitions)
        {
            if (definition.Filters.Any(filter => !filter.Matches(context)))
                continue;

            foreach (var action in definition.Actions)
            {
                foreach (var effectRequest in action.BuildEffectRequests(context))
                    combat.EnqueueEffect(effectRequest);
            }
        }
    }
}




