namespace RogueDeck.Core.Combat;

public sealed class TurnEndedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(TurnEndedCombatEvent);

    public List<ITurnEndedTriggeredEffectFilter> Filters { get; } = new();

    public List<ITurnEndedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<TurnEndedTriggeredEffectContext>> Effects { get; } = new();

    public TurnEndedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record TurnEndedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    TurnEndedCombatEvent CombatEvent,
    CombatantState TurnCombatant);

public interface ITurnEndedTriggeredEffectFilter
{
    bool Matches(TurnEndedTriggeredEffectContext context);
}

public interface ITurnEndedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context);
}

public interface ITurnEndedTriggeredEffectAmountProvider
{
    int ResolveAmount(TurnEndedTriggeredEffectContext context);
}

public sealed record FixedTurnEndedTriggeredEffectAmount(int Amount)
    : ITurnEndedTriggeredEffectAmountProvider
{
    public int ResolveAmount(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed record TurnEndedCombatantStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : ITurnEndedTriggeredEffectAmountProvider,
      ICombatValueProvider<TurnEndedTriggeredEffectContext, int>
{
    public int ResolveAmount(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }

    public int Resolve(TurnEndedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record TurnEndedCombatantHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : ITurnEndedTriggeredEffectFilter
{
    public bool Matches(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public enum TurnEndedTriggeredEffectTarget
{
    TurnCombatant,
    AllAllies,
    AllEnemies
}

public static class TurnEndedTriggeredEffectTargetResolver
{
    public static IReadOnlyCollection<CombatantId> ResolveTargets(
        TurnEndedTriggeredEffectContext context,
        TurnEndedTriggeredEffectTarget target)
    {
        return ResolveTargets(context, ToSelector(target));
    }

    public static IReadOnlyCollection<CombatantId> ResolveTargets(
        TurnEndedTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(targetSelector);

        return targetSelector.ResolveTargets(CreateSelectionContext(context));
    }

    public static ICombatantTargetSelector ToSelector(TurnEndedTriggeredEffectTarget target)
    {
        switch (target)
        {
            case TurnEndedTriggeredEffectTarget.TurnCombatant:
                return CombatantTargetSelectors.Source;

            case TurnEndedTriggeredEffectTarget.AllAllies:
                return CombatantTargetSelectors.AllAlliesOfSource;

            case TurnEndedTriggeredEffectTarget.AllEnemies:
                return CombatantTargetSelectors.AllEnemiesOfSource;

            default:
                throw new InvalidOperationException(
                    $"Unsupported turn ended triggered effect target '{target}'.");
        }
    }

    public static CombatantTargetSelectionContext CreateSelectionContext(
        TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.TurnCombatant);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.CombatantId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnTurnEndedTriggeredEffectAction
    : ITurnEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnEndedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnTurnEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnEndedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public GainBlockOnTurnEndedTriggeredEffectAction(
        TurnEndedTriggeredEffectTarget target,
        ITurnEndedTriggeredEffectAmountProvider amountProvider)
        : this(TurnEndedTriggeredEffectTargetResolver.ToSelector(target), amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            TurnEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record HealOnTurnEndedTriggeredEffectAction
    : ITurnEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnEndedTriggeredEffectAmountProvider AmountProvider { get; }

    public HealOnTurnEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnEndedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public HealOnTurnEndedTriggeredEffectAction(
        TurnEndedTriggeredEffectTarget target,
        ITurnEndedTriggeredEffectAmountProvider amountProvider)
        : this(
            TurnEndedTriggeredEffectTargetResolver.ToSelector(target),
            amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildHealRequests(
            TurnEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnTurnEndedTriggeredEffectAction
    : ITurnEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnEndedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnTurnEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnEndedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public DealDamageOnTurnEndedTriggeredEffectAction(
        TurnEndedTriggeredEffectTarget target,
        ITurnEndedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
        : this(TurnEndedTriggeredEffectTargetResolver.ToSelector(target), amountProvider, kind)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            TurnEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record ApplyStatusOnTurnEndedTriggeredEffectAction
    : ITurnEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public ITurnEndedTriggeredEffectAmountProvider StacksProvider { get; }

    public ITurnEndedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public ITurnEndedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnTurnEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        ITurnEndedTriggeredEffectAmountProvider stacksProvider,
        ITurnEndedTriggeredEffectAmountProvider durationTurnsProvider,
        ITurnEndedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public ApplyStatusOnTurnEndedTriggeredEffectAction(
        TurnEndedTriggeredEffectTarget target,
        StatusDefinitionId statusDefinitionId,
        ITurnEndedTriggeredEffectAmountProvider stacksProvider,
        ITurnEndedTriggeredEffectAmountProvider durationTurnsProvider,
        ITurnEndedTriggeredEffectAmountProvider chargesProvider)
        : this(
            TurnEndedTriggeredEffectTargetResolver.ToSelector(target),
            statusDefinitionId,
            stacksProvider,
            durationTurnsProvider,
            chargesProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            TurnEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed record DrawCardsOnTurnEndedTriggeredEffectAction
    : ITurnEndedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnEndedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnTurnEndedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnEndedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public DrawCardsOnTurnEndedTriggeredEffectAction(
        TurnEndedTriggeredEffectTarget target,
        ITurnEndedTriggeredEffectAmountProvider amountProvider)
        : this(TurnEndedTriggeredEffectTargetResolver.ToSelector(target), amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            TurnEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed class TurnEndedTriggeredEffectHandler
    : CombatEventHandler<TurnEndedCombatEvent>
{
    private readonly IReadOnlyCollection<TurnEndedTriggeredEffectDefinition> _definitions;

    public TurnEndedTriggeredEffectHandler(params TurnEndedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnEndedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var turnCombatant))
            return;

        var context = new TurnEndedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TurnCombatant: turnCombatant!);

        var actionBuildContext =
            TurnEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(TurnEndedCombatEvent))
                .OfType<TurnEndedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(
                filter => filter.Matches(context)),
            definition => definition.Actions
                .SelectMany(
                    action => action.BuildEffectRequests(context))
                .Concat(definition.Effects.SelectMany(
                    effect => effect.BuildEffectRequests(
                        context,
                        actionBuildContext))));
    }
}



















