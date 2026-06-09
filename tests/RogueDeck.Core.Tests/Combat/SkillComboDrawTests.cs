using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class SkillComboDrawTests
{
    private static readonly CombatantId HeroId = new("hero_001");

    [Fact]
    public void StandardCombatPackageRegistersSkillComboDrawPieces()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var definition = registry.GetStatus(StandardCombatIds.SkillComboDrawStatus);

        Assert.Equal(StatusPolarity.Buff, definition.Polarity);
        Assert.True(definition.UsesStacks);
        Assert.True(definition.ShowStacksInUi);
        Assert.Contains(StandardCombatIds.BuffTag, definition.Tags);
        Assert.Contains(StandardCombatIds.ComboTag, definition.Tags);

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(CardPlayedCombatEvent)),
            handler => handler is SkillComboDrawOnCardPlayedHandler);
    }

    [Fact]
    public void SkillComboDrawTriggersAfterThirdSkillPlayedThisTurn()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplySkillComboDraw(
            combat,
            registry,
            HeroId,
            stacks: 1);

        var skillCardId = RegisterZeroCostSkillCard(registry);
        var drawCardId = RegisterMarkerCard(registry);

        AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, drawCardId, CardZone.DrawPile);

        var processor = new CombatCardPlayProcessor();

        var firstSkill = combat.GetCardZones(HeroId).Hand[0];
        processor.PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: firstSkill.Id,
                SourceCombatantId: HeroId));

        Assert.DoesNotContain(
            combat.GetCardZones(HeroId).DiscardPile,
            card => card.DefinitionId == drawCardId);
        Assert.Single(combat.GetCardZones(HeroId).DrawPile);

        var secondSkill = combat.GetCardZones(HeroId).Hand[0];
        processor.PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: secondSkill.Id,
                SourceCombatantId: HeroId));

        Assert.DoesNotContain(
            combat.GetCardZones(HeroId).DiscardPile,
            card => card.DefinitionId == drawCardId);
        Assert.Single(combat.GetCardZones(HeroId).DrawPile);

        var thirdSkill = combat.GetCardZones(HeroId).Hand[0];
        processor.PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: thirdSkill.Id,
                SourceCombatantId: HeroId));

        Assert.Equal(3, combat.GetCardPlayTurnStats(HeroId).GetCardsPlayedWithTagThisTurn(StandardCombatIds.SkillCardTag));
        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == drawCardId);
    }

    [Fact]
    public void SkillComboDrawUsesStacksAsDrawAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplySkillComboDraw(
            combat,
            registry,
            HeroId,
            stacks: 2);

        var skillCardId = RegisterZeroCostSkillCard(registry);
        var firstDrawCardId = new CardDefinitionId("test.marker_one");
        var secondDrawCardId = new CardDefinitionId("test.marker_two");

        RegisterMarkerCard(registry, firstDrawCardId);
        RegisterMarkerCard(registry, secondDrawCardId);

        AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);

        AddCardToZone(combat, HeroId, firstDrawCardId, CardZone.DrawPile);
        AddCardToZone(combat, HeroId, secondDrawCardId, CardZone.DrawPile);

        var processor = new CombatCardPlayProcessor();

        for (var i = 0; i < 3; i++)
        {
            var skill = combat.GetCardZones(HeroId).Hand.First(card => card.DefinitionId == skillCardId);

            processor.PlayCardInstance(
                combat,
                registry,
                new CardInstancePlayRequest(
                    CardInstanceId: skill.Id,
                    SourceCombatantId: HeroId));
        }

        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == firstDrawCardId);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == secondDrawCardId);
    }

    [Fact]
    public void SkillComboDrawDoesNotTriggerForAttackCards()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplySkillComboDraw(
            combat,
            registry,
            HeroId,
            stacks: 1);

        var attackCardId = RegisterZeroCostAttackCard(registry);
        var drawCardId = RegisterMarkerCard(registry);

        AddCardToZone(combat, HeroId, attackCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, attackCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, attackCardId, CardZone.Hand);
        AddCardToZone(combat, HeroId, drawCardId, CardZone.DrawPile);

        var processor = new CombatCardPlayProcessor();

        for (var i = 0; i < 3; i++)
        {
            var attack = combat.GetCardZones(HeroId).Hand.First(card => card.DefinitionId == attackCardId);

            processor.PlayCardInstance(
                combat,
                registry,
                new CardInstancePlayRequest(
                    CardInstanceId: attack.Id,
                    SourceCombatantId: HeroId,
                    TargetCombatantId: new CombatantId("goblin_001")));
        }

        Assert.Single(combat.GetCardZones(HeroId).DrawPile);
        Assert.DoesNotContain(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == drawCardId);
    }

    [Fact]
    public void SkillComboDrawCanTriggerAgainOnSixthSkill()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        ApplySkillComboDraw(
            combat,
            registry,
            HeroId,
            stacks: 1);

        var skillCardId = RegisterZeroCostSkillCard(registry);
        var firstDrawCardId = new CardDefinitionId("test.marker_one");
        var secondDrawCardId = new CardDefinitionId("test.marker_two");

        RegisterMarkerCard(registry, firstDrawCardId);
        RegisterMarkerCard(registry, secondDrawCardId);

        for (var i = 0; i < 6; i++)
            AddCardToZone(combat, HeroId, skillCardId, CardZone.Hand);

        AddCardToZone(combat, HeroId, firstDrawCardId, CardZone.DrawPile);
        AddCardToZone(combat, HeroId, secondDrawCardId, CardZone.DrawPile);

        var processor = new CombatCardPlayProcessor();

        for (var i = 0; i < 6; i++)
        {
            var skill = combat.GetCardZones(HeroId).Hand.First(card => card.DefinitionId == skillCardId);

            processor.PlayCardInstance(
                combat,
                registry,
                new CardInstancePlayRequest(
                    CardInstanceId: skill.Id,
                    SourceCombatantId: HeroId));
        }

        Assert.Empty(combat.GetCardZones(HeroId).DrawPile);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == firstDrawCardId);

        Assert.Contains(
            combat.GetCardZones(HeroId).Hand,
            card => card.DefinitionId == secondDrawCardId);
    }

    [Fact]
    public void SkillComboDrawHandlerRunsAfterTurnStatsTracker()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var handlers = registry.GetCombatEventHandlers(typeof(CardPlayedCombatEvent)).ToArray();

        var trackerIndex = Array.FindIndex(
            handlers,
            handler => handler is TrackCardsPlayedThisTurnHandler);

        var comboIndex = Array.FindIndex(
            handlers,
            handler => handler is SkillComboDrawOnCardPlayedHandler);

        Assert.True(trackerIndex >= 0);
        Assert.True(comboIndex >= 0);
        Assert.True(trackerIndex < comboIndex);
    }

    private static void ApplySkillComboDraw(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int stacks)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: StandardCombatIds.SkillComboDrawStatus,
            Stacks: stacks,
            DurationTurns: 0,
            Charges: 0));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static CardDefinitionId RegisterZeroCostSkillCard(CombatDefinitionRegistry registry)
    {
        var cardId = new CardDefinitionId("test.zero_cost_skill");

        if (registry.TryGetCard(cardId, out _))
            return cardId;

        var card = new CardDefinition(
            cardId,
            new PackageId("test"),
            displayNameKey: "card.test.zero_cost_skill.name",
            descriptionKey: "card.test.zero_cost_skill.description");

        card.Tags.Add(StandardCombatIds.SkillCardTag);
        card.Effects.Add(new GainBlockCardEffectDefinition(
            Amount: 1,
            TargetMode: CardTargetMode.Self));

        registry.RegisterCard(card);

        return cardId;
    }

    private static CardDefinitionId RegisterZeroCostAttackCard(CombatDefinitionRegistry registry)
    {
        var cardId = new CardDefinitionId("test.zero_cost_attack");

        if (registry.TryGetCard(cardId, out _))
            return cardId;

        var card = new CardDefinition(
            cardId,
            new PackageId("test"),
            displayNameKey: "card.test.zero_cost_attack.name",
            descriptionKey: "card.test.zero_cost_attack.description");

        card.Tags.Add(StandardCombatIds.AttackCardTag);
        card.Effects.Add(new DealDamageCardEffectDefinition(
            Amount: 1,
            TargetMode: CardTargetMode.SingleEnemy));

        registry.RegisterCard(card);

        return cardId;
    }

    private static CardDefinitionId RegisterMarkerCard(CombatDefinitionRegistry registry)
    {
        var cardId = new CardDefinitionId("test.marker");

        RegisterMarkerCard(registry, cardId);

        return cardId;
    }

    private static void RegisterMarkerCard(
        CombatDefinitionRegistry registry,
        CardDefinitionId cardId)
    {
        if (registry.TryGetCard(cardId, out _))
            return;

        var card = new CardDefinition(
            cardId,
            new PackageId("test"),
            displayNameKey: $"card.{cardId}.name",
            descriptionKey: $"card.{cardId}.description");

        registry.RegisterCard(card);
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

