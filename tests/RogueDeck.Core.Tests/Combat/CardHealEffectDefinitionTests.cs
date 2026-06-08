using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardHealEffectDefinitionTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void HealCardEffectDefinitionHandlerBuildsHealRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var sourceCard = new CardDefinition(
            new CardDefinitionId("test.heal"),
            new PackageId("test"),
            displayNameKey: "card.test.heal.name",
            descriptionKey: "card.test.heal.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: sourceCard,
            Source: hero,
            RequestedTargetId: null);

        var handler = new HealCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new HealCardEffectDefinition(
                Amount: 5,
                TargetMode: CardTargetMode.Self));

        var request = Assert.IsType<HealEffectRequest>(Assert.Single(requests));

        Assert.Equal(HeroId, request.TargetCombatantId);
        Assert.Equal(5, request.Amount);
        Assert.Equal(HeroId, request.SourceCombatantId);
        Assert.Equal(sourceCard.Id, request.SourceCardId);
    }

    [Fact]
    public void PlayingCardWithHealDefinitionHealsSelfThroughRegisteredHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Health.SetCurrent(10);

        var healCardId = new CardDefinitionId("test.heal");
        var healCard = new CardDefinition(
            healCardId,
            new PackageId("test"),
            displayNameKey: "card.test.heal.name",
            descriptionKey: "card.test.heal.description");

        healCard.Effects.Add(new HealCardEffectDefinition(
            Amount: 5,
            TargetMode: CardTargetMode.Self));

        registry.RegisterCard(healCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            healCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(15, hero.Health.Current);

        Assert.Empty(combat.GetCardZones(HeroId).Hand);
        Assert.Same(playedCard, Assert.Single(combat.GetCardZones(HeroId).DiscardPile));

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.Healed);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.CardMovedToZone);
    }

    [Fact]
    public void PlayingCardWithHealDefinitionCapsAtMaxHealth()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Health.SetCurrent(18);

        var healCardId = new CardDefinitionId("test.big_heal");
        var healCard = new CardDefinition(
            healCardId,
            new PackageId("test"),
            displayNameKey: "card.test.big_heal.name",
            descriptionKey: "card.test.big_heal.description");

        healCard.Effects.Add(new HealCardEffectDefinition(
            Amount: 10,
            TargetMode: CardTargetMode.Self));

        registry.RegisterCard(healCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            healCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(20, hero.Health.Current);
    }

    [Fact]
    public void HealAllAlliesCardHealsEveryLivingAlly()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Health.SetCurrent(12);

        var allyId = new CombatantId("hero_ally");
        var ally = new CombatantState(
            allyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            hero.TeamId,
            new HealthState(current: 4, max: 10));

        combat.AddCombatant(ally);

        var healCardId = new CardDefinitionId("test.group_heal");
        var healCard = new CardDefinition(
            healCardId,
            new PackageId("test"),
            displayNameKey: "card.test.group_heal.name",
            descriptionKey: "card.test.group_heal.description");

        healCard.Effects.Add(new HealCardEffectDefinition(
            Amount: 3,
            TargetMode: CardTargetMode.AllAllies));

        registry.RegisterCard(healCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            healCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(15, hero.Health.Current);
        Assert.Equal(7, ally.Health.Current);
        Assert.Equal(12, combat.GetCombatant(GoblinId).Health.Current);

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry => entry.Type == StandardCombatLogTypes.Healed));
    }

    [Fact]
    public void HealAllAlliesIgnoresNonLivingAllies()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Health.SetCurrent(12);

        var allyId = new CombatantId("hero_ally");
        var ally = new CombatantState(
            allyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            hero.TeamId,
            new HealthState(current: 4, max: 10));

        ally.SetLifecycleState(CombatantLifecycleState.Downed);
        combat.AddCombatant(ally);

        var healCardId = new CardDefinitionId("test.group_heal");
        var healCard = new CardDefinition(
            healCardId,
            new PackageId("test"),
            displayNameKey: "card.test.group_heal.name",
            descriptionKey: "card.test.group_heal.description");

        healCard.Effects.Add(new HealCardEffectDefinition(
            Amount: 3,
            TargetMode: CardTargetMode.AllAllies));

        registry.RegisterCard(healCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            healCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(15, hero.Health.Current);
        Assert.Equal(4, ally.Health.Current);

        Assert.Single(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.Healed);
    }

    [Fact]
    public void StandardCombatPackageRegistersHealCardEffectDefinitionHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.IsType<HealCardEffectDefinitionHandler>(
            registry.GetCardEffectDefinitionHandler(typeof(HealCardEffectDefinition)));
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

