namespace RogueDeck.Core.Combat;

public sealed record ResourceCost(
    ResourceId ResourceId,
    int Amount);

public enum CardTargetMode
{
    Self,
    SingleEnemy,
    SingleAlly,
    AllEnemies,
    AllAllies
}

public interface ICardEffectDefinition
{
    CardTargetMode TargetMode { get; }
}

public sealed record DealDamageCardEffectDefinition(
    int Amount,
    CardTargetMode TargetMode
) : ICardEffectDefinition;

public sealed record GainBlockCardEffectDefinition(
    int Amount,
    CardTargetMode TargetMode
) : ICardEffectDefinition;

public sealed record DrawCardsCardEffectDefinition(
    int Count,
    CardTargetMode TargetMode
) : ICardEffectDefinition;

public sealed record DiscardHandCardEffectDefinition(
    CardTargetMode TargetMode,
    bool IncludeRetainedCards = true
) : ICardEffectDefinition;

public sealed record CreateCardInstanceCardEffectDefinition(
    CardDefinitionId CardDefinitionId,
    CardZone ToZone,
    int Count,
    CardTargetMode TargetMode
) : ICardEffectDefinition;
public sealed record MoveAllCardsFromZoneCardEffectDefinition(
    CardZone FromZone,
    CardZone ToZone,
    CardTargetMode TargetMode
) : ICardEffectDefinition;
public sealed record ApplyStatusCardEffectDefinition(
    StatusDefinitionId StatusDefinitionId,
    int Stacks,
    int DurationTurns,
    int Charges,
    CardTargetMode TargetMode
) : ICardEffectDefinition;
public sealed record RemoveStatusCardEffectDefinition(
    StatusDefinitionId StatusDefinitionId,
    CardTargetMode TargetMode
) : ICardEffectDefinition;
public sealed record GainResourceCardEffectDefinition(
    ResourceId ResourceId,
    int Amount,
    CardTargetMode TargetMode,
    int? DefaultMax = null
) : ICardEffectDefinition;
public sealed record HealCardEffectDefinition(
    int Amount,
    CardTargetMode TargetMode
) : ICardEffectDefinition;
public sealed class CardDefinition
{
    public CardDefinitionId Id { get; }
    public PackageId PackageId { get; }
    public string DisplayNameKey { get; }
    public string DescriptionKey { get; }

    public List<ResourceCost> Costs { get; } = new();
    public List<ICardEffectDefinition> Effects { get; } = new();
    public List<TagId> Tags { get; } = new();

        public bool RetainInHandOnTurnEnd { get; set; } = false;

    public CardZone PlayedCardDestinationZone { get; set; } = CardZone.DiscardPile;

    public CardDefinition(
        CardDefinitionId id,
        PackageId packageId,
        string displayNameKey,
        string descriptionKey)
    {
        Id = id;
        PackageId = packageId;
        DisplayNameKey = displayNameKey;
        DescriptionKey = descriptionKey;
    }
}









