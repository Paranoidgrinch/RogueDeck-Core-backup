namespace RogueDeck.Core.Combat;

public sealed record TurnStartedCombatEvent(
    CombatantId CombatantId,
    int Round,
    int Turn
) : ICombatEvent;

public sealed record TurnEndedCombatEvent(
    CombatantId CombatantId,
    int Round,
    int Turn
) : ICombatEvent;

public sealed record DamageDealtCombatEvent(
    CombatantId TargetCombatantId,
    int HealthDamage,
    int BlockedDamage,
    int RequestedAmount,
    DamageKind Kind = DamageKind.Direct,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : ICombatEvent;

