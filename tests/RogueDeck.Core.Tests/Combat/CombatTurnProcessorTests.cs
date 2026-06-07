using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatTurnProcessorTests
{
    [Fact]
    public void StartCurrentTurnAddsTurnStartedLogEntry()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        Assert.Single(combat.CombatLog);
        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal(new CombatantId("hero_001"), combat.ActiveCombatantId);
        Assert.Equal(CombatTurnPhase.TurnInProgress, combat.TurnPhase);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
    }

    [Fact]
    public void StartCurrentTurnProcessesTurnStartedEventHandlersAndFollowUpEffects()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        registry.RegisterCombatEventHandler(new GainBlockOnTurnStartedHandler());

        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        var hero = combat.GetCombatant(new CombatantId("hero_001"));
        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(4, block.Current);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal("BlockGained", combat.CombatLog[1].Type);
    }

    [Fact]
    public void StartCurrentTurnCannotBeCalledTwice()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        Assert.Throws<InvalidOperationException>(() =>
            processor.StartCurrentTurn(combat, registry));
    }

    [Fact]
    public void EndCurrentTurnRequiresStartedTurn()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.EndCurrentTurn(combat, registry));
    }

    [Fact]
    public void EndCurrentTurnMovesToNextCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.WaitingToStartTurn, combat.TurnPhase);
        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal("TurnEnded", combat.CombatLog[1].Type);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
    }

    [Fact]
    public void EndCurrentTurnProcessesTurnEndedEventHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        registry.RegisterCombatEventHandler(new AddLogOnTurnEndedHandler());

        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal("TurnEnded", combat.CombatLog[1].Type);
        Assert.Equal("TurnEndedEventHandled", combat.CombatLog[2].Type);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
    }

    [Fact]
    public void EndCurrentTurnAdvancesRoundAfterLastCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        combat.SetActiveCombatant(new CombatantId("goblin_001"));

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(new CombatantId("hero_001"), combat.ActiveCombatantId);
        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(1, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.WaitingToStartTurn, combat.TurnPhase);
    }

    [Fact]
    public void EndCurrentTurnResolvesPendingEffectsBeforeAdvancing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        combat.EnqueueEffect(
            new GainBlockEffectRequest(
                TargetCombatantId: heroId,
                Amount: 6));

        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: heroId,
                Amount: 10));

        processor.EndCurrentTurn(combat, registry);

        var hero = combat.GetCombatant(heroId);
        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(16, hero.Health.Current);
        Assert.Equal(0, block.Current);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal("TurnEnded", combat.CombatLog[1].Type);
        Assert.Equal("BlockGained", combat.CombatLog[2].Type);
        Assert.Equal("DamageDealt", combat.CombatLog[3].Type);
    }

    [Fact]
    public void EndCurrentTurnAndStartNextTurnEndsCurrentTurnThenStartsNextTurn()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurnAndStartNextTurn(combat, registry);

        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.TurnInProgress, combat.TurnPhase);
        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal("TurnEnded", combat.CombatLog[1].Type);
        Assert.Equal("TurnStarted", combat.CombatLog[2].Type);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
    }

    [Fact]
    public void StartCurrentTurnRejectsCombatWithoutTurnOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var processor = new CombatTurnProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.StartCurrentTurn(combat, registry));
    }

    [Fact]
    public void EndCurrentTurnSkipsDownedCombatantsInTurnOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndTwoGoblins();

        var firstGoblin = combat.GetCombatant(new CombatantId("goblin_001"));

        firstGoblin.SetLifecycleState(CombatantLifecycleState.Downed);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(new CombatantId("goblin_002"), combat.ActiveCombatantId);
        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
        Assert.Equal(CombatTurnPhase.WaitingToStartTurn, combat.TurnPhase);
    }

    [Fact]
    public void EndCurrentTurnThrowsWhenNoLivingCombatantsRemainInTurnOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        foreach (var combatant in combat.Combatants)
            combatant.SetLifecycleState(CombatantLifecycleState.Downed);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        Assert.Throws<InvalidOperationException>(() =>
            processor.EndCurrentTurn(combat, registry));
    }
    private static CombatState CreateCombatWithHeroAndTwoGoblins()
    {
        var combat = CreateCombatWithHeroAndGoblin();

        var secondGoblin = new CombatantState(
            new CombatantId("goblin_002"),
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            new TeamId("enemy"),
            new HealthState(current: 12, max: 12));

        combat.AddCombatant(secondGoblin);

        return combat;
    }
    [Fact]
    public void StartCurrentTurnRejectsEndedCombat()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        combat.SetResult(CombatResult.Victory);

        var processor = new CombatTurnProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.StartCurrentTurn(combat, registry));
    }

    [Fact]
    public void EndCurrentTurnRejectsEndedCombat()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        combat.SetResult(CombatResult.Defeat);

        var processor = new CombatTurnProcessor();

        Assert.Throws<InvalidOperationException>(() =>
            processor.EndCurrentTurn(combat, registry));
    }
    private static CombatState CreateCombatWithHeroAndGoblin()
    {
        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var hero = new CombatantState(
            new CombatantId("hero_001"),
            new CombatantDefinitionId("standard.hero"),
            "combatant.hero",
            new TeamId("player"),
            new HealthState(current: 20, max: 20));

        var goblin = new CombatantState(
            new CombatantId("goblin_001"),
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            new TeamId("enemy"),
            new HealthState(current: 12, max: 12));

        combat.AddCombatant(hero);
        combat.AddCombatant(goblin);

        return combat;
    }

    private sealed class GainBlockOnTurnStartedHandler : CombatEventHandler<TurnStartedCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TurnStartedCombatEvent combatEvent)
        {
            combat.EnqueueEffect(
                new GainBlockEffectRequest(
                    TargetCombatantId: combatEvent.CombatantId,
                    Amount: 4));
        }
    }

    private sealed class AddLogOnTurnEndedHandler : CombatEventHandler<TurnEndedCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TurnEndedCombatEvent combatEvent)
        {
            combat.AddLogEntry(
                "TurnEndedEventHandled",
                $"Handled turn ended event for '{combatEvent.CombatantId}'.");
        }
    }
}


