namespace RogueDeck.Core.Combat;

public sealed class HealedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(HealedCombatEvent);

    public List<IHealedTriggeredEffectFilter> Filters { get; } = new();

    public List<IHealedTriggeredEffectAction> Actions { get; } = new();

    public HealedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record HealedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    HealedCombatEvent CombatEvent,
    CombatantState TargetCombatant,
    CombatantState? SourceCombatant);

public interface IHealedTriggeredEffectFilter
{
    bool Matches(HealedTriggeredEffectContext context);
}

public interface IHealedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(HealedTriggeredEffectContext context);
}

public interface IHealedTriggeredEffectAmountProvider
{
    int ResolveAmount(HealedTriggeredEffectContext context);
}

public sealed record FixedHealedTriggeredEffectAmount(int Amount)
    : IHealedTriggeredEffectAmountProvider
{
    public int ResolveAmount(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }
}

public sealed class HealedAmount
    : IHealedTriggeredEffectAmountProvider
{
    public int ResolveAmount(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.HealedAmount;
    }
}

public sealed class HealedRequestedAmount
    : IHealedTriggeredEffectAmountProvider
{
    public int ResolveAmount(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.RequestedAmount;
    }
}

public sealed class HealedHasSourceTriggerFilter
    : IHealedTriggeredEffectFilter
{
    public bool Matches(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.SourceCombatant is not null;
    }
}

public sealed record HealedSourceCombatantTriggerFilter(CombatantId SourceCombatantId)
    : IHealedTriggeredEffectFilter
{
    public bool Matches(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.SourceCombatantId == SourceCombatantId;
    }
}

public sealed record HealedSourceCardTriggerFilter(CardDefinitionId SourceCardId)
    : IHealedTriggeredEffectFilter
{
    public bool Matches(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.SourceCardId == SourceCardId;
    }
}

public sealed record HealedMinimumAmountTriggerFilter(int MinimumHealedAmount)
    : IHealedTriggeredEffectFilter
{
    public bool Matches(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.HealedAmount >= MinimumHealedAmount;
    }
}

public static class HealedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant ?? context.TargetCombatant,
            EventTargetId: context.CombatEvent.TargetCombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.SourceCombatantId,
            SourceCardId: context.CombatEvent.SourceCardId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnHealedTriggeredEffectAction
    : IHealedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IHealedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnHealedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IHealedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(HealedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            HealedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class HealedTriggeredEffectHandler
    : CombatEventHandler<HealedCombatEvent>
{
    private readonly IReadOnlyCollection<HealedTriggeredEffectDefinition> _definitions;

    public HealedTriggeredEffectHandler(params HealedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        HealedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var targetCombatant))
            return;

        CombatantState? sourceCombatant = null;

        if (combatEvent.SourceCombatantId is { } sourceCombatantId &&
            combat.TryGetCombatant(sourceCombatantId, out var foundSourceCombatant))
        {
            sourceCombatant = foundSourceCombatant;
        }

        var context = new HealedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            TargetCombatant: targetCombatant!,
            SourceCombatant: sourceCombatant);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(HealedCombatEvent))
                .OfType<HealedTriggeredEffectDefinition>());

        TriggeredEffectDefinitionRunner.EnqueueEffectRequests(
            combat,
            definitions,
            definition => definition.Filters.All(filter => filter.Matches(context)),
            definition => definition.Actions.SelectMany(action => action.BuildEffectRequests(context)));
    }
}
