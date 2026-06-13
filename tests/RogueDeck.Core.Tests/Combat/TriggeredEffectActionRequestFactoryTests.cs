using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TriggeredEffectActionRequestFactoryTests
{
    private static readonly CombatantId SourceId = new("source_001");
    private static readonly CombatantId TargetId = new("target_001");
    private static readonly CardDefinitionId CardId = new("card.test");

    [Fact]
    public void GainBlockFactoryCanUseActionSource()
    {
        var requests = TriggeredEffectActionRequestFactory.BuildGainBlockRequests(
            new[] { TargetId },
            amount: 3,
            new TriggeredEffectActionSource(
                SourceCombatantId: SourceId,
                SourceCardId: CardId));

        var request = Assert.IsType<GainBlockEffectRequest>(Assert.Single(requests));

        Assert.Equal(TargetId, request.TargetCombatantId);
        Assert.Equal(3, request.Amount);
        Assert.Equal(SourceId, request.SourceCombatantId);
        Assert.Equal(CardId, request.SourceCardId);
    }

    [Fact]
    public void DealDamageFactoryCanUseActionSource()
    {
        var requests = TriggeredEffectActionRequestFactory.BuildDealDamageRequests(
            new[] { TargetId },
            amount: 4,
            new TriggeredEffectActionSource(
                SourceCombatantId: SourceId,
                SourceCardId: CardId),
            kind: DamageKind.Direct);

        var request = Assert.IsType<DealDamageEffectRequest>(Assert.Single(requests));

        Assert.Equal(TargetId, request.TargetCombatantId);
        Assert.Equal(4, request.Amount);
        Assert.Equal(SourceId, request.SourceCombatantId);
        Assert.Equal(CardId, request.SourceCardId);
        Assert.Equal(DamageKind.Direct, request.Kind);
    }

    [Fact]
    public void ApplyStatusFactoryCanUseActionSource()
    {
        var statusId = new StatusDefinitionId("test.factory_status");

        var requests = TriggeredEffectActionRequestFactory.BuildApplyStatusRequests(
            new[] { TargetId },
            statusId,
            stacks: 2,
            durationTurns: 1,
            charges: 0,
            new TriggeredEffectActionSource(
                SourceCombatantId: SourceId,
                SourceCardId: CardId));

        var request = Assert.IsType<ApplyStatusEffectRequest>(Assert.Single(requests));

        Assert.Equal(TargetId, request.TargetCombatantId);
        Assert.Equal(statusId, request.StatusDefinitionId);
        Assert.Equal(SourceId, request.SourceCombatantId);
        Assert.Equal(CardId, request.SourceCardId);
        Assert.Equal(2, request.Stacks);
        Assert.Equal(1, request.DurationTurns);
        Assert.Equal(0, request.Charges);
    }

    [Fact]
    public void HealFactoryCanUseActionSource()
    {
        var requests = TriggeredEffectActionRequestFactory.BuildHealRequests(
            new[] { TargetId },
            amount: 6,
            new TriggeredEffectActionSource(
                SourceCombatantId: SourceId,
                SourceCardId: CardId));

        var request = Assert.IsType<HealEffectRequest>(
            Assert.Single(requests));

        Assert.Equal(TargetId, request.TargetCombatantId);
        Assert.Equal(6, request.Amount);
        Assert.Equal(SourceId, request.SourceCombatantId);
        Assert.Equal(CardId, request.SourceCardId);
    }

    [Fact]
    public void HealFactoryReturnsEmptyForNonPositiveAmount()
    {
        var requests = TriggeredEffectActionRequestFactory.BuildHealRequests(
            new[] { TargetId },
            amount: 0,
            TriggeredEffectActionSource.None);

        Assert.Empty(requests);
    }
}
