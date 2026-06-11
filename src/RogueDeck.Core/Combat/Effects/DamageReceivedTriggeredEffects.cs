namespace RogueDeck.Core.Combat;

public sealed class DamageReceivedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(DamageReceivedCombatEvent);

    public List<IDamageReceivedTriggeredEffectFilter> Filters { get; } = new();

    public List<IDamageReceivedTriggeredEffectAction> Actions { get; } = new();

    public DamageReceivedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record DamageReceivedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    DamageReceivedCombatEvent CombatEvent,
    CombatantState ReceiverCombatant,
    CombatantState? SourceCombatant);

public interface IDamageReceivedTriggeredEffectFilter
{
    bool Matches(DamageReceivedTriggeredEffectContext context);
}

public interface IDamageReceivedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageReceivedTriggeredEffectContext context);
}

public interface IDamageReceivedTriggeredEffectAmountProvider
{
    int ResolveAmount(DamageReceivedTriggeredEffectContext context);
}

public sealed record FixedDamageReceivedTriggeredEffectAmount(int Amount)
    : IDamageReceivedTriggeredEffectAmountProvider
{
    public int ResolveAmount(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class DamageReceivedHealthDamageAmount
    : IDamageReceivedTriggeredEffectAmountProvider
{
    public int ResolveAmount(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.HealthDamage;
    }
}

public sealed class DamageReceivedBlockedDamageAmount
    : IDamageReceivedTriggeredEffectAmountProvider
{
    public int ResolveAmount(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.BlockedDamage;
    }
}

public sealed class DamageReceivedRequestedAmount
    : IDamageReceivedTriggeredEffectAmountProvider
{
    public int ResolveAmount(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.RequestedAmount;
    }
}

public sealed record DamageReceivedReceiverStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IDamageReceivedTriggeredEffectAmountProvider
{
    public int ResolveAmount(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ReceiverCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed record DamageReceivedSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IDamageReceivedTriggeredEffectAmountProvider
{
    public int ResolveAmount(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SourceCombatant is null)
            return 0;

        return context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed class DamageReceivedHasSourceTriggerFilter
    : IDamageReceivedTriggeredEffectFilter
{
    public bool Matches(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record DamageReceivedDamageKindTriggerFilter(DamageKind Kind)
    : IDamageReceivedTriggeredEffectFilter
{
    public bool Matches(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Kind == Kind;
    }
}

public sealed record DamageReceivedMinimumHealthDamageTriggerFilter(int MinimumHealthDamage)
    : IDamageReceivedTriggeredEffectFilter
{
    public bool Matches(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.HealthDamage >= MinimumHealthDamage;
    }
}

public sealed record DamageReceivedReceiverHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IDamageReceivedTriggeredEffectFilter
{
    public bool Matches(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.ReceiverCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public sealed record DamageReceivedSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IDamageReceivedTriggeredEffectFilter
{
    public bool Matches(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null &&
            context.SourceCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public static class DamageReceivedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.ReceiverCombatant,
            EventTargetId: context.CombatEvent.ReceiverCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId,
            SourceCardId: context.CombatEvent.SourceCardId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnDamageReceivedTriggeredEffectAction
    : IDamageReceivedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDamageReceivedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnDamageReceivedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDamageReceivedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            DamageReceivedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnDamageReceivedTriggeredEffectAction
    : IDamageReceivedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDamageReceivedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnDamageReceivedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDamageReceivedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            DamageReceivedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record DrawCardsOnDamageReceivedTriggeredEffectAction
    : IDamageReceivedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDamageReceivedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnDamageReceivedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDamageReceivedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            DamageReceivedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record ApplyStatusOnDamageReceivedTriggeredEffectAction
    : IDamageReceivedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public IDamageReceivedTriggeredEffectAmountProvider StacksProvider { get; }

    public IDamageReceivedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public IDamageReceivedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnDamageReceivedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        IDamageReceivedTriggeredEffectAmountProvider stacksProvider,
        IDamageReceivedTriggeredEffectAmountProvider durationTurnsProvider,
        IDamageReceivedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageReceivedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            DamageReceivedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed class DamageReceivedTriggeredEffectHandler
    : CombatEventHandler<DamageReceivedCombatEvent>
{
    private readonly IReadOnlyCollection<DamageReceivedTriggeredEffectDefinition> _definitions;

    public DamageReceivedTriggeredEffectHandler(params DamageReceivedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DamageReceivedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.ReceiverCombatantId, out var receiverCombatant))
            return;

        CombatantState? sourceCombatant = null;

        if (combatEvent.SourceCombatantId is { } sourceCombatantId &&
            combat.TryGetCombatant(sourceCombatantId, out var foundSourceCombatant))
        {
            sourceCombatant = foundSourceCombatant;
        }

        var context = new DamageReceivedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            ReceiverCombatant: receiverCombatant!,
            SourceCombatant: sourceCombatant);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(DamageReceivedCombatEvent))
                .OfType<DamageReceivedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions.SelectMany(action => action.BuildEffectRequests(context)));
    }
}
