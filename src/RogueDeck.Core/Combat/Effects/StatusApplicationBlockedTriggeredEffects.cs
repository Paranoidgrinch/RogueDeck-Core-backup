namespace RogueDeck.Core.Combat;

public sealed class StatusApplicationBlockedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(StatusApplicationBlockedCombatEvent);

    public List<IStatusApplicationBlockedTriggeredEffectFilter> Filters { get; } = new();

    public List<IStatusApplicationBlockedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<StatusApplicationBlockedTriggeredEffectContext>> Effects { get; } = new();

    public StatusApplicationBlockedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record StatusApplicationBlockedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    StatusApplicationBlockedCombatEvent CombatEvent,
    CombatantState TargetCombatant);

public interface IStatusApplicationBlockedTriggeredEffectFilter
{
    bool Matches(StatusApplicationBlockedTriggeredEffectContext context);
}

public interface IStatusApplicationBlockedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusApplicationBlockedTriggeredEffectContext context);
}

public interface IStatusApplicationBlockedTriggeredEffectAmountProvider
{
    int ResolveAmount(StatusApplicationBlockedTriggeredEffectContext context);
}

public sealed record FixedStatusApplicationBlockedTriggeredEffectAmount(int Amount)
    : IStatusApplicationBlockedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusApplicationBlockedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(StatusApplicationBlockedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record StatusApplicationBlockedBlockedStatusTriggerFilter(
    StatusDefinitionId BlockedStatusDefinitionId)
    : IStatusApplicationBlockedTriggeredEffectFilter
{
    public bool Matches(StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.BlockedStatusDefinitionId == BlockedStatusDefinitionId;
    }
}

public sealed record StatusApplicationBlockedBlockingStatusTriggerFilter(
    StatusDefinitionId BlockingStatusDefinitionId)
    : IStatusApplicationBlockedTriggeredEffectFilter
{
    public bool Matches(StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.BlockingStatusDefinitionId == BlockingStatusDefinitionId;
    }
}

public static class StatusApplicationBlockedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnStatusApplicationBlockedTriggeredEffectAction
    : IStatusApplicationBlockedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusApplicationBlockedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnStatusApplicationBlockedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusApplicationBlockedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        StatusApplicationBlockedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            StatusApplicationBlockedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class StatusApplicationBlockedTriggeredEffectHandler
    : CombatEventHandler<StatusApplicationBlockedCombatEvent>
{
    private readonly IReadOnlyCollection<StatusApplicationBlockedTriggeredEffectDefinition> _definitions;

    public StatusApplicationBlockedTriggeredEffectHandler(
        params StatusApplicationBlockedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        StatusApplicationBlockedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        var context = new StatusApplicationBlockedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!);

        var actionBuildContext =
            StatusApplicationBlockedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(StatusApplicationBlockedCombatEvent))
                .OfType<StatusApplicationBlockedTriggeredEffectDefinition>());

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
