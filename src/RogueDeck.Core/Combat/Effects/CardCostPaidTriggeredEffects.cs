namespace RogueDeck.Core.Combat;

public sealed class CardCostPaidTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardCostPaidCombatEvent);

    public List<ICardCostPaidTriggeredEffectFilter> Filters { get; } = new();

    public List<ICombatEffectRecipe<CardCostPaidTriggeredEffectContext>> Steps { get; } = new();

    public CardCostPaidTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CardCostPaidTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardCostPaidCombatEvent CombatEvent,
    CombatantState SourceCombatant);

public interface ICardCostPaidTriggeredEffectFilter
{
    bool Matches(CardCostPaidTriggeredEffectContext context);
}

public interface ICardCostPaidTriggeredEffectAction
    : ICombatEffectRecipe<CardCostPaidTriggeredEffectContext>
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardCostPaidTriggeredEffectContext context);

    IReadOnlyCollection<IEffectRequest> ICombatEffectRecipe<CardCostPaidTriggeredEffectContext>.BuildEffectRequests(
        CardCostPaidTriggeredEffectContext context,
        TriggeredEffectActionBuildContext buildContext)
        => BuildEffectRequests(context);
}

public interface ICardCostPaidTriggeredEffectAmountProvider
{
    int ResolveAmount(CardCostPaidTriggeredEffectContext context);
}

public sealed record FixedCardCostPaidTriggeredEffectAmount(int Amount)
    : ICardCostPaidTriggeredEffectAmountProvider
{
    public int ResolveAmount(CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class CardCostPaidTotalAmount
    : ICardCostPaidTriggeredEffectAmountProvider,
      ICombatValueProvider<CardCostPaidTriggeredEffectContext, int>
{
    public int ResolveAmount(CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Costs.Sum(cost => cost.Amount);
    }

    public int Resolve(CardCostPaidTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardCostPaidCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : ICardCostPaidTriggeredEffectFilter
{
    public bool Matches(CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.CardDefinitionId == CardDefinitionId;
    }
}

public sealed record CardCostPaidResourceIdTriggerFilter(ResourceId ResourceId)
    : ICardCostPaidTriggeredEffectFilter
{
    public bool Matches(CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Costs.Any(cost => cost.ResourceId == ResourceId);
    }
}

public sealed record CardCostPaidMinimumTotalAmountTriggerFilter(int MinimumTotalAmount)
    : ICardCostPaidTriggeredEffectFilter
{
    public bool Matches(CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Costs.Sum(cost => cost.Amount) >= MinimumTotalAmount;
    }
}

public static class CardCostPaidTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant,
            EventTargetId: context.CombatEvent.SourceCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnCardCostPaidTriggeredEffectAction
    : ICardCostPaidTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardCostPaidTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCardCostPaidTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardCostPaidTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardCostPaidTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardCostPaidTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class CardCostPaidTriggeredEffectHandler
    : CombatEventHandler<CardCostPaidCombatEvent>
{
    private readonly IReadOnlyCollection<CardCostPaidTriggeredEffectDefinition> _definitions;

    public CardCostPaidTriggeredEffectHandler(params CardCostPaidTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardCostPaidCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.SourceCombatantId, out var sourceCombatant))
            return;

        var context = new CardCostPaidTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            SourceCombatant: sourceCombatant!);

        var actionBuildContext =
            CardCostPaidTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardCostPaidCombatEvent))
                .OfType<CardCostPaidTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Steps
                .SelectMany(step => step.BuildEffectRequests(context, actionBuildContext)));
    }
}
