using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardLifecycleEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    // CardsDrawn

    [Fact]
    public void CardsDrawnGenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);
        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var definition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new CardsDrawnCardCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 2));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(4, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardsDrawnExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var matching = CreateCardsDrawnBlockDefinition("test.cards_drawn_filters_matching", amount: 5);
        matching.Filters.Add(new CardsDrawnCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));
        matching.Filters.Add(new CardsDrawnCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        var wrongDefinitionId = CreateCardsDrawnBlockDefinition("test.cards_drawn_filters_wrong_card", amount: 10);
        wrongDefinitionId.Filters.Add(new CardsDrawnCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));

        var wrongTag = CreateCardsDrawnBlockDefinition("test.cards_drawn_filters_wrong_tag", amount: 20);
        wrongTag.Filters.Add(new CardsDrawnCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(wrongDefinitionId);
        registry.RegisterTriggeredEffectDefinition(wrongTag);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(5, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardsDrawnGenericEffectsPreserveContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingCardsDrawnRecipe();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var definition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Same(combat, recipe.ContextCombat);
        Assert.Same(registry, recipe.ContextRegistry);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(1, recipe.CardCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var definition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_recipe_order"));

        definition.Steps.Add(new RecordingCardsDrawnAction(executionOrder, "legacy-a"));
        definition.Steps.Add(new RecordingCardsDrawnAction(executionOrder, "legacy-b"));
        definition.Steps.Add(new RecordingCardsDrawnRecipe(executionOrder, "recipe-a"));
        definition.Steps.Add(new RecordingCardsDrawnRecipe(executionOrder, "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(
            new[] { "legacy-a", "legacy-b", "recipe-a", "recipe-b" },
            executionOrder);
    }

    [Fact]
    public void DefinitionsPreserveRegistrationOrderAcrossLegacyAndRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var firstDefinition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_definition_order_first"));
        firstDefinition.Steps.Add(new RecordingCardsDrawnAction(executionOrder, "first-legacy"));
        firstDefinition.Steps.Add(new RecordingCardsDrawnRecipe(executionOrder, "first-recipe"));

        var secondDefinition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_definition_order_second"));
        secondDefinition.Steps.Add(new RecordingCardsDrawnAction(executionOrder, "second-legacy"));
        secondDefinition.Steps.Add(new RecordingCardsDrawnRecipe(executionOrder, "second-recipe"));

        registry.RegisterTriggeredEffectDefinition(firstDefinition);
        registry.RegisterTriggeredEffectDefinition(secondDefinition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(
            new[] { "first-legacy", "first-recipe", "second-legacy", "second-recipe" },
            executionOrder);
    }

    [Fact]
    public void CardsDrawnHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(combat, registry, amount: 5);
        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var drawnDefinition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_recipe_heal"));

        drawnDefinition.Steps.Add(
            new HealEffectRecipe<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(2)));

        var healedDefinition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_healed_follow_up"));

        healedDefinition.Steps.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(drawnDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(hero.Health.Max - 3, hero.Health.Current);
        Assert.Equal(3, GetBlockOrZero(combat, HeroId));
    }

    // DiscardPileShuffled

    [Fact]
    public void DiscardPileShuffledGenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DiscardPile);
        AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.DiscardPile);

        var definition = new DiscardPileShuffledTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.discard_shuffled_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DiscardPileShuffledTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDiscardPileShuffledTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DiscardPileShuffledTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new DiscardPileShuffledCardCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(4, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DiscardPileShuffledExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DiscardPile);

        var matching = CreateDiscardPileShuffledBlockDefinition("test.discard_shuffled_filters_matching", amount: 5);
        matching.Filters.Add(new DiscardPileShuffledCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));

        var wrongDefinitionId = CreateDiscardPileShuffledBlockDefinition("test.discard_shuffled_filters_wrong_card", amount: 10);
        wrongDefinitionId.Filters.Add(new DiscardPileShuffledCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(wrongDefinitionId);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(5, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DiscardPileShuffledGenericEffectsPreserveContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingDiscardPileShuffledRecipe();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DiscardPile);

        var definition = new DiscardPileShuffledTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.discard_shuffled_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(1, recipe.CardCount);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
    }

    [Fact]
    public void DiscardPileShuffledLegacyActionAndGenericRecipeAgreeOnEventTargetAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DiscardPile);

        var definition = new DiscardPileShuffledTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.discard_shuffled_event_target_agreement"));

        definition.Steps.Add(
            new GainBlockOnDiscardPileShuffledTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDiscardPileShuffledTriggeredEffectAmount(3)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DiscardPileShuffledTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedDiscardPileShuffledTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        // Both the legacy action and the recipe now resolve EventTarget to HeroId.
        Assert.Equal(8, GetBlockOrZero(combat, HeroId));
    }

    // HandDiscarded

    [Fact]
    public void HandDiscardedGenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);
        AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.Hand);

        var definition = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<HandDiscardedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<HandDiscardedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new HandDiscardedCardCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(4, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HandDiscardedExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var matching = CreateHandDiscardedBlockDefinition("test.hand_discarded_filters_matching", amount: 5);
        matching.Filters.Add(new HandDiscardedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        var wrongTag = CreateHandDiscardedBlockDefinition("test.hand_discarded_filters_wrong_tag", amount: 10);
        wrongTag.Filters.Add(new HandDiscardedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(wrongTag);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(5, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HandDiscardedGenericEffectsPreserveContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingHandDiscardedRecipe();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var definition = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(1, recipe.CardCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
    }

    // CardExhausted

    [Fact]
    public void CardExhaustedGenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var card = AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var definition = new CardExhaustedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_exhausted_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardExhaustedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardExhaustedTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardExhaustedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new CardExhaustedCardCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new MoveCardToZoneEffectRequest(HeroId, card.Id, CardZone.ExhaustPile));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(3, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardExhaustedExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var card = AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var matching = CreateCardExhaustedBlockDefinition("test.card_exhausted_filters_matching", amount: 5);
        matching.Filters.Add(new CardExhaustedCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));

        var wrongDefinitionId = CreateCardExhaustedBlockDefinition("test.card_exhausted_filters_wrong_card", amount: 10);
        wrongDefinitionId.Filters.Add(new CardExhaustedCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(wrongDefinitionId);

        combat.EnqueueEffect(new MoveCardToZoneEffectRequest(HeroId, card.Id, CardZone.ExhaustPile));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(5, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardExhaustedGenericEffectsPreserveContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var card = AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);
        var recipe = new CapturingCardExhaustedRecipe();

        var definition = new CardExhaustedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_exhausted_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new MoveCardToZoneEffectRequest(HeroId, card.Id, CardZone.ExhaustPile));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(StandardCombatIds.StrikeCard, recipe.CardDefinitionId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
    }

    [Fact]
    public void CardExhaustedMissingCardDataDoesNotBreakGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = CreateCardExhaustedBlockDefinition("test.card_exhausted_missing_card", amount: 3);
        definition.Filters.Add(new CardExhaustedCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEvent(new CardMovedToZoneCombatEvent(
            HeroId,
            new CardInstanceId("missing_card_instance"),
            FromZone: CardZone.Hand,
            ToZone: CardZone.ExhaustPile));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(0, GetBlockOrZero(combat, HeroId));
    }

    // CardBanished

    [Fact]
    public void CardBanishedGenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var card = AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var definition = new CardBanishedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_banished_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardBanishedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardBanishedTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardBanishedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new CardBanishedCardCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new MoveCardToZoneEffectRequest(HeroId, card.Id, CardZone.BanishedPile));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(3, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardBanishedExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var card = AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.Hand);

        var matching = CreateCardBanishedBlockDefinition("test.card_banished_filters_matching", amount: 5);
        matching.Filters.Add(new CardBanishedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        var wrongTag = CreateCardBanishedBlockDefinition("test.card_banished_filters_wrong_tag", amount: 10);
        wrongTag.Filters.Add(new CardBanishedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(wrongTag);

        combat.EnqueueEffect(new MoveCardToZoneEffectRequest(HeroId, card.Id, CardZone.BanishedPile));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(5, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardBanishedGenericEffectsPreserveContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var card = AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.Hand);
        var recipe = new CapturingCardBanishedRecipe();

        var definition = new CardBanishedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_banished_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new MoveCardToZoneEffectRequest(HeroId, card.Id, CardZone.BanishedPile));
        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(StandardCombatIds.DefendCard, recipe.CardDefinitionId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
    }

    // Helpers

    private static CardsDrawnTriggeredEffectDefinition CreateCardsDrawnBlockDefinition(string id, int amount)
    {
        var definition = new CardsDrawnTriggeredEffectDefinition(new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(amount)));

        return definition;
    }

    private static DiscardPileShuffledTriggeredEffectDefinition CreateDiscardPileShuffledBlockDefinition(string id, int amount)
    {
        var definition = new DiscardPileShuffledTriggeredEffectDefinition(new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DiscardPileShuffledTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDiscardPileShuffledTriggeredEffectAmount(amount)));

        return definition;
    }

    private static HandDiscardedTriggeredEffectDefinition CreateHandDiscardedBlockDefinition(string id, int amount)
    {
        var definition = new HandDiscardedTriggeredEffectDefinition(new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<HandDiscardedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(amount)));

        return definition;
    }

    private static CardExhaustedTriggeredEffectDefinition CreateCardExhaustedBlockDefinition(string id, int amount)
    {
        var definition = new CardExhaustedTriggeredEffectDefinition(new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardExhaustedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardExhaustedTriggeredEffectAmount(amount)));

        return definition;
    }

    private static CardBanishedTriggeredEffectDefinition CreateCardBanishedBlockDefinition(string id, int amount)
    {
        var definition = new CardBanishedTriggeredEffectDefinition(new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardBanishedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardBanishedTriggeredEffectAmount(amount)));

        return definition;
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

    private static void DamageHero(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int amount)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: HeroId,
            Amount: amount));

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

    private sealed class RecordingCardsDrawnAction : ICardsDrawnTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCardsDrawnAction(ICollection<string> executionOrder, string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(CardsDrawnTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingCardsDrawnRecipe : ICombatEffectRecipe<CardsDrawnTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCardsDrawnRecipe(ICollection<string> executionOrder, string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardsDrawnTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingCardsDrawnRecipe : ICombatEffectRecipe<CardsDrawnTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatState? ContextCombat { get; private set; }

        public CombatDefinitionRegistry? ContextRegistry { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public int CardCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardsDrawnTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextCombat = context.Combat;
            ContextRegistry = context.Registry;
            ContextSourceId = context.Source.Id;
            CardCount = context.Cards.Count;
            EventTargetId = buildContext.TargetSelectionContext.EventTargetId;
            TargetingSourceId = buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId = buildContext.Source.SourceCombatantId;
            EffectSourceCardId = buildContext.Source.SourceCardId;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingDiscardPileShuffledRecipe : ICombatEffectRecipe<DiscardPileShuffledTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public int CardCount { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            DiscardPileShuffledTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextSourceId = context.Source.Id;
            CardCount = context.Cards.Count;
            TargetingSourceId = buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId = buildContext.Source.SourceCombatantId;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingHandDiscardedRecipe : ICombatEffectRecipe<HandDiscardedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public int CardCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            HandDiscardedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextSourceId = context.Source.Id;
            CardCount = context.Cards.Count;
            EventTargetId = buildContext.TargetSelectionContext.EventTargetId;
            TargetingSourceId = buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId = buildContext.Source.SourceCombatantId;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingCardExhaustedRecipe : ICombatEffectRecipe<CardExhaustedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public CardDefinitionId? CardDefinitionId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardExhaustedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextSourceId = context.Source.Id;
            CardDefinitionId = context.CardDefinition?.Id;
            TargetingSourceId = buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId = buildContext.Source.SourceCombatantId;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingCardBanishedRecipe : ICombatEffectRecipe<CardBanishedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public CardDefinitionId? CardDefinitionId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardBanishedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextSourceId = context.Source.Id;
            CardDefinitionId = context.CardDefinition?.Id;
            TargetingSourceId = buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId = buildContext.Source.SourceCombatantId;

            return Array.Empty<IEffectRequest>();
        }
    }
}
