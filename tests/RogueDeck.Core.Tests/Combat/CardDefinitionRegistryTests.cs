using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardDefinitionRegistryTests
{
    [Fact]
    public void RegistryCanStoreAndRetrieveCardDefinition()
    {
        var registry = new CombatDefinitionRegistry();

        var card = new CardDefinition(
            new CardDefinitionId("test.strike"),
            new PackageId("test"),
            displayNameKey: "card.test.strike.name",
            descriptionKey: "card.test.strike.description");

        card.Costs.Add(new ResourceCost(new ResourceId("standard.energy"), 1));
        card.Tags.Add(new TagId("attack"));

        registry.RegisterCard(card);

        var storedCard = registry.GetCard(new CardDefinitionId("test.strike"));

        Assert.Same(card, storedCard);
        Assert.True(registry.TryGetCard(new CardDefinitionId("test.strike"), out var foundCard));
        Assert.Same(card, foundCard);
        Assert.Single(storedCard.Costs);
        Assert.Single(storedCard.Tags);
    }

    [Fact]
    public void RegistryRejectsDuplicateCardDefinition()
    {
        var registry = new CombatDefinitionRegistry();

        var first = new CardDefinition(
            new CardDefinitionId("test.strike"),
            new PackageId("test"),
            displayNameKey: "card.test.strike.name",
            descriptionKey: "card.test.strike.description");

        var second = new CardDefinition(
            new CardDefinitionId("test.strike"),
            new PackageId("test"),
            displayNameKey: "card.test.other_strike.name",
            descriptionKey: "card.test.other_strike.description");

        registry.RegisterCard(first);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterCard(second));
    }

    [Fact]
    public void RegistryThrowsWhenCardDefinitionIsMissing()
    {
        var registry = new CombatDefinitionRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.GetCard(new CardDefinitionId("missing.card")));
    }

    [Fact]
    public void RegistryReturnsFalseWhenCardDefinitionIsMissing()
    {
        var registry = new CombatDefinitionRegistry();

        Assert.False(registry.TryGetCard(new CardDefinitionId("missing.card"), out var card));
        Assert.Null(card);
    }
}
