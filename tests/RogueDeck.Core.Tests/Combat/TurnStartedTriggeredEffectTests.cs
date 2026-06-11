using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TurnStartedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");
    private static readonly CombatantId AllyId = new("ally_001");

    [Fact]
    public void StandardCombatPackageRegistersTurnStartedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(TurnStartedCombatEvent)),
            handler => handler is TurnStartedTriggeredEffectHandler);
    }

    [Fact]
    public void TurnStartedTriggeredEffectCanDealDamageToAllEnemies()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_start_damage_all_enemies"));

        definition.Actions.Add(
            new DealDamageOnTurnStartedTriggeredEffectAction(
                TurnStartedTriggeredEffectTarget.AllEnemies,
                new FixedTurnStartedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var firstGoblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var firstGoblinHealthBefore = firstGoblin.Health.Current;
        var secondGoblinHealthBefore = secondGoblin.Health.Current;

        ResolveTurnStartedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            firstGoblinHealthBefore - 3,
            firstGoblin.Health.Current);

        Assert.Equal(
            secondGoblinHealthBefore - 3,
            secondGoblin.Health.Current);
    }

    [Fact]
    public void TurnStartedTriggeredEffectCanApplyStatusToAllAllies()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        var statusId = new StatusDefinitionId("test.turn_start_ally_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Buff);

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_start_apply_status_to_all_allies"));

        definition.Actions.Add(
            new ApplyStatusOnTurnStartedTriggeredEffectAction(
                TurnStartedTriggeredEffectTarget.AllAllies,
                statusId,
                new FixedTurnStartedTriggeredEffectAmount(2),
                new FixedTurnStartedTriggeredEffectAmount(0),
                new FixedTurnStartedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnStartedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(HeroId).Statuses,
                status => status.DefinitionId == statusId).Stacks);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(AllyId).Statuses,
                status => status.DefinitionId == statusId).Stacks);

        Assert.DoesNotContain(
            combat.GetCombatant(GoblinId).Statuses,
            status => status.DefinitionId == statusId);
    }

    [Fact]
    public void TurnStartedTriggeredEffectCanUseTurnCombatantStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var counterStatusId = new StatusDefinitionId("test.turn_start_counter");
        RegisterStackingStatus(registry, counterStatusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            counterStatusId,
            stacks: 4);

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_start_damage_equal_to_counter"));

        definition.Filters.Add(
            new TurnStartedCombatantHasStatusTriggerFilter(counterStatusId));

        definition.Actions.Add(
            new DealDamageOnTurnStartedTriggeredEffectAction(
                TurnStartedTriggeredEffectTarget.AllEnemies,
                new TurnStartedCombatantStatusStacksAmount(counterStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var firstGoblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var firstGoblinHealthBefore = firstGoblin.Health.Current;
        var secondGoblinHealthBefore = secondGoblin.Health.Current;

        ResolveTurnStartedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            firstGoblinHealthBefore - 4,
            firstGoblin.Health.Current);

        Assert.Equal(
            secondGoblinHealthBefore - 4,
            secondGoblin.Health.Current);
    }

    [Fact]
    public void TurnStartedTriggeredEffectDoesNotRunWhenFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var requiredStatusId = new StatusDefinitionId("test.missing_turn_start_status");
        RegisterStackingStatus(registry, requiredStatusId, StatusPolarity.Buff);

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_start_filtered_damage"));

        definition.Filters.Add(
            new TurnStartedCombatantHasStatusTriggerFilter(requiredStatusId));

        definition.Actions.Add(
            new DealDamageOnTurnStartedTriggeredEffectAction(
                TurnStartedTriggeredEffectTarget.AllEnemies,
                new FixedTurnStartedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        ResolveTurnStartedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore,
            goblin.Health.Current);
    }

    [Fact]
    public void TurnStartedTriggeredEffectDoesNotCreateRequestsForNonPositiveAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_start_zero_damage"));

        definition.Actions.Add(
            new DealDamageOnTurnStartedTriggeredEffectAction(
                TurnStartedTriggeredEffectTarget.AllEnemies,
                new FixedTurnStartedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        ResolveTurnStartedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore,
            goblin.Health.Current);
    }

    private static void ResolveTurnStartedEvent(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId combatantId)
    {
        combat.EnqueueEvent(new TurnStartedCombatEvent(
            combatantId,
            1,
            1));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void AddPlayerAlly(CombatState combat)
    {
        var ally = new CombatantState(
            AllyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            StandardCombatIds.PlayerTeam,
            new HealthState(current: 10, max: 10));

        combat.AddCombatant(ally);
    }

    private static void RegisterStackingStatus(
        CombatDefinitionRegistry registry,
        StatusDefinitionId statusId,
        StatusPolarity polarity)
    {
        var definition = new StatusDefinition(
            statusId,
            new PackageId("test"),
            displayNameKey: $"status.{statusId}.name",
            descriptionKey: $"status.{statusId}.description",
            polarity: polarity,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        registry.RegisterStatus(definition);
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        int stacks)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusId,
            Stacks: stacks,
            DurationTurns: 0,
            Charges: 0));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}


