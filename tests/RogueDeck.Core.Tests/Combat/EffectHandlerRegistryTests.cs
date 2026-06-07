using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class EffectHandlerRegistryTests
{
    [Fact]
    public void RegistryCanStoreAndRetrieveEffectRequestHandler()
    {
        var registry = new CombatDefinitionRegistry();
        var handler = new TestEffectRequestHandler();

        registry.RegisterEffectRequestHandler(handler);

        var storedHandler = registry.GetEffectRequestHandler(typeof(TestEffectRequest));

        Assert.Same(handler, storedHandler);
        Assert.True(registry.TryGetEffectRequestHandler(typeof(TestEffectRequest), out var foundHandler));
        Assert.Same(handler, foundHandler);
    }

    [Fact]
    public void RegistryRejectsDuplicateEffectRequestHandler()
    {
        var registry = new CombatDefinitionRegistry();

        var first = new TestEffectRequestHandler();
        var second = new TestEffectRequestHandler();

        registry.RegisterEffectRequestHandler(first);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterEffectRequestHandler(second));
    }

    [Fact]
    public void RegistryThrowsWhenEffectRequestHandlerIsMissing()
    {
        var registry = new CombatDefinitionRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.GetEffectRequestHandler(typeof(TestEffectRequest)));
    }

    private sealed record TestEffectRequest : IEffectRequest;

    private sealed class TestEffectRequestHandler : IEffectRequestHandler
    {
        public Type RequestType => typeof(TestEffectRequest);

        public void Resolve(
            CombatState combat,
            CombatDefinitionRegistry registry,
            IEffectRequest request)
        {
            throw new NotImplementedException();
        }
    }
}