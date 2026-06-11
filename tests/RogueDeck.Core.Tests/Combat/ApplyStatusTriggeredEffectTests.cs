using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class ApplyStatusTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void CardPlayedTriggeredEffectCanApplyStatusToPlayedCardTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.triggered_mark");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_status_to_card_target"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
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

        var goblin = combat.GetCombatant(GoblinId);

        var status = Assert.Single(
            goblin.Statuses,
            instance => instance.DefinitionId == statusId);

        Assert.Equal(2, status.Stacks);
    }

    [Fact]
    public void CardPlayedTriggeredEffectCanApplyStatusToSource()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.triggered_focus");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Buff);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_status_to_source"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                statusId,
                new FixedCardPlayedTriggeredEffectAmount(1),
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            skillCardId);

        var hero = combat.GetCombatant(HeroId);

        var status = Assert.Single(
            hero.Statuses,
            instance => instance.DefinitionId == statusId);

        Assert.Equal(1, status.Stacks);
    }

    [Fact]
    public void CardPlayedTriggeredEffectCanApplyStatusUsingSourceStatusStacks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var counterStatusId = new StatusDefinitionId("test.status_application_counter");
        RegisterStackingStatus(registry, counterStatusId, StatusPolarity.Buff);

        var appliedStatusId = new StatusDefinitionId("test.triggered_burn");
        RegisterStackingStatus(registry, appliedStatusId, StatusPolarity.Debuff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            counterStatusId,
            stacks: 4);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_status_equal_to_source_status_stacks"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(counterStatusId));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                appliedStatusId,
                new CardPlayedSourceStatusStacksAmount(counterStatusId),
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        var goblin = combat.GetCombatant(GoblinId);

        var status = Assert.Single(
            goblin.Statuses,
            instance => instance.DefinitionId == appliedStatusId);

        Assert.Equal(4, status.Stacks);
    }

    [Fact]
    public void CardPlayedTriggeredEffectAppliesStatusThroughExistingMergeRules()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.triggered_merge_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            statusId,
            stacks: 1);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_status_merges_with_existing_status"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                statusId,
                new FixedCardPlayedTriggeredEffectAmount(3),
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        var goblin = combat.GetCombatant(GoblinId);

        var status = Assert.Single(
            goblin.Statuses,
            instance => instance.DefinitionId == statusId);

        Assert.Equal(4, status.Stacks);
    }

    [Fact]
    public void CardPlayedTriggeredEffectDoesNotApplyStatusWhenPlayedCardTargetIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.missing_target_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_status_without_target"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                statusId,
                new FixedCardPlayedTriggeredEffectAmount(2),
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            skillCardId);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.DoesNotContain(
            goblin.Statuses,
            instance => instance.DefinitionId == statusId);
    }

    [Fact]
    public void CardPlayedTriggeredEffectDoesNotApplyStatusWhenAllStatusValuesAreZero()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.zero_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.apply_zero_status"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Actions.Add(
            new ApplyStatusOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                statusId,
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0),
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.DoesNotContain(
            goblin.Statuses,
            instance => instance.DefinitionId == statusId);
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
