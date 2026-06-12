using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardLifecycleTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void StandardCombatPackageRegistersCardLifecycleTriggeredEffectHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(CardsDrawnCombatEvent)),
            handler => handler is CardsDrawnTriggeredEffectHandler);
        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(HandDiscardedCombatEvent)),
            handler => handler is HandDiscardedTriggeredEffectHandler);
    }

    [Fact]
    public void CardsDrawnTriggeredEffectFiresForDrawnCards()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var definition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_block"));
        definition.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(3, GetBlock(combat, HeroId));
    }

    [Fact]
    public void CardsDrawnTriggeredEffectCanFilterByCardDefinitionId()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var matching = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_strike_block"));
        matching.Filters.Add(new CardsDrawnCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));
        matching.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(2)));

        var nonMatching = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_defend_block"));
        nonMatching.Filters.Add(new CardsDrawnCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));
        nonMatching.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(2, GetBlock(combat, HeroId));
    }

    [Fact]
    public void CardsDrawnTriggeredEffectCanFilterByCardTag()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var matching = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_attack_block"));
        matching.Filters.Add(new CardsDrawnCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));
        matching.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(2)));

        var nonMatching = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_skill_block"));
        nonMatching.Filters.Add(new CardsDrawnCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));
        nonMatching.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(2, GetBlock(combat, HeroId));
    }

    [Fact]
    public void CardsDrawnTriggeredEffectActionCanTargetCardOwnerSource()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);

        var definition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_source_block"));
        definition.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DrawCardsEffectRequest(HeroId, Count: 1));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(4, GetBlock(combat, HeroId));
        Assert.Equal(0, GetBlock(combat, GoblinId));
    }

    [Fact]
    public void CardsDrawnTriggeredEffectMissingCardDataIsSafe()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new CardsDrawnTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.cards_drawn_missing_card"));
        definition.Filters.Add(new CardsDrawnCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));
        definition.Actions.Add(
            new GainBlockOnCardsDrawnTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardsDrawnTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEvent(new CardsDrawnCombatEvent(
            HeroId,
            new[] { new CardInstanceId("missing_card_instance") }));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(0, GetBlock(combat, HeroId));
    }

    [Fact]
    public void HandDiscardedTriggeredEffectFiresForDiscardedCards()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var definition = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_block"));
        definition.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(3, GetBlock(combat, HeroId));
    }

    [Fact]
    public void HandDiscardedTriggeredEffectCanFilterByCardDefinitionId()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.Hand);

        var matching = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_defend_block"));
        matching.Filters.Add(new HandDiscardedCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));
        matching.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(2)));

        var nonMatching = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_strike_block"));
        nonMatching.Filters.Add(new HandDiscardedCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));
        nonMatching.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(2, GetBlock(combat, HeroId));
    }

    [Fact]
    public void HandDiscardedTriggeredEffectCanFilterByCardTag()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.Hand);

        var matching = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_skill_block"));
        matching.Filters.Add(new HandDiscardedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));
        matching.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(2)));

        var nonMatching = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_attack_block"));
        nonMatching.Filters.Add(new HandDiscardedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));
        nonMatching.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(2, GetBlock(combat, HeroId));
    }

    [Fact]
    public void HandDiscardedTriggeredEffectActionCanTargetCardOwnerSource()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.Hand);

        var definition = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_source_block"));
        definition.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(4, GetBlock(combat, HeroId));
        Assert.Equal(0, GetBlock(combat, GoblinId));
    }

    [Fact]
    public void HandDiscardedTriggeredEffectMissingCardDataIsSafe()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var definition = new HandDiscardedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.hand_discarded_missing_card"));
        definition.Filters.Add(new HandDiscardedCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));
        definition.Actions.Add(
            new GainBlockOnHandDiscardedTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedHandDiscardedTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        combat.EnqueueEvent(new HandDiscardedCombatEvent(
            HeroId,
            new[] { new CardInstanceId("missing_card_instance") }));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(0, GetBlock(combat, HeroId));
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

    private static int GetBlock(
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
