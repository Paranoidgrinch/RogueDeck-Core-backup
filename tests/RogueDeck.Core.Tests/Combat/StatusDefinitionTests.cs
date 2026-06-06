using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusDefinitionTests
{
    [Fact]
    public void RegistryCanStoreAndRetrieveStatusDefinition()
    {
        var registry = new CombatDefinitionRegistry();

        var poison = new StatusDefinition(
            new StatusDefinitionId("standard.poison"),
            new PackageId("standard"),
            displayNameKey: "status.poison.name",
            descriptionKey: "status.poison.description",
            polarity: StatusPolarity.Debuff,
            usesStacks: true,
            showStacksInUi: true);

        poison.Tags.Add(new TagId("debuff"));
        poison.Tags.Add(new TagId("damage_over_time"));

        registry.RegisterStatus(poison);

        var storedPoison = registry.GetStatus(new StatusDefinitionId("standard.poison"));

        Assert.Equal(new PackageId("standard"), storedPoison.PackageId);
        Assert.Equal(StatusPolarity.Debuff, storedPoison.Polarity);
        Assert.True(storedPoison.UsesStacks);
        Assert.True(storedPoison.ShowStacksInUi);
        Assert.Contains(new TagId("damage_over_time"), storedPoison.Tags);
    }

    [Fact]
    public void RegistryRejectsDuplicateStatusDefinition()
    {
        var registry = new CombatDefinitionRegistry();

        var first = new StatusDefinition(
            new StatusDefinitionId("standard.poison"),
            new PackageId("standard"),
            "status.poison.name",
            "status.poison.description");

        var second = new StatusDefinition(
            new StatusDefinitionId("standard.poison"),
            new PackageId("standard"),
            "status.poison.name",
            "status.poison.description");

        registry.RegisterStatus(first);

        Assert.Throws<InvalidOperationException>(() => registry.RegisterStatus(second));
    }
}