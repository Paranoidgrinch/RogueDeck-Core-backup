using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TurnStartedEffectRecipePilotTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void GenericGainBlockRecipeFiresForEnemiesWhenTurnStarts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_generic_gain_block"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatValue<int>(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnStarted(
            combat,
            registry,
            HeroId);

        Assert.Equal(0, GetBlockOrZero(combat, HeroId));
        Assert.Equal(4, GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void GenericHealRecipeCanUseTurnCombatantStatusStacksAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        var statusId = new StatusDefinitionId(
            "test.turn_started_generic_heal_amount");
        var healthBefore = hero.Health.Max - 8;

        hero.Health.SetCurrent(healthBefore);

        RegisterStackingStatus(
            registry,
            statusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 4);

        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_generic_heal"));

        definition.Steps.Add(
            new HealEffectRecipe<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new TurnStartedCombatantStatusStacksAmount(statusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnStarted(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore + 4,
            hero.Health.Current);
    }

    [Fact]
    public void LegacyActionAndGenericEffectEachExecuteExactlyOnce()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        var healthBefore = hero.Health.Max - 10;
        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_legacy_and_generic"));

        hero.Health.SetCurrent(healthBefore);

        definition.Steps.Add(
            new HealOnTurnStartedTriggeredEffectAction(
                TurnStartedTriggeredEffectTarget.TurnCombatant,
                new FixedTurnStartedTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new HealEffectRecipe<TurnStartedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedCombatValue<int>(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnStarted(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore + 5,
            hero.Health.Current);
    }

    [Fact]
    public void ExistingFiltersApplyToGenericEffects()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var presentStatusId = new StatusDefinitionId(
            "test.turn_started_present_filter");
        var missingStatusId = new StatusDefinitionId(
            "test.turn_started_missing_filter");
        var matchingRecipe = new CountingRecipe();
        var nonMatchingRecipe = new CountingRecipe();

        RegisterStackingStatus(
            registry,
            presentStatusId);

        RegisterStackingStatus(
            registry,
            missingStatusId);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            presentStatusId,
            stacks: 1);

        var matching = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_matching_filter"));

        matching.Filters.Add(
            new TurnStartedCombatantHasStatusTriggerFilter(
                presentStatusId));

        matching.Steps.Add(matchingRecipe);

        var nonMatching = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_non_matching_filter"));

        nonMatching.Filters.Add(
            new TurnStartedCombatantHasStatusTriggerFilter(
                missingStatusId));

        nonMatching.Steps.Add(nonMatchingRecipe);

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        ResolveTurnStarted(
            combat,
            registry,
            HeroId);

        Assert.Equal(1, matchingRecipe.ActivationCount);
        Assert.Equal(0, nonMatchingRecipe.ActivationCount);
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();
        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_execution_order"));

        definition.Steps.Add(
            new RecordingAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnStarted(
            combat,
            registry,
            HeroId);

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
    public void GenericEffectsPreserveTurnStartedSourceSemantics()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingRecipe();
        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_generic_context"));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveTurnStarted(
            combat,
            registry,
            HeroId);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Null(recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
    }

    [Fact]
    public void GenericEffectsDoNotRunWhenTurnCombatantIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CountingRecipe();
        var definition = new TurnStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.turn_started_missing_combatant"));

        definition.Steps.Add(recipe);

        ICombatEventHandler handler =
            new TurnStartedTriggeredEffectHandler(definition);

        handler.Handle(
            combat,
            registry,
            new TurnStartedCombatEvent(
                new CombatantId("missing_combatant"),
                1,
                1));

        Assert.Equal(0, recipe.ActivationCount);
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
    }

    private static void ResolveTurnStarted(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId combatantId)
    {
        combat.EnqueueEvent(
            new TurnStartedCombatEvent(
                combatantId,
                1,
                1));

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
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        registry.RegisterStatus(definition);
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        int stacks)
    {
        combat.EnqueueEffect(
            new ApplyStatusEffectRequest(
                TargetCombatantId: targetId,
                StatusDefinitionId: statusId,
                Stacks: stacks,
                DurationTurns: 0,
                Charges: 0));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private sealed class RecordingAction
        : ITurnStartedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            TurnStartedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingRecipe
        : ICombatEffectRecipe<TurnStartedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            TurnStartedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CountingRecipe
        : ICombatEffectRecipe<TurnStartedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            TurnStartedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingRecipe
        : ICombatEffectRecipe<TurnStartedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }
        public CombatantId? TargetingSourceId { get; private set; }
        public CombatantId? EventTargetId { get; private set; }
        public CombatantId? EffectSourceCombatantId { get; private set; }
        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            TurnStartedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            TargetingSourceId =
                buildContext.TargetSelectionContext.Source.Id;
            EventTargetId =
                buildContext.TargetSelectionContext.EventTargetId;
            EffectSourceCombatantId =
                buildContext.Source.SourceCombatantId;
            EffectSourceCardId =
                buildContext.Source.SourceCardId;

            return Array.Empty<IEffectRequest>();
        }
    }
}
