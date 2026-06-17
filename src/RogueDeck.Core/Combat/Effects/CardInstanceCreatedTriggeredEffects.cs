namespace RogueDeck.Core.Combat;

public sealed class CardInstanceCreatedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CardInstanceCreatedCombatEvent);

    public List<ICardInstanceCreatedTriggeredEffectFilter> Filters { get; } = new();

    public List<ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>> Steps { get; } = new();

    public CardInstanceCreatedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CardInstanceCreatedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CardInstanceCreatedCombatEvent CombatEvent,
    CombatantState OwnerCombatant);

public interface ICardInstanceCreatedTriggeredEffectFilter
{
    bool Matches(CardInstanceCreatedTriggeredEffectContext context);
}

public interface ICardInstanceCreatedTriggeredEffectAction
    : ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardInstanceCreatedTriggeredEffectContext context);

    IReadOnlyCollection<IEffectRequest> ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>.BuildEffectRequests(
        CardInstanceCreatedTriggeredEffectContext context,
        TriggeredEffectActionBuildContext buildContext)
        => BuildEffectRequests(context);
}

public interface ICardInstanceCreatedTriggeredEffectAmountProvider
{
    int ResolveAmount(CardInstanceCreatedTriggeredEffectContext context);
}

public sealed record FixedCardInstanceCreatedTriggeredEffectAmount(int Amount)
    : ICardInstanceCreatedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardInstanceCreatedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(CardInstanceCreatedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class CardInstanceCreatedCount
    : ICardInstanceCreatedTriggeredEffectAmountProvider,
      ICombatValueProvider<CardInstanceCreatedTriggeredEffectContext, int>
{
    public int ResolveAmount(CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.CardInstanceIds.Count;
    }

    public int Resolve(CardInstanceCreatedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CardInstanceCreatedCardDefinitionTriggerFilter(CardDefinitionId CardDefinitionId)
    : ICardInstanceCreatedTriggeredEffectFilter
{
    public bool Matches(CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.CardDefinitionId == CardDefinitionId;
    }
}

public sealed record CardInstanceCreatedToZoneTriggerFilter(CardZone ToZone)
    : ICardInstanceCreatedTriggeredEffectFilter
{
    public bool Matches(CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.ToZone == ToZone;
    }
}

public sealed record CardInstanceCreatedMinimumCountTriggerFilter(int MinimumCount)
    : ICardInstanceCreatedTriggeredEffectFilter
{
    public bool Matches(CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.CardInstanceIds.Count >= MinimumCount;
    }
}

public static class CardInstanceCreatedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.OwnerCombatant,
            EventTargetId: context.CombatEvent.CombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.CombatantId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnCardInstanceCreatedTriggeredEffectAction
    : ICardInstanceCreatedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICardInstanceCreatedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCardInstanceCreatedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICardInstanceCreatedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardInstanceCreatedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CardInstanceCreatedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class CardInstanceCreatedTriggeredEffectHandler
    : CombatEventHandler<CardInstanceCreatedCombatEvent>
{
    private readonly IReadOnlyCollection<CardInstanceCreatedTriggeredEffectDefinition> _definitions;

    public CardInstanceCreatedTriggeredEffectHandler(
        params CardInstanceCreatedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardInstanceCreatedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var ownerCombatant))
            return;

        var context = new CardInstanceCreatedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            OwnerCombatant: ownerCombatant!);

        var actionBuildContext =
            CardInstanceCreatedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CardInstanceCreatedCombatEvent))
                .OfType<CardInstanceCreatedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Steps
                .SelectMany(step => step.BuildEffectRequests(context, actionBuildContext)));
    }
}