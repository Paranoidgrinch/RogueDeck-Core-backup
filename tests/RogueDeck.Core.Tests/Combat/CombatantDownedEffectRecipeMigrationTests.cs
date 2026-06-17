using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatantDownedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");

    [Fact]
    public void GenericGainBlockRecipeUsesFixedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_recipe_gain_block"));

        definition.Filters.Add(
            new CombatantDownedTeamTriggerFilter(StandardCombatIds.EnemyTeam));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ExistingFiltersGateGenericRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matchingDefinition = CreateBlockDefinition(
            "test.combatant_downed_filters_matching",
            amount: 5);

        matchingDefinition.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(GoblinId));

        matchingDefinition.Filters.Add(
            new CombatantDownedOldStateTriggerFilter(
                CombatantLifecycleState.Alive));

        var wrongIdDefinition = CreateBlockDefinition(
            "test.combatant_downed_filters_wrong_id",
            amount: 10);

        wrongIdDefinition.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(HeroId));

        var wrongOldStateDefinition = CreateBlockDefinition(
            "test.combatant_downed_filters_wrong_old_state",
            amount: 20);

        wrongOldStateDefinition.Filters.Add(
            new CombatantDownedOldStateTriggerFilter(
                CombatantLifecycleState.Dead));

        registry.RegisterTriggeredEffectDefinition(matchingDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongIdDefinition);
        registry.RegisterTriggeredEffectDefinition(wrongOldStateDefinition);

        DownGoblinWithDamage(combat, registry);

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
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_recipe_order"));

        definition.Steps.Add(
            new RecordingCombatantDownedAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingCombatantDownedAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingCombatantDownedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingCombatantDownedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        DownGoblinWithDamage(combat, registry);

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
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_definition_order_first"));

        firstDefinition.Steps.Add(
            new RecordingCombatantDownedAction(
                executionOrder,
                "first-legacy"));

        firstDefinition.Steps.Add(
            new RecordingCombatantDownedRecipe(
                executionOrder,
                "first-recipe"));

        var secondDefinition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_definition_order_second"));

        secondDefinition.Steps.Add(
            new RecordingCombatantDownedAction(
                executionOrder,
                "second-legacy"));

        secondDefinition.Steps.Add(
            new RecordingCombatantDownedRecipe(
                executionOrder,
                "second-recipe"));

        registry.RegisterTriggeredEffectDefinition(firstDefinition);
        registry.RegisterTriggeredEffectDefinition(secondDefinition);

        DownGoblinWithDamage(combat, registry);

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
        var recipe = new CapturingCombatantDownedRecipe();

        var definition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_recipe_context"));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Same(combat, recipe.ContextCombat);
        Assert.Same(registry, recipe.ContextRegistry);
        Assert.Equal(GoblinId, recipe.ContextDownedCombatantId);
        Assert.Equal(GoblinId, recipe.EventCombatantId);
        Assert.Equal(
            CombatantLifecycleState.Alive,
            recipe.EventOldState);
        Assert.Equal(
            CombatantLifecycleState.Downed,
            recipe.EventNewState);
        Assert.Equal(GoblinId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursiveDownedChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var recipe = new KillOtherEnemyRecipe(SecondGoblinId);

        var definition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_self_reentry"));

        definition.Filters.Add(
            new CombatantDownedTeamTriggerFilter(StandardCombatIds.EnemyTeam));

        definition.Steps.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);

        Assert.Equal(
            CombatantLifecycleState.Downed,
            combat.GetCombatant(SecondGoblinId).LifecycleState);

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionCanContinueDownedChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var killingRecipe = new KillOtherEnemyRecipe(SecondGoblinId);

        var killingDefinition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_chain_kill"));

        killingDefinition.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(GoblinId));

        killingDefinition.Steps.Add(killingRecipe);

        var observingDefinition = CreateBlockDefinition(
            "test.combatant_downed_chain_observe",
            amount: 4);

        observingDefinition.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(SecondGoblinId));

        registry.RegisterTriggeredEffectDefinition(killingDefinition);
        registry.RegisterTriggeredEffectDefinition(observingDefinition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(1, killingRecipe.ActivationCount);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DownedHealCanContinueIntoHealedDefinition()
    {
        // Uses two goblins so the first goblin's downing does not end combat
        // (downing the last enemy sets CombatResult to Victory, which halts
        // further queue processing before the Heal -> Healed chain can run).
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(combat, registry, amount: 5);

        var downedDefinition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_recipe_heal"));

        downedDefinition.Filters.Add(
            new CombatantDownedCombatantIdTriggerFilter(GoblinId));

        downedDefinition.Steps.Add(
            new HealEffectRecipe<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.AllAliveCombatants,
                new FixedCombatantDownedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.combatant_downed_healed_follow_up"));

        healedDefinition.Steps.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(downedDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        DownGoblinWithDamage(combat, registry);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    private static CombatantDownedTriggeredEffectDefinition
        CreateBlockDefinition(
            string id,
            int amount)
    {
        var definition =
            new CombatantDownedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(id));

        definition.Steps.Add(
            new GainBlockEffectRecipe<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new FixedCombatantDownedTriggeredEffectAmount(amount)));

        return definition;
    }

    private static void DownGoblinWithDamage(
        CombatState combat,
        CombatDefinitionRegistry registry)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 99,
            SourceCombatantId: HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
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

    private sealed class RecordingCombatantDownedAction
        : ICombatantDownedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCombatantDownedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CombatantDownedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingCombatantDownedRecipe
        : ICombatEffectRecipe<CombatantDownedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingCombatantDownedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CombatantDownedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingCombatantDownedRecipe
        : ICombatEffectRecipe<CombatantDownedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatState? ContextCombat { get; private set; }

        public CombatDefinitionRegistry? ContextRegistry { get; private set; }

        public CombatantId? ContextDownedCombatantId { get; private set; }

        public CombatantId? EventCombatantId { get; private set; }

        public CombatantLifecycleState? EventOldState { get; private set; }

        public CombatantLifecycleState? EventNewState { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CombatantDownedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;
            ContextCombat = context.Combat;
            ContextRegistry = context.Registry;
            ContextDownedCombatantId = context.DownedCombatant.Id;
            EventCombatantId = context.CombatEvent.CombatantId;
            EventOldState = context.CombatEvent.OldState;
            EventNewState = context.CombatEvent.NewState;
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

    private sealed class KillOtherEnemyRecipe
        : ICombatEffectRecipe<CombatantDownedTriggeredEffectContext>
    {
        private readonly CombatantId _otherCombatantId;

        public KillOtherEnemyRecipe(CombatantId otherCombatantId)
        {
            _otherCombatantId = otherCombatantId;
        }

        public int ActivationCount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CombatantDownedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            if (context.DownedCombatant.Id == _otherCombatantId)
                return Array.Empty<IEffectRequest>();

            return new IEffectRequest[]
            {
                new DealDamageEffectRequest(
                    TargetCombatantId: _otherCombatantId,
                    Amount: 99)
            };
        }
    }
}
