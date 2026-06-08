using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardTurnEndHandDestinationTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void CardDefinitionsDiscardFromHandAtTurnEndByDefault()
    {
        var definition = CreateCardDefinition(new CardDefinitionId("test.default"));

        Assert.Equal(CardZone.DiscardPile, definition.TurnEndHandDestinationZone);
        Assert.False(definition.RetainInHandOnTurnEnd);
    }

    [Fact]
    public void TurnEndHandCleanupCanMoveCardsToExhaustPile()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var etherealCardId = new CardDefinitionId("test.ethereal");
        var etherealCardDefinition = CreateCardDefinition(etherealCardId);
        etherealCardDefinition.TurnEndHandDestinationZone = CardZone.ExhaustPile;

        registry.RegisterCard(etherealCardDefinition);

        var etherealCard = AddCardToZone(
            combat,
            HeroId,
            etherealCardId,
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

        Assert.Empty(zones.Hand);
        Assert.Same(normalCard, Assert.Single(zones.DiscardPile));
        Assert.Same(etherealCard, Assert.Single(zones.ExhaustPile));

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.HandDiscarded);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardsMovedBetweenZones);
    }

    [Fact]
    public void TurnEndHandCleanupCanMoveCardsToBanishedPile()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var fragileCardId = new CardDefinitionId("test.fragile");
        var fragileCardDefinition = CreateCardDefinition(fragileCardId);
        fragileCardDefinition.TurnEndHandDestinationZone = CardZone.BanishedPile;

        registry.RegisterCard(fragileCardDefinition);

        var fragileCard = AddCardToZone(
            combat,
            HeroId,
            fragileCardId,
            CardZone.Hand);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Empty(zones.Hand);
        Assert.Empty(zones.DiscardPile);
        Assert.Empty(zones.ExhaustPile);
        Assert.Same(fragileCard, Assert.Single(zones.BanishedPile));

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardsMovedBetweenZones);
    }

    [Fact]
    public void RetainStillOverridesTurnEndHandDestinationZone()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var retainedEtherealCardId = new CardDefinitionId("test.retained_ethereal");
        var retainedEtherealDefinition = CreateCardDefinition(retainedEtherealCardId);
        retainedEtherealDefinition.RetainInHandOnTurnEnd = true;
        retainedEtherealDefinition.TurnEndHandDestinationZone = CardZone.ExhaustPile;

        registry.RegisterCard(retainedEtherealDefinition);

        var retainedCard = AddCardToZone(
            combat,
            HeroId,
            retainedEtherealCardId,
            CardZone.Hand);

        var processor = new CombatTurnProcessor();

        processor.StartCurrentTurn(combat, registry);
        processor.EndCurrentTurn(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Same(retainedCard, Assert.Single(zones.Hand));
        Assert.Empty(zones.DiscardPile);
        Assert.Empty(zones.ExhaustPile);

        Assert.DoesNotContain(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.HandDiscarded);

        Assert.DoesNotContain(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardsMovedBetweenZones);
    }

    [Fact]
    public void ExplicitDiscardHandEffectStillDiscardsEtherealCardsByDefault()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var etherealCardId = new CardDefinitionId("test.ethereal");
        var etherealCardDefinition = CreateCardDefinition(etherealCardId);
        etherealCardDefinition.TurnEndHandDestinationZone = CardZone.ExhaustPile;

        registry.RegisterCard(etherealCardDefinition);

        var etherealCard = AddCardToZone(
            combat,
            HeroId,
            etherealCardId,
            CardZone.Hand);

        combat.EnqueueEffect(new DiscardHandEffectRequest(HeroId));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var zones = combat.GetCardZones(HeroId);

        Assert.Empty(zones.Hand);
        Assert.Same(etherealCard, Assert.Single(zones.DiscardPile));
        Assert.Empty(zones.ExhaustPile);
    }

    [Fact]
    public void StandardCombatPackageRegistersTurnEndHandCleanupEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.IsType<MoveHandCardsOnTurnEndEffectHandler>(
            registry.GetEffectRequestHandler(typeof(MoveHandCardsOnTurnEndEffectRequest)));
    }

    private static CardDefinition CreateCardDefinition(CardDefinitionId id)
    {
        return new CardDefinition(
            id,
            new PackageId("test"),
            displayNameKey: $"card.{id}.name",
            descriptionKey: $"card.{id}.description");
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
