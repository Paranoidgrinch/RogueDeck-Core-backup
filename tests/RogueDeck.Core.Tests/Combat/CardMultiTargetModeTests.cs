using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardMultiTargetModeTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CombatantId SecondGoblinId = new("goblin_002");

    [Fact]
    public void CardEffectContextResolvesAllLivingEnemies()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var hero = combat.GetCombatant(HeroId);

        var card = CreateCardDefinition("test.all_enemies");
        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        var targets = context.ResolveTargets(CardTargetMode.AllEnemies);

        Assert.Equal(new[] { GoblinId, SecondGoblinId }, targets);
    }

    [Fact]
    public void CardEffectContextResolvesAllLivingAlliesIncludingSelf()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var allyId = new CombatantId("hero_ally");
        combat.AddCombatant(new CombatantState(
            allyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            hero.TeamId,
            new HealthState(current: 10, max: 10)));

        var card = CreateCardDefinition("test.all_allies");
        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        var targets = context.ResolveTargets(CardTargetMode.AllAllies);

        Assert.Equal(new[] { HeroId, allyId }, targets);
    }

    [Fact]
    public void AllEnemiesIgnoresNonLivingEnemies()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var hero = combat.GetCombatant(HeroId);

        combat.GetCombatant(SecondGoblinId)
            .SetLifecycleState(CombatantLifecycleState.Downed);

        var card = CreateCardDefinition("test.all_enemies");
        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        var targets = context.ResolveTargets(CardTargetMode.AllEnemies);

        Assert.Equal(new[] { GoblinId }, targets);
    }

    [Fact]
    public void DealDamageAllEnemiesCardDamagesEveryLivingEnemy()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var cardId = new CardDefinitionId("test.cleave");
        var card = CreateCardDefinition(cardId.ToString());
        card.Effects.Add(new DealDamageCardEffectDefinition(
            Amount: 3,
            TargetMode: CardTargetMode.AllEnemies));

        registry.RegisterCard(card);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(9, combat.GetCombatant(GoblinId).Health.Current);
        Assert.Equal(9, combat.GetCombatant(SecondGoblinId).Health.Current);

        Assert.Empty(combat.GetCardZones(HeroId).Hand);
        Assert.Same(playedCard, Assert.Single(combat.GetCardZones(HeroId).DiscardPile));

        Assert.Equal(
            2,
            combat.CombatLog.Count(entry => entry.Type == StandardCombatLogTypes.DamageDealt));
    }

    [Fact]
    public void GainBlockAllAlliesCardGivesBlockToEveryLivingAlly()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var allyId = new CombatantId("hero_ally");
        combat.AddCombatant(new CombatantState(
            allyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            hero.TeamId,
            new HealthState(current: 10, max: 10)));

        var cardId = new CardDefinitionId("test.group_guard");
        var card = CreateCardDefinition(cardId.ToString());
        card.Effects.Add(new GainBlockCardEffectDefinition(
            Amount: 4,
            TargetMode: CardTargetMode.AllAllies));

        registry.RegisterCard(card);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(
            4,
            combat.GetCombatant(HeroId).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        Assert.Equal(
            4,
            combat.GetCombatant(allyId).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        Assert.False(
            combat.GetCombatant(GoblinId).DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }

    [Fact]
    public void ApplyStatusAllEnemiesCardAppliesStatusToEveryLivingEnemy()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();

        var cardId = new CardDefinitionId("test.mass_weak");
        var card = CreateCardDefinition(cardId.ToString());
        card.Effects.Add(new ApplyStatusCardEffectDefinition(
            StatusDefinitionId: StandardCombatIds.WeakStatus,
            Stacks: 0,
            DurationTurns: 2,
            Charges: 0,
            TargetMode: CardTargetMode.AllEnemies));

        registry.RegisterCard(card);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            cardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId));

        Assert.Single(combat.GetCombatant(GoblinId).Statuses);
        Assert.Single(combat.GetCombatant(SecondGoblinId).Statuses);

        Assert.Equal(
            StandardCombatIds.WeakStatus,
            combat.GetCombatant(GoblinId).Statuses[0].DefinitionId);

        Assert.Equal(
            StandardCombatIds.WeakStatus,
            combat.GetCombatant(SecondGoblinId).Statuses[0].DefinitionId);
    }

    [Fact]
    public void SingleTargetResolveStillRejectsMultiTargetModes()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndTwoGoblins();
        var hero = combat.GetCombatant(HeroId);

        var card = CreateCardDefinition("test.single_only");
        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        Assert.Throws<InvalidOperationException>(() =>
            context.ResolveTarget(CardTargetMode.AllEnemies));
    }

    private static CardDefinition CreateCardDefinition(string id)
    {
        return new CardDefinition(
            new CardDefinitionId(id),
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

