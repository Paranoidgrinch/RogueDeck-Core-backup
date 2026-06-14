using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusChargesReducedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void GenericGainBlockRecipesUseFixedAndReducedChargeAmounts()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 5);

        var definition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_recipe_amounts"));

        definition.Filters.Add(
            new StatusChargesReducedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusChargesReducedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusChargesReducedTriggeredEffectAmount(3)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusChargesReducedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new StatusChargesReducedChargeCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId,
            amount: 2);

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
            charges: 3);

        var definition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_recipe_order"));

        definition.Actions.Add(
            new RecordingStatusChargesReducedAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingStatusChargesReducedAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingStatusChargesReducedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingStatusChargesReducedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

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
    public void GenericEffectsPreserveChargeContextAndSourceFreeAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 5);

        var status = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            candidate =>
                candidate.DefinitionId == StandardCombatIds.WeakStatus);

        var recipe = new CapturingStatusChargesReducedRecipe();

        var definition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_recipe_context"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId,
            amount: 2);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Null(recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Null(recipe.ContextSourceCombatantId);
        Assert.Equal(status.Id, recipe.StatusInstanceId);
        Assert.Equal(
            StandardCombatIds.WeakStatus,
            recipe.StatusDefinitionId);
        Assert.Equal(5, recipe.OldCharges);
        Assert.Equal(3, recipe.NewCharges);
        Assert.True(recipe.StatusStillPresent);
        Assert.Equal(3, recipe.CurrentCharges);
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
            charges: 3);

        var definition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_recipe_source_fallback"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusChargesReducedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new FixedStatusChargesReducedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursiveChargeReduction()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 3);

        var definition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_self_reentry"));

        definition.Effects.Add(
            new DecreaseSameStatusChargesRecipe());

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        var status = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            candidate =>
                candidate.DefinitionId == StandardCombatIds.WeakStatus);

        Assert.Equal(1, status.Charges);

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry =>
                entry.Type == StandardCombatLogTypes.StatusChargesReduced));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionCanContinueRecursiveChargeReductionChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 3);

        var reducingDefinition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_chain_reduce"));

        reducingDefinition.Effects.Add(
            new DecreaseSameStatusChargesRecipe());

        var observingDefinition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_chain_observe"));

        observingDefinition.Filters.Add(
            new StatusChargesTransitionFilter(
                2,
                1));

        observingDefinition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusChargesReducedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusChargesReducedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(reducingDefinition);
        registry.RegisterTriggeredEffectDefinition(observingDefinition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        var status = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            candidate =>
                candidate.DefinitionId == StandardCombatIds.WeakStatus);

        Assert.Equal(1, status.Charges);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void ChargeReductionHealCanContinueIntoHealedDefinition()
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
            charges: 2);

        var chargesDefinition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_recipe_heal"));

        chargesDefinition.Effects.Add(
            new HealEffectRecipe<
                StatusChargesReducedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusChargesReducedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(chargesDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ChargeReductionEffectsResolveBeforeExpirationEffectsAtZeroCharges()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        var healthBefore = hero.Health.Current;

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 1);

        var chargesDefinition =
            new StatusChargesReducedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_zero_order"));

        chargesDefinition.Filters.Add(
            new StatusChargesReducedStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        chargesDefinition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusChargesReducedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusChargesReducedTriggeredEffectAmount(3)));

        var expiredDefinition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_charges_reduced_zero_expired"));

        expiredDefinition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        expiredDefinition.Actions.Add(
            new DealDamageOnStatusExpiredTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusExpiredTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(chargesDefinition);
        registry.RegisterTriggeredEffectDefinition(expiredDefinition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            healthBefore,
            hero.Health.Current);

        Assert.Equal(
            1,
            GetBlockOrZero(combat, HeroId));

        Assert.DoesNotContain(
            hero.Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.WeakStatus);
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        int charges)
    {
        combat.EnqueueEffect(
            new ApplyStatusEffectRequest(
                TargetCombatantId: targetId,
                StatusDefinitionId: statusId,
                Charges: charges));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private static void DecreaseFirstStatusCharges(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount = 1)
    {
        var status = Assert.Single(
            combat.GetCombatant(targetId).Statuses,
            candidate => candidate.Charges > 0);

        combat.EnqueueEffect(
            new DecreaseStatusChargesEffectRequest(
                TargetCombatantId: targetId,
                StatusInstanceId: status.Id,
                Amount: amount));

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

    private sealed class RecordingStatusChargesReducedAction
        : IStatusChargesReducedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusChargesReducedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusChargesReducedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingStatusChargesReducedRecipe
        : ICombatEffectRecipe<
            StatusChargesReducedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusChargesReducedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusChargesReducedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingStatusChargesReducedRecipe
        : ICombatEffectRecipe<
            StatusChargesReducedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextTargetId { get; private set; }

        public CombatantId? ContextSourceCombatantId { get; private set; }

        public StatusInstanceId? StatusInstanceId { get; private set; }

        public StatusDefinitionId? StatusDefinitionId { get; private set; }

        public int OldCharges { get; private set; }

        public int NewCharges { get; private set; }

        public bool StatusStillPresent { get; private set; }

        public int? CurrentCharges { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusChargesReducedTriggeredEffectContext context,
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
            StatusInstanceId = context.CombatEvent.StatusInstanceId;
            StatusDefinitionId = context.CombatEvent.StatusDefinitionId;
            OldCharges = context.CombatEvent.OldCharges;
            NewCharges = context.CombatEvent.NewCharges;

            var currentStatus = context.TargetCombatant.Statuses
                .SingleOrDefault(status =>
                    status.Id == context.CombatEvent.StatusInstanceId);

            StatusStillPresent = currentStatus is not null;
            CurrentCharges = currentStatus?.Charges;

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class DecreaseSameStatusChargesRecipe
        : ICombatEffectRecipe<
            StatusChargesReducedTriggeredEffectContext>
    {
        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusChargesReducedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            return new IEffectRequest[]
            {
                new DecreaseStatusChargesEffectRequest(
                    TargetCombatantId:
                        context.CombatEvent.TargetCombatantId,
                    StatusInstanceId:
                        context.CombatEvent.StatusInstanceId,
                    Amount: 1)
            };
        }
    }

    private sealed record StatusChargesTransitionFilter(
        int OldCharges,
        int NewCharges)
        : IStatusChargesReducedTriggeredEffectFilter
    {
        public bool Matches(
            StatusChargesReducedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.CombatEvent.OldCharges == OldCharges &&
                context.CombatEvent.NewCharges == NewCharges;
        }
    }
}
