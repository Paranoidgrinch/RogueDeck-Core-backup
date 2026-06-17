using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DamageDealtTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersDamageDealtTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(DamageDealtCombatEvent)),
            handler => handler is DamageDealtTriggeredEffectHandler);
    }

    [Fact]
    public void DamageDealtTriggeredEffectCanGainBlockForSourceBasedOnHealthDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_dealt_gain_block"));

        definition.Filters.Add(new DamageDealtHasSourceTriggerFilter());

        definition.Steps.Add(
            new GainBlockOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new DamageDealtHealthDamageAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 3,
            sourceId: HeroId);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageDealtTriggeredEffectCanApplyStatusToDamagedTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var statusId = new StatusDefinitionId("test.damage_dealt_target_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_dealt_apply_status"));

        definition.Steps.Add(
            new ApplyStatusOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                statusId,
                new FixedDamageDealtTriggeredEffectAmount(2),
                new FixedDamageDealtTriggeredEffectAmount(0),
                new FixedDamageDealtTriggeredEffectAmount(0)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 1,
            sourceId: HeroId);

        Assert.Equal(
            2,
            Assert.Single(
                combat.GetCombatant(GoblinId).Statuses,
                status => status.DefinitionId == statusId).Stacks);
    }

    [Fact]
    public void DamageDealtTriggeredEffectCanUseSourceStatusStacksAsAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var sourceStatusId = new StatusDefinitionId("test.damage_dealt_source_counter");
        RegisterStackingStatus(registry, sourceStatusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            sourceStatusId,
            stacks: 4);

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_dealt_source_status_amount"));

        definition.Filters.Add(new DamageDealtSourceHasStatusTriggerFilter(sourceStatusId));

        definition.Steps.Add(
            new GainBlockOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new DamageDealtSourceStatusStacksAmount(sourceStatusId)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 1,
            sourceId: HeroId);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageDealtTriggeredEffectDoesNotRunWhenMinimumHealthDamageFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_dealt_minimum_filter"));

        definition.Filters.Add(
            new DamageDealtMinimumHealthDamageTriggerFilter(MinimumHealthDamage: 5));

        definition.Steps.Add(
            new GainBlockOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 2,
            sourceId: HeroId);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void DamageDealtTriggeredEffectDoesNotRunWhenDamageKindFilterDoesNotMatch()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new DamageDealtTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.damage_dealt_kind_filter"));

        definition.Filters.Add(
            new DamageDealtDamageKindTriggerFilter(DamageKind.Direct));

        definition.Steps.Add(
            new GainBlockOnDamageDealtTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedDamageDealtTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        DealDamage(
            combat,
            registry,
            targetId: GoblinId,
            amount: 2,
            sourceId: HeroId,
            kind: DamageKind.DamageOverTime);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
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




