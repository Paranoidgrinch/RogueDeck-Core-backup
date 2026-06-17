using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DamageReceivedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersDamageReceivedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(DamageReceivedCombatEvent)),
            handler => handler is DamageReceivedTriggeredEffectHandler);
    }

    [Fact]
    public void DamageReceivedTriggeredEffectCanGainBlockForReceiverBasedOnHealthDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_gain_block"));

        definition.Steps.Add(
            new GainBlockOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedHealthDamageAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 3,
            sourceId: GoblinId);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageReceivedTriggeredEffectCanApplyStatusToReceiver()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.damage_received_receiver_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Buff);

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_apply_status"));

        definition.Steps.Add(
            new ApplyStatusOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                new FixedDamageReceivedTriggeredEffectAmount(2),
                new FixedDamageReceivedTriggeredEffectAmount(0),
                new FixedDamageReceivedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceId: GoblinId);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(HeroId).Statuses,
                status => status.DefinitionId == statusId).Stacks);
    }

    [Fact]
    public void DamageReceivedTriggeredEffectCanApplyStatusToSourceWhenSourceExists()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.damage_received_source_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_apply_status_to_source"));

        definition.Filters.Add(new DamageReceivedHasSourceTriggerFilter());

        definition.Steps.Add(
            new ApplyStatusOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                statusId,
                new FixedDamageReceivedTriggeredEffectAmount(1),
                new FixedDamageReceivedTriggeredEffectAmount(0),
                new FixedDamageReceivedTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceId: GoblinId);

        Assert.Equal(
            1,
            Assert.Single(
                combat.GetCombatant(GoblinId).Statuses,
                status => status.DefinitionId == statusId).Stacks);
    }

    [Fact]
    public void DamageReceivedTriggeredEffectCanDealDamageToSourceWhenSourceExists()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_damage_source"));

        definition.Filters.Add(new DamageReceivedHasSourceTriggerFilter());
        definition.Filters.Add(new DamageReceivedDamageKindTriggerFilter(DamageKind.Direct));

        definition.Steps.Add(
            new DealDamageOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedDamageReceivedTriggeredEffectAmount(2),
                kind: DamageKind.Reflected));

        registry.RegisterTriggeredEffectDefinition(definition);

        var goblinHealthBefore = combat.GetCombatant(GoblinId).Health.Current;

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceId: GoblinId);

        Assert.Equal(
            goblinHealthBefore - 2,
            combat.GetCombatant(GoblinId).Health.Current);
    }

    [Fact]
    public void DamageReceivedTriggeredEffectCanUseReceiverStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var receiverStatusId = new StatusDefinitionId("test.damage_received_receiver_counter");
        RegisterStackingStatus(registry, receiverStatusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            receiverStatusId,
            stacks: 4);

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_receiver_status_amount"));

        definition.Filters.Add(new DamageReceivedReceiverHasStatusTriggerFilter(receiverStatusId));

        definition.Steps.Add(
            new GainBlockOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedReceiverStatusStacksAmount(receiverStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceId: GoblinId);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageReceivedTriggeredEffectCanUseSourceStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var sourceStatusId = new StatusDefinitionId("test.damage_received_source_counter");
        RegisterStackingStatus(registry, sourceStatusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            GoblinId,
            sourceStatusId,
            stacks: 5);

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_source_status_amount"));

        definition.Filters.Add(new DamageReceivedSourceHasStatusTriggerFilter(sourceStatusId));

        definition.Steps.Add(
            new GainBlockOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new DamageReceivedSourceStatusStacksAmount(sourceStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1,
            sourceId: GoblinId);

        Assert.Equal(
            5,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageReceivedTriggeredEffectDoesNotRunWhenMinimumHealthDamageFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_minimum_filter"));

        definition.Filters.Add(
            new DamageReceivedMinimumHealthDamageTriggerFilter(MinimumHealthDamage: 5));

        definition.Steps.Add(
            new GainBlockOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 2,
            sourceId: GoblinId);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageReceivedTriggeredEffectDoesNotRunWhenDamageKindFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_kind_filter"));

        definition.Filters.Add(
            new DamageReceivedDamageKindTriggerFilter(DamageKind.Direct));

        definition.Steps.Add(
            new GainBlockOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedDamageReceivedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 2,
            sourceId: GoblinId,
            kind: DamageKind.DamageOverTime);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageReceivedTriggeredEffectWithSourceFilterDoesNotRunWhenSourceIsMissing()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageReceivedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_received_missing_source"));

        definition.Filters.Add(new DamageReceivedHasSourceTriggerFilter());

        definition.Steps.Add(
            new DealDamageOnDamageReceivedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedDamageReceivedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        var heroHealthBefore = combat.GetCombatant(HeroId).Health.Current;
        var goblinHealthBefore = combat.GetCombatant(GoblinId).Health.Current;

        DealDamage(
            combat,
            registry,
            targetId: HeroId,
            amount: 1);

        Assert.Equal(
            heroHealthBefore - 1,
            combat.GetCombatant(HeroId).Health.Current);

        Assert.Equal(
            goblinHealthBefore,
            combat.GetCombatant(GoblinId).Health.Current);
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

    private static void DealDamage(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount,
        CombatantId? sourceId = null,
        DamageKind kind = DamageKind.Direct)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: targetId,
            Amount: amount,
            SourceCombatantId: sourceId,
            Kind: kind));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void RegisterStackingStatus(
        CombatDefinitionRegistry registry,
        StatusDefinitionId statusId,
        StatusPolarity polarity)
    {
        var definition = new StatusDefinition(
            statusId,
            new PackageId("test"),
            displayNameKey: $"status.{statusId}.name",
            descriptionKey: $"status.{statusId}.description",
            polarity: polarity,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        registry.RegisterStatus(definition);
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusId,
        int stacks)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusId,
            Stacks: stacks,
            DurationTurns: 0,
            Charges: 0));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}
