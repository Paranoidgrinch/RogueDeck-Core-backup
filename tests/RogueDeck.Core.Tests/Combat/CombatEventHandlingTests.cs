using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatEventHandlingTests
{
    [Fact]
    public void RegistryCanStoreMultipleCombatEventHandlersForSameEventType()
    {
        var registry = new CombatDefinitionRegistry();

        var first = new FirstTestCombatEventHandler();
        var second = new SecondTestCombatEventHandler();

        registry.RegisterCombatEventHandler(first);
        registry.RegisterCombatEventHandler(second);

        var handlers = registry.GetCombatEventHandlers(typeof(TestCombatEvent)).ToArray();

        Assert.Equal(2, handlers.Length);
        Assert.Contains(first, handlers);
        Assert.Contains(second, handlers);
    }

    [Fact]
    public void RegistryReturnsEmptyCollectionWhenNoCombatEventHandlersAreRegistered()
    {
        var registry = new CombatDefinitionRegistry();

        var handlers = registry.GetCombatEventHandlers(typeof(TestCombatEvent));

        Assert.Empty(handlers);
    }

    [Fact]
    public void RegistryRejectsDuplicateCombatEventHandlerTypeForSameEventType()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCombatEventHandler(new FirstTestCombatEventHandler());

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterCombatEventHandler(new FirstTestCombatEventHandler()));
    }

    [Fact]
    public void CombatEventHandlerRejectsWrongEventType()
    {
        var handler = new FirstTestCombatEventHandler();

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var registry = new CombatDefinitionRegistry();

        Assert.Throws<ArgumentException>(() =>
            handler.Handle(combat, registry, new OtherTestCombatEvent()));
    }

    [Fact]
    public void CombatEventQueueProcessorRunsRegisteredEventHandlers()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCombatEventHandler(new AddLogOnTestEventHandler());

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        combat.EnqueueEvent(new TestCombatEvent());

        var processor = new CombatEventQueueProcessor();

        processor.ResolvePendingEvents(combat, registry);

        Assert.Equal(0, combat.PendingEventCount);
        Assert.Single(combat.CombatLog);
        Assert.Equal("TestEventHandled", combat.CombatLog[0].Type);
    }

    [Fact]
    public void CombatEventQueueProcessorAllowsEventHandlersToEnqueueEffects()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        registry.RegisterCombatEventHandler(new GainBlockOnTestEventHandler());

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var heroId = new CombatantId("hero_001");

        var hero = new CombatantState(
            heroId,
            new CombatantDefinitionId("standard.hero"),
            "combatant.hero",
            new TeamId("player"),
            new HealthState(current: 20, max: 20));

        combat.AddCombatant(hero);
        combat.EnqueueEvent(new TestCombatEvent());

        var eventProcessor = new CombatEventQueueProcessor();
        var effectProcessor = new CombatEffectQueueProcessor();

        eventProcessor.ResolvePendingEvents(combat, registry);
        effectProcessor.ResolvePendingEffects(combat, registry);

        var storedHero = combat.GetCombatant(heroId);
        var block = storedHero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(7, block.Current);
    }

    [Fact]
    public void CombatEventQueueProcessorStopsAfterMaximumEventLimit()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCombatEventHandler(new EndlessEventHandler());

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        combat.EnqueueEvent(new EndlessTestCombatEvent());

        var processor = new CombatEventQueueProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.ResolvePendingEvents(
                combat,
                registry,
                maxEventsToResolve: 3));
    }

    private sealed record TestCombatEvent : ICombatEvent;

    private sealed record OtherTestCombatEvent : ICombatEvent;

    private sealed record EndlessTestCombatEvent : ICombatEvent;

    private sealed class FirstTestCombatEventHandler : CombatEventHandler<TestCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TestCombatEvent combatEvent)
        {
        }
    }

    private sealed class SecondTestCombatEventHandler : CombatEventHandler<TestCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TestCombatEvent combatEvent)
        {
        }
    }

    private sealed class AddLogOnTestEventHandler : CombatEventHandler<TestCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TestCombatEvent combatEvent)
        {
            combat.AddLogEntry(
                "TestEventHandled",
                "Handled test combat event.");
        }
    }

    private sealed class GainBlockOnTestEventHandler : CombatEventHandler<TestCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TestCombatEvent combatEvent)
        {
            combat.EnqueueEffect(
                new GainBlockEffectRequest(
                    TargetCombatantId: new CombatantId("hero_001"),
                    Amount: 7));
        }
    }

    private sealed class EndlessEventHandler : CombatEventHandler<EndlessTestCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            EndlessTestCombatEvent combatEvent)
        {
            combat.EnqueueEvent(new EndlessTestCombatEvent());
        }
    }
}
