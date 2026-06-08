using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardZoneTests
{
    [Fact]
    public void CombatStateCreatesCardZonesForAddedCombatants()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var heroZones = combat.GetCardZones(new CombatantId("hero_001"));
        var goblinZones = combat.GetCardZones(new CombatantId("goblin_001"));

        Assert.NotNull(heroZones);
        Assert.NotNull(goblinZones);
        Assert.Empty(heroZones.AllCards);
        Assert.Empty(goblinZones.AllCards);
    }

    [Fact]
    public void CombatStateCreatesSequentialCardInstanceIds()
    {
        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var first = combat.CreateNextCardInstanceId();
        var second = combat.CreateNextCardInstanceId();

        Assert.Equal(new CardInstanceId("card_000001"), first);
        Assert.Equal(new CardInstanceId("card_000002"), second);
    }

    [Fact]
    public void CombatantCardZonesCanAddCardsToTheirStartingZones()
    {
        var zones = new CombatantCardZones();

        var drawCard = CreateCard("card_001", CardZone.DrawPile);
        var handCard = CreateCard("card_002", CardZone.Hand);
        var discardCard = CreateCard("card_003", CardZone.DiscardPile);
        var exhaustCard = CreateCard("card_004", CardZone.ExhaustPile);

        zones.AddCard(drawCard);
        zones.AddCard(handCard);
        zones.AddCard(discardCard);
        zones.AddCard(exhaustCard);

        Assert.Single(zones.DrawPile);
        Assert.Single(zones.Hand);
        Assert.Single(zones.DiscardPile);
        Assert.Single(zones.ExhaustPile);
        Assert.Equal(4, zones.AllCards.Count);
    }

    [Fact]
    public void CombatantCardZonesRejectDuplicateCardInstanceIds()
    {
        var zones = new CombatantCardZones();

        zones.AddCard(CreateCard("card_001", CardZone.DrawPile));

        Assert.Throws<InvalidOperationException>(() =>
            zones.AddCard(CreateCard("card_001", CardZone.Hand)));
    }

    [Fact]
    public void DrawCardsMovesCardsFromDrawPileToHandInOrder()
    {
        var zones = new CombatantCardZones();

        var first = CreateCard("card_001", CardZone.DrawPile);
        var second = CreateCard("card_002", CardZone.DrawPile);
        var third = CreateCard("card_003", CardZone.DrawPile);

        zones.AddCard(first);
        zones.AddCard(second);
        zones.AddCard(third);

        var drawn = zones.DrawCards(2);

        Assert.Equal(2, drawn.Count);
        Assert.Equal(first, drawn[0]);
        Assert.Equal(second, drawn[1]);

        Assert.Single(zones.DrawPile);
        Assert.Equal(2, zones.Hand.Count);
        Assert.Equal(CardZone.Hand, first.Zone);
        Assert.Equal(CardZone.Hand, second.Zone);
        Assert.Equal(CardZone.DrawPile, third.Zone);
    }

    [Fact]
    public void DrawingMoreCardsThanAvailableDrawsOnlyAvailableCards()
    {
        var zones = new CombatantCardZones();

        var card = CreateCard("card_001", CardZone.DrawPile);

        zones.AddCard(card);

        var drawn = zones.DrawCards(5);

        Assert.Single(drawn);
        Assert.Empty(zones.DrawPile);
        Assert.Single(zones.Hand);
    }

    [Fact]
    public void DrawCardsRejectsNegativeCount()
    {
        var zones = new CombatantCardZones();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            zones.DrawCards(-1));
    }

    [Fact]
    public void MoveCardToZoneMovesExistingCard()
    {
        var zones = new CombatantCardZones();

        var card = CreateCard("card_001", CardZone.DrawPile);

        zones.AddCard(card);

        zones.MoveCardToZone(card.Id, CardZone.ExhaustPile);

        Assert.Empty(zones.DrawPile);
        Assert.Single(zones.ExhaustPile);
        Assert.Equal(CardZone.ExhaustPile, card.Zone);
    }

    [Fact]
    public void DiscardHandMovesAllHandCardsToDiscardPile()
    {
        var zones = new CombatantCardZones();

        var first = CreateCard("card_001", CardZone.Hand);
        var second = CreateCard("card_002", CardZone.Hand);

        zones.AddCard(first);
        zones.AddCard(second);

        zones.DiscardHand();

        Assert.Empty(zones.Hand);
        Assert.Equal(2, zones.DiscardPile.Count);
        Assert.Equal(CardZone.DiscardPile, first.Zone);
        Assert.Equal(CardZone.DiscardPile, second.Zone);
    }

    private static CardInstance CreateCard(
        string id,
        CardZone zone)
    {
        return new CardInstance(
            new CardInstanceId(id),
            StandardCombatIds.StrikeCard,
            new CombatantId("hero_001"),
            zone);
    }
}
