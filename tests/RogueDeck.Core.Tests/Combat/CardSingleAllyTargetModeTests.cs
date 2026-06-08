using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardSingleAllyTargetModeTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId AllyId = new("hero_ally");
    private static readonly CombatantId GoblinId = new("goblin_001");

    [Fact]
    public void CardEffectContextResolvesSingleLivingAlly()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        AddAlly(combat, hero);

        var context = CreateContext(combat, hero, AllyId);

        var target = context.ResolveTarget(CardTargetMode.SingleAlly);

        Assert.Equal(AllyId, target);
    }

    [Fact]
    public void SingleAllyAllowsSelfAsAllyTarget()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var context = CreateContext(combat, hero, HeroId);

        var target = context.ResolveTarget(CardTargetMode.SingleAlly);

        Assert.Equal(HeroId, target);
    }

    [Fact]
    public void SingleAllyRejectsEnemyTarget()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var context = CreateContext(combat, hero, GoblinId);

        Assert.Throws<InvalidOperationException>(() =>
            context.ResolveTarget(CardTargetMode.SingleAlly));
    }

    [Fact]
    public void SingleAllyRejectsMissingRequestedTarget()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        var context = CreateContext(combat, hero, requestedTargetId: null);

        Assert.Throws<InvalidOperationException>(() =>
            context.ResolveTarget(CardTargetMode.SingleAlly));
    }

    [Fact]
    public void SingleAllyRejectsNonLivingAllyTarget()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);
        var ally = AddAlly(combat, hero);

        ally.SetLifecycleState(CombatantLifecycleState.Downed);

        var context = CreateContext(combat, hero, AllyId);

        Assert.Throws<InvalidOperationException>(() =>
            context.ResolveTarget(CardTargetMode.SingleAlly));
    }

    [Fact]
    public void HealSingleAllyCardHealsRequestedAllyOnly()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        hero.Health.SetCurrent(10);

        var ally = AddAlly(combat, hero);
        ally.Health.SetCurrent(4);

        var healCardId = new CardDefinitionId("test.heal_ally");
        var healCard = CreateCardDefinition(healCardId.ToString());

        healCard.Effects.Add(new HealCardEffectDefinition(
            Amount: 5,
            TargetMode: CardTargetMode.SingleAlly));

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
                SourceCombatantId: HeroId,
                TargetCombatantId: AllyId));

        Assert.Equal(10, hero.Health.Current);
        Assert.Equal(9, ally.Health.Current);
        Assert.Equal(12, combat.GetCombatant(GoblinId).Health.Current);

        Assert.Empty(combat.GetCardZones(HeroId).Hand);
        Assert.Same(playedCard, Assert.Single(combat.GetCardZones(HeroId).DiscardPile));

        Assert.Single(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.Healed);
    }

    [Fact]
    public void RemoveStatusSingleAllyCardRemovesStatusFromRequestedAllyOnly()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        var ally = AddAlly(combat, hero);

        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: HeroId,
            StatusDefinitionId: StandardCombatIds.WeakStatus,
            Stacks: 0,
            DurationTurns: 2,
            Charges: 0));

        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: AllyId,
            StatusDefinitionId: StandardCombatIds.WeakStatus,
            Stacks: 0,
            DurationTurns: 2,
            Charges: 0));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);

        var cleanseCardId = new CardDefinitionId("test.cleanse_ally");
        var cleanseCard = CreateCardDefinition(cleanseCardId.ToString());

        cleanseCard.Effects.Add(new RemoveStatusCardEffectDefinition(
            StatusDefinitionId: StandardCombatIds.WeakStatus,
            TargetMode: CardTargetMode.SingleAlly));

        registry.RegisterCard(cleanseCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            cleanseCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId,
                TargetCombatantId: AllyId));

        Assert.Single(hero.Statuses);
        Assert.Empty(ally.Statuses);

        Assert.Contains(
            combat.CombatLog,
            entry => entry.Type == StandardCombatLogTypes.StatusRemoved);
    }

    [Fact]
    public void GainBlockSingleAllyCardGivesBlockToRequestedAllyOnly()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        var ally = AddAlly(combat, hero);

        var guardCardId = new CardDefinitionId("test.guard_ally");
        var guardCard = CreateCardDefinition(guardCardId.ToString());

        guardCard.Effects.Add(new GainBlockCardEffectDefinition(
            Amount: 4,
            TargetMode: CardTargetMode.SingleAlly));

        registry.RegisterCard(guardCard);

        var playedCard = AddCardToZone(
            combat,
            HeroId,
            guardCardId,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: playedCard.Id,
                SourceCombatantId: HeroId,
                TargetCombatantId: AllyId));

        Assert.False(hero.DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));

        Assert.Equal(
            4,
            ally.DefensivePools[StandardCombatIds.BlockDefensivePool].Current);

        Assert.False(
            combat.GetCombatant(GoblinId).DefensivePools.ContainsKey(StandardCombatIds.BlockDefensivePool));
    }

    private static CombatantState AddAlly(
        CombatState combat,
        CombatantState hero)
    {
        var ally = new CombatantState(
            AllyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            hero.TeamId,
            new HealthState(current: 10, max: 10));

        combat.AddCombatant(ally);

        return ally;
    }

    private static CardEffectContext CreateContext(
        CombatState combat,
        CombatantState source,
        CombatantId? requestedTargetId)
    {
        return new CardEffectContext(
            Combat: combat,
            Card: CreateCardDefinition("test.context"),
            Source: source,
            RequestedTargetId: requestedTargetId);
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
