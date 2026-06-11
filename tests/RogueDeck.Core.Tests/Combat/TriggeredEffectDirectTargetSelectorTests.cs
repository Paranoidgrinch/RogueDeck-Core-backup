using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TriggeredEffectDirectTargetSelectorTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");
    private static readonly CombatantId AllyId = new("ally_001");

    [Fact]
    public void CardPlayedActionsCanBeConstructedWithDirectTargetSelectors()
    {
        var amount = new FixedCardPlayedTriggeredEffectAmount(1);
        var statusId = new StatusDefinitionId("test.direct_card_played_status");

        ICardPlayedTriggeredEffectAction[] actions =
        {
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                amount),

            new DealDamageOnCardPlayedTriggeredEffectAction(
                CombatantTargetSelectors.AllEnemiesOfSource,
                amount),

            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                amount),

            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                amount,
                amount,
                amount)
        };

        Assert.Equal(4, actions.Length);
    }

    [Fact]
    public void TurnStartedTriggeredEffectCanUseLowestHealthEnemySelector()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        DealDamage(
            combat,
            registry,
            GoblinId,
            amount: 2);

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_started_lowest_health_enemy"));

        definition.Actions.Add(
            new DealDamageOnTurnStartedTriggeredEffectAction(
                CombatantTargetSelectors.LowestHealthEnemyOfSource,
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
            secondGoblinHealthBefore,
            secondGoblin.Health.Current);
    }

        [Fact]
    public void TurnEndedTriggeredEffectCanUseDamagedAlliesSelector()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        DealDamage(
            combat,
            registry,
            AllyId,
            amount: 2);

        var statusId = new StatusDefinitionId("test.damaged_ally_selector_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Buff);

        var definition = new TurnEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.turn_ended_damaged_allies"));

        definition.Actions.Add(
            new ApplyStatusOnTurnEndedTriggeredEffectAction(
                CombatantTargetSelectors.AllDamagedAlliesOfSource,
                statusId,
                new FixedTurnEndedTriggeredEffectAmount(1),
                new FixedTurnEndedTriggeredEffectAmount(0),
                new FixedTurnEndedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnEndedEvent(
            combat,
            registry,
            HeroId);

        Assert.DoesNotContain(
            combat.GetCombatant(HeroId).Statuses,
            status => status.DefinitionId == statusId);

        Assert.Equal(
            1,
            Assert.Single(
                combat.GetCombatant(AllyId).Statuses,
                status => status.DefinitionId == statusId).Stacks);

        Assert.DoesNotContain(
            combat.GetCombatant(GoblinId).Statuses,
            status => status.DefinitionId == statusId);
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

    private static void DealDamage(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: targetId,
            Amount: amount));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
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
}


