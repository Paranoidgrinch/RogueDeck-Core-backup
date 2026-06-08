using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

internal static class CombatTestFactory
{
    public static CombatDefinitionRegistry CreateStandardRegistry()
    {
        var registry = new CombatDefinitionRegistry();

        var package = new StandardCombatPackage();
        package.RegisterDefinitions(registry);

        return registry;
    }

    public static CombatState CreateCombatWithHero()
    {
        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var hero = new CombatantState(
            new CombatantId("hero_001"),
            new CombatantDefinitionId("standard.hero"),
            "combatant.hero",
            StandardCombatIds.PlayerTeam,
            new HealthState(current: 20, max: 20));

        combat.AddCombatant(hero);

        return combat;
    }

    public static CombatState CreateCombatWithHeroAndGoblin()
    {
        var combat = CreateCombatWithHero();

        var goblin = new CombatantState(
            new CombatantId("goblin_001"),
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            StandardCombatIds.EnemyTeam,
            new HealthState(current: 12, max: 12));

        combat.AddCombatant(goblin);

        return combat;
    }

    public static CombatState CreateCombatWithHeroAndTwoGoblins()
    {
        var combat = CreateCombatWithHeroAndGoblin();

        var secondGoblin = new CombatantState(
            new CombatantId("goblin_002"),
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            StandardCombatIds.EnemyTeam,
            new HealthState(current: 12, max: 12));

        combat.AddCombatant(secondGoblin);

        return combat;
    }
}
