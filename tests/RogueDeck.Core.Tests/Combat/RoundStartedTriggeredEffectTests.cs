using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class RoundStartedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");

    [Fact]
    public void StandardCombatPackageRegistersRoundStartedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(RoundStartedCombatEvent)),
            handler => handler is RoundStartedTriggeredEffectHandler);
    }

    [Fact]
    public void RoundStartedTriggeredEffectFiresWhenNewRoundStarts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_gain_block"));

        definition.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundStartedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(HeroId, combat.ActiveCombatantId);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void RoundStartedTriggeredEffectMinimumRoundFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_matching_minimum"));

        matching.Filters.Add(
            new RoundStartedMinimumRoundTriggerFilter(MinimumRound: 2));

        matching.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundStartedTriggeredEffectAmount(2)));

        var nonMatching = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_non_matching_minimum"));

        nonMatching.Filters.Add(
            new RoundStartedMinimumRoundTriggerFilter(MinimumRound: 3));

        nonMatching.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundStartedTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void RoundStartedTriggeredEffectEveryNthRoundFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_matching_interval"));

        matching.Filters.Add(
            new RoundStartedEveryNthRoundTriggerFilter(Interval: 2));

        matching.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundStartedTriggeredEffectAmount(3)));

        var nonMatching = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_non_matching_interval"));

        nonMatching.Filters.Add(
            new RoundStartedEveryNthRoundTriggerFilter(Interval: 3));

        nonMatching.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundStartedTriggeredEffectAmount(8)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void RoundStartedTriggeredEffectCanUseCurrentRoundAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_current_round_amount"));

        definition.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new RoundStartedCurrentRoundAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void RoundStartedTriggeredEffectCanTargetAllAliveCombatants()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.round_started_all_alive"));

        definition.Actions.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.AllAliveCombatants,
                new FixedRoundStartedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromSecondGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(HeroId, combat.ActiveCombatantId);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            5,
            GetBlockOrZero(combat, GoblinId));

        Assert.Equal(
            5,
            GetBlockOrZero(combat, SecondGoblinId));
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
