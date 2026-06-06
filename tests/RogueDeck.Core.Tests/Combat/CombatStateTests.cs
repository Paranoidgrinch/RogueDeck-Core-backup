using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatStateTests
{
    [Fact]
    public void CombatStateCanStoreCombatantWithGenericStatus()
    {
        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var playerId = new CombatantId("player_001");
        var goblinId = new CombatantId("goblin_001");

        var player = new CombatantState(
            playerId,
            new CombatantDefinitionId("standard.player"),
            "combatant.player",
            new TeamId("player"),
            new HealthState(current: 50, max: 50));

        var goblin = new CombatantState(
            goblinId,
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            new TeamId("enemy"),
            new HealthState(current: 18, max: 24));

        goblin.DefensivePools.Add(
            new DefensivePoolId("standard.block"),
            new ValuePoolState(current: 6));

        var poison = new StatusInstance(
            new StatusInstanceId("status_001"),
            new StatusDefinitionId("standard.poison"),
            ownerCombatantId: goblinId,
            sourceCombatantId: playerId,
            stacks: 5,
            appliedRound: 1,
            appliedTurn: 1,
            visibility: StatusVisibility.Visible,
            polarity: StatusPolarity.Debuff);

        goblin.Statuses.Add(poison);

        combat.AddCombatant(player);
        combat.AddCombatant(goblin);

        var storedGoblin = combat.GetCombatant(goblinId);

        Assert.Equal(2, combat.Combatants.Count);
        Assert.Equal(18, storedGoblin.Health.Current);
        Assert.Single(storedGoblin.Statuses);
        Assert.Equal(5, storedGoblin.Statuses[0].Stacks);
        Assert.True(storedGoblin.DefensivePools.ContainsKey(new DefensivePoolId("standard.block")));
    }
}