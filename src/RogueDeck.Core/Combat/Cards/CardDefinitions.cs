namespace RogueDeck.Core.Combat;

public sealed record ResourceCost(
    ResourceId ResourceId,
    int Amount);

public sealed class CardDefinition
{
    public CardDefinitionId Id { get; }

    public PackageId PackageId { get; }

    public string DisplayNameKey { get; }

    public string DescriptionKey { get; }

    public List<ResourceCost> Costs { get; } = new();

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
