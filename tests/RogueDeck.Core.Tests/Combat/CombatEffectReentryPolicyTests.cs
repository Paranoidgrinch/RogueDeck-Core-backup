using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatEffectReentryPolicyTests
{
    [Fact]
    public void RecursiveTriggeredEffectDefinitionIsSuppressedBeforeFiltersRun()
    {
        var resolvedEffectCount = 0;
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCombatEventHandler(
            new RoundStartedTriggeredEffectHandler());

        registry.RegisterEffectRequestHandler(
            new DelegateEffectHandler<ReenqueueRoundStartedEffectRequest>(currentCombat =>
            {
                resolvedEffectCount++;

                currentCombat.EnqueueEvent(
                    new RoundStartedCombatEvent(currentCombat.CurrentRound));
            }));

        var filter = new CountingRoundStartedFilter();

        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_recursive_reentry"));

        definition.Filters.Add(filter);
        definition.Steps.Add(
            new DelegateRoundStartedRecipe((context, buildContext) =>
                new IEffectRequest[]
                {
                    new ReenqueueRoundStartedEffectRequest()
                }));

        registry.RegisterTriggeredEffectDefinition(definition);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEvent(
            new RoundStartedCombatEvent(combat.CurrentRound));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);

        Assert.Equal(1, filter.MatchCount);
        Assert.Equal(1, resolvedEffectCount);
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void SameDefinitionCanTriggerForSiblingEventsInTheSameChain()
    {
        var activationCount = 0;
        var resolvedEffectCount = 0;
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCombatEventHandler(
            new RoundStartedTriggeredEffectHandler());

        registry.RegisterEffectRequestHandler(
            new DelegateEffectHandler<EmitTwoRoundStartedEventsEffectRequest>(currentCombat =>
            {
                currentCombat.EnqueueEvent(
                    new RoundStartedCombatEvent(currentCombat.CurrentRound));

                currentCombat.EnqueueEvent(
                    new RoundStartedCombatEvent(currentCombat.CurrentRound));
            }));

        registry.RegisterEffectRequestHandler(
            new DelegateEffectHandler<CaptureSiblingTriggeredEffectRequest>(_ =>
                resolvedEffectCount++));

        var definition = new RoundStartedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.round_started_sibling_events"));

        definition.Steps.Add(
            new DelegateRoundStartedRecipe((context, buildContext) =>
            {
                activationCount++;

                return new IEffectRequest[]
                {
                    new CaptureSiblingTriggeredEffectRequest()
                };
            }));

        registry.RegisterTriggeredEffectDefinition(definition);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(
            new EmitTwoRoundStartedEventsEffectRequest());

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);

        Assert.Equal(2, activationCount);
        Assert.Equal(2, resolvedEffectCount);
        Assert.False(combat.HasPendingEffects);
        Assert.False(combat.HasPendingEvents);
        Assert.Null(combat.CurrentEffectChain);
    }

    [Fact]
    public void ExplicitAllowPolicyOverridesAncestorSuppression()
    {
        CombatEffectChainContext? triggeredChain = null;
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCombatEventHandler(
            new RoundStartedTriggeredEffectHandler());

        registry.RegisterEffectRequestHandler(
            new DelegateEffectHandler<CapturePolicyChainEffectRequest>(currentCombat =>
                triggeredChain = currentCombat.CurrentEffectChain));

        var definitionId = new TriggeredEffectDefinitionId(
            "test.round_started_policy_evaluation");

        var definition = new RoundStartedTriggeredEffectDefinition(
            definitionId);

        definition.Steps.Add(
            new DelegateRoundStartedRecipe((context, buildContext) =>
                new IEffectRequest[]
                {
                    new CapturePolicyChainEffectRequest()
                }));

        registry.RegisterTriggeredEffectDefinition(definition);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEvent(
            new RoundStartedCombatEvent(combat.CurrentRound));

        new CombatQueueProcessor().ResolvePendingQueues(
            combat,
            registry);

        Assert.NotNull(triggeredChain);
        Assert.True(
            triggeredChain!.ContainsTriggeredEffectDefinition(definitionId));
        Assert.False(
            triggeredChain.CanEnterTriggeredEffectDefinition(definition));
        Assert.True(
            triggeredChain.CanEnterTriggeredEffectDefinition(
                new AllowRecursiveTestDefinition(definitionId)));
    }

    [Fact]
    public void ReentryPolicyIsEvaluatedBeforeFiltersAndRemainsOutsideEvents()
    {
        var repoRoot = FindRepositoryRoot();

        var chainSource = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "CombatEffectChain.cs"));

        var definitionsSource = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "TriggeredEffects.cs"));

        var runnerSource = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "TriggeredEffectDefinitionRunner.cs"));

        var eventsSource = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "CombatEvents.cs"));

        Assert.Contains(
            "public enum TriggeredEffectReentryPolicy",
            definitionsSource);
        Assert.Contains(
            "TriggeredEffectReentryPolicy ReentryPolicy =>",
            definitionsSource);
        Assert.Contains(
            "TriggeredEffectReentryPolicy.SuppressRecursiveReentry;",
            definitionsSource);
        Assert.Contains(
            "!ContainsTriggeredEffectDefinition(definition.Id)",
            chainSource);
        Assert.Contains(
            "TriggeredEffectReentryPolicy.AllowRecursiveReentry => true",
            chainSource);
        Assert.Contains(
            "currentEffectChain.CanEnterTriggeredEffectDefinition(reentryDefinition)",
            runnerSource);

        var reentryCheckIndex = runnerSource.IndexOf(
            "CanEnterTriggeredEffectDefinition",
            StringComparison.Ordinal);

        var filterCheckIndex = runnerSource.IndexOf(
            "if (!matches(definition))",
            StringComparison.Ordinal);

        Assert.True(reentryCheckIndex >= 0);
        Assert.True(filterCheckIndex > reentryCheckIndex);
        Assert.DoesNotContain(
            "TriggeredEffectReentryPolicy",
            eventsSource);
        Assert.DoesNotContain(
            "CombatEffectChainContext",
            eventsSource);
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

    private sealed record ReenqueueRoundStartedEffectRequest : IEffectRequest;

    private sealed record EmitTwoRoundStartedEventsEffectRequest : IEffectRequest;

    private sealed record CaptureSiblingTriggeredEffectRequest : IEffectRequest;

    private sealed record CapturePolicyChainEffectRequest : IEffectRequest;

    private sealed class AllowRecursiveTestDefinition
        : ITriggeredEffectDefinition
    {
        public TriggeredEffectDefinitionId Id { get; }

        public Type EventType => typeof(RoundStartedCombatEvent);

        public TriggeredEffectReentryPolicy ReentryPolicy =>
            TriggeredEffectReentryPolicy.AllowRecursiveReentry;

        public AllowRecursiveTestDefinition(
            TriggeredEffectDefinitionId id)
        {
            Id = id;
        }
    }

    private sealed class CountingRoundStartedFilter
        : IRoundStartedTriggeredEffectFilter
    {
        public int MatchCount { get; private set; }

        public bool Matches(
            RoundStartedTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            MatchCount++;
            return true;
        }
    }

    private sealed class DelegateRoundStartedRecipe
        : ICombatEffectRecipe<RoundStartedTriggeredEffectContext>
    {
        private readonly Func<
            RoundStartedTriggeredEffectContext,
            TriggeredEffectActionBuildContext,
            IReadOnlyCollection<IEffectRequest>> _buildEffectRequests;

        public DelegateRoundStartedRecipe(
            Func<
                RoundStartedTriggeredEffectContext,
                TriggeredEffectActionBuildContext,
                IReadOnlyCollection<IEffectRequest>> buildEffectRequests)
        {
            _buildEffectRequests = buildEffectRequests
                ?? throw new ArgumentNullException(nameof(buildEffectRequests));
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            RoundStartedTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            return _buildEffectRequests(
                context,
                buildContext);
        }
    }

    private sealed class DelegateEffectHandler<TRequest>
        : EffectRequestHandler<TRequest>
        where TRequest : IEffectRequest
    {
        private readonly Action<CombatState> _resolve;

        public DelegateEffectHandler(
            Action<CombatState> resolve)
        {
            _resolve = resolve;
        }

        protected override void Resolve(
            CombatState combat,
            CombatDefinitionRegistry registry,
            TRequest request)
        {
            _resolve(combat);
        }
    }
}
