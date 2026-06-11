using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CombatantTargetSelectorTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");
    private static readonly CombatantId AllyId = new("ally_001");

    [Fact]
    public void SourceSelectorReturnsSourceCombatant()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.Source.ResolveTargets(context);

        Assert.Equal(HeroId, Assert.Single(targets));
    }

    [Fact]
    public void EventTargetSelectorReturnsEventTargetWhenPresent()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var context = CreateContext(combat, HeroId, GoblinId);

        var targets = CombatantTargetSelectors.EventTarget.ResolveTargets(context);

        Assert.Equal(GoblinId, Assert.Single(targets));
    }

    [Fact]
    public void EventTargetSelectorReturnsEmptyWhenEventTargetIsMissing()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.EventTarget.ResolveTargets(context);

        Assert.Empty(targets);
    }

    [Fact]
    public void AllAlliesOfSourceSelectorReturnsSourceTeam()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.AllAlliesOfSource.ResolveTargets(context);

        Assert.Contains(HeroId, targets);
        Assert.Contains(AllyId, targets);
        Assert.DoesNotContain(GoblinId, targets);
    }

    [Fact]
    public void AllEnemiesOfSourceSelectorReturnsOpposingTeam()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.AllEnemiesOfSource.ResolveTargets(context);

        Assert.Contains(GoblinId, targets);
        Assert.Contains(SecondGoblinId, targets);
        Assert.DoesNotContain(HeroId, targets);
    }

    [Fact]
    public void AllAliveCombatantsSelectorReturnsEveryLivingCombatant()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.AllAliveCombatants.ResolveTargets(context);

        Assert.Contains(HeroId, targets);
        Assert.Contains(GoblinId, targets);
        Assert.Contains(SecondGoblinId, targets);
    }

    [Fact]
    public void LowestHealthEnemyOfSourceSelectorReturnsLowestHealthEnemy()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        DealDamage(
            combat,
            registry,
            GoblinId,
            amount: 2);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.LowestHealthEnemyOfSource.ResolveTargets(context);

        Assert.Equal(GoblinId, Assert.Single(targets));
    }

    [Fact]
    public void HighestHealthEnemyOfSourceSelectorReturnsHighestHealthEnemy()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        DealDamage(
            combat,
            registry,
            GoblinId,
            amount: 2);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.HighestHealthEnemyOfSource.ResolveTargets(context);

        Assert.Equal(SecondGoblinId, Assert.Single(targets));
    }

    [Fact]
    public void AllDamagedAlliesOfSourceSelectorReturnsOnlyDamagedAllies()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        DealDamage(
            combat,
            registry,
            AllyId,
            amount: 2);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors.AllDamagedAlliesOfSource.ResolveTargets(context);

        Assert.Equal(AllyId, Assert.Single(targets));
        Assert.DoesNotContain(HeroId, targets);
        Assert.DoesNotContain(GoblinId, targets);
    }

    [Fact]
    public void AllAlliesOfSourceWithStatusSelectorReturnsAllLivingAlliesWithStatus()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        var statusId = new StatusDefinitionId("test.ally_selector_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Buff);

        ApplyStatus(
            combat,
            registry,
            AllyId,
            statusId,
            stacks: 1);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .AllAlliesOfSourceWithStatus(statusId)
            .ResolveTargets(context);

        Assert.Equal(AllyId, Assert.Single(targets));
        Assert.DoesNotContain(HeroId, targets);
        Assert.DoesNotContain(GoblinId, targets);
    }

    [Fact]
    public void AllEnemiesOfSourceWithStatusSelectorReturnsAllLivingEnemiesWithStatus()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var statusId = new StatusDefinitionId("test.enemy_selector_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        ApplyStatus(
            combat,
            registry,
            SecondGoblinId,
            statusId,
            stacks: 1);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .AllEnemiesOfSourceWithStatus(statusId)
            .ResolveTargets(context);

        Assert.Equal(SecondGoblinId, Assert.Single(targets));
        Assert.DoesNotContain(HeroId, targets);
        Assert.DoesNotContain(GoblinId, targets);
    }
    [Fact]
    public void ComposableDamagedSelectorFiltersInnerTargets()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddPlayerAlly(combat);

        DealDamage(
            combat,
            registry,
            AllyId,
            amount: 2);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .Damaged(CombatantTargetSelectors.AllAlliesOfSource)
            .ResolveTargets(context);

        Assert.Equal(AllyId, Assert.Single(targets));
        Assert.DoesNotContain(HeroId, targets);
        Assert.DoesNotContain(GoblinId, targets);
    }

    [Fact]
    public void ComposableWithStatusSelectorFiltersInnerTargets()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var statusId = new StatusDefinitionId("test.composable_status");
        RegisterStackingStatus(registry, statusId, StatusPolarity.Debuff);

        ApplyStatus(
            combat,
            registry,
            SecondGoblinId,
            statusId,
            stacks: 1);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .WithStatus(
                CombatantTargetSelectors.AllEnemiesOfSource,
                statusId)
            .ResolveTargets(context);

        Assert.Equal(SecondGoblinId, Assert.Single(targets));
        Assert.DoesNotContain(GoblinId, targets);
        Assert.DoesNotContain(HeroId, targets);
    }

    [Fact]
    public void ComposableLowestHealthSelectorSelectsLowestFromInnerTargets()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        DealDamage(
            combat,
            registry,
            SecondGoblinId,
            amount: 2);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .LowestHealth(CombatantTargetSelectors.AllEnemiesOfSource)
            .ResolveTargets(context);

        Assert.Equal(SecondGoblinId, Assert.Single(targets));
    }

    [Fact]
    public void ComposableHighestHealthSelectorSelectsHighestFromInnerTargets()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        DealDamage(
            combat,
            registry,
            SecondGoblinId,
            amount: 2);

        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .HighestHealth(CombatantTargetSelectors.AllEnemiesOfSource)
            .ResolveTargets(context);

        Assert.Equal(GoblinId, Assert.Single(targets));
    }

    [Fact]
    public void ComposableUnionSelectorCombinesTargetsWithoutDuplicates()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .Union(
                CombatantTargetSelectors.Source,
                CombatantTargetSelectors.AllEnemiesOfSource,
                CombatantTargetSelectors.Source)
            .ResolveTargets(context);

        Assert.Equal(
            new[] { HeroId, GoblinId, SecondGoblinId },
            targets);
    }

    [Fact]
    public void ComposableExceptSelectorRemovesExcludedTargets()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var context = CreateContext(combat, HeroId);

        var targets = CombatantTargetSelectors
            .Except(
                CombatantTargetSelectors.AllAliveCombatants,
                CombatantTargetSelectors.Source)
            .ResolveTargets(context);

        Assert.Contains(GoblinId, targets);
        Assert.Contains(SecondGoblinId, targets);
        Assert.DoesNotContain(HeroId, targets);
    }
    private static CombatantTargetSelectionContext CreateContext(
        CombatState combat,
        CombatantId sourceId,
        CombatantId? eventTargetId = null)
    {
        return new CombatantTargetSelectionContext(
            Combat: combat,
            Source: combat.GetCombatant(sourceId),
            EventTargetId: eventTargetId);
    }

    private static void DealDamage(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int amount)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: targetId,
            Amount: amount));

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
    private static void AddPlayerAlly(CombatState combat)
    {
        var ally = new CombatantState(
            AllyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            StandardCombatIds.PlayerTeam,
            new HealthState(current: 10, max: 10));

        combat.AddCombatant(ally);
    }
}



