namespace RogueDeck.Core.Combat;

public sealed class RoundEndedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(RoundEndedCombatEvent);

    public List<IRoundEndedTriggeredEffectFilter> Filters { get; } = new();

    public List<IRoundEndedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<RoundEndedTriggeredEffectContext>> Effects { get; } = new();

    public RoundEndedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record RoundEndedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    RoundEndedCombatEvent CombatEvent,
    CombatantState LastActiveCombatant);

public interface IRoundEndedTriggeredEffectFilter
{
    bool Matches(RoundEndedTriggeredEffectContext context);
}

public interface IRoundEndedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        RoundEndedTriggeredEffectContext context);
}

public interface IRoundEndedTriggeredEffectAmountProvider
{
    int ResolveAmount(RoundEndedTriggeredEffectContext context);
}

public sealed record FixedRoundEndedTriggeredEffectAmount(int Amount)
    : IRoundEndedTriggeredEffectAmountProvider
{
    public int ResolveAmount(RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class RoundEndedCompletedRoundAmount
    : IRoundEndedTriggeredEffectAmountProvider,
      ICombatValueProvider<RoundEndedTriggeredEffectContext, int>
{
    public int ResolveAmount(RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Round;
    }

    public int Resolve(RoundEndedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record RoundEndedMinimumRoundTriggerFilter(int MinimumRound)
    : IRoundEndedTriggeredEffectFilter
{
    public bool Matches(RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Round >= MinimumRound;
    }
}

public sealed record RoundEndedEveryNthRoundTriggerFilter(int Interval)
    : IRoundEndedTriggeredEffectFilter
{
    public bool Matches(RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Interval <= 0)
            throw new InvalidOperationException("Round interval must be greater than zero.");

        return context.CombatEvent.Round % Interval == 0;
    }
}

public static class RoundEndedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.LastActiveCombatant,
            EventTargetId: context.LastActiveCombatant.Id);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.LastActiveCombatant.Id);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnRoundEndedTriggeredEffectAction
    : IRoundEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IRoundEndedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnRoundEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IRoundEndedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            RoundEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record HealOnRoundEndedTriggeredEffectAction
    : IRoundEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IRoundEndedTriggeredEffectAmountProvider AmountProvider { get; }

    public HealOnRoundEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IRoundEndedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector
            ?? throw new ArgumentNullException(nameof(targetSelector));

        AmountProvider = amountProvider
            ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        RoundEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildHealRequests(
            RoundEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class RoundEndedTriggeredEffectHandler
    : CombatEventHandler<RoundEndedCombatEvent>
{
    private readonly IReadOnlyCollection<RoundEndedTriggeredEffectDefinition> _definitions;

    public RoundEndedTriggeredEffectHandler(
        params RoundEndedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        RoundEndedCombatEvent combatEvent)
    {
        if (combatEvent.LastActiveCombatantId is null)
            return;

        if (!combat.TryGetCombatant(
                combatEvent.LastActiveCombatantId.Value,
                out var lastActiveCombatant))
        {
            return;
        }

        var context = new RoundEndedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            LastActiveCombatant: lastActiveCombatant!);

        var actionBuildContext =
            RoundEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(RoundEndedCombatEvent))
                .OfType<RoundEndedTriggeredEffectDefinition>());

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
