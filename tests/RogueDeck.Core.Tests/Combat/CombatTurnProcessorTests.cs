using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatTurnProcessorTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StartCurrentTurnAddsTurnStartedLogEntry()
    {
        var registry = CreateEmptyRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        Assert.Single(combat.CombatLog);
        Assert.Equal(StandardCombatLogTypes.TurnStarted, combat.CombatLog[0].Type);
        Assert.Equal(HeroId, combat.ActiveCombatantId);
        Assert.Equal(CombatTurnPhase.TurnInProgress, combat.TurnPhase);
    }

    [Fact]
    public void StartCurrentTurnCannotBeCalledTwice()
    {
        var registry = CreateEmptyRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        Assert.Throws<InvalidOperationException>(() =>
            processor.StartCurrentTurn(combat, registry));
    }

    [Fact]
    public void EndCurrentTurnRequiresStartedTurn()
    {
        var registry = CreateEmptyRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.EndCurrentTurn(combat, registry));
    }

    [Fact]
    public void EndCurrentTurnMovesToNextCombatant()
    {
        var registry = CreateEmptyRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(GoblinId, combat.ActiveCombatantId);
        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.WaitingToStartTurn, combat.TurnPhase);

        Assert.Equal(
            new[] { StandardCombatLogTypes.TurnStarted, StandardCombatLogTypes.TurnEnded },
            combat.CombatLog.Select(entry => entry.Type).ToArray());
    }

    [Fact]
    public void EndCurrentTurnAdvancesRoundAfterLastCombatant()
    {
        var registry = CreateEmptyRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        combat.SetActiveCombatant(GoblinId);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(HeroId, combat.ActiveCombatantId);
        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(1, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.WaitingToStartTurn, combat.TurnPhase);
    }

    [Fact]
    public void StartCurrentTurnProcessesTurnStartedEventHandlersAndFollowUpEffects()
    {
        var registry = CreateEmptyRegistry();
        registry.RegisterEffectRequestHandler(new GainBlockEffectHandler());
        registry.RegisterCombatEventHandler(new GainBlockOnTurnStartedHandler(amount: 4));

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        var hero = combat.GetCombatant(HeroId);
        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(4, block.Current);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);

        Assert.Equal(StandardCombatLogTypes.TurnStarted, combat.CombatLog[0].Type);
        Assert.Equal(StandardCombatLogTypes.BlockGained, combat.CombatLog[1].Type);
    }

    [Fact]
    public void EndCurrentTurnProcessesTurnEndedEventHandlers()
    {
        var registry = CreateEmptyRegistry();
        registry.RegisterEffectRequestHandler(new GainBlockEffectHandler());
        registry.RegisterCombatEventHandler(new GainBlockOnTurnEndedHandler(amount: 3));

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var hero = combat.GetCombatant(HeroId);
        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(3, block.Current);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);

        Assert.Equal(StandardCombatLogTypes.TurnStarted, combat.CombatLog[0].Type);
        Assert.Equal(StandardCombatLogTypes.TurnEnded, combat.CombatLog[1].Type);
        Assert.Equal(StandardCombatLogTypes.BlockGained, combat.CombatLog[2].Type);
    }

    [Fact]
    public void EndCurrentTurnResolvesPendingEffectsBeforeAdvancing()
    {
        var registry = CreateEmptyRegistry();
        registry.RegisterEffectRequestHandler(new GainBlockEffectHandler());
        registry.RegisterEffectRequestHandler(new DealDamageEffectHandler());

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        combat.EnqueueEffect(new GainBlockEffectRequest(
            TargetCombatantId: HeroId,
            Amount: 6));

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: HeroId,
            Amount: 10));

        processor.EndCurrentTurn(combat, registry);

        var hero = combat.GetCombatant(HeroId);
        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(16, hero.Health.Current);
        Assert.Equal(0, block.Current);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(GoblinId, combat.ActiveCombatantId);

        Assert.Equal(StandardCombatLogTypes.TurnStarted, combat.CombatLog[0].Type);
        Assert.Equal(StandardCombatLogTypes.TurnEnded, combat.CombatLog[1].Type);
        Assert.Equal(StandardCombatLogTypes.BlockGained, combat.CombatLog[2].Type);
        Assert.Equal(StandardCombatLogTypes.DamageDealt, combat.CombatLog[3].Type);
    }

    [Fact]
    public void EndCurrentTurnAndStartNextTurnEndsCurrentTurnThenStartsNextTurn()
    {
        var registry = CreateEmptyRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurnAndStartNextTurn(combat, registry);

        Assert.Equal(GoblinId, combat.ActiveCombatantId);
        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.TurnInProgress, combat.TurnPhase);

        Assert.Equal(
            new[]
            {
                StandardCombatLogTypes.TurnStarted,
                StandardCombatLogTypes.TurnEnded,
                StandardCombatLogTypes.TurnStarted
            },
            combat.CombatLog.Select(entry => entry.Type).ToArray());
    }

    [Fact]
    public void StartCurrentTurnRejectsCombatWithoutTurnOrder()
    {
        var registry = CreateEmptyRegistry();
        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var processor = new CombatTurnProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.StartCurrentTurn(combat, registry));
    }

    [Fact]
    public void StartCurrentTurnPublishesTurnStartedEventToHandlers()
    {
        var registry = CreateEmptyRegistry();
        var eventHandler = new CaptureTurnStartedEventHandler();
        registry.RegisterCombatEventHandler(eventHandler);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        var handledEvent = Assert.Single(eventHandler.HandledEvents);

        Assert.Equal(HeroId, handledEvent.CombatantId);
        Assert.Equal(1, handledEvent.Round);
        Assert.Equal(1, handledEvent.Turn);
        Assert.Equal(0, combat.PendingEventCount);
    }

    [Fact]
    public void EndCurrentTurnPublishesTurnEndedEventToHandlers()
    {
        var registry = CreateEmptyRegistry();
        var eventHandler = new CaptureTurnEndedEventHandler();
        registry.RegisterCombatEventHandler(eventHandler);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var handledEvent = Assert.Single(eventHandler.HandledEvents);

        Assert.Equal(HeroId, handledEvent.CombatantId);
        Assert.Equal(1, handledEvent.Round);
        Assert.Equal(1, handledEvent.Turn);
        Assert.Equal(0, combat.PendingEventCount);
    }

    private static CombatDefinitionRegistry CreateEmptyRegistry()
    {
        return new CombatDefinitionRegistry();
    }

    private sealed class GainBlockOnTurnStartedHandler
        : CombatEventHandler<TurnStartedCombatEvent>
    {
        private readonly int _amount;

        public GainBlockOnTurnStartedHandler(int amount)
        {
            _amount = amount;
        }

        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TurnStartedCombatEvent combatEvent)
        {
            combat.EnqueueEffect(new GainBlockEffectRequest(
                TargetCombatantId: combatEvent.CombatantId,
                Amount: _amount));
        }
    }

    private sealed class GainBlockOnTurnEndedHandler
        : CombatEventHandler<TurnEndedCombatEvent>
    {
        private readonly int _amount;

        public GainBlockOnTurnEndedHandler(int amount)
        {
            _amount = amount;
        }

        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TurnEndedCombatEvent combatEvent)
        {
            combat.EnqueueEffect(new GainBlockEffectRequest(
                TargetCombatantId: combatEvent.CombatantId,
                Amount: _amount));
        }
    }

    private sealed class CaptureTurnStartedEventHandler
        : CombatEventHandler<TurnStartedCombatEvent>
    {
        public List<TurnStartedCombatEvent> HandledEvents { get; } = new();

        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TurnStartedCombatEvent combatEvent)
        {
            HandledEvents.Add(combatEvent);
        }
    }

    private sealed class CaptureTurnEndedEventHandler
        : CombatEventHandler<TurnEndedCombatEvent>
    {
        public List<TurnEndedCombatEvent> HandledEvents { get; } = new();

        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TurnEndedCombatEvent combatEvent)
        {
            HandledEvents.Add(combatEvent);
        }
    }
}
