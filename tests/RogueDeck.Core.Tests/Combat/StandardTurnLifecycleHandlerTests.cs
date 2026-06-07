using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StandardTurnLifecycleHandlerTests
{
    [Fact]
    public void StartCurrentTurnClearsBlockFromActiveCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");
        var hero = combat.GetCombatant(heroId);

        hero.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 9));

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(0, block.Current);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal("TurnStarted", combat.CombatLog[0].Type);
        Assert.Equal("DefensivePoolCleared", combat.CombatLog[1].Type);
    }

    [Fact]
    public void StartCurrentTurnDoesNotClearBlockFromInactiveCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var combat = CreateCombatWithHeroAndGoblin();

        var goblinId = new CombatantId("goblin_001");
        var goblin = combat.GetCombatant(goblinId);

        goblin.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 5));

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);

        var block = goblin.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(5, block.Current);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
    }

    [Fact]
    public void EndCurrentTurnAndStartNextTurnClearsBlockFromNextCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var combat = CreateCombatWithHeroAndGoblin();

        var goblinId = new CombatantId("goblin_001");
        var goblin = combat.GetCombatant(goblinId);

        goblin.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 7));

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurnAndStartNextTurn(combat, registry);

        var block = goblin.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(0, block.Current);
        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
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
