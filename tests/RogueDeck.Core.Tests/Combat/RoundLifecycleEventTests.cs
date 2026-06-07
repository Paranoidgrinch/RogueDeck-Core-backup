using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class RoundLifecycleEventTests
{
    [Fact]
    public void EndingLastCombatantsTurnEmitsRoundEndedAndRoundStartedEvents()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        registry.RegisterCombatEventHandler(new AddLogOnRoundEndedHandler());
        registry.RegisterCombatEventHandler(new AddLogOnRoundStartedHandler());

        var combat = CreateCombatWithHeroAndGoblin();

        combat.SetActiveCombatant(new CombatantId("goblin_001"));

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(1, combat.CurrentTurn);
        Assert.Equal(new CombatantId("hero_001"), combat.ActiveCombatantId);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Contains(combat.CombatLog, entry => entry.Type == "RoundEndedEventHandled");
        Assert.Contains(combat.CombatLog, entry => entry.Type == "RoundStartedEventHandled");
    }

    [Fact]
    public void EndingNonLastCombatantsTurnDoesNotEmitRoundEvents()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        registry.RegisterCombatEventHandler(new AddLogOnRoundEndedHandler());
        registry.RegisterCombatEventHandler(new AddLogOnRoundStartedHandler());

        var combat = CreateCombatWithHeroAndGoblin();

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Equal(1, combat.CurrentRound);
        Assert.Equal(2, combat.CurrentTurn);
        Assert.Equal(new CombatantId("goblin_001"), combat.ActiveCombatantId);
        Assert.DoesNotContain(combat.CombatLog, entry => entry.Type == "RoundEndedEventHandled");
        Assert.DoesNotContain(combat.CombatLog, entry => entry.Type == "RoundStartedEventHandled");
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
            StandardCombatIds.PlayerTeam,
            new HealthState(current: 20, max: 20));

        var goblin = new CombatantState(
            new CombatantId("goblin_001"),
            new CombatantDefinitionId("standard.goblin"),
            "combatant.goblin",
            StandardCombatIds.EnemyTeam,
            new HealthState(current: 12, max: 12));

        combat.AddCombatant(hero);
        combat.AddCombatant(goblin);

        return combat;
    }

    private sealed class AddLogOnRoundEndedHandler : CombatEventHandler<RoundEndedCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            RoundEndedCombatEvent combatEvent)
        {
            combat.AddLogEntry(
                "RoundEndedEventHandled",
                $"Handled round ended event for round {combatEvent.Round}.");
        }
    }

    private sealed class AddLogOnRoundStartedHandler : CombatEventHandler<RoundStartedCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            RoundStartedCombatEvent combatEvent)
        {
            combat.AddLogEntry(
                "RoundStartedEventHandled",
                $"Handled round started event for round {combatEvent.Round}.");
        }
    }
}
