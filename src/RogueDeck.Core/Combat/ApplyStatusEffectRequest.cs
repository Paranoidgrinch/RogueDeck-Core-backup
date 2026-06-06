namespace RogueDeck.Core.Combat;

public sealed record ApplyStatusEffectRequest(
    CombatantId TargetCombatantId,
    StatusDefinitionId StatusDefinitionId,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null,
    int Stacks = 0,
    int DurationTurns = 0,
    int Charges = 0
) : IEffectRequest;