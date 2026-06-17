using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusMergedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId OriginalSourceCardId =
        new("test.status_merged_original_source_card");
    private static readonly CardDefinitionId MergeRequestCardId =
        new("test.status_merged_request_card");

    [Fact]
    public void GenericGainBlockRecipesUseMergedTotalsAndStatusStacks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var mergedStatusId = new StatusDefinitionId(
            "test.status_merged_recipe_values");

        var targetStatusId = new StatusDefinitionId(
            "test.status_merged_recipe_target_stacks");

        var sourceStatusId = new StatusDefinitionId(
            "test.status_merged_recipe_source_stacks");

        RegisterStatus(
            registry,
            mergedStatusId,
            usesStacks: true,
            usesDuration: true,
            usesCharges: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        RegisterStatus(
            registry,
            targetStatusId,
            usesStacks: true);

        RegisterStatus(
            registry,
            sourceStatusId,
            usesStacks: true);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            targetStatusId,
            stacks: 2);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            sourceStatusId,
            stacks: 3);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            mergedStatusId,
            sourceCombatantId: GoblinId,
            sourceCardId: OriginalSourceCardId,
            stacks: 2,
            durationTurns: 5,
            charges: 3);

        var definition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_recipe_amounts"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                mergedStatusId));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusMergedTriggeredEffectAmount(1)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusMergedStacksAmount()));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusMergedDurationTurnsAmount()));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusMergedChargesAmount()));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusMergedTargetStatusStacksAmount(
                        targetStatusId)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusMergedSourceStatusStacksAmount(
                        sourceStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            mergedStatusId,
            sourceCombatantId: HeroId,
            sourceCardId: MergeRequestCardId,
            stacks: 4,
            durationTurns: 7,
            charges: 6);

        Assert.Equal(
            28,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var statusId = new StatusDefinitionId(
            "test.status_merged_recipe_order_status");
        var executionOrder = new List<string>();

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        var definition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_recipe_execution_order"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                statusId));

        definition.Actions.Add(
            new RecordingStatusMergedAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingStatusMergedAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingStatusMergedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingStatusMergedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

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
    public void GenericEffectsPreserveMergedContextInstanceAndOriginalAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var statusId = new StatusDefinitionId(
            "test.status_merged_recipe_attribution_status");
        var recipe = new CapturingStatusMergedRecipe();

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            usesDuration: true,
            usesCharges: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            sourceCombatantId: GoblinId,
            sourceCardId: OriginalSourceCardId,
            stacks: 2,
            durationTurns: 5,
            charges: 3);

        var originalStatus = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status => status.DefinitionId == statusId);

        var definition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_recipe_attribution"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                statusId));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            sourceCombatantId: HeroId,
            sourceCardId: MergeRequestCardId,
            stacks: 4,
            durationTurns: 7,
            charges: 6);

        var mergedStatus = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status => status.DefinitionId == statusId);

        Assert.Equal(originalStatus.Id, mergedStatus.Id);
        Assert.Equal(GoblinId, mergedStatus.SourceCombatantId);
        Assert.Equal(OriginalSourceCardId, mergedStatus.SourceCardId);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Equal(
            GoblinId,
            recipe.EffectSourceCombatantId);
        Assert.Equal(
            OriginalSourceCardId,
            recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Equal(GoblinId, recipe.ContextSourceId);
        Assert.Equal(originalStatus.Id, recipe.StatusInstanceId);
        Assert.Equal(statusId, recipe.StatusDefinitionId);
        Assert.Equal(6, recipe.Stacks);
        Assert.Equal(7, recipe.DurationTurns);
        Assert.Equal(9, recipe.Charges);
    }

    [Fact]
    public void SourceFreeExistingStatusUsesTargetingFallbackWithoutInventingAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var statusId = new StatusDefinitionId(
            "test.status_merged_recipe_source_free_status");
        var recipe = new CapturingStatusMergedRecipe();

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        var definition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_recipe_source_free"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                statusId));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            sourceCombatantId: GoblinId,
            sourceCardId: MergeRequestCardId,
            stacks: 1);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Null(recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Null(recipe.ContextSourceId);
    }

    [Fact]
    public void StatusMergedDefinitionSuppressesOwnRecursiveMerge()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var statusId = new StatusDefinitionId(
            "test.status_merged_self_reentry");

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        var definition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_self_reentry_definition"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                statusId));

        definition.Actions.Add(
            new ApplyStatusOnStatusMergedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                new FixedStatusMergedTriggeredEffectAmount(1),
                new FixedStatusMergedTriggeredEffectAmount(0),
                new FixedStatusMergedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        var status = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            current => current.DefinitionId == statusId);

        Assert.Equal(3, status.Stacks);
        Assert.Equal(
            2,
            combat.CombatLog.Count(
                entry =>
                    entry.Type ==
                    StandardCombatLogTypes.StatusMerged));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentStatusMergedDefinitionsCanContinueTheSameChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var statusId = new StatusDefinitionId(
            "test.status_merged_definition_chain");

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        var mergeAgain =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_chain_merge"));

        mergeAgain.Filters.Add(
            new ExactMergedStacksFilter(2));

        mergeAgain.Actions.Add(
            new ApplyStatusOnStatusMergedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                new FixedStatusMergedTriggeredEffectAmount(1),
                new FixedStatusMergedTriggeredEffectAmount(0),
                new FixedStatusMergedTriggeredEffectAmount(0)));

        var gainBlock =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_chain_block"));

        gainBlock.Filters.Add(
            new ExactMergedStacksFilter(3));

        gainBlock.Effects.Add(
            new GainBlockEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusMergedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(mergeAgain);
        registry.RegisterTriggeredEffectDefinition(gainBlock);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        var status = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            current => current.DefinitionId == statusId);

        Assert.Equal(3, status.Stacks);
        Assert.Equal(4, GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void StatusMergedHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        var statusId = new StatusDefinitionId(
            "test.status_merged_recipe_heal_status");

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        DealDamage(
            combat,
            registry,
            HeroId,
            amount: 5);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            sourceCombatantId: GoblinId,
            sourceCardId: OriginalSourceCardId,
            stacks: 1);

        var mergedDefinition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_recipe_heal"));

        mergedDefinition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                statusId));

        mergedDefinition.Effects.Add(
            new HealEffectRecipe<
                StatusMergedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusMergedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_recipe_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(
            mergedDefinition);
        registry.RegisterTriggeredEffectDefinition(
            healedDefinition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);
        Assert.Equal(3, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusMergedDamageStatusMergedChainSuppressesOriginalDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;
        var statusId = new StatusDefinitionId(
            "test.status_merged_damage_merge_status");

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            stackingBehavior:
                StatusStackingBehavior.MergeWithExistingInstance);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            statusId,
            sourceCombatantId: HeroId,
            sourceCardId: OriginalSourceCardId,
            stacks: 1);

        var mergedDefinition =
            new StatusMergedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_damage_merge_original"));

        mergedDefinition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(
                statusId));

        mergedDefinition.Actions.Add(
            new DealDamageOnStatusMergedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusMergedTriggeredEffectAmount(1)));

        var damageDefinition =
            new DamageDealtTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_merged_damage_merge_damage"));

        damageDefinition.Actions.Add(
            new ApplyStatusOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                new FixedDamageDealtTriggeredEffectAmount(1),
                new FixedDamageDealtTriggeredEffectAmount(0),
                new FixedDamageDealtTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(
            mergedDefinition);
        registry.RegisterTriggeredEffectDefinition(
            damageDefinition);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            statusId,
            stacks: 1);

        var status = Assert.Single(
            goblin.Statuses,
            current => current.DefinitionId == statusId);

        Assert.Equal(healthBefore - 1, goblin.Health.Current);
        Assert.Equal(3, status.Stacks);
        Assert.Equal(
            1,
            combat.CombatLog.Count(
                entry =>
                    entry.Type ==
                    StandardCombatLogTypes.DamageDealt));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    private static void RegisterStatus(
        CombatDefinitionRegistry registry,
        StatusDefinitionId statusId,
        bool usesStacks = false,
        bool usesDuration = false,
        bool usesCharges = false,
        StatusStackingBehavior stackingBehavior =
            StatusStackingBehavior.CreateSeparateInstance)
    {
        registry.RegisterStatus(
            new StatusDefinition(
                statusId,
                new PackageId("test"),
                displayNameKey:
                    $"status.{statusId}.name",
                descriptionKey:
                    $"status.{statusId}.description",
                polarity:
                    StatusPolarity.Neutral,
                usesStacks:
                    usesStacks,
                usesDuration:
                    usesDuration,
                usesCharges:
                    usesCharges,
                showStacksInUi:
                    usesStacks,
                showDurationInUi:
                    usesDuration,
                showChargesInUi:
                    usesCharges,
                stackingBehavior:
                    stackingBehavior));
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        CombatantId? sourceCombatantId = null,
        CardDefinitionId? sourceCardId = null,
        int stacks = 0,
        int durationTurns = 0,
        int charges = 0)
    {
        combat.EnqueueEffect(
            new ApplyStatusEffectRequest(
                TargetCombatantId:
                    targetId,
                StatusDefinitionId:
                    statusId,
                SourceCombatantId:
                    sourceCombatantId,
                SourceCardId:
                    sourceCardId,
                Stacks:
                    stacks,
                DurationTurns:
                    durationTurns,
                Charges:
                    charges));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private static void DealDamage(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount,
        CombatantId? sourceCombatantId = null)
    {
        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId:
                    targetId,
                Amount:
                    amount,
                SourceCombatantId:
                    sourceCombatantId));

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

    private sealed class ExactMergedStacksFilter
        : IStatusMergedTriggeredEffectFilter
    {
        private readonly int _expectedStacks;

        public ExactMergedStacksFilter(int expectedStacks)
        {
            _expectedStacks = expectedStacks;
        }

        public bool Matches(
            StatusMergedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.Stacks ==
                _expectedStacks;
        }
    }

    private sealed class RecordingStatusMergedAction
        : IStatusMergedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusMergedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                StatusMergedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingStatusMergedRecipe
        : ICombatEffectRecipe<
            StatusMergedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusMergedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                StatusMergedTriggeredEffectContext context,
                TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingStatusMergedRecipe
        : ICombatEffectRecipe<
            StatusMergedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId {
            get;
            private set;
        }

        public CardDefinitionId? EffectSourceCardId {
            get;
            private set;
        }

        public CombatantId? ContextTargetId {
            get;
            private set;
        }

        public CombatantId? ContextSourceId {
            get;
            private set;
        }

        public StatusInstanceId? StatusInstanceId {
            get;
            private set;
        }

        public StatusDefinitionId? StatusDefinitionId {
            get;
            private set;
        }

        public int? Stacks { get; private set; }

        public int? DurationTurns { get; private set; }

        public int? Charges { get; private set; }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                StatusMergedTriggeredEffectContext context,
                TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            EventTargetId =
                buildContext
                    .TargetSelectionContext
                    .EventTargetId;

            TargetingSourceId =
                buildContext
                    .TargetSelectionContext
                    .Source
                    ?.Id;

            EffectSourceCombatantId =
                buildContext.Source.SourceCombatantId;

            EffectSourceCardId =
                buildContext.Source.SourceCardId;

            ContextTargetId =
                context.TargetCombatant.Id;

            ContextSourceId =
                context.SourceCombatant?.Id;

            StatusInstanceId =
                context.CombatEvent.StatusInstanceId;

            StatusDefinitionId =
                context.CombatEvent.StatusDefinitionId;

            Stacks =
                context.CombatEvent.Stacks;

            DurationTurns =
                context.CombatEvent.DurationTurns;

            Charges =
                context.CombatEvent.Charges;

            return Array.Empty<IEffectRequest>();
        }
    }
}
