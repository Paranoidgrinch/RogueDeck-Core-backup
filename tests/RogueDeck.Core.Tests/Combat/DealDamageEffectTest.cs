using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DealDamageEffectTests
{
    [Fact]
    public void DealDamageReducesTargetHealth()
    {
        var registry = new CombatDefinitionRegistry();

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var goblinId = new CombatantId("goblin_001");

        var goblin = new CombatantState(
            goblinId,
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            new TeamId("enemy"),
            new HealthState(current: 18, max: 24));

        combat.AddCombatant(goblin);

        var resolver = new CombatEffectResolver();

        resolver.Resolve(
            combat,
            registry,
            new DealDamageEffectRequest(
                TargetCombatantId: goblinId,
                Amount: 7));

        var storedGoblin = combat.GetCombatant(goblinId);

        Assert.Equal(11, storedGoblin.Health.Current);
        Assert.Single(combat.CombatLog);
        Assert.Equal("DamageDealt", combat.CombatLog[0].Type);
    }

    [Fact]
    public void DealDamageCannotReduceHealthBelowZero()
    {
        var registry = new CombatDefinitionRegistry();

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var goblinId = new CombatantId("goblin_001");

        var goblin = new CombatantState(
            goblinId,
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            new TeamId("enemy"),
            new HealthState(current: 5, max: 24));

        combat.AddCombatant(goblin);

        var resolver = new CombatEffectResolver();

        resolver.Resolve(
            combat,
            registry,
            new DealDamageEffectRequest(
                TargetCombatantId: goblinId,
                Amount: 999));

        var storedGoblin = combat.GetCombatant(goblinId);

        Assert.Equal(0, storedGoblin.Health.Current);
        Assert.Single(combat.CombatLog);
        Assert.Equal("DamageDealt", combat.CombatLog[0].Type);
    }

    [Fact]
    public void DealDamageRejectsNegativeAmount()
    {
        var registry = new CombatDefinitionRegistry();

        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var goblinId = new CombatantId("goblin_001");

        var goblin = new CombatantState(
            goblinId,
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            new TeamId("enemy"),
            new HealthState(current: 18, max: 24));

        combat.AddCombatant(goblin);

        var resolver = new CombatEffectResolver();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            resolver.Resolve(
                combat,
                registry,
                new DealDamageEffectRequest(
                    TargetCombatantId: goblinId,
                    Amount: -1)));
    }
}