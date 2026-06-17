using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class HealedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId HealCardId = new("test.heal_card");

    [Fact]
    public void GenericGainBlockRecipeCanUseActualHealedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(
            combat,
            registry,
            amount: 5);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_actual_amount"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<HealedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new HealedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(
            combat,
            registry,
            amount: 3);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void GenericGainBlockRecipeCanUseRequestedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(
            combat,
            registry,
            amount: 1);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_requested_amount"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<HealedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new HealedRequestedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(
            combat,
            registry,
            amount: 5);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void GenericHealRecipeSuppressesItsOwnRecursiveReentry()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(
            combat,
            registry,
            amount: 5);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_self_reentry"));

        definition.Steps.Add(
            new HealEffectRecipe<HealedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(1)));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(
            combat,
            registry,
            amount: 2);

        Assert.Equal(
            hero.Health.Max - 2,
            hero.Health.Current);

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentHealedDefinitionsCanContinueTheSameChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(
            combat,
            registry,
            amount: 5);

        var followUpHeal = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_follow_up_heal"));

        followUpHeal.Filters.Add(
            new ExactHealedAmountFilter(2));

        followUpHeal.Steps.Add(
            new HealEffectRecipe<HealedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(1)));

        var followUpBlock = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_follow_up_block"));

        followUpBlock.Filters.Add(
            new ExactHealedAmountFilter(1));

        followUpBlock.Steps.Add(
            new GainBlockEffectRecipe<HealedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(followUpHeal);
        registry.RegisterTriggeredEffectDefinition(followUpBlock);

        HealHero(
            combat,
            registry,
            amount: 2);

        Assert.Equal(
            hero.Health.Max - 2,
            hero.Health.Current);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();

        DamageHero(
            combat,
            registry,
            amount: 2);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_execution_order"));

        definition.Steps.Add(
            new RecordingHealedAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingHealedAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingHealedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingHealedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(
            combat,
            registry,
            amount: 1);

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
    public void GenericEffectsPreserveSourceTargetAndCardAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingHealedRecipe();

        DamageHero(
            combat,
            registry,
            amount: 2);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.healed_recipe_attribution"));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(
            combat,
            registry,
            amount: 1,
            sourceCombatantId: GoblinId,
            sourceCardId: HealCardId);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Equal(GoblinId, recipe.EffectSourceCombatantId);
        Assert.Equal(HealCardId, recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Equal(GoblinId, recipe.ContextSourceId);
    }

    [Fact]
    public void HealedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var sourcePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "HealedTriggeredEffects.cs");

        var source = File.ReadAllText(sourcePath);

        var definitionSource = Slice(
            source,
            "public sealed class HealedTriggeredEffectDefinition",
            "public sealed record HealedTriggeredEffectContext");

        var fixedProviderSource = Slice(
            source,
            "public sealed record FixedHealedTriggeredEffectAmount",
            "public sealed class HealedAmount");

        var actualProviderSource = Slice(
            source,
            "public sealed class HealedAmount",
            "public sealed class HealedRequestedAmount");

        var requestedProviderSource = Slice(
            source,
            "public sealed class HealedRequestedAmount",
            "public sealed class HealedHasSourceTriggerFilter");

        var handlerSource = SliceToEnd(
            source,
            "public sealed class HealedTriggeredEffectHandler");

        Assert.Contains(
            "List<ICombatEffectRecipe<HealedTriggeredEffectContext>> Steps",
            definitionSource);

        Assert.Contains(
            "ICombatValueProvider<HealedTriggeredEffectContext, int>",
            fixedProviderSource);

        Assert.Contains(
            "ICombatValueProvider<HealedTriggeredEffectContext, int>",
            actualProviderSource);

        Assert.Contains(
            "ICombatValueProvider<HealedTriggeredEffectContext, int>",
            requestedProviderSource);

        Assert.Contains(
            "public int Resolve(HealedTriggeredEffectContext context)",
            fixedProviderSource);

        Assert.Contains(
            "public int Resolve(HealedTriggeredEffectContext context)",
            actualProviderSource);

        Assert.Contains(
            "public int Resolve(HealedTriggeredEffectContext context)",
            requestedProviderSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        Assert.Contains(
            "HealedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        var stepsIndex = handlerSource.IndexOf(
            "definition.Steps",
            StringComparison.Ordinal);

        Assert.True(stepsIndex >= 0);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);
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

    private static void HealHero(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int amount,
        CombatantId? sourceCombatantId = null,
        CardDefinitionId? sourceCardId = null)
    {
        combat.EnqueueEffect(
            new HealEffectRequest(
                TargetCombatantId: HeroId,
                Amount: amount,
                SourceCombatantId: sourceCombatantId,
                SourceCardId: sourceCardId));

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

    private static string Slice(
        string source,
        string startMarker,
        string endMarker)
    {
        var start = source.IndexOf(
            startMarker,
            StringComparison.Ordinal);

        Assert.True(
            start >= 0,
            $"Missing start marker: {startMarker}");

        var end = source.IndexOf(
            endMarker,
            start,
            StringComparison.Ordinal);

        Assert.True(
            end > start,
            $"Missing end marker: {endMarker}");

        return source[start..end];
    }

    private static string SliceToEnd(
        string source,
        string startMarker)
    {
        var start = source.IndexOf(
            startMarker,
            StringComparison.Ordinal);

        Assert.True(
            start >= 0,
            $"Missing start marker: {startMarker}");

        return source[start..];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "RogueDeck.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find repository root.");
    }

    private sealed class ExactHealedAmountFilter
        : IHealedTriggeredEffectFilter
    {
        private readonly int _expectedAmount;

        public ExactHealedAmountFilter(
            int expectedAmount)
        {
            _expectedAmount = expectedAmount;
        }

        public bool Matches(
            HealedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.HealedAmount
                == _expectedAmount;
        }
    }

    private sealed class RecordingHealedAction
        : IHealedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingHealedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            HealedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingHealedRecipe
        : ICombatEffectRecipe<HealedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingHealedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            HealedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingHealedRecipe
        : ICombatEffectRecipe<HealedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextTargetId { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            HealedTriggeredEffectContext context,
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

            return Array.Empty<IEffectRequest>();
        }
    }
}
