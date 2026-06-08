using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardCreationEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void CreateCardInstanceEffectCreatesCardInRequestedZoneAndEmitsEvent()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var eventHandler = new CaptureCardInstanceCreatedEventHandler();
        registry.RegisterCombatEventHandler(eventHandler);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(new CreateCardInstanceEffectRequest(
            CombatantId: HeroId,
            CardDefinitionId: StandardCombatIds.StrikeCard,
            ToZone: CardZone.Hand,
            Count: 2));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Equal(2, zones.Hand.Count);
        Assert.All(zones.Hand, card =>
        {
            Assert.Equal(StandardCombatIds.StrikeCard, card.DefinitionId);
            Assert.Equal(HeroId, card.OwnerId);
            Assert.Equal(CardZone.Hand, card.Zone);
        });

        Assert.Equal(3, combat.NextCardInstanceNumber);
        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(0, combat.PendingEventCount);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardInstanceCreated);

        var handledEvent = Assert.Single(eventHandler.HandledEvents);

        Assert.Equal(HeroId, handledEvent.CombatantId);
        Assert.Equal(StandardCombatIds.StrikeCard, handledEvent.CardDefinitionId);
        Assert.Equal(CardZone.Hand, handledEvent.ToZone);
        Assert.Equal(zones.Hand.Select(card => card.Id).ToArray(), handledEvent.CardInstanceIds);
    }

    [Fact]
    public void CreateCardInstanceEffectCanCreateBanishedCards()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(new CreateCardInstanceEffectRequest(
            CombatantId: HeroId,
            CardDefinitionId: StandardCombatIds.DefendCard,
            ToZone: CardZone.BanishedPile,
            Count: 1));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var zones = combat.GetCardZones(HeroId);
        var card = Assert.Single(zones.BanishedPile);

        Assert.Equal(StandardCombatIds.DefendCard, card.DefinitionId);
        Assert.Equal(HeroId, card.OwnerId);
        Assert.Equal(CardZone.BanishedPile, card.Zone);

        Assert.Empty(zones.DrawPile);
        Assert.Empty(zones.Hand);
        Assert.Empty(zones.DiscardPile);
        Assert.Empty(zones.ExhaustPile);
    }

    [Fact]
    public void CreateCardInstanceEffectRejectsMissingCardDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(new CreateCardInstanceEffectRequest(
            CombatantId: HeroId,
            CardDefinitionId: new CardDefinitionId("missing.card"),
            ToZone: CardZone.Hand,
            Count: 1));

        Assert.Throws<InvalidOperationException>(() =>
            new CombatEffectQueueProcessor().ResolvePendingEffects(combat, registry));

        Assert.Empty(combat.GetCardZones(HeroId).AllCards);
    }

    [Fact]
    public void CreateCardInstanceEffectRejectsNegativeCount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(new CreateCardInstanceEffectRequest(
            CombatantId: HeroId,
            CardDefinitionId: StandardCombatIds.StrikeCard,
            ToZone: CardZone.Hand,
            Count: -1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CombatEffectQueueProcessor().ResolvePendingEffects(combat, registry));
    }

    [Fact]
    public void CreateCardInstanceCardEffectDefinitionHandlerBuildsCreateCardInstanceRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var sourceCard = new CardDefinition(
            new CardDefinitionId("test.generator"),
            new PackageId("test"),
            displayNameKey: "card.test.generator.name",
            descriptionKey: "card.test.generator.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: sourceCard,
            Source: hero,
            RequestedTargetId: null);

        var handler = new CreateCardInstanceCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new CreateCardInstanceCardEffectDefinition(
                CardDefinitionId: StandardCombatIds.StrikeCard,
                ToZone: CardZone.DrawPile,
                Count: 2,
                TargetMode: CardTargetMode.Self));

        var request = Assert.IsType<CreateCardInstanceEffectRequest>(Assert.Single(requests));

        Assert.Equal(HeroId, request.CombatantId);
        Assert.Equal(StandardCombatIds.StrikeCard, request.CardDefinitionId);
        Assert.Equal(CardZone.DrawPile, request.ToZone);
        Assert.Equal(2, request.Count);
    }

    [Fact]
    public void PlayingCardWithCreateCardInstanceDefinitionCreatesCardThroughRegisteredHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var generatorCardId = new CardDefinitionId("test.generator");
        var generatorCard = new CardDefinition(
            generatorCardId,
            new PackageId("test"),
            displayNameKey: "card.test.generator.name",
            descriptionKey: "card.test.generator.description");

        generatorCard.Effects.Add(new CreateCardInstanceCardEffectDefinition(
            CardDefinitionId: StandardCombatIds.StrikeCard,
            ToZone: CardZone.Hand,
            Count: 1,
            TargetMode: CardTargetMode.Self));

        registry.RegisterCard(generatorCard);

        var playedCard = new CardInstance(
            combat.CreateNextCardInstanceId(),
            generatorCardId,
            HeroId,
            CardZone.Hand);

        combat.GetCardZones(HeroId).AddCard(playedCard);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        var zones = combat.GetCardZones(HeroId);

        Assert.Single(zones.DiscardPile);
        Assert.Equal(playedCard.Id, zones.DiscardPile[0].Id);

        var createdCard = Assert.Single(zones.Hand);

        Assert.NotEqual(playedCard.Id, createdCard.Id);
        Assert.Equal(StandardCombatIds.StrikeCard, createdCard.DefinitionId);
        Assert.Equal(HeroId, createdCard.OwnerId);
        Assert.Equal(CardZone.Hand, createdCard.Zone);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardInstanceCreated);
    }

    [Fact]
    public void StandardCombatPackageRegistersCardCreationHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.IsType<CreateCardInstanceEffectHandler>(
            registry.GetEffectRequestHandler(typeof(CreateCardInstanceEffectRequest)));

        Assert.IsType<CreateCardInstanceCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(CreateCardInstanceCardEffectDefinition)));
    }

    private sealed class CaptureCardInstanceCreatedEventHandler
        : CombatEventHandler<CardInstanceCreatedCombatEvent>
    {
        public List<CardInstanceCreatedCombatEvent> HandledEvents { get; } = new();

        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            CardInstanceCreatedCombatEvent combatEvent)
        {
            HandledEvents.Add(combatEvent);
        }
    }
}

