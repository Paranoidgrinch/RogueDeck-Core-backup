namespace RogueDeck.Core.Combat;

public sealed class CombatantDownedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(CombatantLifecycleChangedCombatEvent);

    public List<ICombatantDownedTriggeredEffectFilter> Filters { get; } = new();

    public List<ICombatEffectRecipe<CombatantDownedTriggeredEffectContext>> Steps { get; } = new();

    public CombatantDownedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record CombatantDownedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    CombatantLifecycleChangedCombatEvent CombatEvent,
    CombatantState DownedCombatant);

public interface ICombatantDownedTriggeredEffectFilter
{
    bool Matches(CombatantDownedTriggeredEffectContext context);
}

public interface ICombatantDownedTriggeredEffectAction
    : ICombatEffectRecipe<CombatantDownedTriggeredEffectContext>
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CombatantDownedTriggeredEffectContext context);

    IReadOnlyCollection<IEffectRequest> ICombatEffectRecipe<CombatantDownedTriggeredEffectContext>.BuildEffectRequests(
        CombatantDownedTriggeredEffectContext context,
        TriggeredEffectActionBuildContext buildContext)
        => BuildEffectRequests(context);
}

public interface ICombatantDownedTriggeredEffectAmountProvider
{
    int ResolveAmount(CombatantDownedTriggeredEffectContext context);
}

public sealed record FixedCombatantDownedTriggeredEffectAmount(int Amount)
    : ICombatantDownedTriggeredEffectAmountProvider,
      ICombatValueProvider<CombatantDownedTriggeredEffectContext, int>
{
    public int ResolveAmount(CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(CombatantDownedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record CombatantDownedCombatantIdTriggerFilter(CombatantId CombatantId)
    : ICombatantDownedTriggeredEffectFilter
{
    public bool Matches(CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.CombatantId == CombatantId;
    }
}

public sealed record CombatantDownedTeamTriggerFilter(TeamId TeamId)
    : ICombatantDownedTriggeredEffectFilter
{
    public bool Matches(CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.DownedCombatant.TeamId == TeamId;
    }
}

public sealed record CombatantDownedOldStateTriggerFilter(CombatantLifecycleState OldState)
    : ICombatantDownedTriggeredEffectFilter
{
    public bool Matches(CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.OldState == OldState;
    }
}

public static class CombatantDownedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.DownedCombatant,
            EventTargetId: context.CombatEvent.CombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectActionSource.None;
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnCombatantDownedTriggeredEffectAction
    : ICombatantDownedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ICombatantDownedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnCombatantDownedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ICombatantDownedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CombatantDownedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            CombatantDownedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class CombatantDownedTriggeredEffectHandler
    : CombatEventHandler<CombatantLifecycleChangedCombatEvent>
{
    private readonly IReadOnlyCollection<CombatantDownedTriggeredEffectDefinition> _definitions;

    public CombatantDownedTriggeredEffectHandler(
        params CombatantDownedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantLifecycleChangedCombatEvent combatEvent)
    {
        if (combatEvent.NewState != CombatantLifecycleState.Downed)
            return;

        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var downedCombatant))
            return;

        var context = new CombatantDownedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            DownedCombatant: downedCombatant!);

        var actionBuildContext =
            CombatantDownedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(CombatantLifecycleChangedCombatEvent))
                .OfType<CombatantDownedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Steps
                .SelectMany(step => step.BuildEffectRequests(context, actionBuildContext)));
    }
}
