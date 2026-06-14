using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusRemovedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void GenericGainBlockRecipesUseAllExistingAmountProviders()
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
            StandardCombatIds.RageStatus,
            stacks: 3);

        var definition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_recipe_amounts"));

        definition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusRemovedTriggeredEffectAmount(2)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new StatusRemovedCountAmount()));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new StatusRemovedTargetStatusStacksAmount(
                    StandardCombatIds.RageStatus)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new StatusRemovedSourceStatusStacksAmount(
                    StandardCombatIds.RageStatus)));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            6,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ExistingFiltersGateGenericRecipesAfterRemoval()
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
            StandardCombatIds.RageStatus,
            stacks: 2);

        var matchingDefinition = CreateBlockDefinition(
            "test.status_removed_filters_matching",
            amount: 5);

        matchingDefinition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        matchingDefinition.Filters.Add(
            new StatusRemovedTargetHasStatusTriggerFilter(
                StandardCombatIds.RageStatus));

        var wrongDefinition = CreateBlockDefinition(
            "test.status_removed_filters_wrong_definition",
            amount: 10);

        wrongDefinition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.VulnerableStatus));

        var removedTargetStatusDefinition = CreateBlockDefinition(
            "test.status_removed_filters_removed_target_status",
            amount: 20);

        removedTargetStatusDefinition.Filters.Add(
            new StatusRemovedTargetHasStatusTriggerFilter(
                StandardCombatIds.WeakStatus));

        var requiresSourceDefinition = CreateBlockDefinition(
            "test.status_removed_filters_requires_source",
            amount: 40);

        requiresSourceDefinition.Filters.Add(
            new StatusRemovedHasSourceTriggerFilter());

        var sourceStatusDefinition = CreateBlockDefinition(
            "test.status_removed_filters_source_status",
            amount: 80);

        sourceStatusDefinition.Filters.Add(
            new StatusRemovedSourceHasStatusTriggerFilter(
                StandardCombatIds.RageStatus));

        registry.RegisterTriggeredEffectDefinition(matchingDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongDefinition);
        registry.RegisterTriggeredEffectDefinition(
            removedTargetStatusDefinition);
        registry.RegisterTriggeredEffectDefinition(
            requiresSourceDefinition);
        registry.RegisterTriggeredEffectDefinition(
            sourceStatusDefinition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

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
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_recipe_order"));

        definition.Actions.Add(
            new RecordingStatusRemovedAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingStatusRemovedAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingStatusRemovedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingStatusRemovedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

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
    public void GenericEffectsPreserveRemovedContextAndSourceFreeAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var removedStatus = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.WeakStatus);

        var recipe = new CapturingStatusRemovedRecipe();

        var definition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_recipe_context"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Null(recipe.ContextSourceCombatantId);
        Assert.Equal(
            StandardCombatIds.WeakStatus,
            recipe.StatusDefinitionId);
        Assert.Equal(
            new[] { removedStatus.Id },
            recipe.StatusInstanceIds);
        Assert.False(recipe.RemovedStatusesStillPresent);
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
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_source_fallback"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedStatusRemovedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursiveRemoval()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var recipe =
            new ReapplyAndRemoveStatusRecipe(
                StandardCombatIds.WeakStatus);

        var definition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_self_reentry"));

        definition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Empty(combat.GetCombatant(HeroId).Statuses);

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry =>
                entry.Type == StandardCombatLogTypes.StatusRemoved));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionCanContinueRecursiveRemovalChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var recursiveRecipe =
            new ReapplyAndRemoveStatusRecipe(
                StandardCombatIds.VulnerableStatus);

        var recursiveDefinition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_chain_reapply"));

        recursiveDefinition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        recursiveDefinition.Effects.Add(recursiveRecipe);

        var observingDefinition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_chain_observe"));

        observingDefinition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.VulnerableStatus));

        observingDefinition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusRemovedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(recursiveDefinition);
        registry.RegisterTriggeredEffectDefinition(observingDefinition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(1, recursiveRecipe.ActivationCount);
        Assert.Empty(combat.GetCombatant(HeroId).Statuses);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void StatusRemovalHealCanContinueIntoHealedDefinition()
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

        var removedDefinition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_recipe_heal"));

        removedDefinition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        removedDefinition.Effects.Add(
            new HealEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusRemovedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_removed_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(removedDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    private static StatusRemovedTriggeredEffectDefinition
        CreateBlockDefinition(
            string id,
            int amount)
    {
        var definition =
            new StatusRemovedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(id));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusRemovedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusRemovedTriggeredEffectAmount(amount)));

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

    private static void RemoveStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId)
    {
        combat.EnqueueEffect(
            new RemoveStatusEffectRequest(
                TargetCombatantId: targetId,
                StatusDefinitionId: statusId));

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

    private sealed class RecordingStatusRemovedAction
        : IStatusRemovedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusRemovedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusRemovedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingStatusRemovedRecipe
        : ICombatEffectRecipe<StatusRemovedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusRemovedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusRemovedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingStatusRemovedRecipe
        : ICombatEffectRecipe<StatusRemovedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextTargetId { get; private set; }

        public CombatantId? ContextSourceCombatantId { get; private set; }

        public StatusDefinitionId? StatusDefinitionId { get; private set; }

        public IReadOnlyList<StatusInstanceId> StatusInstanceIds { get; private set; }
            = Array.Empty<StatusInstanceId>();

        public bool RemovedStatusesStillPresent { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusRemovedTriggeredEffectContext context,
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
            ContextSourceCombatantId = context.SourceCombatant?.Id;
            StatusDefinitionId =
                context.CombatEvent.StatusDefinitionId;
            StatusInstanceIds =
                context.CombatEvent.StatusInstanceIds.ToArray();

            RemovedStatusesStillPresent =
                context.TargetCombatant.Statuses.Any(status =>
                    context.CombatEvent.StatusInstanceIds.Contains(
                        status.Id));

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class ReapplyAndRemoveStatusRecipe
        : ICombatEffectRecipe<StatusRemovedTriggeredEffectContext>
    {
        private readonly StatusDefinitionId _statusDefinitionId;

        public ReapplyAndRemoveStatusRecipe(
            StatusDefinitionId statusDefinitionId)
        {
            _statusDefinitionId = statusDefinitionId;
        }

        public int ActivationCount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusRemovedTriggeredEffectContext context,
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
                        _statusDefinitionId,
                    DurationTurns: 1),
                new RemoveStatusEffectRequest(
                    TargetCombatantId:
                        context.CombatEvent.TargetCombatantId,
                    StatusDefinitionId:
                        _statusDefinitionId)
            };
        }
    }
}
