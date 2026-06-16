using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardPlayedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void GenericGainBlockRecipesUseAllExistingAmountProviders()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(registry, attackCardId, StandardCombatIds.AttackCardTag);

        var stackStatusId = new StatusDefinitionId("test.recipe_stack_counter");
        RegisterStackingStatus(registry, stackStatusId);

        ApplyStatus(combat, registry, HeroId, stackStatusId, stacks: 3);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_recipe_amounts"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Effects.Add(
            new GainBlockEffectRecipe<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new CardPlayedSourceStatusStacksAmount(stackStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, attackCardId, GoblinId);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(registry, attackCardId, StandardCombatIds.AttackCardTag);

        var matchingDefinition = CreateBlockDefinition(
            "test.card_played_filters_matching",
            amount: 5);

        matchingDefinition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        var wrongTagDefinition = CreateBlockDefinition(
            "test.card_played_filters_wrong_tag",
            amount: 10);

        wrongTagDefinition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        registry.RegisterTriggeredEffectDefinition(matchingDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongTagDefinition);

        PlayCard(combat, registry, attackCardId, GoblinId);

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

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(registry, attackCardId, StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_recipe_order"));

        definition.Actions.Add(
            new RecordingCardPlayedAction(executionOrder, "legacy-a"));

        definition.Actions.Add(
            new RecordingCardPlayedAction(executionOrder, "legacy-b"));

        definition.Effects.Add(
            new RecordingCardPlayedRecipe(executionOrder, "recipe-a"));

        definition.Effects.Add(
            new RecordingCardPlayedRecipe(executionOrder, "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, attackCardId, GoblinId);

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

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(registry, attackCardId, StandardCombatIds.AttackCardTag);

        var firstDefinition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_definition_order_first"));

        firstDefinition.Actions.Add(
            new RecordingCardPlayedAction(executionOrder, "first-legacy"));

        firstDefinition.Effects.Add(
            new RecordingCardPlayedRecipe(executionOrder, "first-recipe"));

        var secondDefinition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_definition_order_second"));

        secondDefinition.Actions.Add(
            new RecordingCardPlayedAction(executionOrder, "second-legacy"));

        secondDefinition.Effects.Add(
            new RecordingCardPlayedRecipe(executionOrder, "second-recipe"));

        registry.RegisterTriggeredEffectDefinition(firstDefinition);
        registry.RegisterTriggeredEffectDefinition(secondDefinition);

        PlayCard(combat, registry, attackCardId, GoblinId);

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
        var recipe = new CapturingCardPlayedRecipe();

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(registry, attackCardId, StandardCombatIds.AttackCardTag);

        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_recipe_context"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        PlayCard(combat, registry, attackCardId, GoblinId);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Same(combat, recipe.ContextCombat);
        Assert.Same(registry, recipe.ContextRegistry);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(attackCardId, recipe.ContextCardDefinitionId);
        Assert.Equal(HeroId, recipe.EventSourceCombatantId);
        Assert.Equal(GoblinId, recipe.EventTargetCombatantId);
        Assert.Equal(GoblinId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Equal(attackCardId, recipe.EffectSourceCardId);
    }

    [Fact]
    public void CardPlayedHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(combat, registry, amount: 5);

        var attackCardId = new CardDefinitionId("test.zero_cost_attack");
        RegisterZeroCostCard(registry, attackCardId, StandardCombatIds.AttackCardTag);

        var playedDefinition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_recipe_heal"));

        playedDefinition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        playedDefinition.Effects.Add(
            new HealEffectRecipe<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardPlayedTriggeredEffectAmount(2)));

        var healedDefinition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_played_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(playedDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        PlayCard(combat, registry, attackCardId, GoblinId);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    private static CardPlayedTriggeredEffectDefinition CreateBlockDefinition(
        string id,
        int amount)
    {
        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(id));

        definition.Effects.Add(
            new GainBlockEffectRecipe<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCardPlayedTriggeredEffectAmount(amount)));

        return definition;
    }

    private static void PlayCard(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardDefinitionId cardId,
        CombatantId? targetId = null)
    {
        new CombatCardPlayProcessor().PlayCard(
            combat,
            registry,
            new CardPlayRequest(
                CardDefinitionId: cardId,
                SourceCombatantId: HeroId,
                TargetCombatantId: targetId));
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

    private sealed class RecordingCardPlayedAction
        : ICardPlayedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCardPlayedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardPlayedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingCardPlayedRecipe
        : ICombatEffectRecipe<CardPlayedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCardPlayedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardPlayedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingCardPlayedRecipe
        : ICombatEffectRecipe<CardPlayedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatState? ContextCombat { get; private set; }

        public CombatDefinitionRegistry? ContextRegistry { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public CardDefinitionId? ContextCardDefinitionId { get; private set; }

        public CombatantId? EventSourceCombatantId { get; private set; }

        public CombatantId? EventTargetCombatantId { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardPlayedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextCombat = context.Combat;
            ContextRegistry = context.Registry;
            ContextSourceId = context.Source.Id;
            ContextCardDefinitionId = context.Card.Id;
            EventSourceCombatantId = context.CombatEvent.SourceCombatantId;
            EventTargetCombatantId = context.CombatEvent.TargetCombatantId;
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
}
