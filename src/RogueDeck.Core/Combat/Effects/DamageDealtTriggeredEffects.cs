namespace RogueDeck.Core.Combat;

public sealed class DamageDealtTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(DamageDealtCombatEvent);

    public List<IDamageDealtTriggeredEffectFilter> Filters { get; } = new();

    public List<ICombatEffectRecipe<DamageDealtTriggeredEffectContext>> Steps { get; } = new();

    public DamageDealtTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record DamageDealtTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    DamageDealtCombatEvent CombatEvent,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant);

public interface IDamageDealtTriggeredEffectFilter
{
    bool Matches(DamageDealtTriggeredEffectContext context);
}

public interface IDamageDealtTriggeredEffectAction
    : ICombatEffectRecipe<DamageDealtTriggeredEffectContext>
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageDealtTriggeredEffectContext context);

    IReadOnlyCollection<IEffectRequest> ICombatEffectRecipe<DamageDealtTriggeredEffectContext>.BuildEffectRequests(
        DamageDealtTriggeredEffectContext context,
        TriggeredEffectActionBuildContext buildContext)
        => BuildEffectRequests(context);
}

public interface IDamageDealtTriggeredEffectAmountProvider
{
    int ResolveAmount(DamageDealtTriggeredEffectContext context);
}

public sealed record FixedDamageDealtTriggeredEffectAmount(int Amount)
    : IDamageDealtTriggeredEffectAmountProvider,
      ICombatValueProvider<DamageDealtTriggeredEffectContext, int>
{
    public int ResolveAmount(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(DamageDealtTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class DamageDealtHealthDamageAmount
    : IDamageDealtTriggeredEffectAmountProvider,
      ICombatValueProvider<DamageDealtTriggeredEffectContext, int>
{
    public int ResolveAmount(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.HealthDamage;
    }

    public int Resolve(DamageDealtTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class DamageDealtBlockedDamageAmount
    : IDamageDealtTriggeredEffectAmountProvider,
      ICombatValueProvider<DamageDealtTriggeredEffectContext, int>
{
    public int ResolveAmount(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.BlockedDamage;
    }

    public int Resolve(DamageDealtTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class DamageDealtRequestedAmount
    : IDamageDealtTriggeredEffectAmountProvider,
      ICombatValueProvider<DamageDealtTriggeredEffectContext, int>
{
    public int ResolveAmount(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.RequestedAmount;
    }

    public int Resolve(DamageDealtTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record DamageDealtSourceStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IDamageDealtTriggeredEffectAmountProvider,
      ICombatValueProvider<DamageDealtTriggeredEffectContext, int>
{
    public int ResolveAmount(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SourceCombatant is null)
            return 0;

        return context.SourceCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }

    public int Resolve(DamageDealtTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record DamageDealtTargetStatusStacksAmount(StatusDefinitionId StatusDefinitionId)
    : IDamageDealtTriggeredEffectAmountProvider,
      ICombatValueProvider<DamageDealtTriggeredEffectContext, int>
{
    public int ResolveAmount(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses
            .Where(status => status.DefinitionId == StatusDefinitionId)
            .Sum(status => status.Stacks);
    }

    public int Resolve(DamageDealtTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class DamageDealtHasSourceTriggerFilter
    : IDamageDealtTriggeredEffectFilter
{
    public bool Matches(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record DamageDealtDamageKindTriggerFilter(DamageKind Kind)
    : IDamageDealtTriggeredEffectFilter
{
    public bool Matches(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Kind == Kind;
    }
}

public sealed record DamageDealtMinimumHealthDamageTriggerFilter(int MinimumHealthDamage)
    : IDamageDealtTriggeredEffectFilter
{
    public bool Matches(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.HealthDamage >= MinimumHealthDamage;
    }
}

public sealed record DamageDealtSourceHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IDamageDealtTriggeredEffectFilter
{
    public bool Matches(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null &&
            context.SourceCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public sealed record DamageDealtTargetHasStatusTriggerFilter(StatusDefinitionId StatusDefinitionId)
    : IDamageDealtTriggeredEffectFilter
{
    public bool Matches(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TargetCombatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId);
    }
}

public static class DamageDealtTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId,
            SourceCardId: context.CombatEvent.SourceCardId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnDamageDealtTriggeredEffectAction
    : IDamageDealtTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDamageDealtTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnDamageDealtTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDamageDealtTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            DamageDealtTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed record DealDamageOnDamageDealtTriggeredEffectAction
    : IDamageDealtTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDamageDealtTriggeredEffectAmountProvider AmountProvider { get; }

    public DamageKind Kind { get; }

    public DealDamageOnDamageDealtTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDamageDealtTriggeredEffectAmountProvider amountProvider,
        DamageKind kind = DamageKind.Direct)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
        Kind = kind;
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDealDamageRequests(
            DamageDealtTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount,
            kind: Kind);
    }
}

public sealed record DrawCardsOnDamageDealtTriggeredEffectAction
    : IDamageDealtTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IDamageDealtTriggeredEffectAmountProvider AmountProvider { get; }

    public DrawCardsOnDamageDealtTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IDamageDealtTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildDrawCardsRequests(
            DamageDealtTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            count);
    }
}

public sealed record ApplyStatusOnDamageDealtTriggeredEffectAction
    : IDamageDealtTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public StatusDefinitionId StatusDefinitionId { get; }

    public IDamageDealtTriggeredEffectAmountProvider StacksProvider { get; }

    public IDamageDealtTriggeredEffectAmountProvider DurationTurnsProvider { get; }

    public IDamageDealtTriggeredEffectAmountProvider ChargesProvider { get; }

    public ApplyStatusOnDamageDealtTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        StatusDefinitionId statusDefinitionId,
        IDamageDealtTriggeredEffectAmountProvider stacksProvider,
        IDamageDealtTriggeredEffectAmountProvider durationTurnsProvider,
        IDamageDealtTriggeredEffectAmountProvider chargesProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        StatusDefinitionId = statusDefinitionId;
        StacksProvider = stacksProvider ?? throw new ArgumentNullException(nameof(stacksProvider));
        DurationTurnsProvider = durationTurnsProvider ?? throw new ArgumentNullException(nameof(durationTurnsProvider));
        ChargesProvider = chargesProvider ?? throw new ArgumentNullException(nameof(chargesProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(DamageDealtTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            DamageDealtTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            StatusDefinitionId,
            stacks,
            durationTurns,
            charges);
    }
}

public sealed class DamageDealtTriggeredEffectHandler
    : CombatEventHandler<DamageDealtCombatEvent>
{
    private readonly IReadOnlyCollection<DamageDealtTriggeredEffectDefinition> _definitions;

    public DamageDealtTriggeredEffectHandler(params DamageDealtTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DamageDealtCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        CombatantState? sourceCombatant = null;

        if (combatEvent.SourceCombatantId is { } sourceCombatantId &&
            combat.TryGetCombatant(sourceCombatantId, out var foundSourceCombatant))
        {
            sourceCombatant = foundSourceCombatant;
        }

        var context = new DamageDealtTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!,
            SourceCombatant: sourceCombatant);

        var actionBuildContext =
            DamageDealtTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(DamageDealtCombatEvent))
                .OfType<DamageDealtTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Steps
                .SelectMany(step => step.BuildEffectRequests(context, actionBuildContext)));
    }
}
