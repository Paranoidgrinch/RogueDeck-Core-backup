using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatantDownedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersCombatantDownedTriggeredEffectHandlerBeforeResultHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var handlers = registry
            .GetCombatEventHandlers(typeof(CombatantLifecycleChangedCombatEvent))
            .ToList();

        var downedIndex = handlers.FindIndex(handler => handler is CombatantDownedTriggeredEffectHandler);
        var resultIndex = handlers.FindIndex(handler => handler is UpdateStandardCombatResultOnLifecycleChangedHandler);

        Assert.True(downedIndex >= 0);
        Assert.True(resultIndex >= 0);
        Assert.True(downedIndex < resultIndex);
    }

    [Fact]
    public void CombatantDownedTriggeredEffectFiresBeforeCombatResultChanges()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_gain_block"));

        definition.Filters.Add(
            new CombatantDownedTeamTriggerFilter(StandardCombatIds.EnemyTeam));

        definition.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(CombatResult.Victory, combat.Result);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(0, combat.PendingEventCount);
    }

    [Fact]
    public void CombatantDownedTriggeredEffectDoesNotFireWhenNewStateIsNotDowned()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_not_removed"));

        definition.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new SetCombatantLifecycleStateEffectRequest(
            CombatantId: GoblinId,
            LifecycleState: CombatantLifecycleState.Removed));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CombatantDownedTriggeredEffectFiltersByCombatantId()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_matching_id"));

        matching.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(GoblinId));

        matching.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(2)));

        var nonMatching = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_non_matching_id"));

        nonMatching.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(HeroId));

        nonMatching.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(9)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CombatantDownedTriggeredEffectFiltersByOldState()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_matching_old_state"));

        matching.Filters.Add(
            new CombatantDownedOldStateTriggerFilter(CombatantLifecycleState.Alive));

        matching.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(3)));

        var nonMatching = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_non_matching_old_state"));

        nonMatching.Filters.Add(
            new CombatantDownedOldStateTriggerFilter(CombatantLifecycleState.Dead));

        nonMatching.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(8)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CombatantDownedTriggeredEffectCanTargetAllAliveCombatants()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var definition = new CombatantDownedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.combatant_downed_all_alive"));

        definition.Steps.Add(
            new GainBlockOnCombatantDownedTriggeredEffectAction(
                CombatantTargetSelectors.AllAliveCombatants,
                new FixedCombatantDownedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            5,
            GetBlockOrZero(combat, new CombatantId("goblin_002")));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    private static void DownGoblinWithDamage(
        CombatState combat,
        CombatDefinitionRegistry registry)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 99,
            SourceCombatantId: HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
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
