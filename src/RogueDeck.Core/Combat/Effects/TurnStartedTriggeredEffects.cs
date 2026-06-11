namespace RogueDeck.Core.Combat;

public sealed class TurnStartedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(TurnStartedCombatEvent);

    public List<ITurnStartedTriggeredEffectFilter> Filters { get; } = new();

    public List<ITurnStartedTriggeredEffectAction> Actions { get; } = new();

    public TurnStartedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record TurnStartedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    TurnStartedCombatEvent CombatEvent,
    CombatantState TurnCombatant);

public interface ITurnStartedTriggeredEffectFilter
{
    bool Matches(TurnStartedTriggeredEffectContext context);
}

public interface ITurnStartedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context);
}

public interface ITurnStartedTriggeredEffectAmountProvider
{
    int ResolveAmount(TurnStartedTriggeredEffectContext context);
}

public sealed record FixedTurnStartedTriggeredEffectAmount(int Amount)
    : ITurnStartedTriggeredEffectAmountProvider
{
    public int ResolveAmount(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed record TurnStartedCombatantStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : ITurnStartedTriggeredEffectAmountProvider
{
    public int ResolveAmount(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed record TurnStartedCombatantHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : ITurnStartedTriggeredEffectFilter
{
    public bool Matches(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public enum TurnStartedTriggeredEffectTarget
{
    TurnCombatant,
    AllAllies,
    AllEnemies
}

public static class TurnStartedTriggeredEffectTargetResolver
{
    public static IReadOnlyCollection<CombatantId> ResolveTargets(
        TurnStartedTriggeredEffectContext context,
        TurnStartedTriggeredEffectTarget target)
    {
        return ResolveTargets(context, ToSelector(target));
    }

    public static IReadOnlyCollection<CombatantId> ResolveTargets(
        TurnStartedTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(targetSelector);

        return targetSelector.ResolveTargets(CreateSelectionContext(context));
    }

    public static ICombatantTargetSelector ToSelector(TurnStartedTriggeredEffectTarget target)
    {
        switch (target)
        {
            case TurnStartedTriggeredEffectTarget.TurnCombatant:
                return CombatantTargetSelectors.Source;

            case TurnStartedTriggeredEffectTarget.AllAllies:
                return CombatantTargetSelectors.AllAlliesOfSource;

            case TurnStartedTriggeredEffectTarget.AllEnemies:
                return CombatantTargetSelectors.AllEnemiesOfSource;

            default:
                throw new InvalidOperationException(
                    $"Unsupported turn started triggered effect target '{target}'.");
        }
    }

    public static CombatantTargetSelectionContext CreateSelectionContext(
        TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.TurnCombatant);
    }
}

public sealed record GainBlockOnTurnStartedTriggeredEffectAction
    : ITurnStartedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnStartedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnTurnStartedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnStartedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public GainBlockOnTurnStartedTriggeredEffectAction(
        TurnStartedTriggeredEffectTarget target,
        ITurnStartedTriggeredEffectAmountProvider amountProvider)
        : this(TurnStartedTriggeredEffectTargetResolver.ToSelector(target), amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(
            context,
            TargetSelector);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return TriggeredEffectActionRequestFactory.BuildGainBlockRequests(
            targetIds,
            amount,
            sourceCombatantId: context.CombatEvent.CombatantId);
    }
}

public sealed record DealDamageOnTurnStartedTriggeredEffectAction
    : ITurnStartedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnStartedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnTurnStartedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnStartedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public DealDamageOnTurnStartedTriggeredEffectAction(
        TurnStartedTriggeredEffectTarget target,
        ITurnStartedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
        : this(TurnStartedTriggeredEffectTargetResolver.ToSelector(target), amountProvider, kind)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(
            context,
            TargetSelector);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return TriggeredEffectActionRequestFactory.BuildDealDamageRequests(
            targetIds,
            amount,
            sourceCombatantId: context.CombatEvent.CombatantId,
            kind: Kind);
    }
}

public sealed record ApplyStatusOnTurnStartedTriggeredEffectAction
    : ITurnStartedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public ITurnStartedTriggeredEffectAmountProvider StacksProvider { get; }

    public ITurnStartedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public ITurnStartedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnTurnStartedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        ITurnStartedTriggeredEffectAmountProvider stacksProvider,
        ITurnStartedTriggeredEffectAmountProvider durationTurnsProvider,
        ITurnStartedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public ApplyStatusOnTurnStartedTriggeredEffectAction(
        TurnStartedTriggeredEffectTarget target,
        StatusDefinitionId statusDefinitionId,
        ITurnStartedTriggeredEffectAmountProvider stacksProvider,
        ITurnStartedTriggeredEffectAmountProvider durationTurnsProvider,
        ITurnStartedTriggeredEffectAmountProvider chargesProvider)
        : this(
            TurnStartedTriggeredEffectTargetResolver.ToSelector(target),
            statusDefinitionId,
            stacksProvider,
            durationTurnsProvider,
            chargesProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        if (stacks < 0 || durationTurns < 0 || charges < 0)
            return Array.Empty<IEffectRequest>();

        if (stacks == 0 && durationTurns == 0 && charges == 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(
            context,
            TargetSelector);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return TriggeredEffectActionRequestFactory.BuildApplyStatusRequests(
            targetIds,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges,
            sourceCombatantId: context.CombatEvent.CombatantId);
    }
}

public sealed record DrawCardsOnTurnStartedTriggeredEffectAction
    : ITurnStartedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public ITurnStartedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnTurnStartedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        ITurnStartedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public DrawCardsOnTurnStartedTriggeredEffectAction(
        TurnStartedTriggeredEffectTarget target,
        ITurnStartedTriggeredEffectAmountProvider amountProvider)
        : this(TurnStartedTriggeredEffectTargetResolver.ToSelector(target), amountProvider)
    {
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = AmountProvider.ResolveAmount(context);

        if (count <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(
            context,
            TargetSelector);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return TriggeredEffectActionRequestFactory.BuildDrawCardsRequests(
            targetIds,
            count);
    }
}

public sealed class TurnStartedTriggeredEffectHandler
    : CombatEventHandler<TurnStartedCombatEvent>
{
    private readonly IReadOnlyCollection<TurnStartedTriggeredEffectDefinition> _definitions;

    public TurnStartedTriggeredEffectHandler(params TurnStartedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnStartedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var turnCombatant))
            return;

        var context = new TurnStartedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TurnCombatant: turnCombatant!);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(TurnStartedCombatEvent))
                .OfType<TurnStartedTriggeredEffectDefinition>());

        foreach (var definition in definitions)
        {
            if (definition.Filters.Any(filter => !filter.Matches(context)))
                continue;

            foreach (var action in definition.Actions)
            {
                foreach (var effectRequest in action.BuildEffectRequests(context))
                    combat.EnqueueEffect(effectRequest);
            }
        }
    }
}










