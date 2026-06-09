namespace RogueDeck.Core.Combat;

public sealed class RageBlockOnAttackPlayedHandler
    : CombatEventHandler<CardPlayedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CardPlayedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.SourceCombatantId, out var source))
            return;

        var card = registry.GetCard(combatEvent.CardDefinitionId);

        if (!card.Tags.Contains(StandardCombatIds.AttackCardTag))
            return;

        var blockAmount = source!.Statuses
            .Where(status => status.DefinitionId == StandardCombatIds.RageStatus)
            .Sum(status => status.Stacks);

        if (blockAmount <= 0)
            return;

        combat.EnqueueEffect(new GainBlockEffectRequest(
            TargetCombatantId: combatEvent.SourceCombatantId,
            Amount: blockAmount,
            SourceCombatantId: combatEvent.SourceCombatantId,
            SourceCardId: combatEvent.CardDefinitionId));
    }
}
