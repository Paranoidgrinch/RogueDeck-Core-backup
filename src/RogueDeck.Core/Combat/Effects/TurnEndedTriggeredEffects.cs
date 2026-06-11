namespace RogueDeck.Core.Combat;

public sealed class TurnEndedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(TurnEndedCombatEvent);

    public List<ITurnEndedTriggeredEffectFilter> Filters { get; } = new();

    public List<ITurnEndedTriggeredEffectAction> Actions { get; } = new();

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
    : ITurnEndedTriggeredEffectAmountProvider
{
    public int ResolveAmount(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
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

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(
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

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(
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

        if (stacks < 0 || durationTurns < 0 || charges < 0)
            return Array.Empty<IEffectRequest>();

        if (stacks == 0 && durationTurns == 0 && charges == 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(
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

        if (count <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(
            context,
            TargetSelector);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return TriggeredEffectActionRequestFactory.BuildDrawCardsRequests(
            targetIds,
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

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(TurnEndedCombatEvent))
                .OfType<TurnEndedTriggeredEffectDefinition>());

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










