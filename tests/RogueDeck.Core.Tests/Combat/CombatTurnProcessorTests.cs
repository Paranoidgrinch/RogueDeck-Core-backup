using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatTurnProcessorTests
{
    [Fact]
    public void EndCurrentTurnMovesToNextCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
    }

    [Fact]
    public void EndCurrentTurnAdvancesRoundAfterLastCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        combat.SetActiveCombatant(new CombatantId("goblin_001"));

        var processor = new CombatTurnProcessor();

        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(new CombatantId("hero_001"), combat.ActiveCombatantId);
        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(1, combat.CurrentTurn);
    }

    [Fact]
    public void EndCurrentTurnResolvesPendingEffectsBeforeAdvancing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHeroAndGoblin();

        var heroId = new CombatantId("hero_001");

        combat.EnqueueEffect(
            new GainBlockEffectRequest(
                TargetCombatantId: heroId,
                Amount: 6));

        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: heroId,
                Amount: 10));

        var processor = new CombatTurnProcessor();

        processor.EndCurrentTurn(combat, registry);

        var hero = combat.GetCombatant(heroId);
        var block = hero.DefensivePools[StandardCombatIds.BlockDefensivePool];

        Assert.Equal(16, hero.Health.Current);
        Assert.Equal(0, block.Current);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
    }

    [Fact]
    public void EndCurrentTurnRejectsCombatWithoutTurnOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

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
}
