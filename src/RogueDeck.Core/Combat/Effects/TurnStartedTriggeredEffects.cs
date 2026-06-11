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
        ArgumentNullException.ThrowIfNull(context);

        var selectionContext = new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.TurnCombatant);

        switch (target)
        {
            case TurnStartedTriggeredEffectTarget.TurnCombatant:
                return CombatantTargetSelectors.Source.ResolveTargets(selectionContext);

            case TurnStartedTriggeredEffectTarget.AllAllies:
                return CombatantTargetSelectors.AllAlliesOfSource.ResolveTargets(selectionContext);

            case TurnStartedTriggeredEffectTarget.AllEnemies:
                return CombatantTargetSelectors.AllEnemiesOfSource.ResolveTargets(selectionContext);

            default:
                throw new InvalidOperationException(
                    $"Unsupported turn started triggered effect target '{target}'.");
        }
    }
}

public sealed record GainBlockOnTurnStartedTriggeredEffectAction(
    TurnStartedTriggeredEffectTarget Target,
    ITurnStartedTriggeredEffectAmountProvider AmountProvider)
    : ITurnStartedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return targetIds
            .Select(targetId => (IEffectRequest)new GainBlockEffectRequest(
                TargetCombatantId: targetId,
                Amount: amount,
                SourceCombatantId: context.CombatEvent.CombatantId))
            .ToArray();
    }
}

public sealed record DealDamageOnTurnStartedTriggeredEffectAction(
    TurnStartedTriggeredEffectTarget Target,
    ITurnStartedTriggeredEffectAmountProvider AmountProvider,
    DamageKind Kind = DamageKind.Direct)
    : ITurnStartedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return targetIds
            .Select(targetId => (IEffectRequest)new DealDamageEffectRequest(
                TargetCombatantId: targetId,
                Amount: amount,
                SourceCombatantId: context.CombatEvent.CombatantId,
                Kind: Kind))
            .ToArray();
    }
}

public sealed record ApplyStatusOnTurnStartedTriggeredEffectAction(
    TurnStartedTriggeredEffectTarget Target,
    StatusDefinitionId StatusDefinitionId,
    ITurnStartedTriggeredEffectAmountProvider StacksProvider,
    ITurnStartedTriggeredEffectAmountProvider DurationTurnsProvider,
    ITurnStartedTriggeredEffectAmountProvider ChargesProvider)
    : ITurnStartedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(StacksProvider);
        ArgumentNullException.ThrowIfNull(DurationTurnsProvider);
        ArgumentNullException.ThrowIfNull(ChargesProvider);

        var stacks = StacksProvider.ResolveAmount(context);
        var durationTurns = DurationTurnsProvider.ResolveAmount(context);
        var charges = ChargesProvider.ResolveAmount(context);

        if (stacks < 0 || durationTurns < 0 || charges < 0)
            return Array.Empty<IEffectRequest>();

        if (stacks == 0 && durationTurns == 0 && charges == 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return targetIds
            .Select(targetId => (IEffectRequest)new ApplyStatusEffectRequest(
                TargetCombatantId: targetId,
                StatusDefinitionId: StatusDefinitionId,
                SourceCombatantId: context.CombatEvent.CombatantId,
                Stacks: stacks,
                DurationTurns: durationTurns,
                Charges: charges))
            .ToArray();
    }
}

public sealed record DrawCardsOnTurnStartedTriggeredEffectAction(
    TurnStartedTriggeredEffectTarget Target,
    ITurnStartedTriggeredEffectAmountProvider AmountProvider)
    : ITurnStartedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnStartedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        var count = AmountProvider.ResolveAmount(context);

        if (count <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnStartedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return targetIds
            .Select(targetId => (IEffectRequest)new DrawCardsEffectRequest(
                CombatantId: targetId,
                Count: count))
            .ToArray();
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

