using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardGainResourceEffectDefinitionTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void GainResourceEffectCreatesMissingResourceAndEmitsEvent()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var eventHandler = new CaptureResourceGainedEventHandler();
        registry.RegisterCombatEventHandler(eventHandler);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(new GainResourceEffectRequest(
            CombatantId: HeroId,
            ResourceId: StandardCombatIds.EnergyResource,
            Amount: 2,
            DefaultMax: 3));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var hero = combat.GetCombatant(HeroId);
        var energy = hero.Resources[StandardCombatIds.EnergyResource];

        Assert.Equal(2, energy.Current);
        Assert.Equal(3, energy.Max);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.ResourceGained);

        var handledEvent = Assert.Single(eventHandler.HandledEvents);

        Assert.Equal(HeroId, handledEvent.CombatantId);
        Assert.Equal(StandardCombatIds.EnergyResource, handledEvent.ResourceId);
        Assert.Equal(0, handledEvent.PreviousCurrent);
        Assert.Equal(2, handledEvent.NewCurrent);
        Assert.Equal(2, handledEvent.GainedAmount);
        Assert.Equal(3, handledEvent.Max);
    }

    [Fact]
    public void GainResourceEffectCapsExistingResourceAtMax()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var eventHandler = new CaptureResourceGainedEventHandler();
        registry.RegisterCombatEventHandler(eventHandler);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: 2, max: 3));

        combat.EnqueueEffect(new GainResourceEffectRequest(
            CombatantId: HeroId,
            ResourceId: StandardCombatIds.EnergyResource,
            Amount: 5,
            DefaultMax: 3));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var energy = hero.Resources[StandardCombatIds.EnergyResource];

        Assert.Equal(3, energy.Current);
        Assert.Equal(3, energy.Max);

        var handledEvent = Assert.Single(eventHandler.HandledEvents);

        Assert.Equal(2, handledEvent.PreviousCurrent);
        Assert.Equal(3, handledEvent.NewCurrent);
        Assert.Equal(1, handledEvent.GainedAmount);
    }

    [Fact]
    public void GainResourceEffectDoesNothingWhenResourceIsAlreadyAtMax()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var eventHandler = new CaptureResourceGainedEventHandler();
        registry.RegisterCombatEventHandler(eventHandler);

        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: 3, max: 3));

        combat.EnqueueEffect(new GainResourceEffectRequest(
            CombatantId: HeroId,
            ResourceId: StandardCombatIds.EnergyResource,
            Amount: 1,
            DefaultMax: 3));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(3, hero.Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Empty(eventHandler.HandledEvents);
        Assert.DoesNotContain(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.ResourceGained);
    }

    [Fact]
    public void GainResourceEffectCanIncreaseUnboundedResource()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var rageId = new ResourceId("test.rage");

        var hero = combat.GetCombatant(HeroId);
        hero.Resources.Add(
            rageId,
            new ValuePoolState(current: 4));

        combat.EnqueueEffect(new GainResourceEffectRequest(
            CombatantId: HeroId,
            ResourceId: rageId,
            Amount: 6));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        Assert.Equal(10, hero.Resources[rageId].Current);
        Assert.Null(hero.Resources[rageId].Max);
    }

    [Fact]
    public void GainResourceEffectRejectsNegativeAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        combat.EnqueueEffect(new GainResourceEffectRequest(
            CombatantId: HeroId,
            ResourceId: StandardCombatIds.EnergyResource,
            Amount: -1,
            DefaultMax: 3));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CombatEffectQueueProcessor().ResolvePendingEffects(combat, registry));
    }

    [Fact]
    public void GainResourceCardEffectDefinitionHandlerBuildsGainResourceRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var sourceCard = new CardDefinition(
            new CardDefinitionId("test.adrenaline"),
            new PackageId("test"),
            displayNameKey: "card.test.adrenaline.name",
            descriptionKey: "card.test.adrenaline.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: sourceCard,
            Source: hero,
            RequestedTargetId: null);

        var handler = new GainResourceCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new GainResourceCardEffectDefinition(
                ResourceId: StandardCombatIds.EnergyResource,
                Amount: 2,
                TargetMode: CardTargetMode.Self,
                DefaultMax: 3));

        var request = Assert.IsType<GainResourceEffectRequest>(Assert.Single(requests));

        Assert.Equal(HeroId, request.CombatantId);
        Assert.Equal(StandardCombatIds.EnergyResource, request.ResourceId);
        Assert.Equal(2, request.Amount);
        Assert.Equal(3, request.DefaultMax);
    }

    [Fact]
    public void PlayingCardWithGainResourceDefinitionGainsResourceThroughRegisteredHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: 0, max: 3));

        var adrenalineCardId = new CardDefinitionId("test.adrenaline");
        var adrenalineCard = new CardDefinition(
            adrenalineCardId,
            new PackageId("test"),
            displayNameKey: "card.test.adrenaline.name",
            descriptionKey: "card.test.adrenaline.description");

        adrenalineCard.Effects.Add(new GainResourceCardEffectDefinition(
            ResourceId: StandardCombatIds.EnergyResource,
            Amount: 2,
            TargetMode: CardTargetMode.Self,
            DefaultMax: 3));

        registry.RegisterCard(adrenalineCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            adrenalineCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(2, hero.Resources[StandardCombatIds.EnergyResource].Current);
        Assert.Empty(combat.GetCardZones(HeroId).Hand);
        Assert.Same(playedCard, Assert.Single(combat.GetCardZones(HeroId).DiscardPile));

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.ResourceGained);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardMovedToZone);
    }

    [Fact]
    public void StandardCombatPackageRegistersGainResourceHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.IsType<GainResourceEffectHandler>(
            registry.GetEffectRequestHandler(typeof(GainResourceEffectRequest)));

        Assert.IsType<GainResourceCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(GainResourceCardEffectDefinition)));
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

    private sealed class CaptureResourceGainedEventHandler
        : CombatEventHandler<ResourceGainedCombatEvent>
    {
        public List<ResourceGainedCombatEvent> HandledEvents { get; } = new();

        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            ResourceGainedCombatEvent combatEvent)
        {
            HandledEvents.Add(combatEvent);
        }
    }
}
