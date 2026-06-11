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
        ArgumentNullException.ThrowIfNull(context);

        switch (target)
        {
            case TurnEndedTriggeredEffectTarget.TurnCombatant:
                return context.TurnCombatant.IsAlive
                    ? new[] { context.TurnCombatant.Id }
                    : Array.Empty<CombatantId>();

            case TurnEndedTriggeredEffectTarget.AllAllies:
                return context.Combat.Combatants
                    .Where(combatant =>
                        combatant.TeamId == context.TurnCombatant.TeamId &&
                        combatant.IsAlive)
                    .Select(combatant => combatant.Id)
                    .ToArray();

            case TurnEndedTriggeredEffectTarget.AllEnemies:
                return context.Combat.Combatants
                    .Where(combatant =>
                        combatant.TeamId != context.TurnCombatant.TeamId &&
                        combatant.IsAlive)
                    .Select(combatant => combatant.Id)
                    .ToArray();

            default:
                throw new InvalidOperationException(
                    $"Unsupported turn ended triggered effect target '{target}'.");
        }
    }
}

public sealed record GainBlockOnTurnEndedTriggeredEffectAction(
    TurnEndedTriggeredEffectTarget Target,
    ITurnEndedTriggeredEffectAmountProvider AmountProvider)
    : ITurnEndedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

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

public sealed record DealDamageOnTurnEndedTriggeredEffectAction(
    TurnEndedTriggeredEffectTarget Target,
    ITurnEndedTriggeredEffectAmountProvider AmountProvider,
    DamageKind Kind = DamageKind.Direct)
    : ITurnEndedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        var amount = AmountProvider.ResolveAmount(context);

        if (amount <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

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

public sealed record ApplyStatusOnTurnEndedTriggeredEffectAction(
    TurnEndedTriggeredEffectTarget Target,
    StatusDefinitionId StatusDefinitionId,
    ITurnEndedTriggeredEffectAmountProvider StacksProvider,
    ITurnEndedTriggeredEffectAmountProvider DurationTurnsProvider,
    ITurnEndedTriggeredEffectAmountProvider ChargesProvider)
    : ITurnEndedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
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

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

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

public sealed record DrawCardsOnTurnEndedTriggeredEffectAction(
    TurnEndedTriggeredEffectTarget Target,
    ITurnEndedTriggeredEffectAmountProvider AmountProvider)
    : ITurnEndedTriggeredEffectAction
{
    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(TurnEndedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(AmountProvider);

        var count = AmountProvider.ResolveAmount(context);

        if (count <= 0)
            return Array.Empty<IEffectRequest>();

        var targetIds = TurnEndedTriggeredEffectTargetResolver.ResolveTargets(context, Target);

        if (targetIds.Count == 0)
            return Array.Empty<IEffectRequest>();

        return targetIds
            .Select(targetId => (IEffectRequest)new DrawCardsEffectRequest(
                CombatantId: targetId,
                Count: count))
            .ToArray();
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
