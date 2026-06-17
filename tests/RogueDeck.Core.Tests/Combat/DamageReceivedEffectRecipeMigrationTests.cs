using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DamageReceivedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId DamageCardId =
        new("test.damage_received_card");

    [Fact]
    public void GenericGainBlockRecipesCanUseRequestedBlockedAndHealthDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        var healthBefore = hero.Health.Current;

        hero.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 2));

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedHealthDamageAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedBlockedDamageAmount()));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedRequestedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 5,
            sourceCombatantId: GoblinId);

        Assert.Equal(
            healthBefore - 3,
            hero.Health.Current);

        Assert.Equal(
            10,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void GenericGainBlockRecipesCanUseReceiverAndSourceStatusStacks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var receiverStatusId =
            new StatusDefinitionId(
                "test.damage_received_recipe_receiver_status");

        var sourceStatusId =
            new StatusDefinitionId(
                "test.damage_received_recipe_source_status");

        RegisterStackingStatus(
            registry,
            receiverStatusId,
            StatusPolarity.Buff);

        RegisterStackingStatus(
            registry,
            sourceStatusId,
            StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            receiverStatusId,
            stacks: 2);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            sourceStatusId,
            stacks: 3);

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_status_amounts"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedReceiverStatusStacksAmount(
                    receiverStatusId)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedSourceStatusStacksAmount(
                    sourceStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceCombatantId: GoblinId);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var executionOrder = new List<string>();

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_execution_order"));

        definition.Steps.Add(
            new RecordingDamageReceivedAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingDamageReceivedAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingDamageReceivedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingDamageReceivedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceCombatantId: GoblinId);

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
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var recipe = new CapturingDamageReceivedRecipe();

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_attribution"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 4,
            sourceCombatantId: GoblinId,
            sourceCardId: DamageCardId,
            kind: DamageKind.DamageOverTime);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Equal(
            GoblinId,
            recipe.EffectSourceCombatantId);
        Assert.Equal(
            DamageCardId,
            recipe.EffectSourceCardId);
        Assert.Equal(
            HeroId,
            recipe.ContextReceiverId);
        Assert.Equal(
            GoblinId,
            recipe.ContextSourceId);
        Assert.Equal(
            DamageKind.DamageOverTime,
            recipe.Kind);
        Assert.Equal(4, recipe.RequestedAmount);
        Assert.Equal(4, recipe.HealthDamage);
        Assert.Equal(0, recipe.BlockedDamage);
    }

    [Fact]
    public void SourceFreeDamageUsesReceiverAsTargetingFallbackWithoutInventingAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var recipe = new CapturingDamageReceivedRecipe();

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_source_free"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 2);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(
            HeroId,
            recipe.ContextReceiverId);
        Assert.Null(recipe.ContextSourceId);
    }

    [Fact]
    public void DamageReceivedDefinitionSuppressesOwnRecursiveDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        var healthBefore = hero.Health.Current;

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_self_reentry"));

        definition.Steps.Add(
            new DealDamageOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(1)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 2,
            sourceCombatantId: GoblinId);

        Assert.Equal(
            healthBefore - 3,
            hero.Health.Current);

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

    [Fact]
    public void DifferentDamageReceivedDefinitionsCanContinueTheSameChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        var healthBefore = hero.Health.Current;

        var followUpDamage =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_follow_up_damage"));

        followUpDamage.Filters.Add(
            new ExactHealthDamageFilter(2));

        followUpDamage.Steps.Add(
            new DealDamageOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(1)));

        var followUpBlock =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_follow_up_block"));

        followUpBlock.Filters.Add(
            new ExactHealthDamageFilter(1));

        followUpBlock.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(
            followUpDamage);

        registry.RegisterTriggeredEffectDefinition(
            followUpBlock);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 2,
            sourceCombatantId: GoblinId);

        Assert.Equal(
            healthBefore - 3,
            hero.Health.Current);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DamageReceivedHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 5);

        var damageDefinition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_heal"));

        damageDefinition.Filters.Add(
            new DamageReceivedHasSourceTriggerFilter());

        damageDefinition.Steps.Add(
            new HealEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_healed_follow_up"));

        healedDefinition.Steps.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(
            damageDefinition);

        registry.RegisterTriggeredEffectDefinition(
            healedDefinition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceCombatantId: GoblinId,
            sourceCardId: DamageCardId);

        Assert.Equal(
            hero.Health.Max - 4,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void OriginalDamageDealtSiblingStillRuns()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var damageDealtDefinition =
            new DamageDealtTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_dealt_original_sibling"));

        damageDealtDefinition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageDealtTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(2)));

        var damageReceivedDefinition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_sibling"));

        damageReceivedDefinition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(
            damageDealtDefinition);

        registry.RegisterTriggeredEffectDefinition(
            damageReceivedDefinition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceCombatantId: GoblinId);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, GoblinId));

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void LethalDamageStillProcessesRecipeAndStandardLifecycleResult()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat =
            CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition =
            new DamageReceivedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.damage_received_recipe_lethal"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<
                DamageReceivedTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedDamageReceivedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 99,
            sourceCombatantId: GoblinId);

        var hero = combat.GetCombatant(HeroId);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, GoblinId));

        Assert.Equal(0, hero.Health.Current);

        Assert.Equal(
            CombatantLifecycleState.Downed,
            hero.LifecycleState);

        Assert.Equal(
            CombatResult.Defeat,
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

    private sealed class ExactHealthDamageFilter
        : IDamageReceivedTriggeredEffectFilter
    {
        private readonly int _expectedHealthDamage;

        public ExactHealthDamageFilter(
            int expectedHealthDamage)
        {
            _expectedHealthDamage =
                expectedHealthDamage;
        }

        public bool Matches(
            DamageReceivedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.HealthDamage ==
                _expectedHealthDamage;
        }
    }

    private sealed class RecordingDamageReceivedAction
        : IDamageReceivedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingDamageReceivedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                DamageReceivedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingDamageReceivedRecipe
        : ICombatEffectRecipe<
            DamageReceivedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingDamageReceivedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                DamageReceivedTriggeredEffectContext context,
                TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingDamageReceivedRecipe
        : ICombatEffectRecipe<
            DamageReceivedTriggeredEffectContext>
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

        public CombatantId? ContextReceiverId {
            get;
            private set;
        }

        public CombatantId? ContextSourceId {
            get;
            private set;
        }

        public DamageKind? Kind { get; private set; }

        public int? RequestedAmount { get; private set; }

        public int? HealthDamage { get; private set; }

        public int? BlockedDamage { get; private set; }

        public IReadOnlyCollection<IEffectRequest>
            BuildEffectRequests(
                DamageReceivedTriggeredEffectContext context,
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

            ContextReceiverId =
                context.ReceiverCombatant.Id;

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
