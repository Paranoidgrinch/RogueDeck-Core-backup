namespace RogueDeck.Core.Combat;

public sealed class ResourceGainedTriggeredEffectDefinition : ITriggeredEffectDefinition
{
    public TriggeredEffectDefinitionId Id { get; }

    public Type EventType => typeof(ResourceGainedCombatEvent);

    public List<IResourceGainedTriggeredEffectFilter> Filters { get; } = new();

    public List<IResourceGainedTriggeredEffectAction> Actions { get; } = new();

    public List<ICombatEffectRecipe<ResourceGainedTriggeredEffectContext>> Effects { get; } = new();

    public ResourceGainedTriggeredEffectDefinition(TriggeredEffectDefinitionId id)
    {
        Id = id;
    }
}

public sealed record ResourceGainedTriggeredEffectContext(
    CombatState Combat,
    CombatDefinitionRegistry Registry,
    ResourceGainedCombatEvent CombatEvent,
    CombatantState SourceCombatant);

public interface IResourceGainedTriggeredEffectFilter
{
    bool Matches(ResourceGainedTriggeredEffectContext context);
}

public interface IResourceGainedTriggeredEffectAction
{
    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(ResourceGainedTriggeredEffectContext context);
}

public interface IResourceGainedTriggeredEffectAmountProvider
{
    int ResolveAmount(ResourceGainedTriggeredEffectContext context);
}

public sealed record FixedResourceGainedTriggeredEffectAmount(int Amount)
    : IResourceGainedTriggeredEffectAmountProvider,
      ICombatValueProvider<ResourceGainedTriggeredEffectContext, int>
{
    public int ResolveAmount(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Amount;
    }

    public int Resolve(ResourceGainedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class ResourceGainedAmount
    : IResourceGainedTriggeredEffectAmountProvider,
      ICombatValueProvider<ResourceGainedTriggeredEffectContext, int>
{
    public int ResolveAmount(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.GainedAmount;
    }

    public int Resolve(ResourceGainedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed class ResourceGainedNewCurrentAmount
    : IResourceGainedTriggeredEffectAmountProvider,
      ICombatValueProvider<ResourceGainedTriggeredEffectContext, int>
{
    public int ResolveAmount(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.NewCurrent;
    }

    public int Resolve(ResourceGainedTriggeredEffectContext context)
    {
        return ResolveAmount(context);
    }
}

public sealed record ResourceGainedResourceIdTriggerFilter(ResourceId ResourceId)
    : IResourceGainedTriggeredEffectFilter
{
    public bool Matches(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.ResourceId == ResourceId;
    }
}

public sealed record ResourceGainedMinimumAmountTriggerFilter(int MinimumGainedAmount)
    : IResourceGainedTriggeredEffectFilter
{
    public bool Matches(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.GainedAmount >= MinimumGainedAmount;
    }
}

public sealed class ResourceGainedReachedMaxTriggerFilter
    : IResourceGainedTriggeredEffectFilter
{
    public bool Matches(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.CombatEvent.Max is { } max &&
            context.CombatEvent.NewCurrent >= max;
    }
}

public static class ResourceGainedTriggeredEffectTargetResolver
{
    public static CombatantTargetSelectionContext CreateSelectionContext(
        ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CombatantTargetSelectionContext(
            Combat: context.Combat,
            Source: context.SourceCombatant,
            EventTargetId: context.CombatEvent.CombatantId);
    }

    public static TriggeredEffectActionSource CreateActionSource(
        ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionSource(
            SourceCombatantId: context.CombatEvent.CombatantId);
    }

    public static TriggeredEffectActionBuildContext CreateActionBuildContext(
        ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TriggeredEffectActionBuildContext(
            TargetSelectionContext: CreateSelectionContext(context),
            Source: CreateActionSource(context));
    }
}

public sealed record GainBlockOnResourceGainedTriggeredEffectAction
    : IResourceGainedTriggeredEffectAction
{
    public ICombatantTargetSelector TargetSelector { get; }

    public IResourceGainedTriggeredEffectAmountProvider AmountProvider { get; }

    public GainBlockOnResourceGainedTriggeredEffectAction(
        ICombatantTargetSelector targetSelector,
        IResourceGainedTriggeredEffectAmountProvider amountProvider)
    {
        TargetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        AmountProvider = amountProvider ?? throw new ArgumentNullException(nameof(amountProvider));
    }

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(ResourceGainedTriggeredEffectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var amount = AmountProvider.ResolveAmount(context);

        return TriggeredEffectActionBuilder.BuildGainBlockRequests(
            ResourceGainedTriggeredEffectTargetResolver.CreateActionBuildContext(context),
            TargetSelector,
            amount);
    }
}

public sealed class ResourceGainedTriggeredEffectHandler
    : CombatEventHandler<ResourceGainedCombatEvent>
{
    private readonly IReadOnlyCollection<ResourceGainedTriggeredEffectDefinition> _definitions;

    public ResourceGainedTriggeredEffectHandler(params ResourceGainedTriggeredEffectDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Any(definition => definition is null))
            throw new ArgumentException("Triggered effect definitions cannot contain null.", nameof(definitions));

        _definitions = definitions.ToArray();
    }

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ResourceGainedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var sourceCombatant))
            return;

        var context = new ResourceGainedTriggeredEffectContext(
            Combat: combat,
            Registry: registry,
            CombatEvent: combatEvent,
            SourceCombatant: sourceCombatant!);

        var actionBuildContext =
            ResourceGainedTriggeredEffectTargetResolver.CreateActionBuildContext(context);

        var definitions = _definitions.Concat(
            registry.GetTriggeredEffectDefinitions(typeof(ResourceGainedCombatEvent))
                .OfType<ResourceGainedTriggeredEffectDefinition>());

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
