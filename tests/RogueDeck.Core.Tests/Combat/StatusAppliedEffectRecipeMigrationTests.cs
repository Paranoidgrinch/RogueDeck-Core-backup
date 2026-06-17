using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusAppliedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId SourceCardId =
        new("test.status_applied_source_card");

    [Fact]
    public void GenericGainBlockRecipesCanUseAppliedValuesAndStatusStacks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var appliedStatusId = new StatusDefinitionId(
            "test.status_applied_recipe_values");

        var targetStatusId = new StatusDefinitionId(
            "test.status_applied_recipe_target_stacks");

        var sourceStatusId = new StatusDefinitionId(
            "test.status_applied_recipe_source_stacks");

        RegisterStatus(
            registry,
            appliedStatusId,
            usesStacks: true,
            usesDuration: true,
            usesCharges: true);

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

        var definition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_recipe_amounts"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                appliedStatusId));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusAppliedTriggeredEffectAmount(4)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusAppliedStacksAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusAppliedDurationTurnsAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusAppliedChargesAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusAppliedTargetStatusStacksAmount(
                        targetStatusId)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusAppliedSourceStatusStacksAmount(
                        sourceStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            appliedStatusId,
            sourceCombatantId: GoblinId,
            sourceCardId: SourceCardId,
            stacks: 1,
            durationTurns: 2,
            charges: 3);

        Assert.Equal(
            15,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId(
            "test.status_applied_recipe_order_status");

        var executionOrder = new List<string>();

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true);

        var definition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_recipe_execution_order"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                statusId));

        definition.Steps.Add(
            new RecordingStatusAppliedAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingStatusAppliedAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingStatusAppliedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingStatusAppliedRecipe(
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
    public void GenericEffectsPreserveStatusContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId(
            "test.status_applied_recipe_attribution_status");

        var recipe = new CapturingStatusAppliedRecipe();

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true,
            usesDuration: true,
            usesCharges: true);

        var definition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_recipe_attribution"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                statusId));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            sourceCombatantId: GoblinId,
            sourceCardId: SourceCardId,
            stacks: 2,
            durationTurns: 3,
            charges: 4);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Equal(
            GoblinId,
            recipe.EffectSourceCombatantId);
        Assert.Equal(
            SourceCardId,
            recipe.EffectSourceCardId);
        Assert.Equal(
            HeroId,
            recipe.ContextTargetId);
        Assert.Equal(
            GoblinId,
            recipe.ContextSourceId);
        Assert.Equal(
            statusId,
            recipe.StatusDefinitionId);
        Assert.Equal(2, recipe.Stacks);
        Assert.Equal(3, recipe.DurationTurns);
        Assert.Equal(4, recipe.Charges);
    }

    [Fact]
    public void SourceFreeStatusUsesTargetingFallbackWithoutInventingAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId(
            "test.status_applied_recipe_source_free_status");

        var recipe = new CapturingStatusAppliedRecipe();

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true);

        var definition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_recipe_source_free"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                statusId));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            stacks: 1);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(
            HeroId,
            recipe.ContextTargetId);
        Assert.Null(recipe.ContextSourceId);
    }

    [Fact]
    public void StatusAppliedDefinitionSuppressesOwnRecursiveStatusApplication()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var initialStatusId = new StatusDefinitionId(
            "test.status_applied_self_reentry_initial");

        var recursiveStatusId = new StatusDefinitionId(
            "test.status_applied_self_reentry_recursive");

        RegisterStatus(
            registry,
            initialStatusId,
            usesStacks: true);

        RegisterStatus(
            registry,
            recursiveStatusId,
            usesStacks: true);

        var definition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_self_reentry"));

        definition.Steps.Add(
            new ApplyStatusOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                recursiveStatusId,
                new FixedStatusAppliedTriggeredEffectAmount(1),
                new FixedStatusAppliedTriggeredEffectAmount(0),
                new FixedStatusAppliedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            initialStatusId,
            stacks: 1);

        var hero = combat.GetCombatant(HeroId);

        Assert.Single(
            hero.Statuses,
            status =>
                status.DefinitionId == recursiveStatusId);

        Assert.Equal(
            2,
            combat.CombatLog.Count(
                entry =>
                    entry.Type ==
                    StandardCombatLogTypes.StatusApplied));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentStatusAppliedDefinitionsCanContinueTheSameChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var firstStatusId = new StatusDefinitionId(
            "test.status_applied_chain_first");

        var secondStatusId = new StatusDefinitionId(
            "test.status_applied_chain_second");

        RegisterStatus(
            registry,
            firstStatusId,
            usesStacks: true);

        RegisterStatus(
            registry,
            secondStatusId,
            usesStacks: true);

        var firstDefinition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_chain_apply"));

        firstDefinition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                firstStatusId));

        firstDefinition.Steps.Add(
            new ApplyStatusOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                secondStatusId,
                new FixedStatusAppliedTriggeredEffectAmount(1),
                new FixedStatusAppliedTriggeredEffectAmount(0),
                new FixedStatusAppliedTriggeredEffectAmount(0)));

        var secondDefinition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_chain_block"));

        secondDefinition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                secondStatusId));

        secondDefinition.Steps.Add(
            new GainBlockEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusAppliedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(
            firstDefinition);

        registry.RegisterTriggeredEffectDefinition(
            secondDefinition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            firstStatusId,
            stacks: 1);

        Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == secondStatusId);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void StatusAppliedHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);

        var statusId = new StatusDefinitionId(
            "test.status_applied_recipe_heal_status");

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true);

        DealDamage(
            combat,
            registry,
            HeroId,
            amount: 5);

        var statusDefinition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_recipe_heal"));

        statusDefinition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                statusId));

        statusDefinition.Steps.Add(
            new HealEffectRecipe<
                StatusAppliedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusAppliedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_applied_recipe_healed_follow_up"));

        healedDefinition.Steps.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(
            statusDefinition);

        registry.RegisterTriggeredEffectDefinition(
            healedDefinition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            statusId,
            sourceCombatantId: GoblinId,
            sourceCardId: SourceCardId,
            stacks: 1);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageDealtStatusAppliedDamageChainSuppressesOriginalDamageDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Current;

        var statusId = new StatusDefinitionId(
            "test.damage_status_damage_chain_status");

        RegisterStatus(
            registry,
            statusId,
            usesStacks: true);

        var damageDefinition =
            new DamageDealtTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_status_damage_chain_damage"));

        damageDefinition.Steps.Add(
            new ApplyStatusOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                new FixedDamageDealtTriggeredEffectAmount(1),
                new FixedDamageDealtTriggeredEffectAmount(0),
                new FixedDamageDealtTriggeredEffectAmount(0)));

        var statusDefinition =
            new StatusAppliedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_status_damage_chain_status_applied"));

        statusDefinition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(
                statusId));

        statusDefinition.Steps.Add(
            new DealDamageOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusAppliedTriggeredEffectAmount(1)));

        registry.RegisterTriggeredEffectDefinition(
            damageDefinition);

        registry.RegisterTriggeredEffectDefinition(
            statusDefinition);

        DealDamage(
            combat,
            registry,
            GoblinId,
            amount: 2,
            sourceCombatantId: HeroId);

        Assert.Equal(
            healthBefore - 3,
            goblin.Health.Current);

        Assert.Single(
            goblin.Statuses,
            status =>
                status.DefinitionId == statusId);

        Assert.Equal(
            2,
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
        bool usesCharges = false)
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
                    StatusStackingBehavior.CreateSeparateInstance));
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
        var combatant =
            combat.GetCombatant(combatantId);

        return combatant.DefensivePools.TryGetValue(
            StandardCombatIds.BlockDefensivePool,
            out var blockPool)
            ? blockPool.Current
            : 0;
    }

    private sealed class RecordingStatusAppliedAction
        : IStatusAppliedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusAppliedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                StatusAppliedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingStatusAppliedRecipe
        : ICombatEffectRecipe<
            StatusAppliedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusAppliedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                StatusAppliedTriggeredEffectContext context,
                TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingStatusAppliedRecipe
        : ICombatEffectRecipe<
            StatusAppliedTriggeredEffectContext>
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

        public StatusDefinitionId? StatusDefinitionId {
            get;
            private set;
        }

        public int? Stacks { get; private set; }

        public int? DurationTurns { get; private set; }

        public int? Charges { get; private set; }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                StatusAppliedTriggeredEffectContext context,
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
                    .Id;

            EffectSourceCombatantId =
                buildContext.Source.SourceCombatantId;

            EffectSourceCardId =
                buildContext.Source.SourceCardId;

            ContextTargetId =
                context.TargetCombatant.Id;

            ContextSourceId =
                context.SourceCombatant?.Id;

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
