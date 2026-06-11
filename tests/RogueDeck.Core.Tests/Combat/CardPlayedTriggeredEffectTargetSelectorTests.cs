using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardPlayedTriggeredEffectTargetSelectorTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");
    private static readonly CombatantId AllyId = new("ally_001");

    [Fact]
    public void AllEnemiesTargetSelectorCanDealDamageToEveryLivingEnemy()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_all_enemies_on_attack"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Actions.Add(
            new DealDamageOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.AllEnemies,
                new FixedCardPlayedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var firstGoblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var firstGoblinHealthBefore = firstGoblin.Health.Current;
        var secondGoblinHealthBefore = secondGoblin.Health.Current;

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        Assert.Equal(
            firstGoblinHealthBefore - 3,
            firstGoblin.Health.Current);

        Assert.Equal(
            secondGoblinHealthBefore - 3,
            secondGoblin.Health.Current);
    }

    [Fact]
    public void AllEnemiesTargetSelectorCanApplyStatusToEveryLivingEnemy()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var statusId = new StatusDefinitionId("test.all_enemies_mark");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_status_to_all_enemies"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.AllEnemies,
                statusId,
                new FixedCardPlayedTriggeredEffectAmount(2),
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(GoblinId).Statuses,
                status => status.DefinitionId == statusId).Stacks);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(SecondGoblinId).Statuses,
                status => status.DefinitionId == statusId).Stacks);
    }

    [Fact]
    public void AllAlliesTargetSelectorCanGainBlockForSourceTeam()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.block_all_allies_on_skill"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Actions.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.AllAllies,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            skillCardId);

        var hero = combat.GetCombatant(HeroId);
        var ally = combat.GetCombatant(AllyId);
        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(
            2,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        Assert.Equal(
            2,
            ally.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        Assert.False(goblin.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }

    [Fact]
    public void PlayedCardTargetSelectorStillTargetsOnlySelectedCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_selected_enemy_only"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Actions.Add(
            new DealDamageOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                new FixedCardPlayedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var firstGoblin = combat.GetCombatant(GoblinId);
        var secondGoblin = combat.GetCombatant(SecondGoblinId);

        var firstGoblinHealthBefore = firstGoblin.Health.Current;
        var secondGoblinHealthBefore = secondGoblin.Health.Current;

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        Assert.Equal(
            firstGoblinHealthBefore - 3,
            firstGoblin.Health.Current);

        Assert.Equal(
            secondGoblinHealthBefore,
            secondGoblin.Health.Current);
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

    private static void RegisterZeroCostCard(
        CombatDefinitionRegistry registry,
        CardDefinitionId cardId,
        TagId tagId)
    {
        var definition = new CardDefinition(
            cardId,
            new PackageId("test"),
            displayNameKey: $"card.{cardId}.name",
            descriptionKey: $"card.{cardId}.description");

        definition.Tags.Add(tagId);

        registry.RegisterCard(definition);
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

    private static void PlayCard(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinitionId cardId)
    {
        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: cardId,
                SourceCombatantId: HeroId));
    }

    private static void PlayCard(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinitionId cardId,
        CombatantId targetId)
    {
        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: cardId,
                SourceCombatantId: HeroId,
                TargetCombatantId: targetId));
    }
}
