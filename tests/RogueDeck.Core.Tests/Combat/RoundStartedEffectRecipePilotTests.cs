using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class RoundStartedEffectRecipePilotTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void GenericEffectRecipeFiresWhenNewRoundStarts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_generic_gain_block"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<RoundStartedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, combat.CurrentRound);
        Assert.Equal(HeroId, combat.ActiveCombatantId);
        Assert.Equal(4, GetBlockOrZero(combat, HeroId));
        Assert.Equal(0, GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void LegacyActionAndGenericEffectEachExecuteExactlyOnce()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_legacy_and_generic"));

        definition.Steps.Add(
            new GainBlockOnRoundStartedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedRoundStartedTriggeredEffectAmount(2)));

        definition.Steps.Add(
            new GainBlockEffectRecipe<RoundStartedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(5, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ExistingFiltersApplyToGenericEffects()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_generic_matching_filter"));

        matching.Filters.Add(
            new RoundStartedMinimumRoundTriggerFilter(
                MinimumRound: 2));

        matching.Steps.Add(
            new GainBlockEffectRecipe<RoundStartedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(2)));

        var nonMatching = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_generic_non_matching_filter"));

        nonMatching.Filters.Add(
            new RoundStartedMinimumRoundTriggerFilter(
                MinimumRound: 3));

        nonMatching.Steps.Add(
            new GainBlockEffectRecipe<RoundStartedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void GenericEffectCanUseCurrentRoundAmountProvider()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_generic_current_round"));

        definition.Steps.Add(
            new GainBlockEffectRecipe<RoundStartedTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new RoundStartedCurrentRoundAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(2, GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();
        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_execution_order"));

        definition.Steps.Add(
            new RecordingAction(
                executionOrder,
                "legacy-a"));

        definition.Steps.Add(
            new RecordingAction(
                executionOrder,
                "legacy-b"));

        definition.Steps.Add(
            new RecordingRecipe(
                executionOrder,
                "recipe-a"));

        definition.Steps.Add(
            new RecordingRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

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
    public void GenericEffectsReceiveRoundStartedSourceAndEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var recipe = new CapturingRecipe();
        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_generic_context"));

        definition.Steps.Add(recipe);

        registry.RegisterTriggeredEffectDefinition(definition);

        AdvanceFromGoblinTurnToNextRound(combat, registry);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
    }

    private static void AdvanceFromGoblinTurnToNextRound(
        CombatState combat,
        CombatDefinitionRegistry registry)
    {
        combat.SetActiveCombatant(GoblinId);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);
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
        : IRoundStartedTriggeredEffectAction
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
            RoundStartedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingRecipe
        : ICombatEffectRecipe<RoundStartedTriggeredEffectContext>
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
            RoundStartedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingRecipe
        : ICombatEffectRecipe<RoundStartedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }
        public CombatantId? TargetingSourceId { get; private set; }
        public CombatantId? EventTargetId { get; private set; }
        public CombatantId? EffectSourceCombatantId { get; private set; }
        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            RoundStartedTriggeredEffectContext context,
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
