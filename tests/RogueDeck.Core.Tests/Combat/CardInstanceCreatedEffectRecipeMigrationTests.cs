using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardInstanceCreatedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void GenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardInstanceCreatedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardInstanceCreatedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new CardInstanceCreatedCount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 3);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matchingDefinition = CreateBlockDefinition(
            "test.card_instance_created_filters_matching",
            amount: 5);

        matchingDefinition.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(
                StandardCombatIds.StrikeCard));

        matchingDefinition.Filters.Add(
            new CardInstanceCreatedToZoneTriggerFilter(
                CardZone.Hand));

        matchingDefinition.Filters.Add(
            new CardInstanceCreatedMinimumCountTriggerFilter(
                MinimumCount: 2));

        var wrongCardDefinition = CreateBlockDefinition(
            "test.card_instance_created_filters_wrong_card",
            amount: 10);

        wrongCardDefinition.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(
                StandardCombatIds.DefendCard));

        var wrongZoneDefinition = CreateBlockDefinition(
            "test.card_instance_created_filters_wrong_zone",
            amount: 20);

        wrongZoneDefinition.Filters.Add(
            new CardInstanceCreatedToZoneTriggerFilter(
                CardZone.DrawPile));

        var insufficientCountDefinition = CreateBlockDefinition(
            "test.card_instance_created_filters_insufficient_count",
            amount: 40);

        insufficientCountDefinition.Filters.Add(
            new CardInstanceCreatedMinimumCountTriggerFilter(
                MinimumCount: 3));

        registry.RegisterTriggeredEffectDefinition(matchingDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongCardDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongZoneDefinition);
        registry.RegisterTriggeredEffectDefinition(
            insufficientCountDefinition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 2);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();

        var definition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_recipe_order"));

        definition.Steps.Add(
            new RecordingCardInstanceCreatedAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingCardInstanceCreatedAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingCardInstanceCreatedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingCardInstanceCreatedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(
            new[]
            {
                "legacy-a",
                "legacy-b",
                "recipe-a",
                "recipe-b"
            },
            executionOrder);
    }

    [Fact]
    public void DefinitionsPreserveRegistrationOrderAcrossLegacyAndRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();

        var firstDefinition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_definition_order_first"));

        firstDefinition.Steps.Add(
            new RecordingCardInstanceCreatedAction(
                executionOrder,
                "first-legacy"));

        firstDefinition.Steps.Add(
            new RecordingCardInstanceCreatedRecipe(
                executionOrder,
                "first-recipe"));

        var secondDefinition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_definition_order_second"));

        secondDefinition.Steps.Add(
            new RecordingCardInstanceCreatedAction(
                executionOrder,
                "second-legacy"));

        secondDefinition.Steps.Add(
            new RecordingCardInstanceCreatedRecipe(
                executionOrder,
                "second-recipe"));

        registry.RegisterTriggeredEffectDefinition(firstDefinition);
        registry.RegisterTriggeredEffectDefinition(secondDefinition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(
            new[]
            {
                "first-legacy",
                "first-recipe",
                "second-legacy",
                "second-recipe"
            },
            executionOrder);
    }

    [Fact]
    public void GenericEffectsPreserveContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingCardInstanceCreatedRecipe();

        var definition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 2);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Same(combat, recipe.ContextCombat);
        Assert.Same(registry, recipe.ContextRegistry);
        Assert.Equal(HeroId, recipe.ContextOwnerId);
        Assert.Equal(HeroId, recipe.EventCombatantId);
        Assert.Equal(
            StandardCombatIds.StrikeCard,
            recipe.CardDefinitionId);
        Assert.Equal(CardZone.Hand, recipe.EventToZone);
        Assert.Equal(2, recipe.CardInstanceIds.Count);
        Assert.Equal(
            new[] { CardZone.Hand, CardZone.Hand },
            recipe.CurrentZones);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursiveCardCreation()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var zones = combat.GetCardZones(HeroId);
        var strikeCountBefore = zones.AllCards.Count(card =>
            card.DefinitionId == StandardCombatIds.StrikeCard);

        var recipe =
            new CreateCardInstanceRecipe(
                StandardCombatIds.StrikeCard,
                CardZone.Hand);

        var definition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_self_reentry"));

        definition.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(
                StandardCombatIds.StrikeCard));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(
            strikeCountBefore + 2,
            zones.AllCards.Count(card =>
                card.DefinitionId == StandardCombatIds.StrikeCard));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionCanContinueCardCreationChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var creatingRecipe =
            new CreateCardInstanceRecipe(
                StandardCombatIds.DefendCard,
                CardZone.Hand);

        var creatingDefinition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_chain_create"));

        creatingDefinition.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(
                StandardCombatIds.StrikeCard));

        creatingDefinition.Steps.Add(creatingRecipe);

        var observingDefinition = CreateBlockDefinition(
            "test.card_instance_created_chain_observe",
            amount: 4);

        observingDefinition.Filters.Add(
            new CardInstanceCreatedCardDefinitionTriggerFilter(
                StandardCombatIds.DefendCard));

        registry.RegisterTriggeredEffectDefinition(creatingDefinition);
        registry.RegisterTriggeredEffectDefinition(observingDefinition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(1, creatingRecipe.ActivationCount);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void CardCreationHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(
            combat,
            registry,
            amount: 5);

        var createdDefinition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_recipe_heal"));

        createdDefinition.Steps.Add(
            new HealEffectRecipe<CardInstanceCreatedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_healed_follow_up"));

        healedDefinition.Steps.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(createdDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        CreateCardInstances(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand,
            count: 1);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void EventZoneRemainsHistoricalAfterCardMovesBeforeEventHandling()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CurrentZoneCapturingRecipe();

        var definition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.card_instance_created_historical_zone"));

        definition.Filters.Add(
            new CardInstanceCreatedToZoneTriggerFilter(
                CardZone.Hand));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        var createdCardId = new CardInstanceId(
            $"card_{combat.NextCardInstanceNumber:000000}");

        combat.EnqueueEffect(
            new CreateCardInstanceEffectRequest(
                CombatantId: HeroId,
                CardDefinitionId: StandardCombatIds.StrikeCard,
                ToZone: CardZone.Hand,
                Count: 1));

        combat.EnqueueEffect(
            new MoveCardToZoneEffectRequest(
                CombatantId: HeroId,
                CardInstanceId: createdCardId,
                ToZone: CardZone.DiscardPile));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(CardZone.Hand, recipe.EventToZone);
        Assert.Equal(CardZone.DiscardPile, recipe.CurrentZone);
        Assert.Equal(
            CardZone.DiscardPile,
            combat.GetCardZones(HeroId).GetCard(createdCardId).Zone);
    }

    private static CardInstanceCreatedTriggeredEffectDefinition
        CreateBlockDefinition(
            string id,
            int amount)
    {
        var definition =
            new CardInstanceCreatedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CardInstanceCreatedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCardInstanceCreatedTriggeredEffectAmount(amount)));

        return definition;
    }

    private static void CreateCardInstances(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId combatantId,
        CardDefinitionId cardDefinitionId,
        CardZone toZone,
        int count)
    {
        combat.EnqueueEffect(
            new CreateCardInstanceEffectRequest(
                CombatantId: combatantId,
                CardDefinitionId: cardDefinitionId,
                ToZone: toZone,
                Count: count));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private static void DamageHero(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int amount)
    {
        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: HeroId,
                Amount: amount));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
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

    private sealed class RecordingCardInstanceCreatedAction
        : ICardInstanceCreatedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCardInstanceCreatedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardInstanceCreatedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingCardInstanceCreatedRecipe
        : ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCardInstanceCreatedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardInstanceCreatedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingCardInstanceCreatedRecipe
        : ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatState? ContextCombat { get; private set; }

        public CombatDefinitionRegistry? ContextRegistry { get; private set; }

        public CombatantId? ContextOwnerId { get; private set; }

        public CombatantId? EventCombatantId { get; private set; }

        public CardDefinitionId? CardDefinitionId { get; private set; }

        public CardZone? EventToZone { get; private set; }

        public IReadOnlyList<CardInstanceId> CardInstanceIds { get; private set; }
            = Array.Empty<CardInstanceId>();

        public IReadOnlyList<CardZone> CurrentZones { get; private set; }
            = Array.Empty<CardZone>();

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardInstanceCreatedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextCombat = context.Combat;
            ContextRegistry = context.Registry;
            ContextOwnerId = context.OwnerCombatant.Id;
            EventCombatantId = context.CombatEvent.CombatantId;
            CardDefinitionId = context.CombatEvent.CardDefinitionId;
            EventToZone = context.CombatEvent.ToZone;
            CardInstanceIds = context.CombatEvent.CardInstanceIds.ToArray();
            CurrentZones = context.CombatEvent.CardInstanceIds
                .Select(cardInstanceId =>
                    context.Combat
                        .GetCardZones(context.CombatEvent.CombatantId)
                        .GetCard(cardInstanceId)
                        .Zone)
                .ToArray();
            EventTargetId =
                buildContext.TargetSelectionContext.EventTargetId;
            TargetingSourceId =
                buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId =
                buildContext.Source.SourceCombatantId;
            EffectSourceCardId =
                buildContext.Source.SourceCardId;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CreateCardInstanceRecipe
        : ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>
    {
        private readonly CardDefinitionId _cardDefinitionId;
        private readonly CardZone _toZone;

        public CreateCardInstanceRecipe(
            CardDefinitionId cardDefinitionId,
            CardZone toZone)
        {
            _cardDefinitionId = cardDefinitionId;
            _toZone = toZone;
        }

        public int ActivationCount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardInstanceCreatedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            return new IEffectRequest[]
            {
                new CreateCardInstanceEffectRequest(
                    CombatantId: context.CombatEvent.CombatantId,
                    CardDefinitionId: _cardDefinitionId,
                    ToZone: _toZone,
                    Count: 1)
            };
        }
    }

    private sealed class CurrentZoneCapturingRecipe
        : ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CardZone? EventToZone { get; private set; }

        public CardZone? CurrentZone { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardInstanceCreatedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            EventToZone = context.CombatEvent.ToZone;

            var cardInstanceId = Assert.Single(
                context.CombatEvent.CardInstanceIds);

            CurrentZone = context.Combat
                .GetCardZones(context.CombatEvent.CombatantId)
                .GetCard(cardInstanceId)
                .Zone;

            return Array.Empty<IEffectRequest>();
        }
    }
}
