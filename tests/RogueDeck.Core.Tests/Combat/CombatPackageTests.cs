using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatPackageTests
{
    [Fact]
    public void StandardCombatPackageRegistersStatusDefinitions()
    {
        var registry = new CombatDefinitionRegistry();
        var package = new StandardCombatPackage();

        package.RegisterDefinitions(registry);

        Assert.True(registry.TryGetStatus(new StatusDefinitionId("standard.poison"), out var poison));
        Assert.True(registry.TryGetStatus(new StatusDefinitionId("standard.weak"), out var weak));
        Assert.True(registry.TryGetStatus(new StatusDefinitionId("standard.strength"), out var strength));
        Assert.True(registry.TryGetStatus(new StatusDefinitionId("standard.thorns"), out var thorns));
        Assert.True(registry.TryGetStatus(new StatusDefinitionId("standard.stun"), out var stun));

        Assert.Equal(StatusPolarity.Debuff, poison!.Polarity);
        Assert.True(poison.UsesStacks);
        Assert.True(poison.ShowStacksInUi);

        Assert.Equal(StatusPolarity.Debuff, weak!.Polarity);
        Assert.True(weak.UsesDuration);
        Assert.True(weak.ShowDurationInUi);

        Assert.Equal(StatusPolarity.Buff, strength!.Polarity);
        Assert.True(strength.UsesStacks);

        Assert.Equal(StatusPolarity.Buff, thorns!.Polarity);
        Assert.Contains(new TagId("triggered_damage"), thorns.Tags);

        Assert.Equal(StatusPolarity.Debuff, stun!.Polarity);
        Assert.Contains(new TagId("control"), stun.Tags);
    }
}