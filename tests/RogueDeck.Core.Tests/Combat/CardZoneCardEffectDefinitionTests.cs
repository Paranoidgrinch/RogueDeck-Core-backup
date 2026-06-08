using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardZoneCardEffectDefinitionTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void DrawCardsCardEffectDefinitionHandlerBuildsDrawCardsRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var card = new CardDefinition(
            new CardDefinitionId("test.draw"),
            new PackageId("test"),
            displayNameKey: "card.test.draw.name",
            descriptionKey: "card.test.draw.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        var handler = new DrawCardsCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new DrawCardsCardEffectDefinition(2, CardTargetMode.Self));

        var request = Assert.IsType<DrawCardsEffectRequest>(Assert.Single(requests));

        Assert.Equal(HeroId, request.CombatantId);
        Assert.Equal(2, request.Count);
    }

    [Fact]
    public void DiscardHandCardEffectDefinitionHandlerBuildsDiscardHandRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var card = new CardDefinition(
            new CardDefinitionId("test.discard_hand"),
            new PackageId("test"),
            displayNameKey: "card.test.discard_hand.name",
            descriptionKey: "card.test.discard_hand.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        var handler = new DiscardHandCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new DiscardHandCardEffectDefinition(CardTargetMode.Self));

        var request = Assert.IsType<DiscardHandEffectRequest>(Assert.Single(requests));

        Assert.Equal(HeroId, request.CombatantId);
    }

    [Fact]
    public void PlayingCardWithDrawCardsDefinitionDrawsCardsThroughRegisteredHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var drawCardId = new CardDefinitionId("test.draw_card");
        var drawCardDefinition = new CardDefinition(
            drawCardId,
            new PackageId("test"),
            displayNameKey: "card.test.draw_card.name",
            descriptionKey: "card.test.draw_card.description");

        drawCardDefinition.Effects.Add(new DrawCardsCardEffectDefinition(
            Count: 2,
            TargetMode: CardTargetMode.Self));

        registry.RegisterCard(drawCardDefinition);

        var playedCard = AddCardToZone(combat, HeroId, drawCardId, CardZone.Hand);
        var firstDrawnCard = AddCardToZone(combat, HeroId, StandardCombatIds.StrikeCard, CardZone.DrawPile);
        var secondDrawnCard = AddCardToZone(combat, HeroId, StandardCombatIds.DefendCard, CardZone.DrawPile);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        var zones = combat.GetCardZones(HeroId);

        Assert.Empty(zones.DrawPile);

        Assert.Equal(2, zones.Hand.Count);
        Assert.Contains(zones.Hand, card => card.Id == firstDrawnCard.Id);
        Assert.Contains(zones.Hand, card => card.Id == secondDrawnCard.Id);

        Assert.Same(playedCard, Assert.Single(zones.DiscardPile));
        Assert.Equal(CardZone.DiscardPile, playedCard.Zone);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardsDrawn);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardMovedToZone);
    }

    [Fact]
    public void StandardCombatPackageRegistersCardZoneCardEffectDefinitionHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.IsType<DrawCardsCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(DrawCardsCardEffectDefinition)));

        Assert.IsType<DiscardHandCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(DiscardHandCardEffectDefinition)));
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
