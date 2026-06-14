using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusExpiredEffectRecipeMigrationTests
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
            durationTurns: 1);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.RageStatus,
            stacks: 3);

        var definition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_recipe_amounts"));

        definition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusExpiredTriggeredEffectAmount(2)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new StatusExpiredTargetStatusStacksAmount(
                    StandardCombatIds.RageStatus)));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new StatusExpiredSourceStatusStacksAmount(
                    StandardCombatIds.RageStatus)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ExpireStatusByDefinition(
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
            durationTurns: 1);

        var definition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_recipe_order"));

        definition.Actions.Add(
            new RecordingStatusExpiredAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingStatusExpiredAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingStatusExpiredRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingStatusExpiredRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        ExpireStatusByDefinition(
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
    public void GenericEffectsPreserveExpiredContextAndSourceFreeAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        var expiredStatus = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.WeakStatus);

        var recipe = new CapturingStatusExpiredRecipe();

        var definition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_recipe_context"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        ExpireStatusByDefinition(
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
        Assert.Equal(expiredStatus.Id, recipe.StatusInstanceId);
        Assert.Equal(
            StandardCombatIds.WeakStatus,
            recipe.StatusDefinitionId);
        Assert.False(recipe.ExpiredStatusStillPresent);
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
            durationTurns: 1);

        var definition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_source_fallback"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.Source,
                new FixedStatusExpiredTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ExpireStatusByDefinition(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursiveExpiration()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 1);

        var vulnerableStatus = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.VulnerableStatus);

        var recipe = new DecreaseKnownStatusDurationRecipe(
            HeroId,
            vulnerableStatus.Id);

        var definition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_self_reentry"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        ExpireStatusByDefinition(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Empty(combat.GetCombatant(HeroId).Statuses);

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry =>
                entry.Type == StandardCombatLogTypes.StatusExpired));

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionCanContinueExpirationChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 1);

        var vulnerableStatus = Assert.Single(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.VulnerableStatus);

        var expiringDefinition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_chain_expire"));

        expiringDefinition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        expiringDefinition.Effects.Add(
            new DecreaseKnownStatusDurationRecipe(
                HeroId,
                vulnerableStatus.Id));

        var observingDefinition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_chain_observe"));

        observingDefinition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(
                StandardCombatIds.VulnerableStatus));

        observingDefinition.Effects.Add(
            new GainBlockEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusExpiredTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(expiringDefinition);
        registry.RegisterTriggeredEffectDefinition(observingDefinition);

        ExpireStatusByDefinition(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Empty(combat.GetCombatant(HeroId).Statuses);
        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void ExpirationHealCanContinueIntoHealedDefinition()
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
            durationTurns: 1);

        var expiredDefinition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_recipe_heal"));

        expiredDefinition.Effects.Add(
            new HealEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusExpiredTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(expiredDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        ExpireStatusByDefinition(
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

    [Fact]
    public void ChargeDepletionUsesGenericExpirationRecipes()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 1);

        var definition =
            new StatusExpiredTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_expired_charge_depletion"));

        definition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(
                StandardCombatIds.WeakStatus));

        definition.Effects.Add(
            new GainBlockEffectRecipe<StatusExpiredTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusExpiredTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseStatusChargesByDefinition(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));

        Assert.DoesNotContain(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.WeakStatus);
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

    private static void ExpireStatusByDefinition(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId)
    {
        var status = Assert.Single(
            combat.GetCombatant(targetId).Statuses,
            candidate => candidate.DefinitionId == statusId);

        combat.EnqueueEffect(
            new DecreaseStatusDurationEffectRequest(
                TargetCombatantId: targetId,
                StatusInstanceId: status.Id));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);
    }

    private static void DecreaseStatusChargesByDefinition(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId)
    {
        var status = Assert.Single(
            combat.GetCombatant(targetId).Statuses,
            candidate => candidate.DefinitionId == statusId);

        combat.EnqueueEffect(
            new DecreaseStatusChargesEffectRequest(
                TargetCombatantId: targetId,
                StatusInstanceId: status.Id));

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

    private sealed class RecordingStatusExpiredAction
        : IStatusExpiredTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusExpiredAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusExpiredTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingStatusExpiredRecipe
        : ICombatEffectRecipe<StatusExpiredTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingStatusExpiredRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusExpiredTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingStatusExpiredRecipe
        : ICombatEffectRecipe<StatusExpiredTriggeredEffectContext>
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

        public bool ExpiredStatusStillPresent { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusExpiredTriggeredEffectContext context,
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

            ExpiredStatusStillPresent =
                context.TargetCombatant.Statuses.Any(status =>
                    status.Id == context.CombatEvent.StatusInstanceId);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class DecreaseKnownStatusDurationRecipe
        : ICombatEffectRecipe<StatusExpiredTriggeredEffectContext>
    {
        private readonly CombatantId _targetCombatantId;
        private readonly StatusInstanceId _statusInstanceId;

        public DecreaseKnownStatusDurationRecipe(
            CombatantId targetCombatantId,
            StatusInstanceId statusInstanceId)
        {
            _targetCombatantId = targetCombatantId;
            _statusInstanceId = statusInstanceId;
        }

        public int ActivationCount { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusExpiredTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            ActivationCount++;

            return new IEffectRequest[]
            {
                new DecreaseStatusDurationEffectRequest(
                    TargetCombatantId: _targetCombatantId,
                    StatusInstanceId: _statusInstanceId)
            };
        }
    }
}
