using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardEffectDefinitionHandlerTests
{
    [Fact]
    public void DealDamageCardEffectDefinitionHandlerBuildsDamageRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(new CombatantId("hero_001"));
        var goblinId = new CombatantId("goblin_001");

        var card = new CardDefinition(
            new CardDefinitionId("test.attack"),
            new PackageId("test"),
            displayNameKey: "card.test.attack.name",
            descriptionKey: "card.test.attack.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: goblinId);

        var handler = new DealDamageCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new DealDamageCardEffectDefinition(7, CardTargetMode.SingleEnemy));

        var request = Assert.IsType<DealDamageEffectRequest>(Assert.Single(requests));

        Assert.Equal(goblinId, request.TargetCombatantId);
        Assert.Equal(7, request.Amount);
        Assert.Equal(hero.Id, request.SourceCombatantId);
        Assert.Equal(card.Id, request.SourceCardId);
        Assert.Equal(DamageKind.Direct, request.Kind);
    }

    [Fact]
    public void GainBlockCardEffectDefinitionHandlerBuildsBlockRequest()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(new CombatantId("hero_001"));

        var card = new CardDefinition(
            new CardDefinitionId("test.block"),
            new PackageId("test"),
            displayNameKey: "card.test.block.name",
            descriptionKey: "card.test.block.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: null);

        var handler = new GainBlockCardEffectDefinitionHandler();

        var requests = handler.BuildEffectRequests(
            context,
            new GainBlockCardEffectDefinition(4, CardTargetMode.Self));

        var request = Assert.IsType<GainBlockEffectRequest>(Assert.Single(requests));

        Assert.Equal(hero.Id, request.TargetCombatantId);
        Assert.Equal(4, request.Amount);
        Assert.Equal(hero.Id, request.SourceCombatantId);
        Assert.Equal(card.Id, request.SourceCardId);
    }

    [Fact]
    public void CardEffectContextRejectsSingleEnemyTargetFromSameTeam()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(new CombatantId("hero_001"));

        var allyId = new CombatantId("hero_ally");
        combat.AddCombatant(new CombatantState(
            allyId,
            new CombatantDefinitionId("test.ally"),
            "combatant.test.ally",
            hero.TeamId,
            new HealthState(current: 10, max: 10)));

        var card = new CardDefinition(
            new CardDefinitionId("test.attack"),
            new PackageId("test"),
            displayNameKey: "card.test.attack.name",
            descriptionKey: "card.test.attack.description");

        var context = new CardEffectContext(
            Combat: combat,
            Card: card,
            Source: hero,
            RequestedTargetId: allyId);

        Assert.Throws<InvalidOperationException>(() =>
            context.ResolveTarget(CardTargetMode.SingleEnemy));
    }
}


