using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusAppliedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersStatusAppliedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusAppliedCombatEvent)),
            handler => handler is StatusAppliedTriggeredEffectHandler);
    }

    [Fact]
    public void StatusAppliedTriggeredEffectCanReactWhenStatusIsApplied()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new StatusAppliedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_applied_gain_block"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(StandardCombatIds.PoisonStatus));

        definition.Actions.Add(
            new GainBlockOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new StatusAppliedStacksAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.PoisonStatus,
            stacks: 3);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusAppliedTriggeredEffectDoesNotRunWhenAppliedStatusFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new StatusAppliedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_applied_filter"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(StandardCombatIds.PoisonStatus));

        definition.Actions.Add(
            new GainBlockOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusAppliedTriggeredEffectAmount(2)));

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
    public void StatusAppliedTriggeredEffectCanFilterByTargetHavingAnotherStatus()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 2);

        var definition = new StatusAppliedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_applied_target_has_status"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Filters.Add(
            new StatusAppliedTargetHasStatusTriggerFilter(StandardCombatIds.StrengthStatus));

        definition.Actions.Add(
            new ApplyStatusOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                StandardCombatIds.RageStatus,
                new FixedStatusAppliedTriggeredEffectAmount(1),
                new FixedStatusAppliedTriggeredEffectAmount(0),
                new FixedStatusAppliedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            1,
            Assert.Single(
                combat.GetCombatant(HeroId).Statuses,
                status => status.DefinitionId == StandardCombatIds.RageStatus).Stacks);
    }

    [Fact]
    public void StatusAppliedTriggeredEffectCanTargetSourceWhenSourceExists()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 4);

        var definition = new StatusAppliedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_applied_source"));

        definition.Filters.Add(
            new StatusAppliedStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Filters.Add(new StatusAppliedHasSourceTriggerFilter());

        definition.Actions.Add(
            new GainBlockOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new StatusAppliedSourceStatusStacksAmount(StandardCombatIds.StrengthStatus)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            StandardCombatIds.WeakStatus,
            sourceId: HeroId,
            durationTurns: 1);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusAppliedTriggeredEffectWithSourceFilterDoesNotRunWhenSourceIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new StatusAppliedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_applied_missing_source"));

        definition.Filters.Add(new StatusAppliedHasSourceTriggerFilter());

        definition.Actions.Add(
            new DealDamageOnStatusAppliedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusAppliedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var heroHealthBefore = combat.GetCombatant(HeroId).Health.Current;

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        Assert.Equal(
            heroHealthBefore,
            combat.GetCombatant(HeroId).Health.Current);
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
}
