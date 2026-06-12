using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class ResourceGainedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly ResourceId FuryResourceId = new("test.fury");

    [Fact]
    public void StandardCombatPackageRegistersResourceGainedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(ResourceGainedCombatEvent)),
            handler => handler is ResourceGainedTriggeredEffectHandler);
    }

    [Fact]
    public void ResourceGainedTriggeredEffectFiresWhenResourceIsGained()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_block"));

        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedResourceGainedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 2,
            defaultMax: 3);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectFiltersByResourceId()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var matching = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_matching"));

        matching.Filters.Add(new ResourceGainedResourceIdTriggerFilter(StandardCombatIds.EnergyResource));
        matching.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedResourceGainedTriggeredEffectAmount(2)));

        var nonMatching = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_non_matching"));

        nonMatching.Filters.Add(new ResourceGainedResourceIdTriggerFilter(FuryResourceId));
        nonMatching.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedResourceGainedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 2,
            defaultMax: 3);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectMinimumGainedAmountFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_minimum"));

        definition.Filters.Add(new ResourceGainedMinimumAmountTriggerFilter(MinimumGainedAmount: 3));
        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedResourceGainedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 2,
            defaultMax: 3);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectGainedAmountProviderUsesGainedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        hero.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: 2, max: 3));

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_amount"));

        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new ResourceGainedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 5,
            defaultMax: 3);

        Assert.Equal(
            1,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectCanUseNewCurrentAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_new_current"));

        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new ResourceGainedNewCurrentAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 2,
            defaultMax: 3);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectCanTargetResourceOwnerAsEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_event_target"));

        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedResourceGainedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 2,
            defaultMax: 3);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));
        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectReachedMaxFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        hero.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: 2, max: 3));

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_reached_max"));

        definition.Filters.Add(new ResourceGainedReachedMaxTriggerFilter());
        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedResourceGainedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            StandardCombatIds.EnergyResource,
            amount: 5,
            defaultMax: 3);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void ResourceGainedTriggeredEffectReachedMaxFilterIsSafeWhenMaxIsNull()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new ResourceGainedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.resource_gained_null_max"));

        definition.Filters.Add(new ResourceGainedReachedMaxTriggerFilter());
        definition.Actions.Add(
            new GainBlockOnResourceGainedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedResourceGainedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        GainResource(
            combat,
            registry,
            FuryResourceId,
            amount: 2);

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

    private static void GainResource(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ResourceId resourceId,
        int amount,
        int? defaultMax = null)
    {
        combat.EnqueueEffect(new GainResourceEffectRequest(
            CombatantId: HeroId,
            ResourceId: resourceId,
            Amount: amount,
            DefaultMax: defaultMax));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}
