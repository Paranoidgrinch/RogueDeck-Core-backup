using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardRetainLifecycleTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void StandardCardDefinitionsAreNotRetainedByDefault()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.False(registry.GetCard(StandardCombatIds.StrikeCard).RetainInHandOnTurnEnd);
        Assert.False(registry.GetCard(StandardCombatIds.DefendCard).RetainInHandOnTurnEnd);
    }

    [Fact]
    public void TurnEndDiscardKeepsRetainedCardsInHand()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var retainedCardId = new CardDefinitionId("test.retained");
        var retainedCardDefinition = CreateRetainedCardDefinition(retainedCardId);

        registry.RegisterCard(retainedCardDefinition);

        var retainedCard = AddCardToZone(
            combat,
            HeroId,
            retainedCardId,
            CardZone.Hand);

        var normalCard = AddCardToZone(
            combat,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Same(retainedCard, Assert.Single(zones.Hand));
        Assert.Equal(CardZone.Hand, retainedCard.Zone);

        Assert.Same(normalCard, Assert.Single(zones.DiscardPile));
        Assert.Equal(CardZone.DiscardPile, normalCard.Zone);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.HandDiscarded);
    }

    [Fact]
    public void TurnEndDiscardDoesNothingWhenOnlyRetainedCardsAreInHand()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var retainedCardId = new CardDefinitionId("test.retained");
        var retainedCardDefinition = CreateRetainedCardDefinition(retainedCardId);

        registry.RegisterCard(retainedCardDefinition);

        var retainedCard = AddCardToZone(
            combat,
            HeroId,
            retainedCardId,
            CardZone.Hand);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Same(retainedCard, Assert.Single(zones.Hand));
        Assert.Empty(zones.DiscardPile);

        Assert.DoesNotContain(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.HandDiscarded);
    }

    [Fact]
    public void ExplicitDiscardHandEffectDiscardsRetainedCardsByDefault()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var retainedCardId = new CardDefinitionId("test.retained");
        var retainedCardDefinition = CreateRetainedCardDefinition(retainedCardId);

        registry.RegisterCard(retainedCardDefinition);

        var retainedCard = AddCardToZone(
            combat,
            HeroId,
            retainedCardId,
            CardZone.Hand);

        var normalCard = AddCardToZone(
            combat,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Empty(zones.Hand);

        Assert.Equal(2, zones.DiscardPile.Count);
        Assert.Contains(zones.DiscardPile, card => card.Id == retainedCard.Id);
        Assert.Contains(zones.DiscardPile, card => card.Id == normalCard.Id);
    }

    [Fact]
    public void DiscardHandEffectCanKeepRetainedCardsWhenRequested()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var retainedCardId = new CardDefinitionId("test.retained");
        var retainedCardDefinition = CreateRetainedCardDefinition(retainedCardId);

        registry.RegisterCard(retainedCardDefinition);

        var retainedCard = AddCardToZone(
            combat,
            HeroId,
            retainedCardId,
            CardZone.Hand);

        var normalCard = AddCardToZone(
            combat,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand);

        combat.EnqueueEffect(new DiscardHandEffectRequest(
            HeroId,
            IncludeRetainedCards: false));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Same(retainedCard, Assert.Single(zones.Hand));
        Assert.Same(normalCard, Assert.Single(zones.DiscardPile));
    }

    [Fact]
    public void DiscardHandCardEffectDefinitionCanChooseWhetherRetainedCardsAreIncluded()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var sourceCard = CreateCardDefinition(new CardDefinitionId("test.discard_source"));
        var context = new CardEffectContext(
            Combat: combat,
            Card: sourceCard,
            Source: hero,
            RequestedTargetId: null);

        var handler = new DiscardHandCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new DiscardHandCardEffectDefinition(
                TargetMode: CardTargetMode.Self,
                IncludeRetainedCards: false));

        var request = Assert.IsType<DiscardHandEffectRequest>(Assert.Single(requests));

        Assert.Equal(HeroId, request.CombatantId);
        Assert.False(request.IncludeRetainedCards);
    }

    private static CardDefinition CreateCardDefinition(CardDefinitionId id)
    {
        return new CardDefinition(
            id,
            new PackageId("test"),
            displayNameKey: $"card.{id}.name",
            descriptionKey: $"card.{id}.description");
    }

    private static CardDefinition CreateRetainedCardDefinition(CardDefinitionId id)
    {
        var definition = CreateCardDefinition(id);
        definition.RetainInHandOnTurnEnd = true;

        return definition;
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
}

