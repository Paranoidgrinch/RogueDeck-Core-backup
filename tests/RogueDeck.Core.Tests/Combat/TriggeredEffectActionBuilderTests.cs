using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TriggeredEffectActionBuilderTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly CardDefinitionId CardId = new("card.test");

    [Fact]
    public void BuildDealDamageRequestsResolvesTargetsAndUsesSource()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var buildContext = new TriggeredEffectActionBuildContext(
            new CombatantTargetSelectionContext(
                Combat: combat,
                Source: combat.GetCombatant(HeroId)),
            new TriggeredEffectActionSource(
                SourceCombatantId: HeroId,
                SourceCardId: CardId));

        var requests = TriggeredEffectActionBuilder.BuildDealDamageRequests(
            buildContext,
            CombatantTargetSelectors.AllEnemiesOfSource,
            amount: 5);

        var request = Assert.IsType<DealDamageEffectRequest>(Assert.Single(requests));

        Assert.Equal(GoblinId, request.TargetCombatantId);
        Assert.Equal(5, request.Amount);
        Assert.Equal(HeroId, request.SourceCombatantId);
        Assert.Equal(CardId, request.SourceCardId);
    }

    [Fact]
    public void BuildGainBlockRequestsReturnsEmptyForEmptyTargets()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var buildContext = new TriggeredEffectActionBuildContext(
            new CombatantTargetSelectionContext(
                Combat: combat,
                Source: combat.GetCombatant(HeroId)),
            TriggeredEffectActionSource.None);

        var requests = TriggeredEffectActionBuilder.BuildGainBlockRequests(
            buildContext,
            CombatantTargetSelectors.EventTarget,
            amount: 3);

        Assert.Empty(requests);
    }

    [Fact]
    public void BuildApplyStatusRequestsRejectsZeroStatusValues()
    {
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var buildContext = new TriggeredEffectActionBuildContext(
            new CombatantTargetSelectionContext(
                Combat: combat,
                Source: combat.GetCombatant(HeroId)),
            TriggeredEffectActionSource.None);

        var requests = TriggeredEffectActionBuilder.BuildApplyStatusRequests(
            buildContext,
            CombatantTargetSelectors.Source,
            new StatusDefinitionId("test.status"),
            stacks: 0,
            durationTurns: 0,
            charges: 0);

        Assert.Empty(requests);
    }
}
