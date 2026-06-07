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
    public void StartCurrentTurnEnqueuesTurnStartedEvent()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        var combatEvent = Assert.Single(combat.PendingEvents);
        var turnStarted = Assert.IsType<TurnStartedCombatEvent>(combatEvent);

        Assert.Equal(new CombatantId("hero_001"), turnStarted.CombatantId);
        Assert.Equal(1, turnStarted.Round);
        Assert.Equal(1, turnStarted.Turn);
    }

    [Fact]
    public void EndCurrentTurnEnqueuesTurnEndedEvent()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var events = combat.PendingEvents.ToArray();

        Assert.Equal(2, events.Length);

        var turnStarted = Assert.IsType<TurnStartedCombatEvent>(events[0]);
        var turnEnded = Assert.IsType<TurnEndedCombatEvent>(events[1]);

        Assert.Equal(new CombatantId("hero_001"), turnStarted.CombatantId);
        Assert.Equal(new CombatantId("hero_001"), turnEnded.CombatantId);
        Assert.Equal(1, turnEnded.Round);
        Assert.Equal(1, turnEnded.Turn);
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
}


