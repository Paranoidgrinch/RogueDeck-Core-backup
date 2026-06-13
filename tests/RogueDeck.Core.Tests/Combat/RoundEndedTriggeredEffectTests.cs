using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class RoundEndedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");

    [Fact]
    public void StandardCombatPackageRegistersRoundEndedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(RoundEndedCombatEvent)),
            handler => handler is RoundEndedTriggeredEffectHandler);
    }

    [Fact]
    public void RoundEndedTriggeredEffectUsesLastActiveCombatantAsEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_gain_block"));

        definition.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(HeroId, combat.ActiveCombatantId);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            4,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void RoundEndedTriggeredEffectUsesLastActiveCombatantAsSource()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_source_allies"));

        definition.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.AllAlliesOfSource,
                new FixedRoundEndedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromSecondGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(HeroId, combat.ActiveCombatantId);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            3,
            GetBlockOrZero(combat, GoblinId));

        Assert.Equal(
            3,
            GetBlockOrZero(combat, SecondGoblinId));
    }

    [Fact]
    public void RoundEndedTriggeredEffectMinimumRoundFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_matching_minimum"));

        matching.Filters.Add(
            new RoundEndedMinimumRoundTriggerFilter(MinimumRound: 1));

        matching.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(2)));

        var nonMatching = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_non_matching_minimum"));

        nonMatching.Filters.Add(
            new RoundEndedMinimumRoundTriggerFilter(MinimumRound: 2));

        nonMatching.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void RoundEndedTriggeredEffectEveryNthRoundFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_matching_interval"));

        matching.Filters.Add(
            new RoundEndedEveryNthRoundTriggerFilter(Interval: 1));

        matching.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(3)));

        var nonMatching = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_non_matching_interval"));

        nonMatching.Filters.Add(
            new RoundEndedEveryNthRoundTriggerFilter(Interval: 2));

        nonMatching.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(8)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void RoundEndedTriggeredEffectCanUseCompletedRoundAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_completed_round_amount"));

        definition.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new RoundEndedCompletedRoundAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(
            1,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void RoundEndedTriggeredEffectDoesNotFireWhenLastActiveCombatantIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_missing_source"));

        definition.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(9)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEvent(new RoundEndedCombatEvent(
            Round: 1,
            LastActiveCombatantId: null));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void RoundEndedTriggeredEffectCanHealLastActiveCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        var goblin = combat.GetCombatant(GoblinId);

        var heroHealthBefore = hero.Health.Current;
        var goblinHealthBefore = goblin.Health.Max - 5;

        goblin.Health.SetCurrent(goblinHealthBefore);

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_heal_last_active"));

        definition.Actions.Add(
            new HealOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(
            combat,
            registry);

        Assert.Equal(
            heroHealthBefore,
            hero.Health.Current);

        Assert.Equal(
            goblinHealthBefore + 3,
            goblin.Health.Current);
    }

    [Fact]
    public void RoundEndedTriggeredEffectCanHealAllAlliesOfLastActiveCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var hero = combat.GetCombatant(HeroId);
        var goblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var heroHealthBefore = hero.Health.Current;
        var goblinHealthBefore = goblin.Health.Max - 5;
        var secondGoblinHealthBefore = secondGoblin.Health.Max - 4;

        goblin.Health.SetCurrent(goblinHealthBefore);
        secondGoblin.Health.SetCurrent(secondGoblinHealthBefore);

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_ended_heal_source_allies"));

        definition.Actions.Add(
            new HealOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.AllAlliesOfSource,
                new FixedRoundEndedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromSecondGoblinTurnToNextRound(
            combat,
            registry);

        Assert.Equal(
            heroHealthBefore,
            hero.Health.Current);

        Assert.Equal(
            goblinHealthBefore + 2,
            goblin.Health.Current);

        Assert.Equal(
            secondGoblinHealthBefore + 2,
            secondGoblin.Health.Current);
    }

    private static void AdvanceFromGoblinTurnToNextRound(
        CombatState combat,
        CombatDefinitionRegistry registry)
    {
        combat.SetActiveCombatant(GoblinId);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);
    }

    private static void AdvanceFromSecondGoblinTurnToNextRound(
        CombatState combat,
        CombatDefinitionRegistry registry)
    {
        combat.SetActiveCombatant(SecondGoblinId);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);
    }

    private static int GetBlockOrZero(
        CombatState combat,
        CombatantId combatantId)
    {
        var combatant = combat.GetCombatant(combatantId);

        return combatant.DefensivePools.TryGetValue(
            StandardCombatIds.BlockDefensivePool,
            out var blockPool)
                ? blockPool.Current
                : 0;
    }
}
