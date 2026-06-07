namespace RogueDeck.Core.Combat;

public sealed record DealDamageEffectRequest(
    CombatantId TargetCombatantId,
    int Amount,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : IEffectRequest;