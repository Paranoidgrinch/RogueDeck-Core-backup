using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusesRemovedByPolarityTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersStatusesRemovedByPolarityTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusesRemovedByPolarityCombatEvent)),
            handler => handler is StatusesRemovedByPolarityTriggeredEffectHandler);
    }

    [Fact]
    public void StatusesRemovedByPolarityTriggeredEffectFiresWhenStatusesAreRemovedByPolarity()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_gain_block"));

        definition.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatusesByPolarity(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusesRemovedByPolarityTriggeredEffectFiltersByPolarity()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 3);

        var matching = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_matching"));

        matching.Filters.Add(
            new StatusesRemovedByPolarityTriggerFilter(StatusPolarity.Buff));

        matching.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(2)));

        var nonMatching = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_non_matching"));

        nonMatching.Filters.Add(
            new StatusesRemovedByPolarityTriggerFilter(StatusPolarity.Debuff));

        nonMatching.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        RemoveStatusesByPolarity(
            combat,
            registry,
            HeroId,
            StatusPolarity.Buff);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusesRemovedByPolarityTriggeredEffectFiltersByTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var matching = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_matching_target"));

        matching.Filters.Add(
            new StatusesRemovedByPolarityTargetTriggerFilter(HeroId));

        matching.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(3)));

        var nonMatching = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_non_matching_target"));

        nonMatching.Filters.Add(
            new StatusesRemovedByPolarityTargetTriggerFilter(GoblinId));

        nonMatching.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(8)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        RemoveStatusesByPolarity(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusesRemovedByPolarityTriggeredEffectMinimumCountFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_minimum_count"));

        definition.Filters.Add(
            new StatusesRemovedByPolarityMinimumCountTriggerFilter(MinimumCount: 2));

        definition.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatusesByPolarity(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusesRemovedByPolarityTriggeredEffectCanUseRemovedStatusCount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.VulnerableStatus,
            durationTurns: 2);

        var definition = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_count_amount"));

        definition.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new StatusesRemovedByPolarityRemovedStatusCount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatusesByPolarity(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusesRemovedByPolarityTriggeredEffectCanUseSourceSelectorForTargetCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition = new StatusesRemovedByPolarityTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.statuses_removed_by_polarity_source_selector"));

        definition.Steps.Add(
            new GainBlockOnStatusesRemovedByPolarityTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusesRemovedByPolarityTriggeredEffectAmount(6)));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatusesByPolarity(
            combat,
            registry,
            HeroId,
            StatusPolarity.Debuff);

        Assert.Equal(
            6,
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
        int stacks = 0,
        int durationTurns = 0,
        int charges = 0)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusId,
            Stacks: stacks,
            DurationTurns: durationTurns,
            Charges: charges));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void RemoveStatusesByPolarity(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusPolarity polarity)
    {
        combat.EnqueueEffect(new RemoveStatusesByPolarityEffectRequest(
            TargetCombatantId: targetId,
            Polarity: polarity));

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
