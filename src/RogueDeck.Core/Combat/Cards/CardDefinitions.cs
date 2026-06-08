namespace RogueDeck.Core.Combat;

public sealed record ResourceCost(
    ResourceId ResourceId,
    int Amount);

public enum CardTargetMode
{
    Self,
    SingleEnemy
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

public sealed class CardDefinition
{
    public CardDefinitionId Id { get; }

    public PackageId PackageId { get; }

    public string DisplayNameKey { get; }

    public string DescriptionKey { get; }

    public List<ResourceCost> Costs { get; } = new();

    public List<ICardEffectDefinition> Effects { get; } = new();

    public List<TagId> Tags { get; } = new();

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
