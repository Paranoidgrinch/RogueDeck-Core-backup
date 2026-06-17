using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardPlayedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void CardPlayedTriggeredEffectCanGainBlockWhenAttackCardIsPlayed()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.gain_block_when_attack_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: attackCardId,
                SourceCombatantId: HeroId,
                TargetCombatantId: GoblinId));

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            2,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    [Fact]
    public void CardPlayedTriggeredEffectDoesNotRunWhenCardTagFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.gain_block_when_attack_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: skillCardId,
                SourceCombatantId: HeroId));

        var hero = combat.GetCombatant(HeroId);

        Assert.False(hero.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }

    [Fact]
    public void CardPlayedTriggeredEffectIgnoresNonPositiveAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.zero_block_when_attack_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: attackCardId,
                SourceCombatantId: HeroId,
                TargetCombatantId: GoblinId));

        var hero = combat.GetCombatant(HeroId);

        Assert.False(hero.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }

    [Fact]
    public void CardPlayedTriggeredEffectCanUseSourceStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var stackStatusId = new StatusDefinitionId("test.stack_counter");
        RegisterStackingStatus(registry, stackStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            stackStatusId,
            stacks: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            stackStatusId,
            stacks: 3);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.gain_block_equal_to_status_stacks"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(stackStatusId));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(stackStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: attackCardId,
                SourceCombatantId: HeroId,
                TargetCombatantId: GoblinId));

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            5,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    [Fact]
    public void CardPlayedTriggeredEffectCanTriggerOnEveryNthCardWithTagThisTurn()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.gain_block_on_every_third_skill"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 3));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);

        var hero = combat.GetCombatant(HeroId);

        Assert.False(hero.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));

        PlayCard(combat, registry, skillCardId);

        Assert.Equal(
            2,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    [Fact]
    public void CardPlayedEveryNthCardWithTagThisTurnFilterCanTriggerAgainOnNextInterval()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.gain_block_on_every_third_skill"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 3));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            4,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    [Fact]
    public void CardPlayedEveryNthCardWithTagThisTurnFilterRejectsInvalidIntervals()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 0));

        Assert.Equal("interval", exception.ParamName);
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
    [Fact]
    public void CardPlayedTriggeredEffectCanDrawCards()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var markerCardId = new CardDefinitionId("test.marker");
        RegisterMarkerCard(registry, markerCardId);
        AddCardToZone(combat, HeroId, markerCardId, CardZone.DrawPile);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.draw_when_skill_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Steps.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(1)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);

        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == markerCardId);
    }

    [Fact]
    public void CardPlayedTriggeredEffectCanDrawCardsUsingSourceStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var stackStatusId = new StatusDefinitionId("test.draw_counter");
        RegisterStackingStatus(registry, stackStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            stackStatusId,
            stacks: 2);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var firstMarkerCardId = new CardDefinitionId("test.marker_one");
        var secondMarkerCardId = new CardDefinitionId("test.marker_two");

        RegisterMarkerCard(registry, firstMarkerCardId);
        RegisterMarkerCard(registry, secondMarkerCardId);

        AddCardToZone(combat, HeroId, firstMarkerCardId, CardZone.DrawPile);
        AddCardToZone(combat, HeroId, secondMarkerCardId, CardZone.DrawPile);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.draw_equal_to_status_stacks"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(stackStatusId));

        definition.Steps.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(stackStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);

        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == firstMarkerCardId);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == secondMarkerCardId);
    }

    [Fact]
    public void CardPlayedTriggeredEffectDoesNotDrawCardsForNonPositiveAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var markerCardId = new CardDefinitionId("test.marker");
        RegisterMarkerCard(registry, markerCardId);
        AddCardToZone(combat, HeroId, markerCardId, CardZone.DrawPile);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.draw_zero_when_skill_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Steps.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);

        Assert.Single(combat.GetCardZones(HeroId).DrawPile);

        Assert.DoesNotContain(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == markerCardId);
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
    private static void RegisterMarkerCard(
        CombatDefinitionRegistry registry,
        CardDefinitionId cardId)
    {
        if (registry.TryGetCard(cardId, out _))
            return;

        var definition = new CardDefinition(
            cardId,
            new PackageId("test"),
            displayNameKey: $"card.{cardId}.name",
            descriptionKey: $"card.{cardId}.description");

        registry.RegisterCard(definition);
    }

    private static CardInstance AddCardToZone(
        CombatState combat,
        CombatantId ownerId,
        CardDefinitionId definitionId,
        CardZone zone)
    {
        var card = new CardInstance(
            combat.CreateNextCardInstanceId(),
            definitionId,
            ownerId,
            zone);

        combat.GetCardZones(ownerId).AddCard(card);

        return card;
    }
    [Fact]
    public void GenericCardPlayedTriggerCanExpressSkillComboDraw()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var comboStatusId = new StatusDefinitionId("test.generic_skill_combo_draw");
        RegisterStackingStatus(registry, comboStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            comboStatusId,
            stacks: 2);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var firstMarkerCardId = new CardDefinitionId("test.marker_one");
        var secondMarkerCardId = new CardDefinitionId("test.marker_two");

        RegisterMarkerCard(registry, firstMarkerCardId);
        RegisterMarkerCard(registry, secondMarkerCardId);

        AddCardToZone(combat, HeroId, firstMarkerCardId, CardZone.DrawPile);
        AddCardToZone(combat, HeroId, secondMarkerCardId, CardZone.DrawPile);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.generic_skill_combo_draw"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(comboStatusId));

        definition.Filters.Add(
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 3));

        definition.Steps.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(comboStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);

        Assert.Equal(
            2,
            combat.GetCardZones(HeroId).DrawPile.Count);

        Assert.DoesNotContain(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == firstMarkerCardId);

        PlayCard(combat, registry, skillCardId);

        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == firstMarkerCardId);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == secondMarkerCardId);
    }

    [Fact]
    public void GenericSkillComboDrawCanTriggerAgainOnSixthSkill()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var comboStatusId = new StatusDefinitionId("test.generic_skill_combo_draw");
        RegisterStackingStatus(registry, comboStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            comboStatusId,
            stacks: 1);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var firstMarkerCardId = new CardDefinitionId("test.marker_one");
        var secondMarkerCardId = new CardDefinitionId("test.marker_two");

        RegisterMarkerCard(registry, firstMarkerCardId);
        RegisterMarkerCard(registry, secondMarkerCardId);

        AddCardToZone(combat, HeroId, firstMarkerCardId, CardZone.DrawPile);
        AddCardToZone(combat, HeroId, secondMarkerCardId, CardZone.DrawPile);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.generic_skill_combo_draw"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(comboStatusId));

        definition.Filters.Add(
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 3));

        definition.Steps.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(comboStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);

        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == firstMarkerCardId);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == secondMarkerCardId);
    }

    [Fact]
    public void GenericSkillComboDrawDoesNotTriggerWithoutSourceStatus()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var comboStatusId = new StatusDefinitionId("test.generic_skill_combo_draw");
        RegisterStackingStatus(registry, comboStatusId);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var markerCardId = new CardDefinitionId("test.marker");
        RegisterMarkerCard(registry, markerCardId);
        AddCardToZone(combat, HeroId, markerCardId, CardZone.DrawPile);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.generic_skill_combo_draw"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(comboStatusId));

        definition.Filters.Add(
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 3));

        definition.Steps.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(comboStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);
        PlayCard(combat, registry, skillCardId);

        Assert.Single(combat.GetCardZones(HeroId).DrawPile);

        Assert.DoesNotContain(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == markerCardId);
    }
    [Fact]
    public void GenericCardPlayedTriggerCanExpressRage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var rageLikeStatusId = new StatusDefinitionId("test.generic_rage");
        RegisterStackingStatus(registry, rageLikeStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            rageLikeStatusId,
            stacks: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            rageLikeStatusId,
            stacks: 3);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.generic_rage"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(rageLikeStatusId));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(rageLikeStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            5,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    [Fact]
    public void GenericRageDoesNotTriggerWhenSkillIsPlayed()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var rageLikeStatusId = new StatusDefinitionId("test.generic_rage");
        RegisterStackingStatus(registry, rageLikeStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            rageLikeStatusId,
            stacks: 2);

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.generic_rage"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(rageLikeStatusId));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(rageLikeStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            skillCardId);

        var hero = combat.GetCombatant(HeroId);

        Assert.False(hero.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }

    [Fact]
    public void GenericRageDoesNotTriggerWithoutSourceStatus()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var rageLikeStatusId = new StatusDefinitionId("test.generic_rage");
        RegisterStackingStatus(registry, rageLikeStatusId);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.generic_rage"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(rageLikeStatusId));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(rageLikeStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        var hero = combat.GetCombatant(HeroId);

        Assert.False(hero.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }
    [Fact]
    public void CardPlayedTriggeredEffectHandlerCanReadDefinitionsFromRegistry()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.registry_triggered_block"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Steps.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            2,
            hero.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
    }

    [Fact]
    public void CombatDefinitionRegistryRejectsDuplicateTriggeredEffectDefinitions()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var id = new TriggeredEffectDefinitionId("test.duplicate_triggered_effect");

        registry.RegisterTriggeredEffectDefinition(
            new CardPlayedTriggeredEffectDefinition(id));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterTriggeredEffectDefinition(
                new CardPlayedTriggeredEffectDefinition(id)));

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void CombatDefinitionRegistryReturnsTriggeredEffectDefinitionsByEventType()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_triggered_effect"));

        registry.RegisterTriggeredEffectDefinition(definition);

        var definitions = registry.GetTriggeredEffectDefinitions(typeof(CardPlayedCombatEvent));

        Assert.Contains(definition, definitions);
    }
    [Fact]
    public void StandardCombatPackageRegistersCardPlayedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(CardPlayedCombatEvent)),
            handler => handler is CardPlayedTriggeredEffectHandler);
    }
    [Fact]
    public void CardPlayedTriggeredEffectCanDealDamageToPlayedCardTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.deal_damage_when_attack_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Steps.Add(
            new DealDamageOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                new FixedCardPlayedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        Assert.Equal(
            healthBefore - 3,
            goblin.Health.Current);
    }

    [Fact]
    public void CardPlayedTriggeredEffectCanDealDamageUsingSourceStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var damageStatusId = new StatusDefinitionId("test.damage_counter");
        RegisterStackingStatus(registry, damageStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            damageStatusId,
            stacks: 4);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.deal_damage_equal_to_status_stacks"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(damageStatusId));

        definition.Steps.Add(
            new DealDamageOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                new CardPlayedSourceStatusStacksAmount(damageStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        Assert.Equal(
            healthBefore - 4,
            goblin.Health.Current);
    }

    [Fact]
    public void CardPlayedTriggeredEffectDoesNotDealDamageWhenPlayedCardTargetIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var skillCardId = new CardDefinitionId("test.zero_cost_skill");
        RegisterZeroCostCard(
            registry,
            skillCardId,
            StandardCombatIds.SkillCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.deal_damage_without_target"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Steps.Add(
            new DealDamageOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                new FixedCardPlayedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        PlayCard(combat, registry, skillCardId);

        Assert.Equal(
            healthBefore,
            goblin.Health.Current);
    }

    [Fact]
    public void CardPlayedTriggeredEffectDoesNotDealDamageForNonPositiveAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(
            registry,
            attackCardId,
            StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.deal_zero_damage"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Steps.Add(
            new DealDamageOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.PlayedCardTarget,
                new FixedCardPlayedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        PlayCard(
            combat,
            registry,
            attackCardId,
            GoblinId);

        Assert.Equal(
            healthBefore,
            goblin.Health.Current);
    }
    private static void RegisterStackingStatus(
        CombatDefinitionRegistry registry,
        StatusDefinitionId statusId)
    {
        var definition = new StatusDefinition(
            statusId,
            new PackageId("test"),
            displayNameKey: $"status.{statusId}.name",
            descriptionKey: $"status.{statusId}.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);

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
}









