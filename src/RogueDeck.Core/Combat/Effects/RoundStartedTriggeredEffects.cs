namespace RogueDeck.Core.Combat;

public sealed class RoundStartedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(RoundStartedCombatEvent);

    public List<IRoundStartedTriggeredEffectFilter> Filters { get; } = new();

    public List<IRoundStartedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<RoundStartedTriggeredEffectContext>> Effects { get; } = new();

    public RoundStartedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record RoundStartedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    RoundStartedCombatEvent CombatEvent,
    CombatantState ActiveCombatant);

public interface IRoundStartedTriggeredEffectFilter
{
    bool Matches(RoundStartedTriggeredEffectContext context);
}

public interface IRoundStartedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(RoundStartedTriggeredEffectContext context);
}

public interface IRoundStartedTriggeredEffectAmountProvider
{
    int ResolveAmount(RoundStartedTriggeredEffectContext context);
}

public sealed record FixedRoundStartedTriggeredEffectAmount(int Amount)
    : IRoundStartedTriggeredEffectAmountProvider
{
    public int ResolveAmount(RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class RoundStartedCurrentRoundAmount
    : IRoundStartedTriggeredEffectAmountProvider,
      ICombatValueProvider<RoundStartedTriggeredEffectContext, int>
{
    public int ResolveAmount(RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Round;
    }

    public int Resolve(RoundStartedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record RoundStartedMinimumRoundTriggerFilter(int MinimumRound)
    : IRoundStartedTriggeredEffectFilter
{
    public bool Matches(RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Round >= MinimumRound;
    }
}

public sealed record RoundStartedEveryNthRoundTriggerFilter(int Interval)
    : IRoundStartedTriggeredEffectFilter
{
    public bool Matches(RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Interval <= 0)
            throw new InvalidOperationException("Round interval must be greater than zero.");

        return context.CombatEvent.Round % Interval == 0;
    }
}

public static class RoundStartedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.ActiveCombatant,
            EventTargetId: context.ActiveCombatant.Id);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.ActiveCombatant.Id);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnRoundStartedTriggeredEffectAction
    : IRoundStartedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IRoundStartedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnRoundStartedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IRoundStartedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            RoundStartedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record HealOnRoundStartedTriggeredEffectAction
    : IRoundStartedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IRoundStartedTriggeredEffectAmountProvider AmountProvider { get; }

    public HealOnRoundStartedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IRoundStartedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector
            ?? throw new ArgumentNullException(nameof(targetSelector));

        AmountProvider = amountProvider
            ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        RoundStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildHealRequests(
            RoundStartedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class RoundStartedTriggeredEffectHandler
    : CombatEventHandler<RoundStartedCombatEvent>
{
    private readonly IReadOnlyCollection<RoundStartedTriggeredEffectDefinition> _definitions;

    public RoundStartedTriggeredEffectHandler(params RoundStartedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        RoundStartedCombatEvent combatEvent)
    {
        if (combat.ActiveCombatantId is null)
            return;

        if (!combat.TryGetCombatant(combat.ActiveCombatantId.Value, out var activeCombatant))
            return;

        var context = new RoundStartedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            ActiveCombatant: activeCombatant!);

        var actionBuildContext =
            RoundStartedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(RoundStartedCombatEvent))
                .OfType<RoundStartedTriggeredEffectDefinition>());

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
