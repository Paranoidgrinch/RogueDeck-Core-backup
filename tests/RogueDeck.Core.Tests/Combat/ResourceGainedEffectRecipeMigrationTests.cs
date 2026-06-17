using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class ResourceGainedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly ResourceId FuryResourceId = new("test.fury");

    [Fact]
    public void GenericGainBlockRecipeCanUseActualGainedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_actual_amount"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<ResourceGainedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new ResourceGainedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 3,
            defaultMax: 10);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void GenericGainBlockRecipeCanUseNewCurrentAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.GetCombatant(HeroId).Resources.Add(
            FuryResourceId,
            new ValuePoolState(
                current: 2,
                max: 10));

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_new_current"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<ResourceGainedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new ResourceGainedNewCurrentAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 3);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CustomGainResourceRecipeSuppressesItsOwnRecursiveReentry()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var recipe = new GainSameResourceRecipe(
            FuryResourceId,
            amount: 1);

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_self_reentry"));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 2,
            defaultMax: 10);

        Assert.Equal(
            1,
            recipe.ActivationCount);

        Assert.Equal(
            3,
            combat.GetCombatant(HeroId)
                .Resources[FuryResourceId]
                .Current);

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentResourceGainedDefinitionsCanContinueTheSameChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var gainAgain = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_follow_up_gain"));

        gainAgain.Filters.Add(
            new ExactResourceGainedAmountFilter(
                expectedAmount: 2));

        gainAgain.Steps.Add(
            new GainSameResourceRecipe(
                FuryResourceId,
                amount: 1));

        var followUpBlock = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_follow_up_block"));

        followUpBlock.Filters.Add(
            new ExactResourceGainedAmountFilter(
                expectedAmount: 1));

        followUpBlock.Steps.Add(
            new GainBlockEffectRecipe<ResourceGainedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedResourceGainedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(gainAgain);
        registry.RegisterTriggeredEffectDefinition(followUpBlock);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 2,
            defaultMax: 10);

        Assert.Equal(
            3,
            combat.GetCombatant(HeroId)
                .Resources[FuryResourceId]
                .Current);

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

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_execution_order"));

        definition.Steps.Add(
            new RecordingResourceGainedAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingResourceGainedAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingResourceGainedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingResourceGainedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 1,
            defaultMax: 10);

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
    public void GenericEffectsPreserveResourceOwnerTargetAndAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingResourceGainedRecipe();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.resource_gained_recipe_attribution"));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 2,
            defaultMax: 10);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextSourceId);
        Assert.Equal(FuryResourceId, recipe.ResourceId);
        Assert.Equal(2, recipe.GainedAmount);
        Assert.Equal(2, recipe.NewCurrentAmount);
    }

    [Fact]
    public void ResourceGainedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var sourcePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "ResourceGainedTriggeredEffects.cs");

        var source = File.ReadAllText(sourcePath);

        var definitionSource = Slice(
            source,
            "public sealed class ResourceGainedTriggeredEffectDefinition",
            "public sealed record ResourceGainedTriggeredEffectContext");

        var fixedProviderSource = Slice(
            source,
            "public sealed record FixedResourceGainedTriggeredEffectAmount",
            "public sealed class ResourceGainedAmount");

        var gainedProviderSource = Slice(
            source,
            "public sealed class ResourceGainedAmount",
            "public sealed class ResourceGainedNewCurrentAmount");

        var newCurrentProviderSource = Slice(
            source,
            "public sealed class ResourceGainedNewCurrentAmount",
            "public sealed record ResourceGainedResourceIdTriggerFilter");

        var handlerSource = SliceToEnd(
            source,
            "public sealed class ResourceGainedTriggeredEffectHandler");

        Assert.Contains(
            "List<ICombatEffectRecipe<ResourceGainedTriggeredEffectContext>> Steps",
            definitionSource);

        Assert.Contains(
            "ICombatValueProvider<ResourceGainedTriggeredEffectContext, int>",
            fixedProviderSource);

        Assert.Contains(
            "ICombatValueProvider<ResourceGainedTriggeredEffectContext, int>",
            gainedProviderSource);

        Assert.Contains(
            "ICombatValueProvider<ResourceGainedTriggeredEffectContext, int>",
            newCurrentProviderSource);

        Assert.Contains(
            "public int Resolve(ResourceGainedTriggeredEffectContext context)",
            fixedProviderSource);

        Assert.Contains(
            "public int Resolve(ResourceGainedTriggeredEffectContext context)",
            gainedProviderSource);

        Assert.Contains(
            "public int Resolve(ResourceGainedTriggeredEffectContext context)",
            newCurrentProviderSource);

        Assert.Contains(
            "ResourceGainedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var stepsIndex = handlerSource.IndexOf(
            "definition.Steps",
            StringComparison.Ordinal);

        Assert.True(stepsIndex >= 0);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainResourceEffectRequest",
            source);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);
    }

    private static void GainResource(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ResourceId resourceId,
        int amount,
        int? defaultMax = null)
    {
        combat.EnqueueEffect(
            new GainResourceEffectRequest(
                CombatantId: HeroId,
                ResourceId: resourceId,
                Amount: amount,
                DefaultMax: defaultMax));

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

    private sealed class ExactResourceGainedAmountFilter
        : IResourceGainedTriggeredEffectFilter
    {
        private readonly int _expectedAmount;

        public ExactResourceGainedAmountFilter(
            int expectedAmount)
        {
            _expectedAmount = expectedAmount;
        }

        public bool Matches(
            ResourceGainedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.GainedAmount
                == _expectedAmount;
        }
    }

    private sealed class GainSameResourceRecipe
        : ICombatEffectRecipe<ResourceGainedTriggeredEffectContext>
    {
        private readonly ResourceId _resourceId;
        private readonly int _amount;

        public int ActivationCount { get; private set; }

        public GainSameResourceRecipe(
            ResourceId resourceId,
            int amount)
        {
            _resourceId = resourceId;
            _amount = amount;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            ResourceGainedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            return new IEffectRequest[]
            {
                new GainResourceEffectRequest(
                    CombatantId: context.SourceCombatant.Id,
                    ResourceId: _resourceId,
                    Amount: _amount)
            };
        }
    }

    private sealed class RecordingResourceGainedAction
        : IResourceGainedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingResourceGainedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            ResourceGainedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingResourceGainedRecipe
        : ICombatEffectRecipe<ResourceGainedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingResourceGainedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            ResourceGainedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingResourceGainedRecipe
        : ICombatEffectRecipe<ResourceGainedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextSourceId { get; private set; }

        public ResourceId? ResourceId { get; private set; }

        public int? GainedAmount { get; private set; }

        public int? NewCurrentAmount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            ResourceGainedTriggeredEffectContext context,
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
            ContextSourceId =
                context.SourceCombatant.Id;
            ResourceId =
                context.CombatEvent.ResourceId;
            GainedAmount =
                context.CombatEvent.GainedAmount;
            NewCurrentAmount =
                context.CombatEvent.NewCurrent;

            return Array.Empty<IEffectRequest>();
        }
    }
}
