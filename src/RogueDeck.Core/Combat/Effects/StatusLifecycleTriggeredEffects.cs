namespace RogueDeck.Core.Combat;

public sealed class StatusRemovedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(StatusRemovedCombatEvent);

    public List<IStatusRemovedTriggeredEffectFilter> Filters { get; } = new();

    public List<IStatusRemovedTriggeredEffectAction> Actions { get; } = new();

    public StatusRemovedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record StatusRemovedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    StatusRemovedCombatEvent CombatEvent,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant);

public interface IStatusRemovedTriggeredEffectFilter
{
    bool Matches(StatusRemovedTriggeredEffectContext context);
}

public interface IStatusRemovedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusRemovedTriggeredEffectContext context);
}

public interface IStatusRemovedTriggeredEffectAmountProvider
{
    int ResolveAmount(StatusRemovedTriggeredEffectContext context);
}

public sealed record FixedStatusRemovedTriggeredEffectAmount(int Amount)
    : IStatusRemovedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class StatusRemovedCountAmount
    : IStatusRemovedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusInstanceIds.Count;
    }
}

public sealed record StatusRemovedTargetStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusRemovedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed record StatusRemovedSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusRemovedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SourceCombatant is null)
            return 0;

        return context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed class StatusRemovedHasSourceTriggerFilter
    : IStatusRemovedTriggeredEffectFilter
{
    public bool Matches(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record StatusRemovedStatusDefinitionTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusRemovedTriggeredEffectFilter
{
    public bool Matches(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusDefinitionId == StatusDefinitionId;
    }
}

public sealed record StatusRemovedTargetHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusRemovedTriggeredEffectFilter
{
    public bool Matches(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public sealed record StatusRemovedSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusRemovedTriggeredEffectFilter
{
    public bool Matches(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null &&
            context.SourceCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public static class StatusRemovedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource();
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }

    public static bool ShouldSuppressMissingSourceTarget(
        StatusRemovedTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectTargetSelectorGuards.ShouldSuppressMissingSourceTarget(
            context.SourceCombatant,
            targetSelector);
    }
}

public sealed record GainBlockOnStatusRemovedTriggeredEffectAction
    : IStatusRemovedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusRemovedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnStatusRemovedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusRemovedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusRemovedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            StatusRemovedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnStatusRemovedTriggeredEffectAction
    : IStatusRemovedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusRemovedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnStatusRemovedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusRemovedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusRemovedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            StatusRemovedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record DrawCardsOnStatusRemovedTriggeredEffectAction
    : IStatusRemovedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusRemovedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnStatusRemovedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusRemovedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusRemovedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            StatusRemovedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record ApplyStatusOnStatusRemovedTriggeredEffectAction
    : IStatusRemovedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public IStatusRemovedTriggeredEffectAmountProvider StacksProvider { get; }

    public IStatusRemovedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public IStatusRemovedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnStatusRemovedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        IStatusRemovedTriggeredEffectAmountProvider stacksProvider,
        IStatusRemovedTriggeredEffectAmountProvider durationTurnsProvider,
        IStatusRemovedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusRemovedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusRemovedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            StatusRemovedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed class StatusRemovedTriggeredEffectHandler
    : CombatEventHandler<StatusRemovedCombatEvent>
{
    private readonly IReadOnlyCollection<StatusRemovedTriggeredEffectDefinition> _definitions;

    public StatusRemovedTriggeredEffectHandler(params StatusRemovedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        StatusRemovedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        var context = new StatusRemovedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!,
            SourceCombatant: null);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(StatusRemovedCombatEvent))
                .OfType<StatusRemovedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions.SelectMany(action => action.BuildEffectRequests(context)));
    }
}

public sealed class StatusExpiredTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(StatusExpiredCombatEvent);

    public List<IStatusExpiredTriggeredEffectFilter> Filters { get; } = new();

    public List<IStatusExpiredTriggeredEffectAction> Actions { get; } = new();

    public StatusExpiredTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record StatusExpiredTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    StatusExpiredCombatEvent CombatEvent,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant);

public interface IStatusExpiredTriggeredEffectFilter
{
    bool Matches(StatusExpiredTriggeredEffectContext context);
}

public interface IStatusExpiredTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusExpiredTriggeredEffectContext context);
}

public interface IStatusExpiredTriggeredEffectAmountProvider
{
    int ResolveAmount(StatusExpiredTriggeredEffectContext context);
}

public sealed record FixedStatusExpiredTriggeredEffectAmount(int Amount)
    : IStatusExpiredTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed record StatusExpiredTargetStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusExpiredTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed record StatusExpiredSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusExpiredTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SourceCombatant is null)
            return 0;

        return context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed class StatusExpiredHasSourceTriggerFilter
    : IStatusExpiredTriggeredEffectFilter
{
    public bool Matches(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record StatusExpiredStatusDefinitionTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusExpiredTriggeredEffectFilter
{
    public bool Matches(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusDefinitionId == StatusDefinitionId;
    }
}

public sealed record StatusExpiredTargetHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusExpiredTriggeredEffectFilter
{
    public bool Matches(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public sealed record StatusExpiredSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusExpiredTriggeredEffectFilter
{
    public bool Matches(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null &&
            context.SourceCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public static class StatusExpiredTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource();
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }

    public static bool ShouldSuppressMissingSourceTarget(
        StatusExpiredTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectTargetSelectorGuards.ShouldSuppressMissingSourceTarget(
            context.SourceCombatant,
            targetSelector);
    }
}

public sealed record GainBlockOnStatusExpiredTriggeredEffectAction
    : IStatusExpiredTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusExpiredTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnStatusExpiredTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusExpiredTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusExpiredTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            StatusExpiredTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnStatusExpiredTriggeredEffectAction
    : IStatusExpiredTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusExpiredTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnStatusExpiredTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusExpiredTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusExpiredTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            StatusExpiredTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record DrawCardsOnStatusExpiredTriggeredEffectAction
    : IStatusExpiredTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusExpiredTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnStatusExpiredTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusExpiredTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusExpiredTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            StatusExpiredTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record ApplyStatusOnStatusExpiredTriggeredEffectAction
    : IStatusExpiredTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public IStatusExpiredTriggeredEffectAmountProvider StacksProvider { get; }

    public IStatusExpiredTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public IStatusExpiredTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnStatusExpiredTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        IStatusExpiredTriggeredEffectAmountProvider stacksProvider,
        IStatusExpiredTriggeredEffectAmountProvider durationTurnsProvider,
        IStatusExpiredTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusExpiredTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusExpiredTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            StatusExpiredTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed class StatusExpiredTriggeredEffectHandler
    : CombatEventHandler<StatusExpiredCombatEvent>
{
    private readonly IReadOnlyCollection<StatusExpiredTriggeredEffectDefinition> _definitions;

    public StatusExpiredTriggeredEffectHandler(params StatusExpiredTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        StatusExpiredCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        var context = new StatusExpiredTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!,
            SourceCombatant: null);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(StatusExpiredCombatEvent))
                .OfType<StatusExpiredTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions.SelectMany(action => action.BuildEffectRequests(context)));
    }
}

public sealed class StatusMergedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(StatusMergedCombatEvent);

    public List<IStatusMergedTriggeredEffectFilter> Filters { get; } = new();

    public List<IStatusMergedTriggeredEffectAction> Actions { get; } = new();

    public StatusMergedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record StatusMergedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    StatusMergedCombatEvent CombatEvent,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant);

public interface IStatusMergedTriggeredEffectFilter
{
    bool Matches(StatusMergedTriggeredEffectContext context);
}

public interface IStatusMergedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusMergedTriggeredEffectContext context);
}

public interface IStatusMergedTriggeredEffectAmountProvider
{
    int ResolveAmount(StatusMergedTriggeredEffectContext context);
}

public sealed record FixedStatusMergedTriggeredEffectAmount(int Amount)
    : IStatusMergedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class StatusMergedStacksAmount
    : IStatusMergedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Stacks;
    }
}

public sealed class StatusMergedDurationTurnsAmount
    : IStatusMergedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.DurationTurns;
    }
}

public sealed class StatusMergedChargesAmount
    : IStatusMergedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Charges;
    }
}

public sealed record StatusMergedTargetStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusMergedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed record StatusMergedSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusMergedTriggeredEffectAmountProvider
{
    public int ResolveAmount(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SourceCombatant is null)
            return 0;

        return context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }
}

public sealed class StatusMergedHasSourceTriggerFilter
    : IStatusMergedTriggeredEffectFilter
{
    public bool Matches(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record StatusMergedStatusDefinitionTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusMergedTriggeredEffectFilter
{
    public bool Matches(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusDefinitionId == StatusDefinitionId;
    }
}

public sealed record StatusMergedTargetHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusMergedTriggeredEffectFilter
{
    public bool Matches(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public sealed record StatusMergedSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusMergedTriggeredEffectFilter
{
    public bool Matches(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null &&
            context.SourceCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public static class StatusMergedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId,
            SourceCardId: context.CombatEvent.SourceCardId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }

    public static bool ShouldSuppressMissingSourceTarget(
        StatusMergedTriggeredEffectContext context,
        ICombatantTargetSelector targetSelector)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TriggeredEffectTargetSelectorGuards.ShouldSuppressMissingSourceTarget(
            context.SourceCombatant,
            targetSelector);
    }
}

public sealed record GainBlockOnStatusMergedTriggeredEffectAction
    : IStatusMergedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusMergedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnStatusMergedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusMergedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusMergedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            StatusMergedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnStatusMergedTriggeredEffectAction
    : IStatusMergedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusMergedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnStatusMergedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusMergedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusMergedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            StatusMergedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record DrawCardsOnStatusMergedTriggeredEffectAction
    : IStatusMergedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusMergedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnStatusMergedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusMergedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusMergedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            StatusMergedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record ApplyStatusOnStatusMergedTriggeredEffectAction
    : IStatusMergedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public IStatusMergedTriggeredEffectAmountProvider StacksProvider { get; }

    public IStatusMergedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public IStatusMergedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnStatusMergedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        IStatusMergedTriggeredEffectAmountProvider stacksProvider,
        IStatusMergedTriggeredEffectAmountProvider durationTurnsProvider,
        IStatusMergedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusMergedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (StatusMergedTriggeredEffectTargetResolver.ShouldSuppressMissingSourceTarget(context, TargetSelector))
            return Array.Empty<IEffectRequest>();

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            StatusMergedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed class StatusMergedTriggeredEffectHandler
    : CombatEventHandler<StatusMergedCombatEvent>
{
    private readonly IReadOnlyCollection<StatusMergedTriggeredEffectDefinition> _definitions;

    public StatusMergedTriggeredEffectHandler(params StatusMergedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        StatusMergedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        CombatantState? sourceCombatant = null;

        if (combatEvent.SourceCombatantId is { } sourceCombatantId &&
            combat.TryGetCombatant(sourceCombatantId, out var foundSourceCombatant))
        {
            sourceCombatant = foundSourceCombatant;
        }

        var context = new StatusMergedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!,
            SourceCombatant: sourceCombatant);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(StatusMergedCombatEvent))
                .OfType<StatusMergedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions.SelectMany(action => action.BuildEffectRequests(context)));
    }
}
