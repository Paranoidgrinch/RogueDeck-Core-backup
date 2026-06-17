namespace RogueDeck.Core.Combat;

public sealed class StatusesRemovedByPolarityTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(StatusesRemovedByPolarityCombatEvent);

    public List<IStatusesRemovedByPolarityTriggeredEffectFilter> Filters { get; } = new();

    public List<ICombatEffectRecipe<StatusesRemovedByPolarityTriggeredEffectContext>> Steps { get; } = new();

    public StatusesRemovedByPolarityTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record StatusesRemovedByPolarityTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    StatusesRemovedByPolarityCombatEvent CombatEvent,
    CombatantState TargetCombatant);

public interface IStatusesRemovedByPolarityTriggeredEffectFilter
{
    bool Matches(StatusesRemovedByPolarityTriggeredEffectContext context);
}

public interface IStatusesRemovedByPolarityTriggeredEffectAction
    : ICombatEffectRecipe<StatusesRemovedByPolarityTriggeredEffectContext>
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusesRemovedByPolarityTriggeredEffectContext context);

    IReadOnlyCollection<IEffectRequest> ICombatEffectRecipe<StatusesRemovedByPolarityTriggeredEffectContext>.BuildEffectRequests(
        StatusesRemovedByPolarityTriggeredEffectContext context,
        TriggeredEffectActionBuildContext buildContext)
        => BuildEffectRequests(context);
}

public interface IStatusesRemovedByPolarityTriggeredEffectAmountProvider
{
    int ResolveAmount(StatusesRemovedByPolarityTriggeredEffectContext context);
}

public sealed record FixedStatusesRemovedByPolarityTriggeredEffectAmount(int Amount)
    : IStatusesRemovedByPolarityTriggeredEffectAmountProvider,
    ICombatValueProvider<StatusesRemovedByPolarityTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class StatusesRemovedByPolarityRemovedStatusCount
    : IStatusesRemovedByPolarityTriggeredEffectAmountProvider,
    ICombatValueProvider<StatusesRemovedByPolarityTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusInstanceIds.Count;
    }

    public int Resolve(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record StatusesRemovedByPolarityTriggerFilter(StatusPolarity Polarity)
    : IStatusesRemovedByPolarityTriggeredEffectFilter
{
    public bool Matches(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Polarity == Polarity;
    }
}

public sealed record StatusesRemovedByPolarityTargetTriggerFilter(CombatantId TargetCombatantId)
    : IStatusesRemovedByPolarityTriggeredEffectFilter
{
    public bool Matches(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.TargetCombatantId == TargetCombatantId;
    }
}

public sealed record StatusesRemovedByPolarityMinimumCountTriggerFilter(int MinimumCount)
    : IStatusesRemovedByPolarityTriggeredEffectFilter
{
    public bool Matches(StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusInstanceIds.Count >= MinimumCount;
    }
}

public static class StatusesRemovedByPolarityTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionSource.None;
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction
    : IStatusesRemovedByPolarityTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusesRemovedByPolarityTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusesRemovedByPolarityTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        StatusesRemovedByPolarityTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            StatusesRemovedByPolarityTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class StatusesRemovedByPolarityTriggeredEffectHandler
    : CombatEventHandler<StatusesRemovedByPolarityCombatEvent>
{
    private readonly IReadOnlyCollection<StatusesRemovedByPolarityTriggeredEffectDefinition> _definitions;

    public StatusesRemovedByPolarityTriggeredEffectHandler(
        params StatusesRemovedByPolarityTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        StatusesRemovedByPolarityCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        var context = new StatusesRemovedByPolarityTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!);

        var actionBuildContext =
            StatusesRemovedByPolarityTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(StatusesRemovedByPolarityCombatEvent))
                .OfType<StatusesRemovedByPolarityTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Steps
                .SelectMany(step => step.BuildEffectRequests(context, actionBuildContext)));
    }
}
