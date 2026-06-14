using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusApplicationBlockedEffectRecipeMigrationTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId IncomingSourceCardId =
        new("test.status_application_blocked_incoming_card");

    [Fact]
    public void GenericGainBlockRecipeUsesFixedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyArtifact(
            combat,
            registry,
            charges: 1);

        var definition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_recipe_gain_block"));

        definition.Filters.Add(
            new StatusApplicationBlockedBlockedStatusTriggerFilter(
                StandardCombatIds.WeakStatus));

        definition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusApplicationBlockedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusApplicationBlockedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.DoesNotContain(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.WeakStatus);
    }

    [Fact]
    public void LegacyActionsRunBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var executionOrder = new List<string>();

        ApplyArtifact(
            combat,
            registry,
            charges: 1);

        var definition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_recipe_order"));

        definition.Actions.Add(
            new RecordingBlockedAction(
                executionOrder,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingBlockedAction(
                executionOrder,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingBlockedRecipe(
                executionOrder,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingBlockedRecipe(
                executionOrder,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

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
    public void GenericEffectsPreserveBlockedContextAndTargetAttribution()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        ApplyArtifact(
            combat,
            registry,
            charges: 1);

        var artifact = hero.Statuses.Single(status =>
            status.DefinitionId == StandardCombatIds.ArtifactStatus);

        var recipe = new CapturingBlockedRecipe();

        var definition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_recipe_context"));

        definition.Effects.Add(recipe);
        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            sourceCombatantId: GoblinId,
            sourceCardId: IncomingSourceCardId,
            durationTurns: 1);

        Assert.Equal(1, recipe.ActivationCount);
        Assert.Equal(HeroId, recipe.EventTargetId);
        Assert.Equal(HeroId, recipe.TargetingSourceId);
        Assert.Equal(HeroId, recipe.EffectSourceCombatantId);
        Assert.Null(recipe.EffectSourceCardId);
        Assert.Equal(HeroId, recipe.ContextTargetId);
        Assert.Equal(
            StandardCombatIds.WeakStatus,
            recipe.BlockedStatusDefinitionId);
        Assert.Equal(
            StandardCombatIds.ArtifactStatus,
            recipe.BlockingStatusDefinitionId);
        Assert.Equal(
            artifact.Id,
            recipe.BlockingStatusInstanceId);
        Assert.False(recipe.BlockingStatusStillPresent);

        Assert.DoesNotContain(
            hero.Statuses,
            status => status.Id == artifact.Id);
    }

    [Fact]
    public void DefinitionSuppressesOwnRecursiveBlockedApplication()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyArtifact(
            combat,
            registry,
            charges: 2);

        var definition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_self_reentry"));

        definition.Effects.Add(
            new ApplyBlockedStatusRecipe(
                StandardCombatIds.WeakStatus,
                durationTurns: 1));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry =>
                entry.Type ==
                StandardCombatLogTypes.StatusApplicationBlocked));

        Assert.DoesNotContain(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.WeakStatus);

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void DifferentDefinitionsCanContinueTheSameBlockedChain()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyArtifact(
            combat,
            registry,
            charges: 2);

        var firstDefinition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_chain_apply"));

        firstDefinition.Effects.Add(
            new ApplyBlockedStatusRecipe(
                StandardCombatIds.PoisonStatus,
                stacks: 1));

        var secondDefinition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_chain_block"));

        secondDefinition.Filters.Add(
            new StatusApplicationBlockedBlockedStatusTriggerFilter(
                StandardCombatIds.PoisonStatus));

        secondDefinition.Effects.Add(
            new GainBlockEffectRecipe<
                StatusApplicationBlockedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusApplicationBlockedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(firstDefinition);
        registry.RegisterTriggeredEffectDefinition(secondDefinition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry =>
                entry.Type ==
                StandardCombatLogTypes.StatusApplicationBlocked));

        Assert.DoesNotContain(
            combat.GetCombatant(HeroId).Statuses,
            status =>
                status.DefinitionId == StandardCombatIds.PoisonStatus);

        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void BlockedStatusHealCanContinueIntoHealedDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        DamageHero(
            combat,
            registry,
            amount: 5);

        ApplyArtifact(
            combat,
            registry,
            charges: 1);

        var blockedDefinition =
            new StatusApplicationBlockedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_recipe_heal"));

        blockedDefinition.Effects.Add(
            new HealEffectRecipe<
                StatusApplicationBlockedTriggeredEffectContext>(
                    CombatantTargetSelectors.EventTarget,
                    new FixedStatusApplicationBlockedTriggeredEffectAmount(2)));

        var healedDefinition =
            new HealedTriggeredEffectDefinition(
                new TriggeredEffectDefinitionId(
                    "test.status_application_blocked_healed_follow_up"));

        healedDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(blockedDefinition);
        registry.RegisterTriggeredEffectDefinition(healedDefinition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            sourceCombatantId: GoblinId,
            sourceCardId: IncomingSourceCardId,
            durationTurns: 1);

        Assert.Equal(
            hero.Health.Max - 3,
            hero.Health.Current);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    private static void ApplyArtifact(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int charges)
    {
        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.ArtifactStatus,
            charges: charges);
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
                TargetCombatantId: targetId,
                StatusDefinitionId: statusId,
                SourceCombatantId: sourceCombatantId,
                SourceCardId: sourceCardId,
                Stacks: stacks,
                DurationTurns: durationTurns,
                Charges: charges));

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

    private sealed class RecordingBlockedAction
        : IStatusApplicationBlockedTriggeredEffectAction
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingBlockedAction(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusApplicationBlockedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingBlockedRecipe
        : ICombatEffectRecipe<
            StatusApplicationBlockedTriggeredEffectContext>
    {
        private readonly ICollection<string> _executionOrder;
        private readonly string _entry;

        public RecordingBlockedRecipe(
            ICollection<string> executionOrder,
            string entry)
        {
            _executionOrder = executionOrder;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusApplicationBlockedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _executionOrder.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class CapturingBlockedRecipe
        : ICombatEffectRecipe<
            StatusApplicationBlockedTriggeredEffectContext>
    {
        public int ActivationCount { get; private set; }

        public CombatantId? EventTargetId { get; private set; }

        public CombatantId? TargetingSourceId { get; private set; }

        public CombatantId? EffectSourceCombatantId { get; private set; }

        public CardDefinitionId? EffectSourceCardId { get; private set; }

        public CombatantId? ContextTargetId { get; private set; }

        public StatusDefinitionId? BlockedStatusDefinitionId {
            get;
            private set;
        }

        public StatusInstanceId? BlockingStatusInstanceId {
            get;
            private set;
        }

        public StatusDefinitionId? BlockingStatusDefinitionId {
            get;
            private set;
        }

        public bool BlockingStatusStillPresent { get; private set; }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusApplicationBlockedTriggeredEffectContext context,
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
            BlockedStatusDefinitionId =
                context.CombatEvent.BlockedStatusDefinitionId;
            BlockingStatusInstanceId =
                context.CombatEvent.BlockingStatusInstanceId;
            BlockingStatusDefinitionId =
                context.CombatEvent.BlockingStatusDefinitionId;
            BlockingStatusStillPresent =
                context.TargetCombatant.Statuses.Any(status =>
                    status.Id ==
                    context.CombatEvent.BlockingStatusInstanceId);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class ApplyBlockedStatusRecipe
        : ICombatEffectRecipe<
            StatusApplicationBlockedTriggeredEffectContext>
    {
        private readonly StatusDefinitionId _statusDefinitionId;
        private readonly int _stacks;
        private readonly int _durationTurns;

        public ApplyBlockedStatusRecipe(
            StatusDefinitionId statusDefinitionId,
            int stacks = 0,
            int durationTurns = 0)
        {
            _statusDefinitionId = statusDefinitionId;
            _stacks = stacks;
            _durationTurns = durationTurns;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            StatusApplicationBlockedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            return new IEffectRequest[]
            {
                new ApplyStatusEffectRequest(
                    TargetCombatantId:
                        context.CombatEvent.TargetCombatantId,
                    StatusDefinitionId:
                        _statusDefinitionId,
                    SourceCombatantId:
                        buildContext.Source.SourceCombatantId,
                    SourceCardId:
                        buildContext.Source.SourceCardId,
                    Stacks:
                        _stacks,
                    DurationTurns:
                        _durationTurns)
            };
        }
    }
}
