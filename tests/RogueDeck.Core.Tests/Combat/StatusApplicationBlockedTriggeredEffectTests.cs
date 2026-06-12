using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusApplicationBlockedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersStatusApplicationBlockedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusApplicationBlockedCombatEvent)),
            handler => handler is StatusApplicationBlockedTriggeredEffectHandler);
    }

    [Fact]
    public void StatusApplicationBlockedTriggeredEffectFiresWhenStatusApplicationIsBlocked()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.ArtifactStatus,
            charges: 1);

        var definition = new StatusApplicationBlockedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_application_blocked_gain_block"));

        definition.Filters.Add(
            new StatusApplicationBlockedBlockedStatusTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Actions.Add(
            new GainBlockOnStatusApplicationBlockedTriggeredEffectAction(
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
            status => status.DefinitionId == StandardCombatIds.WeakStatus);
    }

    [Fact]
    public void StatusApplicationBlockedTriggeredEffectDoesNotFireWhenBlockedStatusFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.ArtifactStatus,
            charges: 1);

        var definition = new StatusApplicationBlockedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_application_blocked_filter"));

        definition.Filters.Add(
            new StatusApplicationBlockedBlockedStatusTriggerFilter(StandardCombatIds.PoisonStatus));

        definition.Actions.Add(
            new GainBlockOnStatusApplicationBlockedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusApplicationBlockedTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusApplicationBlockedTriggeredEffectCanFilterByBlockingStatus()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.ArtifactStatus,
            charges: 1);

        var matching = new StatusApplicationBlockedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_application_blocked_matching_blocker"));

        matching.Filters.Add(
            new StatusApplicationBlockedBlockingStatusTriggerFilter(StandardCombatIds.ArtifactStatus));

        matching.Actions.Add(
            new GainBlockOnStatusApplicationBlockedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusApplicationBlockedTriggeredEffectAmount(3)));

        var nonMatching = new StatusApplicationBlockedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_application_blocked_non_matching_blocker"));

        nonMatching.Filters.Add(
            new StatusApplicationBlockedBlockingStatusTriggerFilter(StandardCombatIds.StrengthStatus));

        nonMatching.Actions.Add(
            new GainBlockOnStatusApplicationBlockedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusApplicationBlockedTriggeredEffectAmount(9)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusApplicationBlockedTriggeredEffectCanUseSourceSelectorForBlockedTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.ArtifactStatus,
            charges: 1);

        var definition = new StatusApplicationBlockedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_application_blocked_source_selector"));

        definition.Actions.Add(
            new GainBlockOnStatusApplicationBlockedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusApplicationBlockedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            sourceId: GoblinId,
            durationTurns: 1);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        CombatantId? sourceId = null,
        int stacks = 0,
        int durationTurns = 0,
        int charges = 0)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusId,
            SourceCombatantId: sourceId,
            Stacks: stacks,
            DurationTurns: durationTurns,
            Charges: charges));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
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
}
