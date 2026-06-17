using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DamageDealtEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId DamageCardId = new("test.damage_card");

    [Fact]
    public void GenericGainBlockRecipesCanUseRequestedBlockedAndHealthDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var goblin = combat.GetCombatant(GoblinId);

        goblin.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 2));

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new DamageDealtHealthDamageAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new DamageDealtBlockedDamageAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new DamageDealtRequestedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 5,
            sourceCombatantId: HeroId);

        Assert.Equal(9, goblin.Health.Current);
        Assert.Equal(
            0,
            goblin.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        Assert.Equal(
            10,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void GenericGainBlockRecipesCanUseSourceAndTargetStatusStacks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var sourceStatusId = new StatusDefinitionId(
            "test.damage_dealt_recipe_source_status");
        var targetStatusId = new StatusDefinitionId(
            "test.damage_dealt_recipe_target_status");

        RegisterStackingStatus(
            registry,
            sourceStatusId,
            StatusPolarity.Buff);

        RegisterStackingStatus(
            registry,
            targetStatusId,
            StatusPolarity.Debuff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            sourceStatusId,
            stacks: 2);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            targetStatusId,
            stacks: 3);

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_status_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new DamageDealtSourceStatusStacksAmount(sourceStatusId)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new DamageDealtTargetStatusStacksAmount(targetStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 1,
            sourceCombatantId: HeroId);

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

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_execution_order"));

        definition.Steps.Add(
            new RecordingDamageDealtAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingDamageDealtAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingDamageDealtRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingDamageDealtRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 1,
            sourceCombatantId: HeroId);

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
    public void GenericEffectsPreserveDamageContextTargetingAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingDamageDealtRecipe();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_attribution"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 4,
            sourceCombatantId: HeroId,
            sourceCardId: DamageCardId,
            kind: DamageKind.DamageOverTime);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(GoblinId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Equal(DamageCardId, recipe.EffectSourceCardId);
        Assert.Equal(GoblinId, recipe.ContextTargetId);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(DamageKind.DamageOverTime, recipe.Kind);
        Assert.Equal(4, recipe.RequestedAmount);
        Assert.Equal(4, recipe.HealthDamage);
        Assert.Equal(0, recipe.BlockedDamage);
    }

    [Fact]
    public void SourceFreeDamageUsesTargetingFallbackWithoutInventingAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingDamageDealtRecipe();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_source_free"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 2);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(GoblinId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(GoblinId, recipe.ContextTargetId);
        Assert.Null(recipe.ContextSourceId);
    }

    [Fact]
    public void DamageDealtDefinitionSuppressesOwnRecursiveDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_self_reentry"));

        definition.Steps.Add(
            new DealDamageOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageDealtTriggeredEffectAmount(1)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 2,
            sourceCombatantId: HeroId);

        Assert.Equal(
            9,
            combat.GetCombatant(GoblinId).Health.Current);
        Assert.Equal(
            2,
            combat.CombatLog.Count(
                entry => entry.Type == StandardCombatLogTypes.DamageDealt));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDamageDealtDefinitionsCanContinueTheSameChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var followUpDamage = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_follow_up_damage"));

        followUpDamage.Filters.Add(
            new ExactHealthDamageFilter(2));

        followUpDamage.Steps.Add(
            new DealDamageOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageDealtTriggeredEffectAmount(1)));

        var followUpBlock = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_follow_up_block"));

        followUpBlock.Filters.Add(
            new ExactHealthDamageFilter(1));

        followUpBlock.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(followUpDamage);
        registry.RegisterTriggeredEffectDefinition(followUpBlock);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 2,
            sourceCombatantId: HeroId);

        Assert.Equal(
            9,
            combat.GetCombatant(GoblinId).Health.Current);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DamageDealtHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(
            combat,
            registry,
            amount: 5);

        var damageDefinition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_heal"));

        damageDefinition.Filters.Add(
            new DamageDealtHasSourceTriggerFilter());

        damageDefinition.Steps.Add(
            new HealEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(2)));

        var healedDefinition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_healed_follow_up"));

        healedDefinition.Steps.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(damageDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 1,
            sourceCombatantId: HeroId,
            sourceCardId: DamageCardId);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);
        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void OriginalDamageReceivedSiblingStillRuns()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var damageDealtDefinition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_sibling"));

        damageDealtDefinition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(2)));

        var damageReceivedDefinition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_received_original_sibling"));

        damageReceivedDefinition.Steps.Add(
            new GainBlockOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(damageDealtDefinition);
        registry.RegisterTriggeredEffectDefinition(damageReceivedDefinition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 1,
            sourceCombatantId: HeroId);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
        Assert.Equal(
            3,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void LethalDamageStillProcessesRecipeAndStandardLifecycleResult()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.damage_dealt_recipe_lethal"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 99,
            sourceCombatantId: HeroId);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
        Assert.Equal(0, goblin.Health.Current);
        Assert.Equal(
            CombatantLifecycleState.Downed,
            goblin.LifecycleState);
        Assert.Equal(
            CombatResult.Victory,
            combat.Result);
    }

    private static void DealDamage(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount,
        CombatantId? sourceCombatantId = null,
        CardDefinitionId? sourceCardId = null,
        DamageKind kind = DamageKind.Direct)
    {
        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: targetId,
                Amount: amount,
                SourceCombatantId: sourceCombatantId,
                SourceCardId: sourceCardId,
                Kind: kind));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private static void DamageHero(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int amount)
    {
        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: amount);
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
        StatusDefinitionId statusId,
        StatusPolarity polarity)
    {
        var definition = new StatusDefinition(
            statusId,
            new PackageId("test"),
            displayNameKey: $"status.{statusId}.name",
            descriptionKey: $"status.{statusId}.description",
            polarity: polarity,
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

    private sealed class ExactHealthDamageFilter
        : IDamageDealtTriggeredEffectFilter
    {
        private readonly int _expectedHealthDamage;

        public ExactHealthDamageFilter(
            int expectedHealthDamage)
        {
            _expectedHealthDamage = expectedHealthDamage;
        }

        public bool Matches(
            DamageDealtTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.HealthDamage
                == _expectedHealthDamage;
        }
    }

    private sealed class RecordingDamageDealtAction
        : IDamageDealtTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingDamageDealtAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            DamageDealtTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingDamageDealtRecipe
        : ICombatEffectRecipe<DamageDealtTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingDamageDealtRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            DamageDealtTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingDamageDealtRecipe
        : ICombatEffectRecipe<DamageDealtTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextTargetId { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public DamageKind? Kind { get; private set; }

        public int? RequestedAmount { get; private set; }

        public int? HealthDamage { get; private set; }

        public int? BlockedDamage { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            DamageDealtTriggeredEffectContext context,
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
            ContextTargetId =
                context.TargetCombatant.Id;
            ContextSourceId =
                context.SourceCombatant?.Id;
            Kind =
                context.CombatEvent.Kind;
            RequestedAmount =
                context.CombatEvent.RequestedAmount;
            HealthDamage =
                context.CombatEvent.HealthDamage;
            BlockedDamage =
                context.CombatEvent.BlockedDamage;

            return Array.Empty<IEffectRequest>();
        }
    }
}
