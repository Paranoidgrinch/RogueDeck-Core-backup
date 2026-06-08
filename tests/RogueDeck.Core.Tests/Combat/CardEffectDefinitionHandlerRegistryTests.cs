using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardEffectDefinitionHandlerRegistryTests
{
    [Fact]
    public void RegistryCanStoreAndRetrieveCardEffectDefinitionHandler()
    {
        var registry = new CombatDefinitionRegistry();
        var handler = new TestCardEffectDefinitionHandler();

        registry.RegisterCardEffectDefinitionHandler(handler);

        var storedHandler = registry.GetCardEffectDefinitionHandler(typeof(TestCardEffectDefinition));

        Assert.Same(handler, storedHandler);
        Assert.True(registry.TryGetCardEffectDefinitionHandler(
            typeof(TestCardEffectDefinition),
            out var foundHandler));
        Assert.Same(handler, foundHandler);
    }

    [Fact]
    public void RegistryRejectsDuplicateCardEffectDefinitionHandler()
    {
        var registry = new CombatDefinitionRegistry();
        var first = new TestCardEffectDefinitionHandler();
        var second = new TestCardEffectDefinitionHandler();

        registry.RegisterCardEffectDefinitionHandler(first);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterCardEffectDefinitionHandler(second));
    }

    [Fact]
    public void RegistryThrowsWhenCardEffectDefinitionHandlerIsMissing()
    {
        var registry = new CombatDefinitionRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.GetCardEffectDefinitionHandler(typeof(TestCardEffectDefinition)));
    }

    [Fact]
    public void StandardCombatPackageRegistersStandardCardEffectDefinitionHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.IsType<DealDamageCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(DealDamageCardEffectDefinition)));

        Assert.IsType<GainBlockCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(GainBlockCardEffectDefinition)));
    }

    private sealed record TestCardEffectDefinition(CardTargetMode TargetMode) : ICardEffectDefinition;

    private sealed class TestCardEffectDefinitionHandler
        : CardEffectDefinitionHandler<TestCardEffectDefinition>
    {
        protected override IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardEffectContext context,
            TestCardEffectDefinition definition)
        {
            return Array.Empty<IEffectRequest>();
        }
    }
}
