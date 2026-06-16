namespace RogueDeck.Core.Combat;

internal static class CardLifecycleTriggeredEffectSupport
{
    public static IReadOnlyCollection<CardInstance> ResolveCards(
        CombatState combat,
        CombatantId combatantId,
        IReadOnlyCollection<CardInstanceId> cardInstanceIds)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(cardInstanceIds);

        if (cardInstanceIds.Count == 0)
            return Array.Empty<CardInstance>();

        var requestedIds = cardInstanceIds.ToHashSet();

        return combat.GetCardZones(combatantId)
            .AllCards
            .Where(card => requestedIds.Contains(card.Id))
            .ToArray();
    }

    public static IReadOnlyCollection<CardDefinition> ResolveCardDefinitions(
        CombatDefinitionRegistry registry,
        IEnumerable<CardInstance> cards)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(cards);

        var definitions = new List<CardDefinition>();

        foreach (var card in cards)
        {
            if (registry.TryGetCard(card.DefinitionId, out var definition))
                definitions.Add(definition!);
        }

        return definitions;
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        CombatState combat,
        CombatantState source)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(source);

        return new TriggeredEffectActionBuildContext(
            new CombatantTargetSelectionContext(
                Combat: combat,
                Source: source,
                EventTargetId: source.Id),
            new TriggeredEffectActionSource(SourceCombatantId: source.Id));
    }
}

public sealed class CardsDrawnTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardsDrawnCombatEvent);

    public List<ICardsDrawnTriggeredEffectFilter> Filters { get; } = new();

    public List<ICardsDrawnTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<CardsDrawnTriggeredEffectContext>> Effects { get; } = new();

    public CardsDrawnTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CardsDrawnTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardsDrawnCombatEvent CombatEvent,
    CombatantState Source,
    IReadOnlyCollection<CardInstance> Cards,
    IReadOnlyCollection<CardDefinition> CardDefinitions);

public interface ICardsDrawnTriggeredEffectFilter
{
    bool Matches(CardsDrawnTriggeredEffectContext context);
}

public interface ICardsDrawnTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardsDrawnTriggeredEffectContext context);
}

public interface ICardsDrawnTriggeredEffectAmountProvider
{
    int ResolveAmount(CardsDrawnTriggeredEffectContext context);
}

public sealed record FixedCardsDrawnTriggeredEffectAmount(int Amount)
    : ICardsDrawnTriggeredEffectAmountProvider,
      ICombatValueProvider<CardsDrawnTriggeredEffectContext, int>
{
    public int ResolveAmount(CardsDrawnTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(CardsDrawnTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardsDrawnCardCountAmount
    : ICardsDrawnTriggeredEffectAmountProvider,
      ICombatValueProvider<CardsDrawnTriggeredEffectContext, int>
{
    public int ResolveAmount(CardsDrawnTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Cards.Count;
    }

    public int Resolve(CardsDrawnTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardsDrawnCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : ICardsDrawnTriggeredEffectFilter
{
    public bool Matches(CardsDrawnTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinitions.Any(card => card.Id == CardDefinitionId);
    }
}

public sealed record CardsDrawnCardHasTagTriggerFilter(TagId TagId)
    : ICardsDrawnTriggeredEffectFilter
{
    public bool Matches(CardsDrawnTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinitions.Any(card => card.Tags.Contains(TagId));
    }
}

public sealed record GainBlockOnCardsDrawnTriggeredEffectAction
    : ICardsDrawnTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardsDrawnTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCardsDrawnTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardsDrawnTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector;
        AmountProvider = amountProvider;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardsDrawnTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(context.Combat, context.Source),
            TargetSelector,
            AmountProvider.ResolveAmount(context));
    }
}

public sealed class CardsDrawnTriggeredEffectHandler
    : CombatEventHandler<CardsDrawnCombatEvent>
{
    private readonly IReadOnlyCollection<CardsDrawnTriggeredEffectDefinition> _definitions;

    public CardsDrawnTriggeredEffectHandler(params CardsDrawnTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions;
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardsDrawnCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var source))
            return;

        var cards = CardLifecycleTriggeredEffectSupport.ResolveCards(
            combat,
            combatEvent.CombatantId,
            combatEvent.CardInstanceIds);
        var cardDefinitions = CardLifecycleTriggeredEffectSupport.ResolveCardDefinitions(registry, cards);

        var context = new CardsDrawnTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            Source: source!,
            Cards: cards,
            CardDefinitions: cardDefinitions);

        var actionBuildContext =
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(combat, source!);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardsDrawnCombatEvent))
                .OfType<CardsDrawnTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions
                .SelectMany(action => action.BuildEffectRequests(context))
                .Concat(definition.Effects.SelectMany(
                    effect => effect.BuildEffectRequests(
                        context,
                        actionBuildContext))));
    }
}

public sealed class DiscardPileShuffledTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(DiscardPileShuffledIntoDrawPileCombatEvent);

    public List<IDiscardPileShuffledTriggeredEffectFilter> Filters { get; } = new();

    public List<IDiscardPileShuffledTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<DiscardPileShuffledTriggeredEffectContext>> Effects { get; } = new();

    public DiscardPileShuffledTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record DiscardPileShuffledTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    DiscardPileShuffledIntoDrawPileCombatEvent CombatEvent,
    CombatantState Source,
    IReadOnlyCollection<CardInstance> Cards,
    IReadOnlyCollection<CardDefinition> CardDefinitions);

public interface IDiscardPileShuffledTriggeredEffectFilter
{
    bool Matches(DiscardPileShuffledTriggeredEffectContext context);
}

public interface IDiscardPileShuffledTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DiscardPileShuffledTriggeredEffectContext context);
}

public interface IDiscardPileShuffledTriggeredEffectAmountProvider
{
    int ResolveAmount(DiscardPileShuffledTriggeredEffectContext context);
}

public sealed record FixedDiscardPileShuffledTriggeredEffectAmount(int Amount)
    : IDiscardPileShuffledTriggeredEffectAmountProvider,
      ICombatValueProvider<DiscardPileShuffledTriggeredEffectContext, int>
{
    public int ResolveAmount(DiscardPileShuffledTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(DiscardPileShuffledTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record DiscardPileShuffledCardCountAmount
    : IDiscardPileShuffledTriggeredEffectAmountProvider,
      ICombatValueProvider<DiscardPileShuffledTriggeredEffectContext, int>
{
    public int ResolveAmount(DiscardPileShuffledTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Cards.Count;
    }

    public int Resolve(DiscardPileShuffledTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record DiscardPileShuffledCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : IDiscardPileShuffledTriggeredEffectFilter
{
    public bool Matches(DiscardPileShuffledTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinitions.Any(card => card.Id == CardDefinitionId);
    }
}

public sealed record DiscardPileShuffledCardHasTagTriggerFilter(TagId TagId)
    : IDiscardPileShuffledTriggeredEffectFilter
{
    public bool Matches(DiscardPileShuffledTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinitions.Any(card => card.Tags.Contains(TagId));
    }
}

public sealed record GainBlockOnDiscardPileShuffledTriggeredEffectAction
    : IDiscardPileShuffledTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDiscardPileShuffledTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnDiscardPileShuffledTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDiscardPileShuffledTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector;
        AmountProvider = amountProvider;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DiscardPileShuffledTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            new TriggeredEffectActionBuildContext(
                new CombatantTargetSelectionContext(
                    Combat: context.Combat,
                    Source: context.Source),
                new TriggeredEffectActionSource(SourceCombatantId: context.Source.Id)),
            TargetSelector,
            AmountProvider.ResolveAmount(context));
    }
}

public sealed class DiscardPileShuffledTriggeredEffectHandler
    : CombatEventHandler<DiscardPileShuffledIntoDrawPileCombatEvent>
{
    private readonly IReadOnlyCollection<DiscardPileShuffledTriggeredEffectDefinition> _definitions;

    public DiscardPileShuffledTriggeredEffectHandler(params DiscardPileShuffledTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions;
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DiscardPileShuffledIntoDrawPileCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var source))
            return;

        var cards = CardLifecycleTriggeredEffectSupport.ResolveCards(
            combat,
            combatEvent.CombatantId,
            combatEvent.CardInstanceIds);
        var cardDefinitions = CardLifecycleTriggeredEffectSupport.ResolveCardDefinitions(registry, cards);

        var context = new DiscardPileShuffledTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            Source: source!,
            Cards: cards,
            CardDefinitions: cardDefinitions);

        var actionBuildContext =
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(combat, source!);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(DiscardPileShuffledIntoDrawPileCombatEvent))
                .OfType<DiscardPileShuffledTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions
                .SelectMany(action => action.BuildEffectRequests(context))
                .Concat(definition.Effects.SelectMany(
                    effect => effect.BuildEffectRequests(
                        context,
                        actionBuildContext))));
    }
}

public sealed class HandDiscardedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(HandDiscardedCombatEvent);

    public List<IHandDiscardedTriggeredEffectFilter> Filters { get; } = new();

    public List<IHandDiscardedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<HandDiscardedTriggeredEffectContext>> Effects { get; } = new();

    public HandDiscardedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record HandDiscardedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    HandDiscardedCombatEvent CombatEvent,
    CombatantState Source,
    IReadOnlyCollection<CardInstance> Cards,
    IReadOnlyCollection<CardDefinition> CardDefinitions);

public interface IHandDiscardedTriggeredEffectFilter
{
    bool Matches(HandDiscardedTriggeredEffectContext context);
}

public interface IHandDiscardedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(HandDiscardedTriggeredEffectContext context);
}

public interface IHandDiscardedTriggeredEffectAmountProvider
{
    int ResolveAmount(HandDiscardedTriggeredEffectContext context);
}

public sealed record FixedHandDiscardedTriggeredEffectAmount(int Amount)
    : IHandDiscardedTriggeredEffectAmountProvider,
      ICombatValueProvider<HandDiscardedTriggeredEffectContext, int>
{
    public int ResolveAmount(HandDiscardedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(HandDiscardedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record HandDiscardedCardCountAmount
    : IHandDiscardedTriggeredEffectAmountProvider,
      ICombatValueProvider<HandDiscardedTriggeredEffectContext, int>
{
    public int ResolveAmount(HandDiscardedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Cards.Count;
    }

    public int Resolve(HandDiscardedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record HandDiscardedCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : IHandDiscardedTriggeredEffectFilter
{
    public bool Matches(HandDiscardedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinitions.Any(card => card.Id == CardDefinitionId);
    }
}

public sealed record HandDiscardedCardHasTagTriggerFilter(TagId TagId)
    : IHandDiscardedTriggeredEffectFilter
{
    public bool Matches(HandDiscardedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinitions.Any(card => card.Tags.Contains(TagId));
    }
}

public sealed record GainBlockOnHandDiscardedTriggeredEffectAction
    : IHandDiscardedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IHandDiscardedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnHandDiscardedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IHandDiscardedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector;
        AmountProvider = amountProvider;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(HandDiscardedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(context.Combat, context.Source),
            TargetSelector,
            AmountProvider.ResolveAmount(context));
    }
}

public sealed class HandDiscardedTriggeredEffectHandler
    : CombatEventHandler<HandDiscardedCombatEvent>
{
    private readonly IReadOnlyCollection<HandDiscardedTriggeredEffectDefinition> _definitions;

    public HandDiscardedTriggeredEffectHandler(params HandDiscardedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions;
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        HandDiscardedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var source))
            return;

        var cards = CardLifecycleTriggeredEffectSupport.ResolveCards(
            combat,
            combatEvent.CombatantId,
            combatEvent.CardInstanceIds);
        var cardDefinitions = CardLifecycleTriggeredEffectSupport.ResolveCardDefinitions(registry, cards);

        var context = new HandDiscardedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            Source: source!,
            Cards: cards,
            CardDefinitions: cardDefinitions);

        var actionBuildContext =
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(combat, source!);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(HandDiscardedCombatEvent))
                .OfType<HandDiscardedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions
                .SelectMany(action => action.BuildEffectRequests(context))
                .Concat(definition.Effects.SelectMany(
                    effect => effect.BuildEffectRequests(
                        context,
                        actionBuildContext))));
    }
}

public sealed class CardExhaustedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardMovedToZoneCombatEvent);

    public List<ICardExhaustedTriggeredEffectFilter> Filters { get; } = new();

    public List<ICardExhaustedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<CardExhaustedTriggeredEffectContext>> Effects { get; } = new();

    public CardExhaustedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CardExhaustedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardMovedToZoneCombatEvent CombatEvent,
    CombatantState Source,
    CardInstance? Card,
    CardDefinition? CardDefinition);

public interface ICardExhaustedTriggeredEffectFilter
{
    bool Matches(CardExhaustedTriggeredEffectContext context);
}

public interface ICardExhaustedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardExhaustedTriggeredEffectContext context);
}

public interface ICardExhaustedTriggeredEffectAmountProvider
{
    int ResolveAmount(CardExhaustedTriggeredEffectContext context);
}

public sealed record FixedCardExhaustedTriggeredEffectAmount(int Amount)
    : ICardExhaustedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardExhaustedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardExhaustedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(CardExhaustedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardExhaustedCardCountAmount
    : ICardExhaustedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardExhaustedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardExhaustedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Card is null ? 0 : 1;
    }

    public int Resolve(CardExhaustedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardExhaustedCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : ICardExhaustedTriggeredEffectFilter
{
    public bool Matches(CardExhaustedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinition?.Id == CardDefinitionId;
    }
}

public sealed record CardExhaustedCardHasTagTriggerFilter(TagId TagId)
    : ICardExhaustedTriggeredEffectFilter
{
    public bool Matches(CardExhaustedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinition?.Tags.Contains(TagId) == true;
    }
}

public sealed record GainBlockOnCardExhaustedTriggeredEffectAction
    : ICardExhaustedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardExhaustedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCardExhaustedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardExhaustedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector;
        AmountProvider = amountProvider;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardExhaustedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(context.Combat, context.Source),
            TargetSelector,
            AmountProvider.ResolveAmount(context));
    }
}

public sealed class CardExhaustedTriggeredEffectHandler
    : CombatEventHandler<CardMovedToZoneCombatEvent>
{
    private readonly IReadOnlyCollection<CardExhaustedTriggeredEffectDefinition> _definitions;

    public CardExhaustedTriggeredEffectHandler(params CardExhaustedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions;
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardMovedToZoneCombatEvent combatEvent)
    {
        if (combatEvent.ToZone != CardZone.ExhaustPile)
            return;

        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var source))
            return;

        var card = CardLifecycleTriggeredEffectSupport.ResolveCards(
            combat,
            combatEvent.CombatantId,
            new[] { combatEvent.CardInstanceId })
            .SingleOrDefault();
        var cardDefinition = card is not null && registry.TryGetCard(card.DefinitionId, out var definition)
            ? definition
            : null;

        var context = new CardExhaustedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            Source: source!,
            Card: card,
            CardDefinition: cardDefinition);

        var actionBuildContext =
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(combat, source!);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardMovedToZoneCombatEvent))
                .OfType<CardExhaustedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions
                .SelectMany(action => action.BuildEffectRequests(context))
                .Concat(definition.Effects.SelectMany(
                    effect => effect.BuildEffectRequests(
                        context,
                        actionBuildContext))));
    }
}

public sealed class CardBanishedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardMovedToZoneCombatEvent);

    public List<ICardBanishedTriggeredEffectFilter> Filters { get; } = new();

    public List<ICardBanishedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<CardBanishedTriggeredEffectContext>> Effects { get; } = new();

    public CardBanishedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CardBanishedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardMovedToZoneCombatEvent CombatEvent,
    CombatantState Source,
    CardInstance? Card,
    CardDefinition? CardDefinition);

public interface ICardBanishedTriggeredEffectFilter
{
    bool Matches(CardBanishedTriggeredEffectContext context);
}

public interface ICardBanishedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardBanishedTriggeredEffectContext context);
}

public interface ICardBanishedTriggeredEffectAmountProvider
{
    int ResolveAmount(CardBanishedTriggeredEffectContext context);
}

public sealed record FixedCardBanishedTriggeredEffectAmount(int Amount)
    : ICardBanishedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardBanishedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardBanishedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(CardBanishedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardBanishedCardCountAmount
    : ICardBanishedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardBanishedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardBanishedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Card is null ? 0 : 1;
    }

    public int Resolve(CardBanishedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardBanishedCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : ICardBanishedTriggeredEffectFilter
{
    public bool Matches(CardBanishedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinition?.Id == CardDefinitionId;
    }
}

public sealed record CardBanishedCardHasTagTriggerFilter(TagId TagId)
    : ICardBanishedTriggeredEffectFilter
{
    public bool Matches(CardBanishedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CardDefinition?.Tags.Contains(TagId) == true;
    }
}

public sealed record GainBlockOnCardBanishedTriggeredEffectAction
    : ICardBanishedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardBanishedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCardBanishedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardBanishedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector;
        AmountProvider = amountProvider;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardBanishedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(context.Combat, context.Source),
            TargetSelector,
            AmountProvider.ResolveAmount(context));
    }
}

public sealed class CardBanishedTriggeredEffectHandler
    : CombatEventHandler<CardMovedToZoneCombatEvent>
{
    private readonly IReadOnlyCollection<CardBanishedTriggeredEffectDefinition> _definitions;

    public CardBanishedTriggeredEffectHandler(params CardBanishedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions;
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardMovedToZoneCombatEvent combatEvent)
    {
        if (combatEvent.ToZone != CardZone.BanishedPile)
            return;

        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var source))
            return;

        var card = CardLifecycleTriggeredEffectSupport.ResolveCards(
            combat,
            combatEvent.CombatantId,
            new[] { combatEvent.CardInstanceId })
            .SingleOrDefault();
        var cardDefinition = card is not null && registry.TryGetCard(card.DefinitionId, out var definition)
            ? definition
            : null;

        var context = new CardBanishedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            Source: source!,
            Card: card,
            CardDefinition: cardDefinition);

        var actionBuildContext =
            CardLifecycleTriggeredEffectSupport.CreateActionBuildContext(combat, source!);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardMovedToZoneCombatEvent))
                .OfType<CardBanishedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions
                .SelectMany(action => action.BuildEffectRequests(context))
                .Concat(definition.Effects.SelectMany(
                    effect => effect.BuildEffectRequests(
                        context,
                        actionBuildContext))));
    }
}
