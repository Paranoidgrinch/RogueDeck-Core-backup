using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardInstanceCreatedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersCardInstanceCreatedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(CardInstanceCreatedCombatEvent)),
            handler => handler is CardInstanceCreatedTriggeredEffectHandler);
    }

    [Fact]
    public void CardInstanceCreatedTriggeredEffectFiresWhenCardInstanceIsCreated()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_gain_block"));

        definition.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void CardInstanceCreatedTriggeredEffectFiltersByCardDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_matching_card"));

        matching.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));

        matching.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(2)));

        var nonMatching = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_non_matching_card"));

        nonMatching.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));

        nonMatching.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardInstanceCreatedTriggeredEffectFiltersByTargetZone()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_matching_zone"));

        matching.Filters.Add(
            new CardInstanceCreatedToZoneTriggerFilter(CardZone.DrawPile));

        matching.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(3)));

        var nonMatching = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_non_matching_zone"));

        nonMatching.Filters.Add(
            new CardInstanceCreatedToZoneTriggerFilter(CardZone.Hand));

        nonMatching.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(9)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.DrawPile,
            count: 1);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardInstanceCreatedTriggeredEffectMinimumCountFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_minimum_count"));

        definition.Filters.Add(
            new CardInstanceCreatedMinimumCountTriggerFilter(MinimumCount: 3));

        definition.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 2);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardInstanceCreatedTriggeredEffectCanUseCreatedCardCount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_count_amount"));

        definition.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new CardInstanceCreatedCount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 2);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardInstanceCreatedTriggeredEffectCanUseSourceSelectorForOwner()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CardInstanceCreatedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_instance_created_source_selector"));

        definition.Actions.Add(
            new GainBlockOnCardInstanceCreatedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardInstanceCreatedTriggeredEffectAmount(6)));

        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(
            6,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    private static void CreateCardInstances(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId combatantId,
        CardDefinitionId cardDefinitionId,
        CardZone toZone,
        int count)
    {
        combat.EnqueueEffect(new CreateCardInstanceEffectRequest(
            CombatantId: combatantId,
            CardDefinitionId: cardDefinitionId,
            ToZone: toZone,
            Count: count));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static int GetBlockOrZero(
        CombatState combat,
        CombatantId combatantId)
    {
        var combatant = combat.GetCombatant(combatantId);

        return combatant.DefensivePools.TryGetValue(
            StandardCombatIds.BlockDefensivePool,
            out var blockPool)
                ? blockPool.Current
                : 0;
    }
}