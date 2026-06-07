namespace RogueDeck.Core.Combat;

public sealed record GainBlockEffectRequest(
    CombatantId TargetCombatantId,
    int Amount,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : IEffectRequest;