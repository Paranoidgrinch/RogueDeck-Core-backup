using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class StatusLifecycleTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersStatusLifecycleTriggeredEffectHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusRemovedCombatEvent)),
            handler => handler is StatusRemovedTriggeredEffectHandler);

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusExpiredCombatEvent)),
            handler => handler is StatusExpiredTriggeredEffectHandler);

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusMergedCombatEvent)),
            handler => handler is StatusMergedTriggeredEffectHandler);

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(StatusChargesReducedCombatEvent)),
            handler => handler is StatusChargesReducedTriggeredEffectHandler);
    }

    [Fact]
    public void StatusRemovedTriggeredEffectCanTargetEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition = new StatusRemovedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_removed_gain_block"));

        definition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Steps.Add(
            new GainBlockOnStatusRemovedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new StatusRemovedCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            1,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusRemovedTriggeredEffectSourceSelectorDoesNotTargetWhenSourceIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 2);

        var definition = new StatusRemovedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_removed_missing_source"));

        definition.Filters.Add(
            new StatusRemovedStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Steps.Add(
            new DealDamageOnStatusRemovedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusRemovedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var heroHealthBefore = combat.GetCombatant(HeroId).Health.Current;

        RemoveStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus);

        Assert.Equal(
            heroHealthBefore,
            combat.GetCombatant(HeroId).Health.Current);
    }

    [Fact]
    public void StatusChargesReducedTriggeredEffectCanTargetEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 3);

        var definition = new StatusChargesReducedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_charges_reduced_gain_block"));

        definition.Steps.Add(
            new GainBlockOnStatusChargesReducedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
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
    public void StatusChargesReducedTriggeredEffectFiltersByStatusDefinitionId()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 3);

        var matching = new StatusChargesReducedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_charges_reduced_matching"));

        matching.Filters.Add(
            new StatusChargesReducedStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        matching.Steps.Add(
            new GainBlockOnStatusChargesReducedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusChargesReducedTriggeredEffectAmount(2)));

        var nonMatching = new StatusChargesReducedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_charges_reduced_non_matching"));

        nonMatching.Filters.Add(
            new StatusChargesReducedStatusDefinitionTriggerFilter(StandardCombatIds.PoisonStatus));

        nonMatching.Steps.Add(
            new GainBlockOnStatusChargesReducedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedStatusChargesReducedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusChargesReducedTriggeredEffectAmountUsesReducedChargeCount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 5);

        var definition = new StatusChargesReducedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_charges_reduced_amount"));

        definition.Steps.Add(
            new GainBlockOnStatusChargesReducedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new StatusChargesReducedChargeCountAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId,
            amount: 2);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusChargesReducedTriggeredEffectSourceSelectorDoesNotTargetWhenSourceIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            charges: 3);

        var definition = new StatusChargesReducedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_charges_reduced_missing_source"));

        definition.Steps.Add(
            new GainBlockOnStatusChargesReducedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusChargesReducedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DecreaseFirstStatusCharges(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusExpiredTriggeredEffectCanTargetEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        var definition = new StatusExpiredTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_expired_apply_status"));

        definition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Steps.Add(
            new ApplyStatusOnStatusExpiredTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                StandardCombatIds.RageStatus,
                new FixedStatusExpiredTriggeredEffectAmount(2),
                new FixedStatusExpiredTriggeredEffectAmount(0),
                new FixedStatusExpiredTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ExpireFirstStatus(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(HeroId).Statuses,
                status => status.DefinitionId == StandardCombatIds.RageStatus).Stacks);
    }

    [Fact]
    public void StatusExpiredTriggeredEffectSourceSelectorDoesNotTargetWhenSourceIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            durationTurns: 1);

        var definition = new StatusExpiredTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_expired_missing_source"));

        definition.Filters.Add(
            new StatusExpiredStatusDefinitionTriggerFilter(StandardCombatIds.WeakStatus));

        definition.Steps.Add(
            new DealDamageOnStatusExpiredTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusExpiredTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var heroHealthBefore = combat.GetCombatant(HeroId).Health.Current;

        ExpireFirstStatus(
            combat,
            registry,
            HeroId);

        Assert.Equal(
            heroHealthBefore,
            combat.GetCombatant(HeroId).Health.Current);
    }

    [Fact]
    public void StatusMergedTriggeredEffectCanTargetEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            StandardCombatIds.PoisonStatus,
            sourceId: HeroId,
            stacks: 3);

        var definition = new StatusMergedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_merged_gain_block"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(StandardCombatIds.PoisonStatus));

        definition.Steps.Add(
            new GainBlockOnStatusMergedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new StatusMergedStacksAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            StandardCombatIds.PoisonStatus,
            sourceId: HeroId,
            stacks: 2);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void StatusMergedTriggeredEffectCanTargetSourceWhenSourceExists()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            StandardCombatIds.PoisonStatus,
            sourceId: HeroId,
            stacks: 3);

        var definition = new StatusMergedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_merged_source"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(StandardCombatIds.PoisonStatus));

        definition.Filters.Add(new StatusMergedHasSourceTriggerFilter());

        definition.Steps.Add(
            new GainBlockOnStatusMergedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusMergedTriggeredEffectAmount(2)));

        registry.RegisterTriggeredEffectDefinition(definition);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            StandardCombatIds.PoisonStatus,
            sourceId: HeroId,
            stacks: 2);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void StatusMergedTriggeredEffectSourceSelectorDoesNotTargetWhenSourceIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.PoisonStatus,
            stacks: 3);

        var definition = new StatusMergedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.status_merged_missing_source"));

        definition.Filters.Add(
            new StatusMergedStatusDefinitionTriggerFilter(StandardCombatIds.PoisonStatus));

        definition.Steps.Add(
            new DealDamageOnStatusMergedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedStatusMergedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var heroHealthBefore = combat.GetCombatant(HeroId).Health.Current;

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.PoisonStatus,
            stacks: 2);

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

    private static void RemoveStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId)
    {
        combat.EnqueueEffect(new RemoveStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void DecreaseFirstStatusCharges(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount = 1)
    {
        var status = Assert.Single(combat.GetCombatant(targetId).Statuses);

        combat.EnqueueEffect(new DecreaseStatusChargesEffectRequest(
            TargetCombatantId: targetId,
            StatusInstanceId: status.Id,
            Amount: amount));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void ExpireFirstStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId)
    {
        var status = Assert.Single(combat.GetCombatant(targetId).Statuses);

        combat.EnqueueEffect(new DecreaseStatusDurationEffectRequest(
            TargetCombatantId: targetId,
            StatusInstanceId: status.Id));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}
