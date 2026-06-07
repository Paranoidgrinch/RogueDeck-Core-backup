using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatantLifecycleEffectTests
{
    [Fact]
    public void SetCombatantLifecycleStateChangesLifecycleState()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");

        var resolver = new CombatEffectResolver();

        resolver.Resolve(
            combat,
            registry,
            new SetCombatantLifecycleStateEffectRequest(
                CombatantId: heroId,
                LifecycleState: CombatantLifecycleState.Downed));

        var hero = combat.GetCombatant(heroId);

        Assert.Equal(CombatantLifecycleState.Downed, hero.LifecycleState);
        Assert.False(hero.IsAlive);
        Assert.Contains(combat.CombatLog, entry => entry.Type == "CombatantLifecycleChanged");
    }

    [Fact]
    public void DamageThatReducesHealthToZeroMarksCombatantDowned()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");
        var goblinId = new CombatantId("goblin_001");

        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: goblinId,
                Amount: 99,
                SourceCombatantId: heroId));

        var processor = new CombatQueueProcessor();

        processor.ResolvePendingQueues(combat, registry);

        var goblin = combat.GetCombatant(goblinId);

        Assert.Equal(0, goblin.Health.Current);
        Assert.Equal(CombatantLifecycleState.Downed, goblin.LifecycleState);
        Assert.False(goblin.IsAlive);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Contains(combat.CombatLog, entry => entry.Type == "DamageDealt");
        Assert.Contains(combat.CombatLog, entry => entry.Type == "CombatantLifecycleChanged");
    }

    [Fact]
    public void FullyBlockedDamageDoesNotMarkCombatantDowned()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");
        var goblinId = new CombatantId("goblin_001");
        var goblin = combat.GetCombatant(goblinId);

        goblin.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 99));

        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: goblinId,
                Amount: 12,
                SourceCombatantId: heroId));

        var processor = new CombatQueueProcessor();

        processor.ResolvePendingQueues(combat, registry);

        Assert.Equal(12, goblin.Health.Current);
        Assert.Equal(CombatantLifecycleState.Alive, goblin.LifecycleState);
        Assert.True(goblin.IsAlive);
        Assert.DoesNotContain(combat.CombatLog, entry => entry.Type == "CombatantLifecycleChanged");
    }

    [Fact]
    public void DamageDoesNotMarkAlreadyNonAliveCombatantAgain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");
        var goblinId = new CombatantId("goblin_001");
        var goblin = combat.GetCombatant(goblinId);

        goblin.SetLifecycleState(CombatantLifecycleState.Downed);

        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: goblinId,
                Amount: 99,
                SourceCombatantId: heroId));

        var processor = new CombatQueueProcessor();

        processor.ResolvePendingQueues(combat, registry);

        Assert.Equal(CombatantLifecycleState.Downed, goblin.LifecycleState);
        Assert.DoesNotContain(combat.CombatLog, entry => entry.Type == "CombatantLifecycleChanged");
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
