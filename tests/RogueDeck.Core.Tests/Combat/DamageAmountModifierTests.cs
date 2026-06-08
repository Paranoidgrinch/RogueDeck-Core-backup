using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DamageAmountModifierTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void RegistryCanStoreDamageAmountModifiersInPriorityOrder()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterDamageAmountModifier(new WeakDamageAmountModifier());
        registry.RegisterDamageAmountModifier(new StrengthDamageAmountModifier());

        var modifiers = registry.GetDamageAmountModifiers();

        Assert.IsType<StrengthDamageAmountModifier>(modifiers[0]);
        Assert.IsType<WeakDamageAmountModifier>(modifiers[1]);
    }

    [Fact]
    public void RegistryRejectsDuplicateDamageAmountModifierType()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterDamageAmountModifier(new StrengthDamageAmountModifier());

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterDamageAmountModifier(new StrengthDamageAmountModifier()));
    }

    [Fact]
    public void StrengthIncreasesDirectDamageFromSourceByStacks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 2,
            durationTurns: 0);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 6,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(4, goblin.Health.Current);
    }

    [Fact]
    public void WeakReducesDirectDamageFromSourceByTwentyFivePercentRoundedDown()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            stacks: 0,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 8,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(6, goblin.Health.Current);
    }

    [Fact]
    public void StrengthAndWeakAreAppliedInPriorityOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 2,
            durationTurns: 0);

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.WeakStatus,
            stacks: 0,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 6,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(6, goblin.Health.Current);
    }

    [Fact]
    public void DamageModifiersDoNotAffectDamageOverTime()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 5,
            durationTurns: 0);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 5,
            SourceCombatantId: HeroId,
            Kind: DamageKind.DamageOverTime));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(7, goblin.Health.Current);
    }

    [Fact]
    public void DamageModifiersDoNotAffectDamageWithoutSourceCombatant()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyStatus(
            combat,
            registry,
            HeroId,
            StandardCombatIds.StrengthStatus,
            stacks: 5,
            durationTurns: 0);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 6,
            SourceCombatantId: null,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var goblin = combat.GetCombatant(GoblinId);

        Assert.Equal(6, goblin.Health.Current);
    }

    [Fact]
    public void StandardCombatPackageRegistersStandardDamageAmountModifiers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetDamageAmountModifiers(),
            modifier => modifier is StrengthDamageAmountModifier);

        Assert.Contains(
            registry.GetDamageAmountModifiers(),
            modifier => modifier is WeakDamageAmountModifier);
    }

    private static void ApplyStatus(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        StatusDefinitionId statusDefinitionId,
        int stacks,
        int durationTurns)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: statusDefinitionId,
            Stacks: stacks,
            DurationTurns: durationTurns,
            Charges: 0));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}
