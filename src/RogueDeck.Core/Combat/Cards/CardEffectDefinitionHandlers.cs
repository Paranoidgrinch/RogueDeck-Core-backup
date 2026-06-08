namespace RogueDeck.Core.Combat;

public sealed record CardEffectContext(
    CombatState Combat,
    CardDefinition Card,
    CombatantState Source,
    CombatantId? RequestedTargetId)
{
    public CombatantId ResolveTarget(CardTargetMode targetMode)
    {
        switch (targetMode)
        {
            case CardTargetMode.Self:
                return Source.Id;

            case CardTargetMode.SingleEnemy:
            {
                if (RequestedTargetId is null)
                    throw new InvalidOperationException("Card requires a single enemy target.");

                var target = Combat.GetCombatant(RequestedTargetId.Value);

                if (!target.IsAlive)
                    throw new InvalidOperationException(
                        $"Target combatant '{RequestedTargetId}' is not alive.");

                if (target.TeamId == Source.TeamId)
                    throw new InvalidOperationException(
                        $"Target combatant '{RequestedTargetId}' is not an enemy of '{Source.Id}'.");

                return target.Id;
            }

            default:
                throw new InvalidOperationException(
                    $"Unsupported card target mode '{targetMode}'.");
        }
    }
}

public interface ICardEffectDefinitionHandler
{
    Type DefinitionType { get; }

    IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        ICardEffectDefinition definition);
}

public abstract class CardEffectDefinitionHandler<TDefinition> : ICardEffectDefinitionHandler
    where TDefinition : ICardEffectDefinition
{
    public Type DefinitionType => typeof(TDefinition);

    public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        ICardEffectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not TDefinition typedDefinition)
            throw new InvalidOperationException(
                $"Card effect definition handler '{GetType().Name}' cannot handle definition type '{definition.GetType().Name}'.");

        return BuildEffectRequests(context, typedDefinition);
    }

    protected abstract IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        TDefinition definition);
}

public sealed class DealDamageCardEffectDefinitionHandler
    : CardEffectDefinitionHandler<DealDamageCardEffectDefinition>
{
    protected override IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        DealDamageCardEffectDefinition definition)
    {
        var targetId = context.ResolveTarget(definition.TargetMode);

        return new IEffectRequest[]
        {
            new DealDamageEffectRequest(
                TargetCombatantId: targetId,
                Amount: definition.Amount,
                SourceCombatantId: context.Source.Id,
                SourceCardId: context.Card.Id,
                Kind: DamageKind.Direct)
        };
    }
}

public sealed class GainBlockCardEffectDefinitionHandler
    : CardEffectDefinitionHandler<GainBlockCardEffectDefinition>
{
    protected override IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        GainBlockCardEffectDefinition definition)
    {
        var targetId = context.ResolveTarget(definition.TargetMode);

        return new IEffectRequest[]
        {
            new GainBlockEffectRequest(
                TargetCombatantId: targetId,
                Amount: definition.Amount,
                SourceCombatantId: context.Source.Id,
                SourceCardId: context.Card.Id)
        };
    }
}

public sealed class DrawCardsCardEffectDefinitionHandler
    : CardEffectDefinitionHandler<DrawCardsCardEffectDefinition>
{
    protected override IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        DrawCardsCardEffectDefinition definition)
    {
        var targetId = context.ResolveTarget(definition.TargetMode);

        return new IEffectRequest[]
        {
            new DrawCardsEffectRequest(
                CombatantId: targetId,
                Count: definition.Count)
        };
    }
}

public sealed class DiscardHandCardEffectDefinitionHandler
    : CardEffectDefinitionHandler<DiscardHandCardEffectDefinition>
{
    protected override IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        DiscardHandCardEffectDefinition definition)
    {
        var targetId = context.ResolveTarget(definition.TargetMode);

        return new IEffectRequest[]
        {
            new DiscardHandEffectRequest(
                CombatantId: targetId)
        };
    }
}

public sealed class CreateCardInstanceCardEffectDefinitionHandler
    : CardEffectDefinitionHandler<CreateCardInstanceCardEffectDefinition>
{
    protected override IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
        CardEffectContext context,
        CreateCardInstanceCardEffectDefinition definition)
    {
        var targetId = context.ResolveTarget(definition.TargetMode);

        return new IEffectRequest[]
        {
            new CreateCardInstanceEffectRequest(
                CombatantId: targetId,
                CardDefinitionId: definition.CardDefinitionId,
                ToZone: definition.ToZone,
                Count: definition.Count)
        };
    }
}

