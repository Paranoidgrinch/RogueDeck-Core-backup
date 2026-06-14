using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusesRemovedByPolarityEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void GenericGainBlockRecipesUseFixedAndRemovedStatusCountAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 2);

        var definition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_recipe_amounts"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusesRemovedByPolarityTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusesRemovedByPolarityTriggeredEffectAmount(3)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusesRemovedByPolarityTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusesRemovedByPolarityRemovedStatusCount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 2);

        var matchingDefinition = CreateBlockDefinition(
            "test.statuses_removed_by_polarity_filters_matching",
            amount: 5);

        matchingDefinition.Filters.Add(
            new StatusesRemovedByPolarityTriggerFilter(
                StatusPolarity.Debuff));

        matchingDefinition.Filters.Add(
            new StatusesRemovedByPolarityTargetTriggerFilter(
                HeroId));

        matchingDefinition.Filters.Add(
            new StatusesRemovedByPolarityMinimumCountTriggerFilter(
                MinimumCount: 2));

        var wrongPolarityDefinition = CreateBlockDefinition(
            "test.statuses_removed_by_polarity_filters_wrong_polarity",
            amount: 10);

        wrongPolarityDefinition.Filters.Add(
            new StatusesRemovedByPolarityTriggerFilter(
                StatusPolarity.Buff));

        var wrongTargetDefinition = CreateBlockDefinition(
            "test.statuses_removed_by_polarity_filters_wrong_target",
            amount: 20);

        wrongTargetDefinition.Filters.Add(
            new StatusesRemovedByPolarityTargetTriggerFilter(
                GoblinId));

        var insufficientCountDefinition = CreateBlockDefinition(
            "test.statuses_removed_by_polarity_filters_insufficient_count",
            amount: 40);

        insufficientCountDefinition.Filters.Add(
            new StatusesRemovedByPolarityMinimumCountTriggerFilter(
                MinimumCount: 3));

        registry.RegisterTriggeredEffectDefinition(matchingDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongPolarityDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongTargetDefinition);
        registry.RegisterTriggeredEffectDefinition(insufficientCountDefinition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

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

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_recipe_order"));

        definition.Actions.Add(
            new RecordingStatusesRemovedByPolarityAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingStatusesRemovedByPolarityAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingStatusesRemovedByPolarityRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingStatusesRemovedByPolarityRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

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
    public void GenericEffectsPreserveAggregatedContextAndSourceFreeAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 2);

        var hero = combat.GetCombatant(HeroId);
        var expectedRemovedStatusIds = hero.Statuses
            .Where(status =>
                status.DefinitionId == StandardCombatIds.WeakStatus ||
                status.DefinitionId == StandardCombatIds.VulnerableStatus)
            .Select(status => status.Id)
            .ToArray();

        var recipe =
            new CapturingStatusesRemovedByPolarityRecipe();

        var definition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_recipe_context"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Equal(StatusPolarity.Debuff, recipe.Polarity);
        Assert.Equal(expectedRemovedStatusIds, recipe.StatusInstanceIds);
        Assert.False(recipe.RemovedStatusesStillPresent);

        var remainingStatus = Assert.Single(hero.Statuses);
        Assert.Equal(
            StandardCombatIds.StrengthStatus,
            remainingStatus.DefinitionId);
    }

    [Fact]
    public void GenericSourceSelectorUsesTargetingFallbackWithoutEffectAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_source_fallback"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusesRemovedByPolarityTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new FixedStatusesRemovedByPolarityTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursivePolarityRemoval()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var recipe = new ReapplyAndRemoveDebuffRecipe();

        var definition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_self_reentry"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Empty(combat.GetCombatant(HeroId).Statuses);

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry =>
                entry.Type ==
                    StandardCombatLogTypes.StatusesRemovedByPolarity));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionCanContinueRecursivePolarityRemovalChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 2);

        var recursiveDefinition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_chain_reapply"));

        recursiveDefinition.Effects.Add(
            new ReapplyAndRemoveDebuffRecipe());

        var observingDefinition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_chain_observe"));

        observingDefinition.Filters.Add(
            new ExactRemovedStatusCountFilter(1));

        observingDefinition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusesRemovedByPolarityTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusesRemovedByPolarityTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(recursiveDefinition);
        registry.RegisterTriggeredEffectDefinition(observingDefinition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Empty(combat.GetCombatant(HeroId).Statuses);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void PolarityRemovalHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(
            combat,
            registry,
            amount: 5);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var removalDefinition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_recipe_heal"));

        removalDefinition.Effects.Add(
            new HealEffectRecipe<
                StatusesRemovedByPolarityTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusesRemovedByPolarityTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.statuses_removed_by_polarity_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(removalDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        RemoveStatuses(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    private static StatusesRemovedByPolarityTriggeredEffectDefinition
        CreateBlockDefinition(
            string id,
            int amount)
    {
        var definition =
            new StatusesRemovedByPolarityTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(id));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusesRemovedByPolarityTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusesRemovedByPolarityTriggeredEffectAmount(
                        amount)));

        return definition;
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        int stacks = 0,
        int durationTurns = 0,
        int charges = 0)
    {
        combat.EnqueueEffect(
            new ApplyStatusEffectRequest(
                TargetCombatantId: targetId,
                StatusDefinitionId: statusId,
                Stacks: stacks,
                DurationTurns: durationTurns,
                Charges: charges));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private static void RemoveStatuses(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusPolarity polarity)
    {
        combat.EnqueueEffect(
            new RemoveStatusesByPolarityEffectRequest(
                TargetCombatantId: targetId,
                Polarity: polarity));

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

    private sealed class RecordingStatusesRemovedByPolarityAction
        : IStatusesRemovedByPolarityTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusesRemovedByPolarityAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusesRemovedByPolarityTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingStatusesRemovedByPolarityRecipe
        : ICombatEffectRecipe<
            StatusesRemovedByPolarityTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusesRemovedByPolarityRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusesRemovedByPolarityTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingStatusesRemovedByPolarityRecipe
        : ICombatEffectRecipe<
            StatusesRemovedByPolarityTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextTargetId { get; private set; }

        public StatusPolarity? Polarity { get; private set; }

        public IReadOnlyList<StatusInstanceId> StatusInstanceIds { get; private set; }
            = Array.Empty<StatusInstanceId>();

        public bool RemovedStatusesStillPresent { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusesRemovedByPolarityTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            EventTargetId =
                buildContext.TargetSelectionContext.EventTargetId;
            TargetingSourceId =
                buildContext.TargetSelectionContext.Source.Id;
            EffectSourceCombatantId =
                buildContext.Source.SourceCombatantId;
            EffectSourceCardId =
                buildContext.Source.SourceCardId;
            ContextTargetId = context.TargetCombatant.Id;
            Polarity = context.CombatEvent.Polarity;
            StatusInstanceIds =
                context.CombatEvent.StatusInstanceIds.ToArray();

            RemovedStatusesStillPresent =
                context.TargetCombatant.Statuses.Any(status =>
                    context.CombatEvent.StatusInstanceIds.Contains(
                        status.Id));

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class ReapplyAndRemoveDebuffRecipe
        : ICombatEffectRecipe<
            StatusesRemovedByPolarityTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusesRemovedByPolarityTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            return new IEffectRequest[]
            {
                new ApplyStatusEffectRequest(
                    TargetCombatantId:
                        context.CombatEvent.TargetCombatantId,
                    StatusDefinitionId:
                        StandardCombatIds.WeakStatus,
                    DurationTurns: 1),
                new RemoveStatusesByPolarityEffectRequest(
                    TargetCombatantId:
                        context.CombatEvent.TargetCombatantId,
                    Polarity: StatusPolarity.Debuff)
            };
        }
    }

    private sealed record ExactRemovedStatusCountFilter(int Count)
        : IStatusesRemovedByPolarityTriggeredEffectFilter
    {
        public bool Matches(
            StatusesRemovedByPolarityTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.StatusInstanceIds.Count == Count;
        }
    }
}
