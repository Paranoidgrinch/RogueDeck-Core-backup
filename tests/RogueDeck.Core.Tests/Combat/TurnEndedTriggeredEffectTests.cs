using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TurnEndedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");
    private static readonly CombatantId AllyId = new("ally_001");

    [Fact]
    public void StandardCombatPackageRegistersTurnEndedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(TurnEndedCombatEvent)),
            handler => handler is TurnEndedTriggeredEffectHandler);
    }

    [Fact]
    public void TurnEndedTriggeredEffectCanDealDamageToAllEnemies()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_end_damage_all_enemies"));

        definition.Actions.Add(
            new DealDamageOnTurnEndedTriggeredEffectAction(
                TurnEndedTriggeredEffectTarget.AllEnemies,
                new FixedTurnEndedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var firstGoblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var firstGoblinHealthBefore = firstGoblin.Health.Current;
        var secondGoblinHealthBefore = secondGoblin.Health.Current;

        ResolveTurnEndedEvent(
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
    public void TurnEndedTriggeredEffectCanApplyStatusToAllAllies()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        var statusId = new StatusDefinitionId("test.turn_end_ally_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Buff);

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_end_apply_status_to_all_allies"));

        definition.Actions.Add(
            new ApplyStatusOnTurnEndedTriggeredEffectAction(
                TurnEndedTriggeredEffectTarget.AllAllies,
                statusId,
                new FixedTurnEndedTriggeredEffectAmount(2),
                new FixedTurnEndedTriggeredEffectAmount(0),
                new FixedTurnEndedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnEndedEvent(
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
    public void TurnEndedTriggeredEffectCanUseTurnCombatantStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var counterStatusId = new StatusDefinitionId("test.turn_end_counter");
        RegisterStackingStatus(registry, counterStatusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            counterStatusId,
            stacks: 4);

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_end_damage_equal_to_counter"));

        definition.Filters.Add(
            new TurnEndedCombatantHasStatusTriggerFilter(counterStatusId));

        definition.Actions.Add(
            new DealDamageOnTurnEndedTriggeredEffectAction(
                TurnEndedTriggeredEffectTarget.AllEnemies,
                new TurnEndedCombatantStatusStacksAmount(counterStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var firstGoblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var firstGoblinHealthBefore = firstGoblin.Health.Current;
        var secondGoblinHealthBefore = secondGoblin.Health.Current;

        ResolveTurnEndedEvent(
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
    public void TurnEndedTriggeredEffectDoesNotRunWhenFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var requiredStatusId = new StatusDefinitionId("test.missing_turn_end_status");
        RegisterStackingStatus(registry, requiredStatusId, StatusPolarity.Buff);

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_end_filtered_damage"));

        definition.Filters.Add(
            new TurnEndedCombatantHasStatusTriggerFilter(requiredStatusId));

        definition.Actions.Add(
            new DealDamageOnTurnEndedTriggeredEffectAction(
                TurnEndedTriggeredEffectTarget.AllEnemies,
                new FixedTurnEndedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        ResolveTurnEndedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore,
            goblin.Health.Current);
    }

    [Fact]
    public void TurnEndedTriggeredEffectRunsBeforeTimedStatusDurationDecrease()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var requiredStatusId = new StatusDefinitionId("test.one_turn_end_trigger_status");
        RegisterTimedStatus(registry, requiredStatusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            requiredStatusId,
            stacks: 0,
            durationTurns: 1,
            charges: 0);

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_end_uses_status_before_duration_decrease"));

        definition.Filters.Add(
            new TurnEndedCombatantHasStatusTriggerFilter(requiredStatusId));

        definition.Actions.Add(
            new GainBlockOnTurnEndedTriggeredEffectAction(
                TurnEndedTriggeredEffectTarget.TurnCombatant,
                new FixedTurnEndedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnEndedEvent(
            combat,
            registry,
            HeroId);

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            3,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        Assert.DoesNotContain(
            hero.Statuses,
            status => status.DefinitionId == requiredStatusId);
    }

    [Fact]
    public void TurnEndedTriggeredEffectDoesNotCreateRequestsForNonPositiveAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_end_zero_damage"));

        definition.Actions.Add(
            new DealDamageOnTurnEndedTriggeredEffectAction(
                TurnEndedTriggeredEffectTarget.AllEnemies,
                new FixedTurnEndedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        ResolveTurnEndedEvent(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore,
            goblin.Health.Current);
    }

    private static void ResolveTurnEndedEvent(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId combatantId)
    {
        combat.EnqueueEvent(new TurnEndedCombatEvent(
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

    private static void RegisterTimedStatus(
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
            usesDuration: true,
            showDurationInUi: true,
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
        ApplyStatus(
            combat,
            registry,
            targetId,
            statusId,
            stacks,
            durationTurns: 0,
            charges: 0);
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        int stacks,
        int durationTurns,
        int charges)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusId,
            Stacks: stacks,
            DurationTurns: durationTurns,
            Charges: charges));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}
