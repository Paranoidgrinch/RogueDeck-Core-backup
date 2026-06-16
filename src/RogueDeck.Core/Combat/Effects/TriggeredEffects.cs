namespace RogueDeck.Core.Combat;

public readonly record struct TriggeredEffectDefinitionId(string value)
{
    public override string ToString() => value;
}

public enum TriggeredEffectReentryPolicy
{
    SuppressRecursiveReentry = 0,
    AllowRecursiveReentry = 1
}

public interface ITriggeredEffectDefinition
{
    TriggeredEffectDefinitionId Id { get; }

    Type EventType { get; }

    TriggeredEffectReentryPolicy ReentryPolicy =>
        TriggeredEffectReentryPolicy.SuppressRecursiveReentry;
}

public sealed class CardPlayedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardPlayedCombatEvent);

    public List<ICardPlayedTriggeredEffectFilter> Filters { get; } = new();

    public List<ICardPlayedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<CardPlayedTriggeredEffectContext>> Effects { get; } = new();

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
    : ICardPlayedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardPlayedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(CardPlayedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardPlayedSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : ICardPlayedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardPlayedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Source.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }

    public int Resolve(CardPlayedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
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
    PlayedCardTarget,
    AllAllies,
    AllEnemies
}

public static class CardPlayedTriggeredEffectTargetResolver
{
    public static IReadOnlyCollection<CombatantId> ResolveTargets(
        CardPlayedTriggeredEffectContext context,
        CardPlayedTriggeredEffectTarget target)
    {
        return ResolveTargets(context, ToSelector(target));
    }

    public static IReadOnlyCollection<CombatantId> ResolveTargets(
        CardPlayedTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(targetSelector);

        return targetSelector.ResolveTargets(CreateSelectionContext(context));
    }

    public static ICombatantTargetSelector ToSelector(CardPlayedTriggeredEffectTarget target)
    {
        switch (target)
        {
            case CardPlayedTriggeredEffectTarget.Source:
                return CombatantTargetSelectors.Source;

            case CardPlayedTriggeredEffectTarget.PlayedCardTarget:
                return CombatantTargetSelectors.EventTarget;

            case CardPlayedTriggeredEffectTarget.AllAllies:
                return CombatantTargetSelectors.AllAlliesOfSource;

            case CardPlayedTriggeredEffectTarget.AllEnemies:
                return CombatantTargetSelectors.AllEnemiesOfSource;

            default:
                throw new InvalidOperationException(
                    $"Unsupported card played triggered effect target '{target}'.");
        }
    }

    public static CombatantTargetSelectionContext CreateSelectionContext(
        CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.Source,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId,
            SourceCardId: context.CombatEvent.CardDefinitionId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }

    public static bool TryResolveTarget(
        CardPlayedTriggeredEffectContext context,
        CardPlayedTriggeredEffectTarget target,
        out CombatantId targetId)
    {
        var targetIds = ResolveTargets(context, target);

        if (targetIds.Count == 0)
        {
            targetId = default;
            return false;
        }

        targetId = targetIds.First();
        return true;
    }

    public static bool TryResolveTarget(
        CardPlayedTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector,
        out CombatantId targetId)
    {
        var targetIds = ResolveTargets(context, targetSelector);

        if (targetIds.Count == 0)
        {
            targetId = default;
            return false;
        }

        targetId = targetIds.First();
        return true;
    }
}

public sealed record GainBlockOnCardPlayedTriggeredEffectAction
    : ICardPlayedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardPlayedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCardPlayedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardPlayedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public GainBlockOnCardPlayedTriggeredEffectAction(
        CardPlayedTriggeredEffectTarget target,
        ICardPlayedTriggeredEffectAmountProvider amountProvider)
        : this(CardPlayedTriggeredEffectTargetResolver.ToSelector(target), amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardPlayedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DrawCardsOnCardPlayedTriggeredEffectAction
    : ICardPlayedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardPlayedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnCardPlayedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardPlayedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public DrawCardsOnCardPlayedTriggeredEffectAction(
        CardPlayedTriggeredEffectTarget target,
        ICardPlayedTriggeredEffectAmountProvider amountProvider)
        : this(CardPlayedTriggeredEffectTargetResolver.ToSelector(target), amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            CardPlayedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record DealDamageOnCardPlayedTriggeredEffectAction
    : ICardPlayedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardPlayedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnCardPlayedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardPlayedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public DealDamageOnCardPlayedTriggeredEffectAction(
        CardPlayedTriggeredEffectTarget target,
        ICardPlayedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
        : this(CardPlayedTriggeredEffectTargetResolver.ToSelector(target), amountProvider, kind)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            CardPlayedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record ApplyStatusOnCardPlayedTriggeredEffectAction
    : ICardPlayedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public ICardPlayedTriggeredEffectAmountProvider StacksProvider { get; }

    public ICardPlayedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public ICardPlayedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnCardPlayedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        ICardPlayedTriggeredEffectAmountProvider stacksProvider,
        ICardPlayedTriggeredEffectAmountProvider durationTurnsProvider,
        ICardPlayedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public ApplyStatusOnCardPlayedTriggeredEffectAction(
        CardPlayedTriggeredEffectTarget target,
        StatusDefinitionId statusDefinitionId,
        ICardPlayedTriggeredEffectAmountProvider stacksProvider,
        ICardPlayedTriggeredEffectAmountProvider durationTurnsProvider,
        ICardPlayedTriggeredEffectAmountProvider chargesProvider)
        : this(
            CardPlayedTriggeredEffectTargetResolver.ToSelector(target),
            statusDefinitionId,
            stacksProvider,
            durationTurnsProvider,
            chargesProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardPlayedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            CardPlayedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
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

        var actionBuildContext =
            CardPlayedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardPlayedCombatEvent))
                .OfType<CardPlayedTriggeredEffectDefinition>());

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




















