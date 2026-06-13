using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class RoundEndedEffectRecipePilotTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void GenericGainBlockRecipeFiresWhenRoundEnds()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_generic_gain_block"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveRoundEnded(
            combat,
            registry,
            round: 1,
            lastActiveCombatantId: GoblinId);

        Assert.Equal(0, GetBlockOrZero(combat, HeroId));
        Assert.Equal(4, GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void GenericHealRecipeCanUseCompletedRoundAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var goblin = combat.GetCombatant(GoblinId);
        var healthBefore = goblin.Health.Max - 5;

        goblin.Health.SetCurrent(healthBefore);

        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_generic_heal"));

        definition.Effects.Add(
            new HealEffectRecipe<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new RoundEndedCompletedRoundAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveRoundEnded(
            combat,
            registry,
            round: 3,
            lastActiveCombatantId: GoblinId);

        Assert.Equal(
            healthBefore + 3,
            goblin.Health.Current);
    }

    [Fact]
    public void LegacyActionAndGenericEffectEachExecuteExactlyOnce()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_legacy_and_generic"));

        definition.Actions.Add(
            new GainBlockOnRoundEndedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundEndedTriggeredEffectAmount(2)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveRoundEnded(
            combat,
            registry,
            round: 1,
            lastActiveCombatantId: GoblinId);

        Assert.Equal(5, GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void ExistingFiltersApplyToGenericEffects()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_generic_matching_filter"));

        matching.Filters.Add(
            new RoundEndedMinimumRoundTriggerFilter(
                MinimumRound: 1));

        matching.Effects.Add(
            new GainBlockEffectRecipe<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(2)));

        var nonMatching = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_generic_non_matching_filter"));

        nonMatching.Filters.Add(
            new RoundEndedMinimumRoundTriggerFilter(
                MinimumRound: 2));

        nonMatching.Effects.Add(
            new GainBlockEffectRecipe<RoundEndedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        ResolveRoundEnded(
            combat,
            registry,
            round: 1,
            lastActiveCombatantId: GoblinId);

        Assert.Equal(2, GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();
        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_execution_order"));

        definition.Actions.Add(
            new RecordingAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveRoundEnded(
            combat,
            registry,
            round: 1,
            lastActiveCombatantId: GoblinId);

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
    public void GenericEffectsReceiveRoundEndedSourceAndEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingRecipe();
        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_generic_context"));

        definition.Effects.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveRoundEnded(
            combat,
            registry,
            round: 1,
            lastActiveCombatantId: GoblinId);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(GoblinId, recipe.TargetingSourceId);
        Assert.Equal(GoblinId, recipe.EventTargetId);
        Assert.Equal(GoblinId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
    }

    [Fact]
    public void GenericEffectsDoNotRunWhenLastActiveCombatantIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingRecipe();
        var definition = new RoundEndedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_ended_missing_context"));

        definition.Effects.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        ResolveRoundEnded(
            combat,
            registry,
            round: 1,
            lastActiveCombatantId: null);

        Assert.Equal(0, recipe.ActivationCount);
    }

    private static void ResolveRoundEnded(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int round,
        CombatantId? lastActiveCombatantId)
    {
        combat.EnqueueEvent(
            new RoundEndedCombatEvent(
                Round: round,
                LastActiveCombatantId: lastActiveCombatantId));

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

    private sealed class RecordingAction
        : IRoundEndedTriggeredEffectAction
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
            RoundEndedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingRecipe
        : ICombatEffectRecipe<RoundEndedTriggeredEffectContext>
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
            RoundEndedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingRecipe
        : ICombatEffectRecipe<RoundEndedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }
        public CombatantId? TargetingSourceId { get; private set; }
        public CombatantId? EventTargetId { get; private set; }
        public CombatantId? EffectSourceCombatantId { get; private set; }
        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            RoundEndedTriggeredEffectContext context,
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
