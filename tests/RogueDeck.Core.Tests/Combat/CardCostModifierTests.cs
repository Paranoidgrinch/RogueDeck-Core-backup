using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardCostModifierTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void RegistryStoresCardCostModifiersInPriorityOrder()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCardCostModifier(new AddFlatCostModifier(1, priority: 200));
        registry.RegisterCardCostModifier(new ReduceFlatCostModifier(1, priority: 100));

        var modifiers = registry.GetCardCostModifiers();

        Assert.IsType<ReduceFlatCostModifier>(modifiers[0]);
        Assert.IsType<AddFlatCostModifier>(modifiers[1]);
    }

    [Fact]
    public void RegistryRejectsDuplicateCardCostModifierTypes()
    {
        var registry = new CombatDefinitionRegistry();

        registry.RegisterCardCostModifier(new ReduceFlatCostModifier(1));

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterCardCostModifier(new ReduceFlatCostModifier(2)));
    }

    [Fact]
    public void StandardCombatPackageHasNoCardCostModifiersByDefault()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Empty(registry.GetCardCostModifiers());
    }

    [Fact]
    public void CostReductionCanMakeExpensiveCardPlayable()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        registry.RegisterCardCostModifier(new ReduceFlatCostModifier(1));

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        EnsureEnergy(hero, current: 1, max: 3);

        var cardId = new CardDefinitionId("test.expensive_strike");
        var card = CreateDamageCard(cardId, cost: 2, damage: 6);
        registry.RegisterCard(card);

        var cardInstance = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: cardInstance.Id,
                SourceCombatantId: HeroId,
                TargetCombatantId: GoblinId));

        Assert.Equal(0, hero.Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(6, combat.GetCombatant(GoblinId).Health.Current);

        Assert.Empty(combat.GetCardZones(HeroId).Hand);
        Assert.Same(cardInstance, Assert.Single(combat.GetCardZones(HeroId).DiscardPile));
    }

    [Fact]
    public void CostIncreaseCanMakeCardUnplayable()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        registry.RegisterCardCostModifier(new AddFlatCostModifier(2));

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        EnsureEnergy(hero, current: 1, max: 3);

        var cardId = new CardDefinitionId("test.normal_strike");
        var card = CreateDamageCard(cardId, cost: 1, damage: 6);
        registry.RegisterCard(card);

        var cardInstance = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        Assert.Throws<InvalidOperationException>(() =>
            new CombatCardPlayProcessor().PlayCardInstance(
                combat,
                registry,
                new CardInstancePlayRequest(
                    CardInstanceId: cardInstance.Id,
                    SourceCombatantId: HeroId,
                    TargetCombatantId: GoblinId)));

        Assert.Equal(1, hero.Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(12, combat.GetCombatant(GoblinId).Health.Current);

        Assert.Same(cardInstance, Assert.Single(combat.GetCardZones(HeroId).Hand));
        Assert.Empty(combat.GetCardZones(HeroId).DiscardPile);
    }

    [Fact]
    public void CostModifiersCannotMakeCostNegative()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        registry.RegisterCardCostModifier(new ReduceFlatCostModifier(99));

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        EnsureEnergy(hero, current: 0, max: 3);

        var cardId = new CardDefinitionId("test.free_strike");
        var card = CreateDamageCard(cardId, cost: 1, damage: 6);
        registry.RegisterCard(card);

        var cardInstance = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: cardInstance.Id,
                SourceCombatantId: HeroId,
                TargetCombatantId: GoblinId));

        Assert.Equal(0, hero.Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(6, combat.GetCombatant(GoblinId).Health.Current);
    }

    [Fact]
    public void MultipleCostsForSameResourceAreAggregatedAfterModifiers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        EnsureEnergy(hero, current: 3, max: 3);

        var cardId = new CardDefinitionId("test.double_cost");
        var card = new CardDefinition(
            cardId,
            new PackageId("test"),
            displayNameKey: "card.test.double_cost.name",
            descriptionKey: "card.test.double_cost.description");

        card.Costs.Add(new ResourceCost(StandardCombatIds.EnergyResource, 2));
        card.Costs.Add(new ResourceCost(StandardCombatIds.EnergyResource, 2));
        card.Effects.Add(new DealDamageCardEffectDefinition(6, CardTargetMode.SingleEnemy));

        registry.RegisterCard(card);

        var cardInstance = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        Assert.Throws<InvalidOperationException>(() =>
            new CombatCardPlayProcessor().PlayCardInstance(
                combat,
                registry,
                new CardInstancePlayRequest(
                    CardInstanceId: cardInstance.Id,
                    SourceCombatantId: HeroId,
                    TargetCombatantId: GoblinId)));

        Assert.Equal(3, hero.Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Equal(12, combat.GetCombatant(GoblinId).Health.Current);
        Assert.Same(cardInstance, Assert.Single(combat.GetCardZones(HeroId).Hand));
    }

    private static CardDefinition CreateDamageCard(
        CardDefinitionId id,
        int cost,
        int damage)
    {
        var card = new CardDefinition(
            id,
            new PackageId("test"),
            displayNameKey: $"card.{id}.name",
            descriptionKey: $"card.{id}.description");

        card.Costs.Add(new ResourceCost(StandardCombatIds.EnergyResource, cost));
        card.Effects.Add(new DealDamageCardEffectDefinition(damage, CardTargetMode.SingleEnemy));

        return card;
    }

    private static void EnsureEnergy(
        CombatantState combatant,
        int current,
        int max)
    {
        if (combatant.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var energy))
        {
            energy.SetMax(max);
            energy.SetCurrent(current);
            return;
        }

        combatant.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: current, max: max));
    }

    private static CardInstance AddCardToZone(
        CombatState combat,
        CombatantId ownerId,
        CardDefinitionId definitionId,
        CardZone zone)
    {
        var card = new CardInstance(
            combat.CreateNextCardInstanceId(),
            definitionId,
            ownerId,
            zone);

        combat.GetCardZones(ownerId).AddCard(card);

        return card;
    }

    private sealed class ReduceFlatCostModifier : ICardCostModifier
    {
        private readonly int _amount;

        public ReduceFlatCostModifier(
            int amount,
            int priority = 100)
        {
            _amount = amount;
            Priority = priority;
        }

        public int Priority { get; }

        public int ModifyCostAmount(
            CardCostModificationContext context,
            int currentAmount)
        {
            return currentAmount - _amount;
        }
    }

    private sealed class AddFlatCostModifier : ICardCostModifier
    {
        private readonly int _amount;

        public AddFlatCostModifier(
            int amount,
            int priority = 100)
        {
            _amount = amount;
            Priority = priority;
        }

        public int Priority { get; }

        public int ModifyCostAmount(
            CardCostModificationContext context,
            int currentAmount)
        {
            return currentAmount + _amount;
        }
    }
}
