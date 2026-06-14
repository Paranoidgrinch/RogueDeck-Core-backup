namespace RogueDeck.Core.Combat;

public sealed class StatusAppliedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(StatusAppliedCombatEvent);

    public List<IStatusAppliedTriggeredEffectFilter> Filters { get; } = new();

    public List<IStatusAppliedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<StatusAppliedTriggeredEffectContext>> Effects { get; } = new();

    public StatusAppliedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record StatusAppliedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    StatusAppliedCombatEvent CombatEvent,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant);

public interface IStatusAppliedTriggeredEffectFilter
{
    bool Matches(StatusAppliedTriggeredEffectContext context);
}

public interface IStatusAppliedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusAppliedTriggeredEffectContext context);
}

public interface IStatusAppliedTriggeredEffectAmountProvider
{
    int ResolveAmount(StatusAppliedTriggeredEffectContext context);
}

public sealed record FixedStatusAppliedTriggeredEffectAmount(int Amount)
    : IStatusAppliedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(StatusAppliedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class StatusAppliedStacksAmount
    : IStatusAppliedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Stacks;
    }

    public int Resolve(StatusAppliedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class StatusAppliedDurationTurnsAmount
    : IStatusAppliedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.DurationTurns;
    }

    public int Resolve(StatusAppliedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class StatusAppliedChargesAmount
    : IStatusAppliedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Charges;
    }

    public int Resolve(StatusAppliedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record StatusAppliedTargetStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusAppliedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }

    public int Resolve(StatusAppliedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record StatusAppliedSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IStatusAppliedTriggeredEffectAmountProvider,
      ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>
{
    public int ResolveAmount(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SourceCombatant is null)
            return 0;

        return context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }

    public int Resolve(StatusAppliedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class StatusAppliedHasSourceTriggerFilter
    : IStatusAppliedTriggeredEffectFilter
{
    public bool Matches(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record StatusAppliedStatusDefinitionTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusAppliedTriggeredEffectFilter
{
    public bool Matches(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.StatusDefinitionId == StatusDefinitionId;
    }
}

public sealed record StatusAppliedTargetHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusAppliedTriggeredEffectFilter
{
    public bool Matches(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public sealed record StatusAppliedSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IStatusAppliedTriggeredEffectFilter
{
    public bool Matches(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null &&
            context.SourceCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public static class StatusAppliedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId,
            SourceCardId: context.CombatEvent.SourceCardId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnStatusAppliedTriggeredEffectAction
    : IStatusAppliedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusAppliedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnStatusAppliedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusAppliedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            StatusAppliedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnStatusAppliedTriggeredEffectAction
    : IStatusAppliedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusAppliedTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnStatusAppliedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusAppliedTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            StatusAppliedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record DrawCardsOnStatusAppliedTriggeredEffectAction
    : IStatusAppliedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IStatusAppliedTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnStatusAppliedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IStatusAppliedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            StatusAppliedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record ApplyStatusOnStatusAppliedTriggeredEffectAction
    : IStatusAppliedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public IStatusAppliedTriggeredEffectAmountProvider StacksProvider { get; }

    public IStatusAppliedTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public IStatusAppliedTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnStatusAppliedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        IStatusAppliedTriggeredEffectAmountProvider stacksProvider,
        IStatusAppliedTriggeredEffectAmountProvider durationTurnsProvider,
        IStatusAppliedTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(StatusAppliedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            StatusAppliedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed class StatusAppliedTriggeredEffectHandler
    : CombatEventHandler<StatusAppliedCombatEvent>
{
    private readonly IReadOnlyCollection<StatusAppliedTriggeredEffectDefinition> _definitions;

    public StatusAppliedTriggeredEffectHandler(params StatusAppliedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        StatusAppliedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        CombatantState? sourceCombatant = null;

        if (combatEvent.SourceCombatantId is { } sourceCombatantId &&
            combat.TryGetCombatant(sourceCombatantId, out var foundSourceCombatant))
        {
            sourceCombatant = foundSourceCombatant;
        }

        var context = new StatusAppliedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!,
            SourceCombatant: sourceCombatant);

        var actionBuildContext =
            StatusAppliedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(StatusAppliedCombatEvent))
                .OfType<StatusAppliedTriggeredEffectDefinition>());

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
