using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class HealedTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId HealCardId = new("test.heal_card");
    private static readonly CardDefinitionId OtherCardId = new("test.other_card");

    [Fact]
    public void StandardCombatPackageRegistersHealedTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(HealedCombatEvent)),
            handler => handler is HealedTriggeredEffectHandler);
    }

    [Fact]
    public void HealedTriggeredEffectFiresWhenHealingOccurs()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 6);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_gain_block"));

        definition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(combat, registry, amount: 4);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HealedTriggeredEffectCanUseActualHealedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 2);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_amount"));

        definition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new HealedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(combat, registry, amount: 5);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HealedTriggeredEffectSourceCardFilterMatchesPresentSourceCard()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 6);

        var matching = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_source_card_matching"));

        matching.Filters.Add(new HealedSourceCardTriggerFilter(HealCardId));
        matching.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(2)));

        var nonMatching = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_source_card_non_matching"));

        nonMatching.Filters.Add(new HealedSourceCardTriggerFilter(OtherCardId));
        nonMatching.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        HealHero(
            combat,
            registry,
            amount: 4,
            sourceId: GoblinId,
            sourceCardId: HealCardId);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HealedTriggeredEffectMinimumHealedAmountFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 6);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_minimum"));

        definition.Filters.Add(
            new HealedMinimumAmountTriggerFilter(MinimumHealedAmount: 5));

        definition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(combat, registry, amount: 4);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HealedTriggeredEffectCanTargetSourceWhenSourceExists()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 6);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_source"));

        definition.Filters.Add(new HealedSourceCombatantTriggerFilter(GoblinId));

        definition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHealedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(
            combat,
            registry,
            amount: 4,
            sourceId: GoblinId);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void HealedTriggeredEffectMissingOptionalSourceAndCardDataIsSafe()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 6);

        var sourceDefinition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_missing_source"));

        sourceDefinition.Filters.Add(new HealedHasSourceTriggerFilter());
        sourceDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(3)));

        var cardDefinition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_missing_source_card"));

        cardDefinition.Filters.Add(new HealedSourceCardTriggerFilter(HealCardId));
        cardDefinition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedHealedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(sourceDefinition);
        registry.RegisterTriggeredEffectDefinition(cardDefinition);

        HealHero(combat, registry, amount: 4);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void HealedTriggeredEffectCanUseRequestedAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        DamageHero(combat, registry, amount: 2);

        var definition = new HealedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.healed_requested_amount"));

        definition.Actions.Add(
            new GainBlockOnHealedTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new HealedRequestedAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        HealHero(combat, registry, amount: 5);

        Assert.Equal(
            5,
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

    private static void DamageHero(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int amount)
    {
        combat.EnqueueEffect(new DealDamageEffectRequest(
            TargetCombatantId: HeroId,
            Amount: amount));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void HealHero(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int amount,
        CombatantId? sourceId = null,
        CardDefinitionId? sourceCardId = null)
    {
        combat.EnqueueEffect(new HealEffectRequest(
            TargetCombatantId: HeroId,
            Amount: amount,
            SourceCombatantId: sourceId,
            SourceCardId: sourceCardId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }
}
