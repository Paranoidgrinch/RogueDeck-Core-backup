using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class VulnerableStatusTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersVulnerablePieces()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var definition = registry.GetStatus(StandardCombatIds.VulnerableStatus);

        Assert.Equal(StatusPolarity.Debuff, definition.Polarity);
        Assert.True(definition.UsesDuration);
        Assert.True(definition.ShowDurationInUi);
        Assert.Contains(StandardCombatIds.DebuffTag, definition.Tags);
        Assert.Contains(StandardCombatIds.DamageModifierTag, definition.Tags);

        Assert.Contains(
            registry.GetDamageAmountModifiers(),
            modifier => modifier is VulnerableDamageAmountModifier);
    }

    [Fact]
    public void VulnerableIncreasesDirectDamageAgainstTargetByFiftyPercentRoundedDown()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyVulnerable(
            combat,
            registry,
            GoblinId,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 5,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(5, combat.GetCombatant(GoblinId).Health.Current);
    }

    [Fact]
    public void VulnerableIsAppliedBeforeBlockConsumesIncomingDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var goblin = combat.GetCombatant(GoblinId);
        goblin.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 5));

        ApplyVulnerable(
            combat,
            registry,
            GoblinId,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 6,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(8, goblin.Health.Current);
        Assert.Equal(0, goblin.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        var damageLog = combat.CombatLog.Last(entry =>
            entry.Type == StandardCombatLogTypes.DamageDealt);

        Assert.Contains("Dealt 4 damage", damageLog.Message);
        Assert.Contains("blocked 5 damage", damageLog.Message);
    }

    [Fact]
    public void VulnerableDoesNotAffectDamageOverTime()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyVulnerable(
            combat,
            registry,
            GoblinId,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 5,
            SourceCombatantId: HeroId,
            Kind: DamageKind.DamageOverTime));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(7, combat.GetCombatant(GoblinId).Health.Current);
    }

    [Fact]
    public void VulnerableDoesNotAffectReflectedDamage()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyVulnerable(
            combat,
            registry,
            GoblinId,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 5,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Reflected));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(7, combat.GetCombatant(GoblinId).Health.Current);
    }

    [Fact]
    public void VulnerableWorksTogetherWithStrengthAndWeakInModifierOrder()
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

        ApplyVulnerable(
            combat,
            registry,
            GoblinId,
            durationTurns: 2);

        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: GoblinId,
            Amount: 6,
            SourceCombatantId: HeroId,
            Kind: DamageKind.Direct));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(3, combat.GetCombatant(GoblinId).Health.Current);
    }

    [Fact]
    public void VulnerableDurationExpiresOnOwnersTurnEnd()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplyVulnerable(
            combat,
            registry,
            HeroId,
            durationTurns: 1);

        var hero = combat.GetCombatant(HeroId);
        Assert.Single(hero.Statuses);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        Assert.Empty(hero.Statuses);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.StatusExpired);
    }

    [Fact]
    public void VulnerableModifierRunsAfterStrengthAndWeak()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var modifiers = registry.GetDamageAmountModifiers().ToArray();

        var strengthIndex = Array.FindIndex(
            modifiers,
            modifier => modifier is StrengthDamageAmountModifier);

        var weakIndex = Array.FindIndex(
            modifiers,
            modifier => modifier is WeakDamageAmountModifier);

        var vulnerableIndex = Array.FindIndex(
            modifiers,
            modifier => modifier is VulnerableDamageAmountModifier);

        Assert.True(strengthIndex >= 0);
        Assert.True(weakIndex >= 0);
        Assert.True(vulnerableIndex >= 0);

        Assert.True(strengthIndex < weakIndex);
        Assert.True(weakIndex < vulnerableIndex);
    }

    private static void ApplyVulnerable(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int durationTurns)
    {
        ApplyStatus(
            combat,
            registry,
            targetId,
            StandardCombatIds.VulnerableStatus,
            stacks: 0,
            durationTurns: durationTurns);
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

