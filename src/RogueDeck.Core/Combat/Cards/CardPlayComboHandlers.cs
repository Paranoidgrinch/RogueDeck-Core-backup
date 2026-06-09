namespace RogueDeck.Core.Combat;

public sealed class SkillComboDrawOnCardPlayedHandler
    : CombatEventHandler<CardPlayedCombatEvent>
{
    private const int SkillInterval = 3;

    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardPlayedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.SourceCombatantId, out var source))
            return;

        var card = registry.GetCard(combatEvent.CardDefinitionId);

        if (!card.Tags.Contains(StandardCombatIds.SkillCardTag))
            return;

        var drawAmount = source!.Statuses
            .Where(status => status.DefinitionId == StandardCombatIds.SkillComboDrawStatus)
            .Sum(status => status.Stacks);

        if (drawAmount <= 0)
            return;

        var stats = combat.GetCardPlayTurnStats(combatEvent.SourceCombatantId);
        var skillsPlayedThisTurn = stats.GetCardsPlayedWithTagThisTurn(StandardCombatIds.SkillCardTag);

        if (skillsPlayedThisTurn <= 0)
            return;

        if (skillsPlayedThisTurn % SkillInterval != 0)
            return;

        combat.EnqueueEffect(new DrawCardsEffectRequest(
            CombatantId: combatEvent.SourceCombatantId,
            Count: drawAmount));
    }
}
